using System;using System.Collections;using System.Collections.Generic;using System.Reflection;
namespace Paris.Engine.Scene {
 public class GameObjectGroup {public enum GroupType{Activation} public string Name{get;set;} public GroupType Type{get;set;} public int Difficulty{get;set;} public Dictionary<Guid,object> Members{get;set;} public GameObjectGroup(){Members=new Dictionary<Guid,object>();}}
 public class GroupID {public string ID{get;set;}public GroupID(string id){ID=id;}}
}
namespace Paris.Game.Actor {
 public struct Vec {public float X,Y,Z;public Vec(float x,float y,float z){X=x;Y=y;Z=z;}}
 public class FootShortMelee {public Guid Id{get;set;}public string Name{get;set;}public Vec InitialPosition{get;set;}}
 public class Baxter {public Guid Id{get;set;}}
}
namespace Malcolm.Runtime {
internal static class ResidentialRuntimeTests {
 public class Wave {public Paris.Engine.Scene.GroupID Group{get;set;} public bool StartActive{get;set;}public bool Disabled{get;set;} public int EnemyThresholdToNextWave{get;set;}public float DelayToNextWave{get;set;}public int AvailableDifficulty{get;set;}}
 public class Block{public IList Waves{get;set;}}
 public class Data{public object GameObject{get;set;}}
 public class Scene{public IList Groups{get;set;}public Dictionary<Guid,Data> Actors=new Dictionary<Guid,Data>();public Data GetGameObjectDataByID(Guid id){return Actors[id];}}
 public class FailInsert:ArrayList{public override void Insert(int i,object value){throw new InvalidOperationException("synthetic insert failure");}}
 private static readonly string[] ids={"6b673a21-3b4c-4c6e-933b-b615240c912a","5123f926-f88a-4edd-8756-50254a6160e3","58afa53a-3cb6-4031-a171-f77027df9a57"};
 private static Scene Fixture(out Block block,bool fail){
  var scene=new Scene{Groups=new ArrayList()};block=new Block{Waves=fail?(IList)new FailInsert():new ArrayList()};
  foreach(string name in new[]{"#Cutscene_Hop","#Cutscene_BossIntro","#Cutscene_BossBanner","#BossFight"})block.Waves.Add(new Wave{Group=new Paris.Engine.Scene.GroupID(name),AvailableDifficulty=7});
  for(int i=0;i<3;i++){Guid id=new Guid(ids[i]);var group=new Paris.Engine.Scene.GameObjectGroup{Name="#CamBlock15_MP"+(3-i),Difficulty=1};group.Members.Add(id,new object());scene.Groups.Add(group);scene.Actors.Add(id,new Data{GameObject=new Paris.Game.Actor.FootShortMelee{Id=id,Name="FootSoldierAdvance_0"+(18+i),InitialPosition=new Paris.Game.Actor.Vec(5392+i*72,368,0)}});}
  Guid boss=new Guid("fdcb9860-cc62-4637-9f2a-b986160c54f7");scene.Actors.Add(boss,new Data{GameObject=new Paris.Game.Actor.Baxter{Id=boss}});return scene;
 }
 public static int Main(){try{
  Type type=typeof(ResidentialRuntime);type.GetField("engine",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,Assembly.GetExecutingAssembly());type.GetField("log",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,new Action<string>(Console.WriteLine));var apply=type.GetMethod("Apply",BindingFlags.NonPublic|BindingFlags.Static);
  Block block;Scene scene=Fixture(out block,false);apply.Invoke(null,new object[]{scene,block});
  if(scene.Groups.Count!=4||block.Waves.Count!=5||((Wave)block.Waves[1]).Group.ID!="#MalcolmResidentialFootWave"||((Wave)block.Waves[4]).Group.ID!="#BossFight")throw new Exception("Wave insertion contract failed");
  for(int i=0;i<3;i++)if(((Paris.Engine.Scene.GameObjectGroup)scene.Groups[i]).Members.Count!=0||((Paris.Game.Actor.FootShortMelee)scene.Actors[new Guid(ids[i])].GameObject).InitialPosition.X!=6096+i*56)throw new Exception("Actor transfer failed");
  scene=Fixture(out block,true);bool failed=false;try{apply.Invoke(null,new object[]{scene,block});}catch(TargetInvocationException){failed=true;}
  if(!failed||scene.Groups.Count!=3||block.Waves.Count!=4)throw new Exception("Rollback counts failed");
  for(int i=0;i<3;i++)if(((Paris.Engine.Scene.GameObjectGroup)scene.Groups[i]).Members.Count!=1||((Paris.Game.Actor.FootShortMelee)scene.Actors[new Guid(ids[i])].GameObject).InitialPosition.X!=5392+i*72)throw new Exception("Rollback failed");
  Console.WriteLine("RESIDENTIAL_SELF_TEST_PASS native-shaped transfer and rollback");return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}}
