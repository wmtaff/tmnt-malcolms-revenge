using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
namespace Malcolm.Runtime {
// Optional exact-object render substitution. Configure does no game initialization;
// PNG GPU upload occurs only on the game's render thread after the selector matches.
internal static class BackgroundRuntime {
    private const BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
    private static readonly Guid GroundId=new Guid("0b1d668c-f0fa-4ec2-a90f-dbe122c9c83d");
    private static Assembly engine;
    private sealed class Image {internal byte[] Png;internal int Width,Height;internal object Texture;}
    private static Image[] images;
    private static object device;
    private static Action<string> logger;
    private static bool disabled,reported;
    internal sealed class Tile {internal int X,Width,Index;}
    internal static IEnumerable<Tile> Plan() {
        yield return new Tile{X=4096,Width=800,Index=0};
        yield return new Tile{X=4896,Width=768,Index=1};
        yield return new Tile{X=5664,Width=800,Index=2};
    }
    internal static void Configure(Assembly installedEngine,string directory,Action<string> log) {
        // Validate all three CPU-side assets before replacing any current setup.
        var next=new Image[3];string[] names={"home.png","street.png","park.png"};
        for(int i=0;i<3;i++)next[i]=ReadImage(Path.Combine(directory,names[i]));
        Dispose();engine=installedEngine;images=next;logger=log;disabled=false;reported=false;
    }
    private static Image ReadImage(string path) {
        byte[] bytes;
        using(var input=File.OpenRead(path))using(var output=new MemoryStream()) {
            byte[] buffer=new byte[8192];int count;
            while((count=input.Read(buffer,0,buffer.Length))!=0){if(output.Length+count>16*1024*1024)throw new InvalidDataException("Background PNG exceeds 16 MiB");output.Write(buffer,0,count);}
            bytes=output.ToArray();
        }
        int width,height;ValidatePng(bytes,out width,out height);
        return new Image{Png=bytes,Width=width,Height=height};
    }
    private static int BigEndian(byte[] bytes,int offset){return checked((int)((uint)bytes[offset]<<24|(uint)bytes[offset+1]<<16|(uint)bytes[offset+2]<<8|bytes[offset+3]));}
    private static void ValidatePng(byte[] bytes,out int width,out int height) {
        byte[] signature={137,80,78,71,13,10,26,10};
        if(bytes.Length<33)throw new InvalidDataException("Truncated PNG header");
        for(int i=0;i<8;i++)if(bytes[i]!=signature[i])throw new InvalidDataException("PNG signature required");
        if(BigEndian(bytes,8)!=13||bytes[12]!=73||bytes[13]!=72||bytes[14]!=68||bytes[15]!=82)throw new InvalidDataException("PNG IHDR required");
        width=BigEndian(bytes,16);height=BigEndian(bytes,20);
        if(width<1||height<1||width>4096||height>2048||(long)width*height>4194304)throw new InvalidDataException("Background PNG dimensions exceed bounds");
        if(bytes[24]!=8||(bytes[25]!=2&&bytes[25]!=6)||bytes[26]!=0||bytes[27]!=0||bytes[28]>1)throw new InvalidDataException("Background requires 8-bit RGB/RGBA PNG");
    }
    private static object Property(object value,string name){if(value==null)throw new InvalidOperationException("Missing "+name);var p=value.GetType().GetProperty(name,Flags);if(p==null)throw new MissingMemberException(value.GetType().FullName,name);return p.GetValue(value,null);}
    private static object Singleton(Type type){return type.GetProperty("Singleton",Flags).GetValue(null,null);}
    private static void Log(string text){try{if(logger!=null)logger(text);}catch{/* Render diagnostics must not break the game. */}}
    // Harmony prefix return convention: true preserves the complete native render.
    internal static bool TryRender(object instance) {
        if(images==null||disabled)return true;
        try {
            if(!GroundId.Equals(Property(instance,"Id"))||Convert.ToString(Property(instance,"Name"))!="BG_Ground02")return true;
            object scene=Property(instance,"Scene");
            string path=Convert.ToString(Property(scene,"PlayfieldPath")).Replace('\\','/').ToLowerInvariant();
            if(path!="2d/level/playfield/stage/stage_12/level_12_art"||Convert.ToString(Property(instance,"TexturePath")).Replace('\\','/').ToLowerInvariant()!="2d/level/tileset/level12_ground02")return true;
            object position=Property(instance,"Position");Type vector3=position.GetType();
            if((float)vector3.GetField("X").GetValue(position)!=4096f||(float)vector3.GetField("Y").GetValue(position)!=0f||(float)vector3.GetField("Z").GetValue(position)!=0f)return true;
            object renderer=Singleton(engine.GetType("Paris.Engine.Graphics.Renderer",true));object currentDevice=Property(renderer,"GraphicsDevice");
            bool reload=!Object.ReferenceEquals(device,currentDevice);
            foreach(Image image in images)if(image.Texture==null||(bool)Property(image.Texture,"IsDisposed"))reload=true;
            if(reload) {
                ReleaseTextures();
                Type textureType=currentDevice.GetType().Assembly.GetType("Microsoft.Xna.Framework.Graphics.Texture2D",true);
                var fromStream=textureType.GetMethod("FromStream",new Type[]{currentDevice.GetType(),typeof(Stream)});
                if(fromStream==null)throw new MissingMethodException("Texture2D.FromStream(GraphicsDevice,Stream)");
                // Upload every segment before submitting any draw, so missing or
                // invalid artwork falls back to the entire original background.
                foreach(Image image in images) {
                    using(var stream=new MemoryStream(image.Png,false))image.Texture=fromStream.Invoke(null,new object[]{currentDevice,stream});
                    if((int)Property(image.Texture,"Width")!=image.Width||(int)Property(image.Texture,"Height")!=image.Height)throw new InvalidDataException("Decoded PNG dimensions changed");
                }
                device=currentDevice;
            }
            MethodInfo draw=null;
            foreach(var m in renderer.GetType().GetMethods(Flags))if(m.Name=="DrawTexture"&&m.GetParameters().Length==10){if(draw!=null)throw new AmbiguousMatchException("DrawTexture");draw=m;}
            if(draw==null)throw new MissingMethodException("Renderer.DrawTexture(10 parameters)");
            var parameters=draw.GetParameters();Type vector2=parameters[1].ParameterType;
            object zero=Activator.CreateInstance(vector2,new object[]{0f,0f});
            object white=parameters[8].ParameterType.GetProperty("White",Flags).GetValue(null,null);
            object effects=Enum.ToObject(parameters[7].ParameterType,0);
            object priority=instance.GetType().GetMethod("GetRenderPriority",Flags,null,Type.EmptyTypes,null).Invoke(instance,null);
            foreach(Tile tile in Plan()) {
                Image image=images[tile.Index];
                object crop=Activator.CreateInstance(vector2,new object[]{(float)image.Width,(float)image.Height});
                object pos=Activator.CreateInstance(vector2,new object[]{(float)tile.X,0f});
                object size=Activator.CreateInstance(vector2,new object[]{(float)tile.Width,456f});
                draw.Invoke(renderer,new object[]{image.Texture,zero,crop,pos,size,0f,zero,effects,white,priority});
            }
            if(!reported){reported=true;Log("BACKGROUND_RENDERED object=BG_Ground02 segments=home:800,street:768,park:800 height=456 coverage=4096..6464");}
            return false;
        }catch(Exception error){disabled=true;ReleaseTextures();Log("BACKGROUND_FALLBACK native=true error="+error.Message);return true;}
    }
    private static void ReleaseTextures(){if(images!=null)foreach(Image image in images){var disposable=image.Texture as IDisposable;if(disposable!=null){try{disposable.Dispose();}catch{}}image.Texture=null;}device=null;}
    internal static void Dispose(){ReleaseTextures();images=null;engine=null;}
    private struct TestPosition {public float X,Y,Z;public TestPosition(float x,float y,float z){X=x;Y=y;Z=z;}}
    private sealed class TestScene {public string PlayfieldPath{get{return "2d/level/playfield/stage/stage_12/level_12_art";}}}
    private sealed class TestGround {
        public Guid Id{get{return GroundId;}}public string Name{get{return "BG_Ground02";}}
        public TestScene Scene{get{return new TestScene();}}public string TexturePath{get{return "2d/level/tileset/level12_ground02";}}
        public TestPosition Position{get{return new TestPosition(4096,0,0);}}
    }
    internal static void SelfTest() {
        var tiles=new List<Tile>(Plan());
        if(tiles.Count!=3||tiles[0].X!=4096||tiles[1].X!=4896||tiles[2].X!=5664||tiles[0].Width!=800||tiles[1].Width!=768||tiles[2].Width!=800)throw new Exception("Background segment planning failed");
        for(int i=0;i<3;i++)if(tiles[i].Index!=i||(i>0&&tiles[i-1].X+tiles[i-1].Width!=tiles[i].X))throw new Exception("Segment gap or overlap");
        if(tiles[2].X+tiles[2].Width!=6464)throw new Exception("Native coverage changed");
        if(!TryRender(new object()))throw new Exception("Unconfigured background did not preserve native rendering");
        byte[] header=new byte[33];byte[] prefix={137,80,78,71,13,10,26,10,0,0,0,13,73,72,68,82};Array.Copy(prefix,header,prefix.Length);header[18]=6;header[19]=238;header[22]=3;header[23]=119;header[24]=8;header[25]=6;
        int w,h;ValidatePng(header,out w,out h);if(w!=1774||h!=887)throw new Exception("PNG dimensions failed");
        string testDirectory=Path.Combine(Path.GetTempPath(),"malcolm-background-test-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDirectory);
        try {
            string[] names={"home.png","street.png","park.png"};
            File.WriteAllBytes(Path.Combine(testDirectory,names[0]),header);
            bool missing=false;try{Configure(typeof(BackgroundRuntime).Assembly,testDirectory,null);}catch(FileNotFoundException){missing=true;}
            if(!missing||images!=null)throw new Exception("Missing segment partially configured");
            foreach(string name in names)File.WriteAllBytes(Path.Combine(testDirectory,name),header);string messages="";
            Configure(typeof(BackgroundRuntime).Assembly,testDirectory,delegate(string message){messages+=message;});
            if(images.Length!=3||images[2].Width!=1774)throw new Exception("Three images not retained");
            if(!TryRender(new TestGround())||!messages.Contains("BACKGROUND_FALLBACK"))throw new Exception("Reflection mismatch failed to preserve native render");
        }finally{Dispose();foreach(string name in new[]{"home.png","street.png","park.png"})File.Delete(Path.Combine(testDirectory,name));Directory.Delete(testDirectory);}
        header[16]=1;bool rejected=false;try{ValidatePng(header,out w,out h);}catch(InvalidDataException){rejected=true;}if(!rejected)throw new Exception("Oversized PNG accepted");
        Console.WriteLine("BACKGROUND_TEST_PASS");
    }
}
}
