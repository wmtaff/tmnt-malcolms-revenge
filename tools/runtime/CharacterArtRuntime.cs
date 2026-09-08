// Render-only existing-Leo-slot appearance. Native animation/combat data is never mutated.
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
namespace Malcolm.Runtime
{
    internal static class CharacterArtRuntime
    {
        const BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;
        const string PlayerPath="2d/animations/players/leonardo";
        static readonly HashSet<string> PortraitPaths=new HashSet<string>(StringComparer.Ordinal)
        {
            "2d/animations/hud/playerhud/leo","2d/animations/menu/characterselect/leo","2d/animations/menu/levelcomplete/leo","2d/animations/menu/pause/powerlevel/leo","2d/animations/menu/worldmap/playerpanels/leo"
        }
        ;
        static CharacterArtConfig config;
        static Assembly engine;
        static Action<string> logger;
        static object device;
        static bool failed;
        static readonly HashSet<object> checkedCollections=new HashSet<object>();
        static readonly HashSet<string> reported=new HashSet<string>();
        static readonly HashSet<string> observedPaths=new HashSet<string>();
        internal static string NormalizeCollectionFolder(string path) { return (path??String.Empty).Replace('\\','/').TrimEnd('/').ToLowerInvariant(); }
        internal static string DisplayName
        {
            get
            {
                if(config==null)throw new InvalidOperationException("Configure character artwork first");
                return config.DisplayName;
            }
        }
        internal static void Configure(string manifestPath,Action<string> log)
        {
            if(Path.GetFileName(manifestPath)!="manifest.json")throw new InvalidDataException("Character manifest must be named manifest.json");
            var next=CharacterArtConfig.Load(Path.GetDirectoryName(Path.GetFullPath(manifestPath)));
            Dispose();
            config=next;
            logger=log;
            failed=false;
            Log("CHARACTER_ART_VALIDATED character="+config.CharacterId+" nativeMappings="+config.NativeMap.Count+" originalFrames="+config.Frames.Count+" renderScale="+config.RenderScale);
        }
        internal static void Install(Harmony harmony,Assembly installedEngine)
        {
            if(config==null)throw new InvalidOperationException("Configure character artwork first");
            engine=installedEngine;
            Type data=engine.GetType("Paris.Engine.Graphics.Animations.AnimatedObject2dData",true);
            MethodInfo render=Method(data,"Render",12);
            harmony.Patch(render,new HarmonyMethod(typeof(CharacterArtRuntime),"Render"));
            Log("CHARACTER_ART_HOOK installed native collection render");
        }
        static object Get(object value,string name)
        {
            if(value==null)throw new InvalidOperationException("Missing "+name);
            var p=value.GetType().GetProperty(name,F);
            if(p==null)throw new MissingMemberException(value.GetType().FullName,name);
            return p.GetValue(value,null);
        }
        static MethodInfo Method(Type type,string name,int count)
        {
            MethodInfo found=null;
            foreach(var m in type.GetMethods(F))if(m.Name==name&&m.GetParameters().Length==count)
            {
                if(found!=null)throw new AmbiguousMatchException(name);
                found=m;
            }
            if(found==null)throw new MissingMethodException(type.FullName,name);
            return found;
        }
        static float Field(object value,string name)
        {
            return Convert.ToSingle(value.GetType().GetField(name).GetValue(value));
        }
        static void Log(string s)
        {
            try
            {
                if(logger!=null)logger(s);
            }
            catch
            {
            }
        }
        static void EnsureTexture(CharacterArtConfig.Sheet sheet,object currentDevice)
        {
            if(!Object.ReferenceEquals(device,currentDevice))
            {
                ReleaseTextures();
                device=currentDevice;
            }
            if(sheet.Texture!=null&&!(bool)Get(sheet.Texture,"IsDisposed"))return;
            Type textureType=currentDevice.GetType().Assembly.GetType("Microsoft.Xna.Framework.Graphics.Texture2D",true);
            sheet.Texture=Activator.CreateInstance(textureType,new object[]
            {
                currentDevice,sheet.Width,sheet.Height
            }
            );
            Method(textureType,"SetData",1).MakeGenericMethod(typeof(byte)).Invoke(sheet.Texture,new object[]
            {
                sheet.Pixels
            }
            );
        }
        // Harmony supplies all original values in order, including the unchanged native
        // frame index. Art sampling never advances gameplay animation time or fires events.
        private static bool Render(object __instance,object[] __args)
        {
            if(config==null||failed)return true;
            string path;
            try
            {
                path=NormalizeCollectionFolder(Convert.ToString(Get(__instance,"Path")));
                if(observedPaths.Count<24&&observedPaths.Add(path))Log("CHARACTER_ART_OBSERVED folder="+path);
            }
            catch(Exception error)
            {
                if(observedPaths.Add("<path-error>"))Log("CHARACTER_ART_PATH_ERROR "+error);
                return true;
            }
            bool player=path==PlayerPath;
            if(!player&&!PortraitPaths.Contains(path))return true;
            object renderer=null;
            bool pushed=false;
            try
            {
                var animations=(IList)Get(__instance,"Animations");
                int animationIndex=(int)__args[1],nativeFrame=(int)__args[2];
                if(animationIndex<0||animationIndex>=animations.Count)return true;
                object animation=animations[animationIndex];
                string animationName=Convert.ToString(Get(animation,"Name"));
                var nativeFrames=(IList)Get(animation,"Frames");
                if(nativeFrames.Count==0)throw new InvalidDataException("Native animation has no frames");
                if(player&&checkedCollections.Add(__instance))
                {
                    if(animations.Count!=150)throw new InvalidDataException("Unexpected native animation count");
                    foreach(object a in animations)if(!config.NativeMap.ContainsKey(Convert.ToString(Get(a,"Name")))&&!config.NativePassthrough.Contains(Convert.ToString(Get(a,"Name"))))throw new InvalidDataException("Unmapped native animation "+Get(a,"Name"));
                    int nativeFrameTotal=0;
                    foreach(object a in animations)nativeFrameTotal+=((IList)Get(a,"Frames")).Count;
                    Log("CHARACTER_COVERAGE complete=150 preservedNativeFrames="+nativeFrameTotal+" nativeDataUntouched=true");
                }
                if(player&&config.NativePassthrough.Contains(animationName))return true;
                // Palette squares are color-selection UI widgets, not portraits. Preserve them.
                if(!player&&animationName.Equals("Palettesquare",StringComparison.OrdinalIgnoreCase))return true;
                CharacterArtConfig.Frame art=player?config.Pick(animationName,nativeFrame,nativeFrames.Count):config.Animations["portrait"][0];
                renderer=engine.GetType("Paris.Engine.Graphics.Renderer",true).GetProperty("Singleton",F).GetValue(null,null);
                EnsureTexture(art.Sheet,Get(renderer,"GraphicsDevice"));
                MethodInfo draw=Method(renderer.GetType(),"DrawTexture",9);
                var args=draw.GetParameters();
                Type rectType=args[1].ParameterType,vectorType=args[2].ParameterType;
                object rect=Activator.CreateInstance(rectType,new object[]
                {
                    art.X,art.Y,art.Width,art.Height
                }
                );
                float originX=art.PivotX,originY=art.PivotY,scale=(float)__args[8]*art.Sheet.RenderScale;
                if(!player)
                {
                    object frame=nativeFrames[Math.Max(0,Math.Min(nativeFrame,nativeFrames.Count-1))];
                    object nativeRect=Get(frame,"Rect"),nativePos=Get(frame,"Pos");
                    float width=Field(nativeRect,"Width"),height=Field(nativeRect,"Height");
                    if(width<=0||height<=0)return true;
                    float fit=Math.Min(width/art.Width,height/art.Height);
                    scale=(float)__args[8]*fit;
                    originX=(-Field(nativePos,"X")-(width-art.Width*fit)/2)/fit;
                    originY=(-Field(nativePos,"Y")-(height-art.Height*fit)/2)/fit;
                }
                float angle=(float)__args[6];
                int effects=0;
                if((bool)__args[4])
                {
                    effects|=1;
                    originX=art.Width-originX;
                    angle=-angle;
                }
                if((bool)__args[5])
                {
                    effects|=2;
                    originY=art.Height-originY;
                }
                object origin=Activator.CreateInstance(vectorType,new object[]
                {
                    originX,originY
                }
                );
                // Explicit SimpleShader state avoids inheriting an indexed/tinted shader from
                // nested callers. The existing renderer state is restored even if Draw throws.
                MethodInfo push=Method(renderer.GetType(),"Push",9);
                object shaderData=Activator.CreateInstance(engine.GetType("Paris.Engine.Graphics.Shaders.SimpleShaderData",true));
                object projection=Get(renderer,"CurrentProjection");
                object optionalProjection=Activator.CreateInstance(push.GetParameters()[6].ParameterType,new[]
                {
                    projection
                }
                );
                push.Invoke(renderer,new object[]
                {
                    false,Get(renderer,"CurrentPos"),Get(renderer,"CurrentViewport"),Get(renderer,"CurrentScale"),Get(renderer,"CurrentDepthEnabled"),Get(renderer,"CurrentRenderTarget"),optionalProjection,Get(renderer,"SimpleShader"),shaderData
                }
                );
                pushed=true;
                draw.Invoke(renderer,new object[]
                {
                    art.Sheet.Texture,rect,__args[0],origin,angle,scale,Enum.ToObject(args[6].ParameterType,effects),__args[9],__args[7]
                }
                );
                if(reported.Add(path))Log("CHARACTER_ART_RENDERED collection="+path+" fullColor=true nativeMetadataPreserved=true");
                return false;
            }
            catch(Exception error)
            {
                failed=true;
                Log("CHARACTER_ART_FALLBACK native=true error="+error);
                return true;
            }
            finally
            {
                if(pushed)try
                {
                    Method(renderer.GetType(),"Pop",0).Invoke(renderer,null);
                }
                catch(Exception e)
                {
                    failed=true;
                    Log("CHARACTER_ART_RESTORE_FAILED "+e.Message);
                    throw;
                }
            }
        }
        static void ReleaseTextures()
        {
            if(config!=null)foreach(var s in config.Sheets.Values)
            {
                var disposable=s.Texture as IDisposable;
                if(disposable!=null)try
                {
                    disposable.Dispose();
                }
                catch
                {
                }
                s.Texture=null;
            }
            device=null;
        }
        internal static void Dispose()
        {
            ReleaseTextures();
            config=null;
            engine=null;
            checkedCollections.Clear();
            reported.Clear();
            observedPaths.Clear();
        }
        internal static void SelfTest()
        {
            CharacterArtConfig.SelfTest();
            if(NormalizeCollectionFolder(@"2d\Animations\Players\Leonardo\")!=PlayerPath||!PortraitPaths.Contains(NormalizeCollectionFolder(@"2d\Animations\Menu\CharacterSelect\Leo\")))throw new Exception("Native collection folder selection");
        }
    }
}
