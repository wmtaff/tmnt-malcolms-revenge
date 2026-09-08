using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
namespace Malcolm.Runtime {
internal sealed class EncounterRollbackException : InvalidOperationException {
    internal EncounterRollbackException():base("Encounter rollback incomplete; stop this playtest"){}
}
internal static class EncounterRuntime {
    private sealed class Edit {
        public object Target,Before,After; public PropertyInfo Property;
        public void Set(bool restore){Property.SetValue(Target,restore?Before:After,null);}
    }
    internal static object Read(object value,string name) {
        if(value==null)throw new InvalidOperationException("Missing object for "+name);
        var property=value.GetType().GetProperty(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
        if(property==null)throw new InvalidOperationException("Missing property "+name);
        return property.GetValue(value,null);
    }
    private static object Call(object value,string name,Type[] types,object[] args){
        var method=value.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,types,null);
        if(method==null)throw new InvalidOperationException("Missing method "+name);
        return method.Invoke(value,args);
    }
    private static Edit Change(object target,string name,object after){
        var property=target.GetType().GetProperty(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
        if(property==null || !property.CanRead || !property.CanWrite)throw new InvalidOperationException("Unwritable property "+name);
        return new Edit{Target=target,Property=property,Before=property.GetValue(target,null),After=after};
    }
    private static bool EqualsVector(object value,ConfigVector3 expected){
        return Coordinate(value,"X")==expected.X && Coordinate(value,"Y")==expected.Y && Coordinate(value,"Z")==expected.Z;
    }
    private static float Coordinate(object value,string name){return (float)value.GetType().GetField(name).GetValue(value);}
    private static object Vector(object value,ConfigVector3 next){
        object result=Activator.CreateInstance(value.GetType());
        value.GetType().GetField("X").SetValue(result,next.X);value.GetType().GetField("Y").SetValue(result,next.Y);value.GetType().GetField("Z").SetValue(result,next.Z);
        return result;
    }
    internal static bool Apply(EncounterConfig config,object block,bool baseline,Action<string> log){
        var edits=new List<Edit>();
        try{
            object scene=Read(block,"Scene");
            string path=Convert.ToString(Read(scene,"PlayfieldPath")).Replace('\\','/').ToLowerInvariant();
            if(path!=config.Playfield || !config.Waves[0].BlockId.Equals(Read(block,"Id")) || Convert.ToString(Read(block,"Name"))!=config.Waves[0].BlockName)return false;
            var waves=Read(block,"Waves") as IList;
            if(waves==null)throw new InvalidOperationException("Missing native waves");
            foreach(var wave in config.Waves){
                if(wave.WaveIndex>=waves.Count)throw new InvalidOperationException("Native wave index absent");
                object native=waves[wave.WaveIndex];object groupId=Read(native,"Group");
                if((bool)Read(native,"StartActive"))throw new InvalidOperationException("Wave must reset its enemies: "+wave.Id);
                if(Convert.ToString(Read(groupId,"ID"))!=wave.GroupId)throw new InvalidOperationException("Wave group mismatch: "+wave.Id);
                float delay=(float)Read(native,"DelayToNextWave");
                if(delay!=wave.ExpectedDelay && (baseline || delay!=wave.Delay))throw new InvalidOperationException("Wave delay mismatch: "+wave.Id);
                edits.Add(Change(native,"DelayToNextWave",wave.Delay));
                object group=Call(groupId,"GetGroup",Type.EmptyTypes,new object[0]);
                var members=Read(group,"Members") as IDictionary;
                foreach(var enemy in wave.Enemies){
                    if(members==null || !members.Contains(enemy.Id))throw new InvalidOperationException("Enemy not in native wave: "+enemy.Name);
                    object data=Call(scene,"GetGameObjectDataByID",new Type[]{typeof(Guid)},new object[]{enemy.Id});
                    object actor=Read(data,"GameObject");
                    if(!enemy.Id.Equals(Read(actor,"Id")) || Convert.ToString(Read(actor,"Name"))!=enemy.Name)throw new InvalidOperationException("Enemy identity mismatch: "+enemy.Name);
                    object position=Read(actor,"InitialPosition");
                    if(!EqualsVector(position,enemy.Original) && (baseline || !EqualsVector(position,enemy.Position)))throw new InvalidOperationException("Enemy original position mismatch: "+enemy.Name);
                    edits.Add(Change(actor,"InitialPosition",Vector(position,enemy.Position)));
                }
            }
        }catch(Exception error){log("ENCOUNTER_REJECTED preflight="+error.Message);return false;}
        if(baseline){log("ENCOUNTER_BASELINE_VALIDATED id="+config.Id);return true;}
        int applied=0;
        try{foreach(var edit in edits){edit.Set(false);applied++;}}
        catch(Exception error){
            // Restore the failing setter too, in case it mutated before throwing.
            bool incomplete=false;
            for(int i=Math.Min(applied,edits.Count-1);i>=0;i--){
                try{edits[i].Set(true);}catch(Exception restoreError){incomplete=true;log("ENCOUNTER_ROLLBACK_ERROR property="+edits[i].Property.Name+" error="+restoreError.Message);}
            }
            log("ENCOUNTER_REJECTED rollbackIncomplete="+incomplete+" error="+error.Message);
            if(incomplete)throw new EncounterRollbackException();
            return false;
        }
        foreach(var wave in config.Waves){
            log("WAVE_CONFIGURED id="+wave.Id+" index="+wave.WaveIndex+" group="+wave.GroupId+" delay="+wave.Delay.ToString(System.Globalization.CultureInfo.InvariantCulture));
            foreach(var enemy in wave.Enemies)log("ENEMY_CONFIGURED id="+enemy.Id+" name="+enemy.Name+" initial="+enemy.Position.X.ToString(System.Globalization.CultureInfo.InvariantCulture)+","+enemy.Position.Y.ToString(System.Globalization.CultureInfo.InvariantCulture)+","+enemy.Position.Z.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        return true;
    }
}
}
