using System;
using System.Collections.Generic;
namespace Malcolm.Runtime {
internal static class EncounterRuntimeTests {
    internal static void Run() {
        var config=EncounterConfig.Parse(EncounterConfigTests.Fixture());
        var block=new Block(config);
        if (!EncounterRuntime.Apply(config,block,true,delegate(string s){})) throw new Exception("Baseline rejected");
        if(block.Waves[0].DelayToNextWave!=0 || block.Scene.Data[config.Waves[0].Enemies[0].Id].GameObject.InitialPosition.X!=10) throw new Exception("Baseline mutated");
        block.Scene.Data[config.Waves[2].Enemies[0].Id].GameObject.Name="Wrong";
        if(EncounterRuntime.Apply(config,block,false,delegate(string s){})) throw new Exception("Invalid third wave accepted");
        if(block.Waves[0].DelayToNextWave!=0 || block.Scene.Data[config.Waves[0].Enemies[0].Id].GameObject.InitialPosition.X!=10) throw new Exception("Partial edit before preflight");
        block=new Block(config);
        if(!EncounterRuntime.Apply(config,block,false,delegate(string s){})) throw new Exception("Valid edit rejected");
        foreach(var wave in config.Waves) {
            if(block.Waves[wave.WaveIndex].DelayToNextWave!=1.5f || block.Scene.Data[wave.Enemies[0].Id].GameObject.InitialPosition.X!=30) throw new Exception("Wave not changed");
        }
        if(!EncounterRuntime.Apply(config,block,false,delegate(string s){})) throw new Exception("Repeat edit rejected");
        block=new Block(config); block.Waves[1].Group.Value.Members.Clear();
        if(EncounterRuntime.Apply(config,block,false,delegate(string s){})) throw new Exception("Nonmember edited");
        block=new Block(config);block.Id=Guid.Empty;
        if(EncounterRuntime.Apply(config,block,false,delegate(string s){})) throw new Exception("Wrong block edited");
        block=new Block(config);block.Scene.PlayfieldPath="other";
        if(EncounterRuntime.Apply(config,block,false,delegate(string s){})) throw new Exception("Wrong scene edited");
        block=new Block(config);block.Waves[2].StartActive=true;
        if(EncounterRuntime.Apply(config,block,false,delegate(string s){})) throw new Exception("Already active wave accepted");
        block=new Block(config);block.Waves[1].ThrowAfterSet=true;
        try{EncounterRuntime.Apply(config,block,false,delegate(string s){});}catch(EncounterRollbackException){}
        if(block.Waves[0].DelayToNextWave!=0 || block.Waves[1].DelayToNextWave!=0 || block.Scene.Data[config.Waves[0].Enemies[0].Id].GameObject.InitialPosition.X!=10)throw new Exception("Rollback failed to restore earlier edits");
        block=new Block(config);block.Waves[1].ThrowOnRestore=true;string messages="";
        bool aborted=false;
        try{EncounterRuntime.Apply(config,block,false,delegate(string s){messages+=s;});}catch(EncounterRollbackException){aborted=true;}
        if(!aborted)throw new Exception("Unrestorable setter accepted");
        if(block.Waves[0].DelayToNextWave!=0 || block.Scene.Data[config.Waves[0].Enemies[0].Id].GameObject.InitialPosition.X!=10 || !messages.Contains("rollbackIncomplete=True"))throw new Exception("Incomplete rollback not handled");
        Console.WriteLine("ENCOUNTER_RUNTIME_TEST_PASS");
    }
    public struct Vector { public float X,Y,Z; public Vector(float x,float y,float z){X=x;Y=y;Z=z;} }
    public class Enemy {public Guid Id{get;set;}public string Name{get;set;}public Vector InitialPosition{get;set;}}
    public class Data {public Enemy GameObject{get;set;}}
    public class Scene {
        public string PlayfieldPath{get;set;}
        public Dictionary<Guid,Data> Data=new Dictionary<Guid,Data>();
        public Data GetGameObjectDataByID(Guid id){Data item;return Data.TryGetValue(id,out item)?item:null;}
    }
    public class Group {public Dictionary<Guid,object> Members{get;set;}}
    public class GroupId {public string ID{get;set;}public Group Value;public Group GetGroup(){return Value;}}
    public class Wave {
        public GroupId Group{get;set;}public bool StartActive{get;set;}
        private float delay;public bool ThrowAfterSet,ThrowOnRestore;
        public float DelayToNextWave{get{return delay;}set{
            if(ThrowOnRestore && value==0)throw new Exception("Restore fails");
            delay=value;
            if(ThrowAfterSet || ThrowOnRestore)throw new Exception("Setter mutated before failure");
        }}
    }
    public class Block {
        public Guid Id{get;set;}public string Name{get;set;}public Scene Scene{get;set;}
        public List<Wave> Waves{get;set;}
        public Block(EncounterConfig config){
            Id=config.Waves[0].BlockId;Name=config.Waves[0].BlockName;
            Scene=new Scene{PlayfieldPath=config.Playfield};Waves=new List<Wave>();
            foreach(var edit in config.Waves){
                var group=new Group{Members=new Dictionary<Guid,object>()};
                Waves.Add(new Wave{DelayToNextWave=edit.ExpectedDelay,Group=new GroupId{ID=edit.GroupId,Value=group}});
                foreach(var enemy in edit.Enemies){group.Members.Add(enemy.Id,new object());Scene.Data.Add(enemy.Id,new Data{GameObject=new Enemy{Id=enemy.Id,Name=enemy.Name,InitialPosition=new Vector(enemy.Original.X,enemy.Original.Y,enemy.Original.Z)}});}
            }
        }
    }
}
}
