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
    private static byte[] png;
    private static int imageWidth,imageHeight;
    private static object texture,device;
    private static Action<string> logger;
    private static bool disabled,reported;
    internal sealed class Tile {internal int X,Width,SourceWidth;}
    internal static IEnumerable<Tile> Plan(int sourceWidth) {
        if(sourceWidth<1||sourceWidth>4096)throw new ArgumentOutOfRangeException("sourceWidth");
        for(int x=0;x<2368;x+=960){int width=Math.Min(960,2368-x);yield return new Tile{X=4096+x,Width=width,SourceWidth=Math.Max(1,(int)((long)sourceWidth*width/960))};}
    }
    internal static void Configure(Assembly installedEngine,string path,Action<string> log) {
        byte[] bytes;
        using(var input=File.OpenRead(path))using(var output=new MemoryStream()) {
            byte[] buffer=new byte[8192];int count;
            while((count=input.Read(buffer,0,buffer.Length))!=0){if(output.Length+count>16*1024*1024)throw new InvalidDataException("Background PNG exceeds 16 MiB");output.Write(buffer,0,count);}
            bytes=output.ToArray();
        }
        int width,height;ValidatePng(bytes,out width,out height);
        Dispose();engine=installedEngine;png=bytes;imageWidth=width;imageHeight=height;logger=log;disabled=false;reported=false;
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
        if(png==null||disabled)return true;
        try {
            if(!GroundId.Equals(Property(instance,"Id"))||Convert.ToString(Property(instance,"Name"))!="BG_Ground02")return true;
            object scene=Property(instance,"Scene");
            string path=Convert.ToString(Property(scene,"PlayfieldPath")).Replace('\\','/').ToLowerInvariant();
            if(path!="2d/level/playfield/stage/stage_12/level_12_art"||Convert.ToString(Property(instance,"TexturePath")).Replace('\\','/').ToLowerInvariant()!="2d/level/tileset/level12_ground02")return true;
            object position=Property(instance,"Position");Type vector3=position.GetType();
            if((float)vector3.GetField("X").GetValue(position)!=4096f||(float)vector3.GetField("Y").GetValue(position)!=0f||(float)vector3.GetField("Z").GetValue(position)!=0f)return true;
            object renderer=Singleton(engine.GetType("Paris.Engine.Graphics.Renderer",true));object currentDevice=Property(renderer,"GraphicsDevice");
            if(texture==null||!Object.ReferenceEquals(device,currentDevice)||(bool)Property(texture,"IsDisposed")) {
                ReleaseTexture();
                Type textureType=currentDevice.GetType().Assembly.GetType("Microsoft.Xna.Framework.Graphics.Texture2D",true);
                var fromStream=textureType.GetMethod("FromStream",new Type[]{currentDevice.GetType(),typeof(Stream)});
                if(fromStream==null)throw new MissingMethodException("Texture2D.FromStream(GraphicsDevice,Stream)");
                using(var stream=new MemoryStream(png,false))texture=fromStream.Invoke(null,new object[]{currentDevice,stream});
                device=currentDevice;
                if((int)Property(texture,"Width")!=imageWidth||(int)Property(texture,"Height")!=imageHeight)throw new InvalidDataException("Decoded PNG dimensions changed");
            }
            MethodInfo draw=null;
            foreach(var m in renderer.GetType().GetMethods(Flags))if(m.Name=="DrawTexture"&&m.GetParameters().Length==10){if(draw!=null)throw new AmbiguousMatchException("DrawTexture");draw=m;}
            if(draw==null)throw new MissingMethodException("Renderer.DrawTexture(10 parameters)");
            var parameters=draw.GetParameters();Type vector2=parameters[1].ParameterType;
            object zero=Activator.CreateInstance(vector2,new object[]{0f,0f});
            object white=parameters[8].ParameterType.GetProperty("White",Flags).GetValue(null,null);
            object effects=Enum.ToObject(parameters[7].ParameterType,0);
            object priority=instance.GetType().GetMethod("GetRenderPriority",Flags,null,Type.EmptyTypes,null).Invoke(instance,null);
            foreach(Tile tile in Plan(imageWidth)) {
                object crop=Activator.CreateInstance(vector2,new object[]{(float)tile.SourceWidth,(float)imageHeight});
                object pos=Activator.CreateInstance(vector2,new object[]{(float)tile.X,0f});
                object size=Activator.CreateInstance(vector2,new object[]{(float)tile.Width,480f});
                draw.Invoke(renderer,new object[]{texture,zero,crop,pos,size,0f,zero,effects,white,priority});
            }
            if(!reported){reported=true;Log("BACKGROUND_RENDERED object=BG_Ground02 tileWorld=960x480 source="+imageWidth+"x"+imageHeight+" coverage=4096..6464");}
            return false;
        }catch(Exception error){disabled=true;ReleaseTexture();Log("BACKGROUND_FALLBACK native=true error="+error.Message);return true;}
    }
    private static void ReleaseTexture(){var disposable=texture as IDisposable;if(disposable!=null){try{disposable.Dispose();}catch{}}texture=null;device=null;}
    internal static void Dispose(){ReleaseTexture();png=null;engine=null;}
    private struct TestPosition {public float X,Y,Z;public TestPosition(float x,float y,float z){X=x;Y=y;Z=z;}}
    private sealed class TestScene {public string PlayfieldPath{get{return "2d/level/playfield/stage/stage_12/level_12_art";}}}
    private sealed class TestGround {
        public Guid Id{get{return GroundId;}}public string Name{get{return "BG_Ground02";}}
        public TestScene Scene{get{return new TestScene();}}public string TexturePath{get{return "2d/level/tileset/level12_ground02";}}
        public TestPosition Position{get{return new TestPosition(4096,0,0);}}
    }
    internal static void SelfTest() {
        var tiles=new List<Tile>(Plan(1774));
        if(tiles.Count!=3||tiles[0].X!=4096||tiles[1].X!=5056||tiles[2].X!=6016||tiles[2].Width!=448||tiles[2].SourceWidth!=827)throw new Exception("Background tile planning failed");
        if(!TryRender(new object()))throw new Exception("Unconfigured background did not preserve native rendering");
        byte[] header=new byte[33];byte[] prefix={137,80,78,71,13,10,26,10,0,0,0,13,73,72,68,82};Array.Copy(prefix,header,prefix.Length);header[18]=6;header[19]=238;header[22]=3;header[23]=119;header[24]=8;header[25]=6;
        int w,h;ValidatePng(header,out w,out h);if(w!=1774||h!=887)throw new Exception("PNG dimensions failed");
        string testFile=Path.Combine(Path.GetTempPath(),"malcolm-background-test-"+Guid.NewGuid().ToString("N")+".png");
        try {
            File.WriteAllBytes(testFile,header);string messages="";
            Configure(typeof(BackgroundRuntime).Assembly,testFile,delegate(string message){messages+=message;});
            if(!TryRender(new TestGround())||!messages.Contains("BACKGROUND_FALLBACK"))throw new Exception("Reflection mismatch failed to preserve native render");
        }finally{Dispose();File.Delete(testFile);}
        header[16]=1;bool rejected=false;try{ValidatePng(header,out w,out h);}catch(InvalidDataException){rejected=true;}if(!rejected)throw new Exception("Oversized PNG accepted");
        Console.WriteLine("BACKGROUND_TEST_PASS");
    }
}
}
