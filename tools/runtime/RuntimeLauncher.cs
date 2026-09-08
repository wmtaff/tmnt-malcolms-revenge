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
    private static string capturePath;
    private static int presentCount;
    private static bool captureReported;
    private static bool captureFailureReported;
    private static readonly System.Collections.Generic.HashSet<string> exceptionKeys = new System.Collections.Generic.HashSet<string>();
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
            ValidatePlaytest(gameDirectory);
            Directory.SetCurrentDirectory(gameDirectory);
            if (!SetDllDirectory(gameDirectory)) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            AppDomain.CurrentDomain.FirstChanceException += delegate(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e) { TraceStartupException(e.Exception); };
            AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
            AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e) { Log("UNHANDLED " + e.ExceptionObject); };
            InitializeCrashReporter();
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
            InstallRenderDiagnostics(harmony);
            MethodInfo main = RequireMethod(RequireType(game, "Paris.Program"), "Main", 1);
            Log("HOOKS_READY entering Paris.Program.Main");
            main.Invoke(null, new object[] { new string[] { "-AllowMultiInstance", "-Windowed", "-scale=2" } });
            Log("GAME_RETURNED");
            return 0;
        } catch (Exception error) {
            Log("FATAL " + error);
            Console.Error.WriteLine(error);
            return 1;
        }
    }
    private static void InstallRenderDiagnostics(Harmony harmony) {
        capturePath = Environment.GetEnvironmentVariable("MALCOLM_CAPTURE_FRAME");
        if (String.IsNullOrEmpty(capturePath)) return;
        capturePath = Path.GetFullPath(capturePath);
        Type device = RequireType(Assembly.LoadFrom(Path.Combine(gameDirectory, "FNA.dll")), "Microsoft.Xna.Framework.Graphics.GraphicsDevice");
        Patch(harmony, RequireMethod(device, "Present", 0), "CaptureFrame", null);
        Patch(harmony, RequireMethod(device, "Present", 3), "CaptureFrame", null);
        Log("FRAME_CAPTURE_ARMED every=120 path=" + capturePath);
    }
    private static void CaptureFrame(object __instance) {
        int frame = ++presentCount;
        if (frame == 1 || frame % 600 == 0) Log("PRESENT frame=" + frame);
        if (frame % 120 != 0) return;
        try {
            object parameters = Property(__instance, "PresentationParameters");
            int width = Convert.ToInt32(Property(parameters, "BackBufferWidth"));
            int height = Convert.ToInt32(Property(parameters, "BackBufferHeight"));
            if (width <= 0 || height <= 0 || width > 3840 || height > 2160) throw new InvalidOperationException("Backbuffer outside capture bound");
            if (54L + (((width * 3 + 3) & ~3) * (long)height) > 32L * 1024 * 1024) throw new InvalidOperationException("Capture exceeds 32 MiB file limit");
            string format = Convert.ToString(Property(parameters, "BackBufferFormat"));
            if (format != "Color") throw new InvalidOperationException("Unsupported backbuffer format " + format);
            byte[] pixels = new byte[checked(width * height * 4)];
            MethodInfo read = RequireMethod(__instance.GetType(), "GetBackBufferData", 1).MakeGenericMethod(typeof(byte));
            read.Invoke(__instance, new object[] { pixels });
            Directory.CreateDirectory(Path.GetDirectoryName(capturePath));
            using (Stream output = new FileStream(capturePath, FileMode.Create, FileAccess.Write)) WriteBitmap(output, pixels, width, height);
            if (!captureReported) { captureReported = true; Log("FRAME_CAPTURED updatesEvery=120 frame=" + frame + " size=" + width + "x" + height + " path=" + capturePath); }
        } catch (Exception error) { if (!captureFailureReported) { captureFailureReported = true; Log("FRAME_CAPTURE_FAILED " + error); } }
    }
    private static void WriteBitmap(Stream output, byte[] rgba, int width, int height) {
        if (width <= 0 || height <= 0 || width > 3840 || height > 2160 || rgba.Length != checked(width * height * 4)) throw new ArgumentException("Invalid bounded RGBA image");
        int stride = (width * 3 + 3) & ~3;
        var writer = new BinaryWriter(output);
        writer.Write((byte)'B'); writer.Write((byte)'M'); writer.Write(54 + stride * height);
        writer.Write(0); writer.Write(54); writer.Write(40); writer.Write(width); writer.Write(-height);
        writer.Write((short)1); writer.Write((short)24); writer.Write(0); writer.Write(stride * height);
        writer.Write(0); writer.Write(0); writer.Write(0); writer.Write(0);
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                int index = (y * width + x) * 4;
                writer.Write(rgba[index + 2]); writer.Write(rgba[index + 1]); writer.Write(rgba[index]);
            }
            for (int padding = width * 3; padding < stride; padding++) writer.Write((byte)0);
        }
        writer.Flush();
    }
    private static void ValidatePlaytest(string directory) {
        string marker = Path.Combine(directory, ".malcolm-playtest.json");
        if (!File.Exists(marker) || new FileInfo(marker).Length > 256 || File.ReadAllText(marker).Trim() != "{\"schemaVersion\":1,\"owner\":\"malcolm-mod-runtime\"}")
            throw new InvalidOperationException("Playtest ownership marker missing or invalid; use build.ps1");
        string[] names = { "TMNT.exe", "ParisEngine.dll", "ParisSerializers.dll" };
        string[] hashes = {
            "36435CC7E1063F76E4641C92F95601414B62C1FAEDA39A64CCC270EEF6D82CC6",
            "02FAE3072962C2304F9F086DB1680D8111D24CBF2F16A362C262809C62E4E7DE",
            "12A8F1B1288664D343EE8E8E68BB5EB593696D4CEB62D6ADD27AF5B72B81F554"
        };
        for (int i = 0; i < names.Length; i++) {
            using (var algorithm = System.Security.Cryptography.SHA256.Create())
            using (var stream = File.OpenRead(Path.Combine(directory, names[i]))) {
                string hash = BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", "");
                if (hash != hashes[i]) throw new InvalidOperationException("Unsupported game build: " + names[i]);
            }
        }
        Log("PLAYTEST_VERIFIED marker and three assembly hashes");
    }
    private static void InitializeCrashReporter() {
        // NBug discovers protocol factories by calling GetTypes on every loaded
        // assembly. Run its normal initialization before Harmony eagerly loads
        // Steamworks method dependencies containing unsupported unused union types.
        Assembly nbug = Assembly.LoadFrom(Path.Combine(gameDirectory, "NBug.dll"));
        Type settings = RequireType(nbug, "NBug.Settings");
        settings.GetProperty("Destinations", BindingFlags.Public | BindingFlags.Static).GetValue(null, null);
        object protocols = settings.GetField("_availableProtocols", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
        protocols.GetType().GetProperty("Value").GetValue(protocols, null);
        Log("CRASH_REPORTER_READY before game hooks");
    }
    private static void TraceStartupException(Exception error) {
        string type = error.GetType().FullName;
        if (!(error is ReflectionTypeLoadException) && !(error is FileNotFoundException) && !(error is DllNotFoundException) && type.IndexOf("ContentLoadException", StringComparison.Ordinal) < 0) return;
        string key = type + ":" + error.Message;
        lock (exceptionKeys) {
            if (exceptionKeys.Count >= 100 || !exceptionKeys.Add(key)) return;
        }
        try {
            Log("FIRST_CHANCE " + error);
            var load = error as ReflectionTypeLoadException;
            if (load != null) foreach (Exception loader in load.LoaderExceptions) if (loader != null) Log("LOADER_EXCEPTION " + loader);
        } catch { /* Diagnostics must never replace the exception being diagnosed. */ }
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
        if (scene != "2d/level/playfield/stage/stage_01/level_01_art") return;
        string name = Convert.ToString(Property(__instance, "Name"));
        object position = Property(__instance, "InitialPosition");
        Log("ENEMY_RESET name=" + name + " scene=" + scene + " initial=" + position);
        if (name != "FootSoldierRegular_202" || !new Guid("6f229aef-a56f-4457-b5a0-60d158b48fb1").Equals(Property(__instance, "Id"))) return;
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








