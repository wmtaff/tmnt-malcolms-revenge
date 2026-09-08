using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
namespace Malcolm.Runtime
{
    internal sealed class CharacterArtConfig
    {
        internal sealed class Sheet
        {
            internal string Id;
            internal int Width,Height;
            internal float RenderScale;
            internal byte[] Pixels;
            internal object Texture;
        }
        internal sealed class Frame
        {
            internal Sheet Sheet;
            internal int X,Y,Width,Height;
            internal float PivotX,PivotY;
        }
        internal readonly Dictionary<string,Sheet> Sheets=new Dictionary<string,Sheet>(StringComparer.Ordinal);
        internal readonly Dictionary<string,Frame> Frames=new Dictionary<string,Frame>(StringComparer.Ordinal);
        internal readonly Dictionary<string,List<Frame>> Animations=new Dictionary<string,List<Frame>>(StringComparer.Ordinal);
        internal readonly Dictionary<string,string> NativeMap=new Dictionary<string,string>(StringComparer.Ordinal);
        internal readonly HashSet<string> NativePassthrough=new HashSet<string>(StringComparer.Ordinal);
        internal float RenderScale=1;
        internal string CharacterId,DisplayName;
        static InvalidDataException Bad(string m)
        {
            return new InvalidDataException(m);
        }
        static Dictionary<string,object> Obj(object x)
        {
            var d=x as Dictionary<string,object>;
            if(d==null)throw Bad("JSON object required");
            return d;
        }
        static object Field(Dictionary<string,object> d,string key)
        {
            object x;
            if(!d.TryGetValue(key,out x))throw Bad("Missing "+key);
            return x;
        }
        static string Str(object x)
        {
            var s=x as string;
            if(String.IsNullOrWhiteSpace(s)||s.Length>256)throw Bad("Invalid identifier");
            return s;
        }
        static object[] Arr(object x,int min,int max)
        {
            var a=x as object[];
            if(a==null||a.Length<min||a.Length>max)throw Bad("Array bound");
            return a;
        }
        static float Num(object x,float min,float max)
        {
            if(!(x is int)&&!(x is long)&&!(x is decimal)&&!(x is double))throw Bad("Number required");
            double n=Convert.ToDouble(x);
            if(Double.IsInfinity(n)||Double.IsNaN(n)||n<min||n>max)throw Bad("Number outside bound");
            return(float)n;
        }
        static int Int(object x,int min,int max)
        {
            if(!(x is int))throw Bad("Integer required");
            return checked((int)Num(x,min,max));
        }
        static string LocalPath(string root,string relative)
        {
            if(Path.IsPathRooted(relative))throw Bad("Sheet path must be relative");
            string p=Path.GetFullPath(Path.Combine(root,relative));
            if(!p.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw Bad("Sheet escapes art directory");
            for(string c=p;!String.IsNullOrEmpty(c);c=Path.GetDirectoryName(c))if((File.Exists(c)||Directory.Exists(c))&&(File.GetAttributes(c)&FileAttributes.ReparsePoint)!=0)throw Bad("Reparse asset path rejected");
            return p;
        }
        static byte[] ReadBounded(string path,int limit)
        {
            using(var f=File.OpenRead(path))using(var m=new MemoryStream())
            {
                byte[] b=new byte[8192];
                int n;
                while((n=f.Read(b,0,b.Length))>0)
                {
                    if(m.Length+n>limit)throw Bad("Asset size bound");
                    m.Write(b,0,n);
                }
                return m.ToArray();
            }
        }
        internal static void Premultiply(byte[] rgba,int[] key,int tolerance)
        {
            for(int i=0;i<rgba.Length;i+=4)
            {
                if(key!=null&&Math.Abs(rgba[i]-key[0])<=tolerance&&Math.Abs(rgba[i+1]-key[1])<=tolerance&&Math.Abs(rgba[i+2]-key[2])<=tolerance)
                {
                    rgba[i]=rgba[i+1]=rgba[i+2]=rgba[i+3]=0;
                    continue;
                }
                int a=rgba[i+3];
                rgba[i]=(byte)((rgba[i]*a+127)/255);
                rgba[i+1]=(byte)((rgba[i+1]*a+127)/255);
                rgba[i+2]=(byte)((rgba[i+2]*a+127)/255);
            }
        }
        static Sheet Decode(string id,string path,int[] key,int tolerance)
        {
            byte[] bytes=ReadBounded(path,16777216);
            if(bytes.Length<24||bytes[0]!=137||bytes[1]!=80||bytes[2]!=78||bytes[3]!=71)throw Bad("PNG required");
            long w=((long)bytes[16]<<24)|((long)bytes[17]<<16)|((long)bytes[18]<<8)|bytes[19];
            long h=((long)bytes[20]<<24)|((long)bytes[21]<<16)|((long)bytes[22]<<8)|bytes[23];
            if(w<1||h<1||w>4096||h>4096||w*h>4194304)throw Bad("PNG dimensions bound");
            using(var stream=new MemoryStream(bytes))using(var input=new Bitmap(stream))
            {
                if(input.Width!=w||input.Height!=h)throw Bad("PNG header mismatch");
                using(var bitmap=new Bitmap(input.Width,input.Height,PixelFormat.Format32bppArgb))
                {
                    using(var g=Graphics.FromImage(bitmap))
                    {
                        g.CompositingMode=System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                        g.DrawImageUnscaled(input,0,0);
                    }
                    var data=bitmap.LockBits(new Rectangle(0,0,bitmap.Width,bitmap.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
                    byte[] rgba=new byte[bitmap.Width*bitmap.Height*4];
                    try
                    {
                        byte[] row=new byte[bitmap.Width*4];
                        for(int y=0;y<bitmap.Height;y++)
                        {
                            Marshal.Copy(IntPtr.Add(data.Scan0,y*data.Stride),row,0,row.Length);
                            for(int x=0;x<bitmap.Width;x++)
                            {
                                int s=x*4,d=(y*bitmap.Width+x)*4;
                                rgba[d]=row[s+2];
                                rgba[d+1]=row[s+1];
                                rgba[d+2]=row[s];
                                rgba[d+3]=row[s+3];
                            }
                        }
                    }
                    finally
                    {
                        bitmap.UnlockBits(data);
                    }
                    Premultiply(rgba,key,tolerance);
                    bool transparent=false;
                    for(int i=3;i<rgba.Length;i+=4)if(rgba[i]<255)
                    {
                        transparent=true;
                        break;
                    }
                    if(!transparent)throw Bad("Character sheet has no transparency; supply real alpha or chroma key");
                    return new Sheet
                    {
                        Id=id,Width=bitmap.Width,Height=bitmap.Height,Pixels=rgba
                    }
                    ;
                }
            }
        }
        internal static CharacterArtConfig Load(string directory)
        {
            string root=Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);
            string json=new System.Text.UTF8Encoding(false,true).GetString(ReadBounded(LocalPath(root,"manifest.json"),1048576));
            var d=Obj(new JavaScriptSerializer
            {
                MaxJsonLength=1048576,RecursionLimit=32
            }
            .DeserializeObject(json));
            if(Int(Field(d,"schema_version"),1,1)!=1)throw Bad("Schema version");
            var result=new CharacterArtConfig
            {
                CharacterId=Str(Field(d,"character_id")),DisplayName=Str(Field(d,"display_name"))
            }
            ;
            if(d.ContainsKey("render_scale"))result.RenderScale=Num(d["render_scale"],0.01f,4f);
            long total=0;
            foreach(var value in Arr(Field(d,"sheets"),1,16))
            {
                var s=Obj(value);
                string id=Str(Field(s,"id"));
                if(result.Sheets.ContainsKey(id))throw Bad("Duplicate sheet");
                int[] key=null;
                int tolerance=32;
                if(s.ContainsKey("chroma_key"))
                {
                    var k=Arr(s["chroma_key"],3,3);
                    key=new[]
                    {
                        Int(k[0],0,255),Int(k[1],0,255),Int(k[2],0,255)
                    }
                    ;
                    if(s.ContainsKey("chroma_tolerance"))tolerance=Int(s["chroma_tolerance"],0,64);
                }
                else if(s.ContainsKey("chroma_tolerance"))throw Bad("Tolerance requires key");
                var sheet=Decode(id,LocalPath(root,Str(Field(s,"path"))),key,tolerance);
                sheet.RenderScale=s.ContainsKey("render_scale")?Num(s["render_scale"],0.01f,4f):result.RenderScale;
                total+=(long)sheet.Width*sheet.Height;
                if(total>33554432)throw Bad("Total character pixels exceed32M");
                result.Sheets.Add(id,sheet);
            }
            foreach(var value in Arr(Field(d,"frames"),1,4096))
            {
                var f=Obj(value);
                string id=Str(Field(f,"id"));
                Sheet sheet;
                if(!result.Sheets.TryGetValue(Str(Field(f,"sheet")),out sheet))throw Bad("Unknown sheet");
                var r=Arr(Field(f,"rect"),4,4);
                var p=Arr(Field(f,"pivot"),2,2);
                var frame=new Frame
                {
                    Sheet=sheet,X=Int(r[0],0,sheet.Width),Y=Int(r[1],0,sheet.Height),Width=Int(r[2],1,sheet.Width),Height=Int(r[3],1,sheet.Height),PivotX=Num(p[0],-4096,4096),PivotY=Num(p[1],-4096,4096)
                }
                ;
                if(frame.X+frame.Width>sheet.Width||frame.Y+frame.Height>sheet.Height||result.Frames.ContainsKey(id))throw Bad("Frame bounds or duplicate");
                result.Frames.Add(id,frame);
            }
            foreach(var value in Arr(Field(d,"animations"),1,512))
            {
                var a=Obj(value);
                string id=Str(Field(a,"id"));
                var frames=new List<Frame>();
                foreach(var fv in Arr(Field(a,"frames"),1,4096))
                {
                    Frame frame;
                    if(!result.Frames.TryGetValue(Str(Field(Obj(fv),"frame")),out frame))throw Bad("Unknown animation frame");
                    frames.Add(frame);
                }
                if(result.Animations.ContainsKey(id))throw Bad("Duplicate animation");
                result.Animations.Add(id,frames);
            }
            foreach(var entry in Obj(Field(d,"native_animation_map")))
            {
                string target=Str(entry.Value);
                if(!result.Animations.ContainsKey(target))throw Bad("Unknown art animation mapping");
                result.NativeMap.Add(entry.Key,target);
            }
            if(d.ContainsKey("native_passthrough"))foreach(object name in Arr(d["native_passthrough"],0,150))
            {
                string n=Str(name);
                if(result.NativeMap.ContainsKey(n)||!result.NativePassthrough.Add(n))throw Bad("Duplicate or overlapping native passthrough");
            }
            if(result.NativeMap.Count+result.NativePassthrough.Count!=150)throw Bad("Exactly150 mapped or passthrough native animations required");
            if(!result.Animations.ContainsKey("portrait"))throw Bad("Portrait animation required for UI");
            return result;
        }
        internal Frame Pick(string nativeName,int nativeFrame,int nativeCount)
        {
            string mapped;
            if(!NativeMap.TryGetValue(nativeName,out mapped))throw Bad("Unmapped native animation "+nativeName);
            var list=Animations[mapped];
            int index=(int)((long)Math.Max(0,Math.Min(nativeFrame,nativeCount-1))*list.Count/Math.Max(1,nativeCount));
            return list[Math.Min(index,list.Count-1)];
        }
        internal static void SelfTest()
        {
            byte[] pixels=
            {
                255,0,255,255,128,64,32,128,220,15,225,255
            }
            ;
            Premultiply(pixels,new[]
            {
                255,0,255
            }
            ,32);
            if(pixels[3]!=0||pixels[0]!=0||pixels[4]!=64||pixels[5]!=32||pixels[6]!=16||pixels[11]!=255)throw new Exception("Chroma/premultiply test");
            var c=new CharacterArtConfig();
            var a=new Frame();
            var b=new Frame();
            c.Animations["walk"]=new List<Frame>
            {
                a,b
            }
            ;
            c.NativeMap["Walk"]= "walk";
            if(c.Pick("Walk",0,6)!=a||c.Pick("Walk",5,6)!=b)throw new Exception("Native frame remap");
            bool reject=false;
            try
            {
                c.Pick("Unknown",0,1);
            }
            catch(InvalidDataException)
            {
                reject=true;
            }
            if(!reject)throw new Exception("Unmapped pose accepted");
            Console.WriteLine("CHARACTER_ART_CONFIG_TEST_PASS");
        }
    }
}
