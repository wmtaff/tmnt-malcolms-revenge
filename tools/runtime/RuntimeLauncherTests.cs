using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
namespace Malcolm.Runtime {
public static partial class RuntimeLauncher {
    private static void SelfTest() {
        RuntimeOptions options = RuntimeOptions.Parse(new string[] { "game", "output.log", "--baseline" });
        if (!options.Baseline || options.EncounterPath != null) throw new Exception("Baseline CLI wrong");
        options = RuntimeOptions.Parse(new string[] { "game", "output.log", "--encounter", "encounter.json" });
        if (options.Baseline || options.EncounterPath != "encounter.json") throw new Exception("Encounter CLI wrong");
        foreach (string[] bad in new string[][] {
            new string[] { "game", "output.log" },
            new string[] { "game", "output.log", "--encounter" },
            new string[] { "game", "output.log", "--baseline", "--encounter", "x.json" },
            new string[] { "game", "output.log", "--unknown" }
        }) {
            bool invalid = false;
            try { RuntimeOptions.Parse(bad); } catch (ArgumentException) { invalid = true; }
            if (!invalid) throw new Exception("Ambiguous CLI accepted");
        }
        string temp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "malcolm-output-test-" + Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(temp);
        string existing = System.IO.Path.Combine(temp, "existing.log");
        try {
            System.IO.File.WriteAllText(existing, "preserve");
            bool blocked = false;
            try { AssertDiagnosticPath(existing, ".log"); } catch (InvalidOperationException) { blocked = true; }
            if (!blocked || System.IO.File.ReadAllText(existing) != "preserve") throw new Exception("Existing log not protected");
            blocked = false;
            try { AssertDiagnosticPath(System.IO.Path.Combine(temp,"game.exe"), ".log"); } catch (InvalidOperationException) { blocked = true; }
            if (!blocked) throw new Exception("Unsafe diagnostic extension accepted");
            AssertDiagnosticPath(System.IO.Path.Combine(temp,"fresh.log"), ".log");
        } finally { System.IO.File.Delete(existing); System.IO.Directory.Delete(temp); }
        using (var bmp = new System.IO.MemoryStream()) {
            WriteBitmap(bmp, new byte[] {255,0,0,255, 0,0,255,255}, 2, 1);
            byte[] bytes = bmp.ToArray();
            if (bytes.Length != 62 || bytes[0] != 66 || bytes[1] != 77 || bytes[54] != 0 || bytes[56] != 255 || bytes[57] != 255 || bytes[59] != 0)
                throw new Exception("BMP header or RGB channel conversion wrong");
        }
        using (var large = new System.IO.MemoryStream()) {
            WriteBitmap(large, new byte[3440 * 1440 * 4], 3440, 1440);
            if (large.Length != 54L + 3440L * 1440 * 3 || large.Length > 32L * 1024 * 1024) throw new Exception("Ultrawide capture size wrong");
        }
        bool rejected = false;
        try { WriteBitmap(System.IO.Stream.Null, new byte[0], 3841, 2160); } catch (ArgumentException) { rejected = true; }
        if (!rejected) throw new Exception("Oversized capture accepted");
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
        EncounterConfigTests.Run();
        EncounterRuntimeTests.Run();
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








