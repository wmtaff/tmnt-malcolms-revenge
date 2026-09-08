using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using HarmonyLib;
namespace Malcolm.Runtime {
public static partial class RuntimeLauncher {
    private static string gameDirectory;
    private static string logFile;
    private static bool baseline;
    private static readonly object logLock = new object();
    [DllImport("kernel32.dll", SetLastError=true, CharSet=CharSet.Unicode)]
    private static extern bool SetDllDirectory(string directory);
    [STAThread]
    public static int Main(string[] args) {
        try {
            if (args.Length == 1 && args[0] == "--self-test") { SelfTest(); return 0; }
            if (args.Length < 2 || args.Length > 3 || (args.Length == 3 && args[2] != "--baseline"))
                throw new ArgumentException("Usage: RuntimeLauncher.exe <game-dir> <log-file> [--baseline] or --self-test");
            gameDirectory = Path.GetFullPath(args[0]);
            logFile = Path.GetFullPath(args[1]);
            baseline = args.Length == 3;
            Directory.CreateDirectory(Path.GetDirectoryName(logFile));
            Log("START baseline=" + baseline + " game=" + gameDirectory + " runtime=" + Environment.Version + " x64=" + Environment.Is64BitProcess);
            if (!Environment.Is64BitProcess) throw new InvalidOperationException("A 64-bit launcher is required");
            Directory.SetCurrentDirectory(gameDirectory);
            if (!SetDllDirectory(gameDirectory)) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
            AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e) { Log("UNHANDLED " + e.ExceptionObject); };
            Assembly engine = Assembly.LoadFrom(Path.Combine(gameDirectory, "ParisEngine.dll"));
            Assembly game = Assembly.LoadFrom(Path.Combine(gameDirectory, "TMNT.exe"));
            Log("ASSEMBLY " + game.FullName);
            var harmony = new Harmony("malcolm.runtime.playtest");
            Type save = RequireType(engine, "Paris.Engine.Save.SaveSystem");
            Patch(harmony, RequireMethod(save, "Save", 0), "SuppressWrite", null);
            Patch(harmony, RequireMethod(save, "DeleteSave", 1), "SuppressWrite", null);
            // DeleteCrossSave is inherited and currently empty; suppress it defensively as well.
            Patch(harmony, RequireMethod(save, "DeleteCrossSave", 1), "SuppressWrite", null);
            Type scene = RequireType(engine, "Paris.Engine.Scene.Scene2d");
            Patch(harmony, RequireMethod(scene, "Load", 0), "SceneBegin", "SceneEnd");
            Patch(harmony, RequireMethod(scene, "LoadPlayfield", 0), "SceneBegin", "SceneEnd");
            InstallEncounterPatch(harmony, game);
            MethodInfo main = RequireMethod(RequireType(game, "Paris.Program"), "Main", 1);
            Log("HOOKS_READY entering Paris.Program.Main");
            main.Invoke(null, new object[] { new string[] { "-AllowMultiInstance", "-Windowed" } });
            Log("GAME_RETURNED");
            return 0;
        } catch (Exception error) {
            Log("FATAL " + error);
            Console.Error.WriteLine(error);
            return 1;
        }
    }
    private static Assembly ResolveAssembly(object sender, ResolveEventArgs args) {
        string name = new AssemblyName(args.Name).Name;
        if (name.IndexOfAny(new char[] {'/', '\\'}) >= 0) return null;
        string path = Path.Combine(gameDirectory, name + ".dll");
        return File.Exists(path) ? Assembly.LoadFrom(path) : null;
    }
    private static Type RequireType(Assembly assembly, string name) { return assembly.GetType(name, true); }
    private static MethodInfo RequireMethod(Type type, string name, int count) {
        MethodInfo result = null;
        foreach (MethodInfo method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)) {
            if (method.Name != name || method.GetParameters().Length != count) continue;
            if (result != null) throw new AmbiguousMatchException(type.FullName + "." + name);
            result = method;
        }
        if (result == null) throw new MissingMethodException(type.FullName, name);
        return result;
    }
    private static void Patch(Harmony harmony, MethodInfo method, string prefix, string postfix) {
        // Reflection returns inherited methods with ReflectedType=derived; Harmony 2.2.1
        // requires the MethodInfo obtained directly from its declaring type.
        ParameterInfo[] parameters = method.GetParameters();
        Type[] signature = new Type[parameters.Length];
        for (int i = 0; i < parameters.Length; i++) signature[i] = parameters[i].ParameterType;
        method = method.DeclaringType.GetMethod(method.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly, null, signature, null);
        harmony.Patch(method, prefix == null ? null : new HarmonyMethod(typeof(RuntimeLauncher), prefix), postfix == null ? null : new HarmonyMethod(typeof(RuntimeLauncher), postfix));
        Log("PATCH " + method.DeclaringType.FullName + "." + method.Name);
    }
    private static bool SuppressWrite(MethodBase __originalMethod) { Log("WRITE_SUPPRESSED " + __originalMethod.DeclaringType.FullName + "." + __originalMethod.Name); return false; }
    private static object Property(object instance, string name) {
        if (instance == null) return null;
        PropertyInfo property = instance.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        return property == null ? null : property.GetValue(instance, null);
    }
    private static void SceneBegin(object __instance, MethodBase __originalMethod) { Log("SCENE_BEGIN " + __originalMethod.Name + " path=" + Property(__instance, "PlayfieldPath")); }
    private static void SceneEnd(object __instance, MethodBase __originalMethod) { Log("SCENE_END " + __originalMethod.Name + " path=" + Property(__instance, "PlayfieldPath")); }
    private static void Log(string message) {
        string line = DateTime.UtcNow.ToString("o") + " " + message;
        lock (logLock) {
            Console.WriteLine(line);
            if (logFile != null) File.AppendAllText(logFile, line + Environment.NewLine);
        }
    }
    private static string NormalizeScene(object scene) {
        return Convert.ToString(Property(scene, "PlayfieldPath")).Replace('\\', '/').ToLowerInvariant();
    }
    private static void EncounterBegin(object __instance, out bool __state) {
        __state = false;
        string scene = NormalizeScene(Property(__instance, "Scene"));
        if (scene != "2d/level/scene2d/stage/stage_01/level_01_complete") return;
        string name = Convert.ToString(Property(__instance, "Name"));
        object position = Property(__instance, "InitialPosition");
        Log("ENEMY_RESET name=" + name + " scene=" + scene + " initial=" + position);
        if (name != "FootSoldierRegular_202") return;
        FieldInfo x = position.GetType().GetField("X");
        FieldInfo y = position.GetType().GetField("Y");
        FieldInfo z = position.GetType().GetField("Z");
        float originalX = (float)x.GetValue(position);
        if (originalX != 483f || (float)y.GetValue(position) != 228f || (float)z.GetValue(position) != 0f) {
            Log("TARGET_GUARD_SKIP unexpected initial=" + position); return;
        }
        __state = true;
        if (baseline) { Log("TARGET_BASELINE name=" + name + " xyz=483,228,0"); return; }
        x.SetValue(position, 563f);
        __instance.GetType().GetProperty("InitialPosition").SetValue(__instance, position, null);
        Log("ENCOUNTER_CHANGED name=" + name + " original=483,228,0 new=563,228,0");
    }
    private static void EncounterEnd(object __instance, bool __state) {
        if (__state) Log("TARGET_AFTER_RESET name=" + Property(__instance, "Name") + " position=" + Property(__instance, "Position") + " initial=" + Property(__instance, "InitialPosition"));
    }
    private static void InstallEncounterPatch(Harmony harmony, Assembly game) {
        Type enemy = RequireType(game, "Paris.Game.Actor.EnemySpawn");
        if (!enemy.IsAssignableFrom(RequireType(game, "Paris.Game.Actor.FootSoldier"))) throw new InvalidOperationException("FootSoldier inheritance changed");
        Patch(harmony, RequireMethod(enemy, "Reset", 0), "EncounterBegin", "EncounterEnd");
    }
}}

