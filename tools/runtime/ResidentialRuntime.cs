// Original opt-in profile. Existing actors and boss infrastructure are reused in memory.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
namespace Malcolm.Runtime {
internal static class ResidentialRuntime {
    private const string Scene1="2d/level/scene2d/stage/stage_01/level_01_complete";
    private const string Scene12="2d/level/scene2d/stage/stage_12/level_12_complete";
    private const string Playfield12="2d/level/playfield/stage/stage_12/level_12_art";
    private static Assembly game,engine;
    private static Action<string> log;
    private static object previousForcedSpawn;
    private static readonly HashSet<object> prepared=new HashSet<object>();
    private static readonly string[] Ids={"6b673a21-3b4c-4c6e-933b-b615240c912a","5123f926-f88a-4edd-8756-50254a6160e3","58afa53a-3cb6-4031-a171-f77027df9a57"};
    private static readonly string[] Names={"FootSoldierAdvance_018","FootSoldierAdvance_019","FootSoldierAdvance_020"};
    private static readonly string[] Owners={"#CamBlock15_MP3","#CamBlock15_MP2","#CamBlock15_MP1"};
    private static readonly float[] OldX={5392,5464,5536};
    private static readonly float[] NewX={6096,6152,6208};
    private static readonly string[] RouteIds={"045e4b20-04bf-46d1-8d58-bb90a6582bac","f30a3cb8-3e82-481f-b3c9-b6b6c80c16d9","3befdc61-fe75-41c9-adf0-98d859493d4d","05633fe4-05fc-479b-942c-c73d9bc43d0f","e44cc08a-8d79-4130-929c-69ffe2a268f5","7030b144-5e4e-4716-99d5-199e0d206d8c"};
    private const BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
    internal static void Install(Harmony harmony,Assembly gameAssembly,Assembly engineAssembly,Action<string> logger) {
        game=gameAssembly;engine=engineAssembly;log=logger;
        Patch(harmony,game.GetType("Paris.Game.GameInfo",true).GetProperty("CurrentStageData").GetSetMethod(),"SelectStage");
        Patch(harmony,game.GetType("Paris.Game.Triggers.CameraBlockTrigger",true).GetMethod("PostReset",Flags),"PrepareBoss");
        Patch(harmony,game.GetType("Paris.Game.Triggers.CameraBlockTrigger",true).GetMethod("TriggerBlock",Flags),"AllowTrigger");
        harmony.Patch(game.GetType("Paris.Game.Actor.Camera.BeatEmUpCamera",true).GetMethod("Reset",Flags),null,new HarmonyMethod(typeof(ResidentialRuntime),"PrepareRoute"));
        log("RESIDENTIAL_READY Episode1 routes to native Stage12 boss approach");
    }
    private static void Patch(Harmony harmony,MethodInfo method,string prefix) {
        if(method==null)throw new MissingMethodException(prefix);
        harmony.Patch(method,new HarmonyMethod(typeof(ResidentialRuntime),prefix));
    }
    private static string Norm(object value){return Convert.ToString(value).Replace('\\','/').ToLowerInvariant();}
    private static object Get(object value,string name){if(value==null)throw new InvalidOperationException("Missing "+name);return value.GetType().GetProperty(name,Flags).GetValue(value,null);}
    private static void Set(object value,string name,object next){value.GetType().GetProperty(name,Flags).SetValue(value,next,null);}
    private static object Invoke(object value,string name,Type[] signature,params object[] args){return value.GetType().GetMethod(name,Flags,null,signature,null).Invoke(value,args);}
    private static object Actor(object scene,Guid id){return Get(Invoke(scene,"GetGameObjectDataByID",new[]{typeof(Guid)},id),"GameObject");}
    private static object Vector(Type type,float x,float y,float z){object result=Activator.CreateInstance(type);type.GetField("X").SetValue(result,x);type.GetField("Y").SetValue(result,y);type.GetField("Z").SetValue(result,z);return result;}
    private static bool PositionIs(object p,float x,float y,float z){return (float)p.GetType().GetField("X").GetValue(p)==x&&(float)p.GetType().GetField("Y").GetValue(p)==y&&(float)p.GetType().GetField("Z").GetValue(p)==z;}
    private static bool HasScene(object stage,string wanted){if(stage==null)return false;foreach(object path in (IEnumerable)Get(stage,"ScenePaths"))if(Norm(path)==wanted)return true;return false;}
    private static void SelectStage(object[] __args) {
        if(!HasScene(__args[0],Scene1)) {
            if(previousForcedSpawn!=null && !HasScene(__args[0],Scene12)) {
                engine.GetType("Paris.Engine.Scene.Scene2d",true).GetProperty("ForcedSpawnPos",Flags).SetValue(null,previousForcedSpawn,null);
                previousForcedSpawn=null;
            }
            return;
        }
        Type listType=game.GetType("Paris.Game.System.StageList",true);
        object list=listType.GetProperty("Singleton",Flags).GetValue(null,null);
        object target=null;foreach(object candidate in (IEnumerable)Get(list,"Items"))if(HasScene(candidate,Scene12)){if(target!=null)throw new InvalidOperationException("Duplicate native Stage12");target=candidate;}
        if(!HasScene(target,Scene12))throw new InvalidOperationException("Native Stage12 lookup failed");
        PropertyInfo spawn=engine.GetType("Paris.Engine.Scene.Scene2d",true).GetProperty("ForcedSpawnPos",Flags);
        if(previousForcedSpawn==null)previousForcedSpawn=spawn.GetValue(null,null);
        spawn.SetValue(null,Vector(spawn.PropertyType,4250,360,0),null);
        __args[0]=target;
        log("RESIDENTIAL_STAGE selected native Stage12 forcedSpawn=4250,360,0");
    }
    private static bool IsRouteBlock(object block) {
        for(int i=0;i<RouteIds.Length;i++)if(new Guid(RouteIds[i]).Equals(Get(block,"Id")))return Convert.ToString(Get(block,"Name"))=="CamBlock"+(11+i);
        return false;
    }
    private static bool AllowTrigger(object __instance) {
        // Trigger volumes can also enter TriggerBlock independently of the camera list.
        return !IsRouteBlock(__instance)||Norm(Get(Get(__instance,"Scene"),"PlayfieldPath"))!=Playfield12;
    }
    private static void PrepareRoute(object __instance) {
        object scene=Get(__instance,"Scene");if(Norm(Get(scene,"PlayfieldPath"))!=Playfield12)return;
        var cameraBlocks=(IList)__instance.GetType().GetField("_camBlocks",Flags).GetValue(__instance);
        var skip=new object[RouteIds.Length];
        // Validate every identity before native callbacks can alter any end-of-block objects.
        for(int i=0;i<skip.Length;i++) {
            skip[i]=Actor(scene,new Guid(RouteIds[i]));
            if(!IsRouteBlock(skip[i]))throw new InvalidOperationException("Residential route block mismatch");
        }
        object boss=Actor(scene,new Guid("8cbcf846-5120-4edf-82f4-a2f344678e2a"));
        if(Convert.ToString(Get(boss,"Name"))!="CamBlockBoss"||!cameraBlocks.Contains(boss))throw new InvalidOperationException("Residential boss camera missing");
        foreach(object block in skip) {
            // Same callbacks and list removal as the native forced-spawn checkpoint path.
            if(!(bool)Get(block,"DebugDisabled"))Invoke(block,"ToggleEndOfBlock",new[]{typeof(bool)},true);
            Set(block,"DebugDisabled",true);cameraBlocks.Remove(block);
        }
        log("RESIDENTIAL_ROUTE native blocks11..16 skipped; boss camera retained");
    }
    private static void PrepareBoss(object __instance) {
        if(!new Guid("8cbcf846-5120-4edf-82f4-a2f344678e2a").Equals(Get(__instance,"Id"))||Convert.ToString(Get(__instance,"Name"))!="CamBlockBoss")return;
        object scene=Get(__instance,"Scene");
        if(Norm(Get(scene,"PlayfieldPath"))!=Playfield12||prepared.Contains(__instance))return;
        try { Apply(scene,__instance); prepared.Add(__instance);log("RESIDENTIAL_CONFIGURED three native Foot Soldiers before Baxter intro"); }
        catch(Exception error){log("RESIDENTIAL_ABORTED "+error);throw;}
    }
    private static void Apply(object scene,object block) {
        var waves=(IList)Get(block,"Waves");var groups=(IList)Get(scene,"Groups");
        string[] expected={"#Cutscene_Hop","#Cutscene_BossIntro","#Cutscene_BossBanner","#BossFight"};
        if(waves.Count!=4)throw new InvalidOperationException("Boss wave count changed");
        for(int i=0;i<4;i++)if(Convert.ToString(Get(Get(waves[i],"Group"),"ID"))!=expected[i]||(bool)Get(waves[i],"StartActive"))throw new InvalidOperationException("Boss wave contract changed");
        object boss=Actor(scene,new Guid("fdcb9860-cc62-4637-9f2a-b986160c54f7"));
        if(boss.GetType().FullName!="Paris.Game.Actor.Baxter"||!new Guid("fdcb9860-cc62-4637-9f2a-b986160c54f7").Equals(Get(boss,"Id")))throw new InvalidOperationException("Baxter identity mismatch");
        var actors=new object[3];var positions=new object[3];var ownerMembers=new IDictionary[3];var memberships=new object[3];
        for(int i=0;i<3;i++) {
            Guid id=new Guid(Ids[i]);actors[i]=Actor(scene,id);positions[i]=Get(actors[i],"InitialPosition");
            if(!id.Equals(Get(actors[i],"Id"))||Convert.ToString(Get(actors[i],"Name"))!=Names[i]||actors[i].GetType().FullName!="Paris.Game.Actor.FootShortMelee"||!PositionIs(positions[i],OldX[i],368,0))throw new InvalidOperationException("Foot identity or position mismatch");
            object owner=null;foreach(object group in groups)if(Convert.ToString(Get(group,"Name"))==Owners[i]){if(owner!=null)throw new InvalidOperationException("Duplicate source group");owner=group;}
            ownerMembers[i]=(IDictionary)Get(owner,"Members");if(!ownerMembers[i].Contains(id))throw new InvalidOperationException("Missing original membership");memberships[i]=ownerMembers[i][id];
        }
        const string newName="#MalcolmResidentialFootWave";
        foreach(object group in groups)if(Convert.ToString(Get(group,"Name"))==newName)throw new InvalidOperationException("Residential group already exists unexpectedly");
        Type groupType=engine.GetType("Paris.Engine.Scene.GameObjectGroup",true);
        object addedGroup=Activator.CreateInstance(groupType);Set(addedGroup,"Name",newName);
        Set(addedGroup,"Type",Enum.Parse(groupType.GetProperty("Type").PropertyType,"Activation"));
        // Preserve the source group's native difficulty classification.
        object source=null;foreach(object group in groups)if(Convert.ToString(Get(group,"Name"))==Owners[0])source=group;
        Set(addedGroup,"Difficulty",Get(source,"Difficulty"));
        var newMembers=(IDictionary)Get(addedGroup,"Members");for(int i=0;i<3;i++)newMembers.Add(new Guid(Ids[i]),memberships[i]);
        object addedWave=Activator.CreateInstance(waves[0].GetType());
        object groupId=Activator.CreateInstance(Get(waves[0],"Group").GetType(),new object[]{newName});
        Set(addedWave,"Group",groupId);Set(addedWave,"Disabled",false);Set(addedWave,"StartActive",false);Set(addedWave,"EnemyThresholdToNextWave",0);Set(addedWave,"DelayToNextWave",1f);Set(addedWave,"AvailableDifficulty",Get(waves[0],"AvailableDifficulty"));
        var undo=new List<Action>();
        try {
            groups.Add(addedGroup);undo.Add(delegate{groups.Remove(addedGroup);});
            for(int i=0;i<3;i++){int j=i;Guid id=new Guid(Ids[j]);ownerMembers[j].Remove(id);undo.Add(delegate{ownerMembers[j][id]=memberships[j];});undo.Add(delegate{Set(actors[j],"InitialPosition",positions[j]);});Set(actors[j],"InitialPosition",Vector(positions[j].GetType(),NewX[j],368,0));}
            waves.Insert(1,addedWave);undo.Add(delegate{waves.Remove(addedWave);});
        } catch {for(int i=undo.Count-1;i>=0;i--)try{undo[i]();}catch(Exception restore){log("RESIDENTIAL_ROLLBACK_FAILED "+restore.Message);}throw;}
    }
}
}


