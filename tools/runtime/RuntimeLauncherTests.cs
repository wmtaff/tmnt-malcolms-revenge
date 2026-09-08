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
        options = RuntimeOptions.Parse(new string[] { "game", "output.log", "--residential", "art directory" });
        if (options.Baseline || options.EncounterPath != null || options.ResidentialDirectory != "art directory") throw new Exception("Residential CLI wrong");
        options = RuntimeOptions.Parse(new[] { "game", "output.log", "--baseline", "--character", "Malcolm manifest.json" });
        if (!options.Baseline || options.CharacterManifestPath != "Malcolm manifest.json") throw new Exception("Baseline plus character CLI wrong");
        options = RuntimeOptions.Parse(new[] { "game", "output.log", "--character", "Malcolm.json", "--encounter", "waves.json" });
        if (options.EncounterPath != "waves.json" || options.CharacterManifestPath != "Malcolm.json") throw new Exception("Character before mode CLI wrong");
        options = RuntimeOptions.Parse(new[] { "game", "output.log", "--residential", "art", "--character", "Malcolm.json" });
        if (options.ResidentialDirectory != "art" || options.CharacterManifestPath != "Malcolm.json") throw new Exception("Residential plus character CLI wrong");
        foreach (string[] bad in new string[][] {
            new string[] { "game", "output.log" },
            new string[] { "game", "output.log", "--encounter" },
            new string[] { "game", "output.log", "--baseline", "--encounter", "x.json" },
            new string[] { "game", "output.log", "--unknown" }
            ,new string[] { "game", "output.log", "--residential" }
            ,new string[] { "game", "output.log", "--residential", " " }
            ,new string[] { "game", "output.log", "--baseline", "--residential", "art" }
            ,new string[] { "game", "output.log", "--encounter", "x.json", "--residential", "art" }
            ,new string[] { "game", "output.log", "--character", "Malcolm.json" }
            ,new string[] { "game", "output.log", "--baseline", "--character" }
            ,new string[] { "game", "output.log", "--baseline", "--character", " " }
            ,new string[] { "game", "output.log", "--baseline", "--character", "--encounter" }
            ,new string[] { "game", "output.log", "--baseline", "--baseline" }
            ,new string[] { "game", "output.log", "--baseline", "--character", "a", "--character", "b" }
            ,new string[] { "game", "output.log", "--character", "a", "--baseline", "--encounter", "b" }
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
        BackgroundRuntime.SelfTest();
        var waveOutput = new System.IO.StringWriter();
        System.IO.TextWriter previousOutput = Console.Out;
        try {
            Console.SetOut(waveOutput);
            var diagnosticBlock = new DiagnosticBlock();
            ResidentialWaveEnd(diagnosticBlock, false);
            if (waveOutput.ToString().Length != 0) throw new Exception("Failed wave logged as started");
            foreach (string group in new string[] { "#MalcolmResidentialFootWave", "#Cutscene_BossIntro", "#Cutscene_BossBanner", "#BossFight" }) {
                diagnosticBlock.Waves[0].Group.ID = group;
                ResidentialWaveEnd(diagnosticBlock, true);
                if (!waveOutput.ToString().Contains("group=" + group)) throw new Exception("Residential wave group missing");
            }
            ResidentialBlockEnd(diagnosticBlock, new object[] { false });
            if (waveOutput.ToString().Contains("RESIDENTIAL_BOSS_BLOCK_COMPLETED")) throw new Exception("Block reset logged as completion");
            ResidentialBlockEnd(diagnosticBlock, new object[] { true });
            if (!waveOutput.ToString().Contains("RESIDENTIAL_BOSS_BLOCK_COMPLETED")) throw new Exception("Boss block completion missing");
            waveOutput.GetStringBuilder().Length = 0;
            diagnosticBlock.Name = "OtherBlock";
            ResidentialWaveEnd(diagnosticBlock, true);
            ResidentialBlockEnd(diagnosticBlock, new object[] { true });
            if (waveOutput.ToString().Length != 0) throw new Exception("Unrelated block produced residential diagnostics");
        } finally { Console.SetOut(previousOutput); }
        var renderHarmony = new Harmony("malcolm.runtime.background.selftest");
        try {
            Patch(renderHarmony, RequireMethod(typeof(RenderSurrogate), "Render", 0), "RenderResidentialBackground", null);
            var rendered = new RenderSurrogate();
            rendered.Render();
            if (rendered.Calls != 1) throw new Exception("Unconfigured background hook suppressed native rendering");
        } finally { renderHarmony.UnpatchAll("malcolm.runtime.background.selftest"); }
        Console.WriteLine("SELF_TEST_PASS save suppressed and original restored");
    }
    public struct TestVector { public float X, Y, Z; public TestVector(float x, float y, float z) { X=x;Y=y;Z=z; } }
    public sealed class TestScene { public string PlayfieldPath {get;set;} public TestScene(){PlayfieldPath="2d\\Level\\Playfield\\Stage\\Stage_01\\Level_01_art";} }
    public sealed class TestEnemy {
        public Guid Id {get;set;} public string Name {get;set;} public TestScene Scene {get;set;} public TestVector InitialPosition {get;set;}
        public TestEnemy(){Id=new Guid("6f229aef-a56f-4457-b5a0-60d158b48fb1");Name="FootSoldierRegular_202";Scene=new TestScene();InitialPosition=new TestVector(483,228,0);}
    }
    public sealed class InheritedSurrogate : Surrogate { }
    public sealed class RenderSurrogate {
        public int Calls;
        [MethodImpl(MethodImplOptions.NoInlining)] public void Render() { Calls++; }
    }
    public sealed class DiagnosticGroup { public string ID { get; set; } }
    public sealed class DiagnosticWave { public DiagnosticGroup Group { get; set; } }
    public sealed class DiagnosticBlock {
        public Guid Id { get { return new Guid("8cbcf846-5120-4edf-82f4-a2f344678e2a"); } }
        public string Name { get; set; }
        public TestScene Scene { get; set; }
        public int CurrentWaveIndex { get { return 0; } }
        public DiagnosticWave[] Waves { get; set; }
        public DiagnosticBlock() { Name = "CamBlockBoss"; Scene = new TestScene { PlayfieldPath = "2d/level/playfield/stage/stage_12/level_12_art" }; Waves = new[] { new DiagnosticWave { Group = new DiagnosticGroup() } }; }
    }
    public class Surrogate {
        public int Writes;
        [MethodImpl(MethodImplOptions.NoInlining)] public void Save() { Writes++; }
    }
}}








