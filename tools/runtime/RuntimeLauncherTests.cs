using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
namespace Malcolm.Runtime {
public static partial class RuntimeLauncher {
    private static void SelfTest() {
        var h = new Harmony("malcolm.runtime.selftest");
        MethodInfo target = typeof(Surrogate).GetMethod("Save");
        h.Patch(target, prefix: new HarmonyMethod(typeof(RuntimeLauncher).GetMethod("SuppressWrite", BindingFlags.Static | BindingFlags.NonPublic)));
                var inherited = new InheritedSurrogate();
        Patch(h, RequireMethod(typeof(InheritedSurrogate), "Save", 0), "SuppressWrite", null);
        inherited.Save();
        if (inherited.Writes != 0) throw new Exception("Inherited method suppression failed");
        var s = new Surrogate(); s.Save();
        if (s.Writes != 0) throw new Exception("Save suppression failed");
        h.UnpatchAll("malcolm.runtime.selftest"); s.Save();
        if (s.Writes != 1) throw new Exception("Unpatch restore failed");
                var enemy = new TestEnemy();
        baseline = true; bool matched; EncounterBegin(enemy, out matched);
        if (!matched || enemy.InitialPosition.X != 483) throw new Exception("Baseline mutated target");
        baseline = false; enemy.Name = "Different"; EncounterBegin(enemy, out matched);
        if (matched || enemy.InitialPosition.X != 483) throw new Exception("Wrong object mutated");
        enemy.Name = "FootSoldierRegular_202"; enemy.Scene.PlayfieldPath = "other"; EncounterBegin(enemy, out matched);
        if (matched || enemy.InitialPosition.X != 483) throw new Exception("Wrong scene mutated");
        enemy.Scene.PlayfieldPath = "2d\\Level\\Playfield\\Stage\\Stage_01\\Level_01_art";
        enemy.Id = Guid.Empty; EncounterBegin(enemy, out matched);
        if (matched || enemy.InitialPosition.X != 483) throw new Exception("Wrong ID mutated");
        enemy.Id = new Guid("6f229aef-a56f-4457-b5a0-60d158b48fb1");
        EncounterBegin(enemy, out matched);
        if (!matched || enemy.InitialPosition.X != 563) throw new Exception("Target not shifted");
        EncounterBegin(enemy, out matched);
        if (enemy.InitialPosition.X != 563) throw new Exception("Repeated reset accumulated shift");
        Console.WriteLine("SELF_TEST_PASS save suppressed and original restored");
    }
    public struct TestVector { public float X, Y, Z; public TestVector(float x, float y, float z) { X=x;Y=y;Z=z; } }
    public sealed class TestScene { public string PlayfieldPath {get;set;} public TestScene(){PlayfieldPath="2d\\Level\\Playfield\\Stage\\Stage_01\\Level_01_art";} }
    public sealed class TestEnemy {
        public Guid Id {get;set;} public string Name {get;set;} public TestScene Scene {get;set;} public TestVector InitialPosition {get;set;}
        public TestEnemy(){Id=new Guid("6f229aef-a56f-4457-b5a0-60d158b48fb1");Name="FootSoldierRegular_202";Scene=new TestScene();InitialPosition=new TestVector(483,228,0);}
    }
    public sealed class InheritedSurrogate : Surrogate { }
    public class Surrogate {
        public int Writes;
        [MethodImpl(MethodImplOptions.NoInlining)] public void Save() { Writes++; }
    }
}}





