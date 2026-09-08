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
        var s = new Surrogate(); s.Save();
        if (s.Writes != 0) throw new Exception("Save suppression failed");
        h.UnpatchAll("malcolm.runtime.selftest"); s.Save();
        if (s.Writes != 1) throw new Exception("Unpatch restore failed");
        Console.WriteLine("SELF_TEST_PASS save suppressed and original restored");
    }
    public sealed class Surrogate {
        public int Writes;
        [MethodImpl(MethodImplOptions.NoInlining)] public void Save() { Writes++; }
    }
}}

