using System;
using System.Collections;
using System.Collections.Generic;
namespace Malcolm.Runtime {
public static partial class RuntimeLauncher {
    private static readonly HashSet<object> configuredBlocks=new HashSet<object>();
    private static readonly HashSet<object> abortedBlocks=new HashSet<object>();
    private static bool IsConfiguredBlock(object block){
        return encounter!=null && encounter.Waves[0].BlockId.Equals(Property(block,"Id")) && Convert.ToString(Property(block,"Name"))==encounter.Waves[0].BlockName && NormalizeScene(Property(block,"Scene"))==encounter.Playfield;
    }
    private static bool ConfiguredWaveBegin(object __instance,ref bool __result){
        if(!IsConfiguredBlock(__instance))return true;
        if(abortedBlocks.Contains(__instance)){__result=false;return false;}
        int index=Convert.ToInt32(Property(__instance,"CurrentWaveIndex"));
        if(index==0 && !configuredBlocks.Contains(__instance)){
            try{if(EncounterRuntime.Apply(encounter,__instance,false,Log)) configuredBlocks.Add(__instance);}
            catch(EncounterRollbackException){
                abortedBlocks.Add(__instance);__result=false;
                Log("ENCOUNTER_ABORTED incomplete rollback; stop and restart the playtest");return false;
            }
        }
        Log("NATIVE_WAVE_START index="+index+" configured="+configuredBlocks.Contains(__instance));
        return true;
    }
    private static void ConfiguredWaveEnd(object __instance){
        if(!IsConfiguredBlock(__instance) || !configuredBlocks.Contains(__instance))return;
        int index=Convert.ToInt32(Property(__instance,"CurrentWaveIndex"));
        if(index<0 || index>=encounter.Waves.Count)return;
        object scene=Property(__instance,"Scene");
        var find=scene.GetType().GetMethod("GetGameObjectDataByID",new Type[]{typeof(Guid)});
        foreach(var enemy in encounter.Waves[index].Enemies){
            object actor=Property(find.Invoke(scene,new object[]{enemy.Id}),"GameObject");
            Log("NATIVE_ENEMY_SPAWN wave="+index+" name="+enemy.Name+" position="+Property(actor,"Position")+" active="+Property(actor,"Active"));
        }
    }
}
}
