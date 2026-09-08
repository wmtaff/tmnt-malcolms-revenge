using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.Serialization;
using System.Collections;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using System.Security.Cryptography;
internal static class PlayerAssetExporter
{
    const BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
    static object Get(object o,string n)
    {
        return o.GetType().GetProperty(n,F).GetValue(o,null);
    }
    static string Hash(string path)
    {
        using(var h=SHA256.Create())using(var f=File.OpenRead(path))return BitConverter.ToString(h.ComputeHash(f)).Replace("-","");
    }
    static object Value(object o,int depth)
    {
        if(o==null)return null;
        if(depth>8)throw new InvalidDataException("Metadata nesting bound");
        var t=o.GetType();
        if(o is string||t.IsPrimitive||o is decimal)return o;
        if(t.IsEnum||o is Guid)return o.ToString();
        if(o is IEnumerable)
        {
            var list=new List<object>();
            foreach(object item in (IEnumerable)o)
            {
                if(list.Count>=8192)throw new InvalidDataException("Metadata collection bound");
                list.Add(Value(item,depth+1));
            }
            return list;
        }
        var map=new Dictionary<string,object>();
        map["type"]=t.FullName;
        if(t.Namespace=="Microsoft.Xna.Framework")
        {
            foreach(var field in t.GetFields(BindingFlags.Public|BindingFlags.Instance))if(field.FieldType.IsPrimitive)map[field.Name]=field.GetValue(o);
            return map;
        }
        foreach(var p in t.GetProperties(BindingFlags.Public|BindingFlags.Instance))
        {
            if(!p.CanRead||p.GetIndexParameters().Length!=0||p.Name.EndsWith("ByName"))continue;
            map[p.Name]=Value(p.GetValue(o,null),depth+1);
        }
        return map;
    }
    static byte[] Inflate(string path)
    {
        using(var f=File.OpenRead(path))using(var z=new DeflateStream(f,CompressionMode.Decompress))using(var m=new MemoryStream())
        {
            byte[] b=new byte[8192];
            int n;
            while((n=z.Read(b,0,b.Length))>0)
            {
                if(m.Length+n>16777216)throw new InvalidDataException("Native metadata exceeds 16 MiB");
                m.Write(b,0,n);
            }
            return m.ToArray();
        }
    }
    public static int Main(string[] args)
    {
        try
        {
            if(args.Length!=2)throw new ArgumentException("Usage: PlayerAssetExporter.exe GAME_ROOT FRESH_OUTPUT_JSON");
            string root=Path.GetFullPath(args[0]);
            string output=Path.GetFullPath(args[1]);
            if(File.Exists(output)||Directory.Exists(output))throw new IOException("Choose a fresh output path");
            if(Path.GetExtension(output)!=".json")throw new IOException("Output must be JSON");
            var hashes=new Dictionary<string,object>();
            string[] names=
            {
                "TMNT.exe","ParisEngine.dll","ParisSerializers.dll"
            }
            ;
            string[] expected=
            {
                "36435CC7E1063F76E4641C92F95601414B62C1FAEDA39A64CCC270EEF6D82CC6","02FAE3072962C2304F9F086DB1680D8111D24CBF2F16A362C262809C62E4E7DE","12A8F1B1288664D343EE8E8E68BB5EB593696D4CEB62D6ADD27AF5B72B81F554"
            }
            ;
            for(int i=0;i<3;i++)
            {
                string hash=Hash(Path.Combine(root,names[i]));
                if(hash!=expected[i])throw new InvalidDataException("Unsupported assembly "+names[i]);
                hashes[names[i]]=hash;
            }
            AppDomain.CurrentDomain.AssemblyResolve+=(s,e)=>
            {
                string name=new AssemblyName(e.Name).Name;
                if(name.IndexOfAny(new[]
                {
                    '\\','/'
                }
                )>=0)return null;
                string path=Path.Combine(root,name+".dll");
                return File.Exists(path)?Assembly.LoadFrom(path):null;
            }
            ;
            var eng=Assembly.LoadFrom(Path.Combine(root,"ParisEngine.dll"));
            Assembly.LoadFrom(Path.Combine(root,"TMNT.exe"));
            var ser=Assembly.LoadFrom(Path.Combine(root,"ParisSerializers.dll"));
            var t=eng.GetType("Paris.Engine.BinaryContentManager",true);
            var manager=FormatterServices.GetUninitializedObject(t);
            var registry=t.GetField("_readWriters",F);
            registry.SetValue(manager,Activator.CreateInstance(registry.FieldType));
            t.GetField("_singleton",F).SetValue(null,manager);
            t.GetField("_parisSerializerAssembly",F).SetValue(manager,ser);
            t.GetMethod("AddAssemblyReadWriters",F).Invoke(manager,new object[]
            {
                eng
            }
            );
            t.GetMethod("AddAssemblyReadWriters",F).Invoke(manager,new object[]
            {
                ser
            }
            );
            string relative="2d/Animations/Players/Leonardo/Leonardo";
            string source=Path.Combine(root,"Content",relative+".zpbn");
            object data;
            using(var stream=new MemoryStream(Inflate(source)))data=t.GetMethod("Load",F).Invoke(manager,new object[]
            {
                stream
            }
            );
            var animations=(IList)Get(data,"Animations");
            if(animations.Count!=150)throw new InvalidDataException("Expected supported 150-animation Leonardo collection");
            int frameCount=0;
            var exported=new List<object>();
            foreach(object anim in animations)
            {
                var frames=(IList)Get(anim,"Frames");
                frameCount+=frames.Count;
                if(frameCount>4096)throw new InvalidDataException("Frame count bound");
                exported.Add(Value(anim,0));
            }
            var report=new Dictionary<string,object>
            {
                {
                    "schema_version",1
                }
                ,
                {
                    "native_asset",relative
                }
                ,
                {
                    "source_sha256",Hash(source)
                }
                ,
                {
                    "assembly_sha256",hashes
                }
                ,
                {
                    "animation_count",animations.Count
                }
                ,
                {
                    "frame_reference_count",frameCount
                }
                ,
                {
                    "base_texture_count",Get(data,"BaseTextureCount")
                }
                ,
                {
                    "texture_filenames",Value(Get(data,"TextureFilenames"),0)
                }
                ,
                {
                    "shader_palette",Get(data,"ShaderPalette")
                }
                ,
                {
                    "shader_palette_enabled",Get(data,"IsShaderPaletteEnabled")
                }
                ,
                {
                    "animations",exported
                }
            }
            ;
            var json=new JavaScriptSerializer
            {
                MaxJsonLength=16777216,RecursionLimit=32
            }
            .Serialize(report);
            using(var file=new FileStream(output,FileMode.CreateNew,FileAccess.Write,FileShare.Read))using(var writer=new StreamWriter(file,new System.Text.UTF8Encoding(false)))writer.Write(json);
            Console.WriteLine("EXPORTED animations="+animations.Count+" frames="+frameCount+" path="+output);
            return 0;
        }
        catch(Exception e)
        {
            Console.Error.WriteLine(e);
            return 1;
        }
    }
}
