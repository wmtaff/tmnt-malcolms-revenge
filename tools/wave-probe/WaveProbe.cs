// Original diagnostic code. No game code or assets are distributed here.
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

internal static class WaveProbe
{
    private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic |
        BindingFlags.Static | BindingFlags.Instance;
    private const int Limit = 16 * 1024 * 1024;

    internal static IEnumerable<int> FindRecords(byte[] data, string reader)
    {
        byte[] token = Encoding.ASCII.GetBytes(reader);
        if (token.Length >= 128) throw new ArgumentException("Reader token too long.");
        for (int i = 1; i <= data.Length - token.Length; i++)
        {
            if (data[i - 1] != token.Length) continue;
            int j = 0;
            while (j < token.Length && data[i + j] == token[j]) j++;
            if (j == token.Length) yield return i + token.Length;
        }
    }

    private static byte[] Inflate(string path)
    {
        using (var file = File.OpenRead(path))
        using (var zip = new DeflateStream(file, CompressionMode.Decompress))
        using (var output = new MemoryStream())
        {
            var buffer = new byte[8192];
            int count;
            while ((count = zip.Read(buffer, 0, buffer.Length)) != 0)
            {
                if (output.Length + count > Limit) throw new InvalidDataException("Scene exceeds 16 MiB limit.");
                output.Write(buffer, 0, count);
            }
            return output.ToArray();
        }
    }

    private static void SelfTest()
    {
        int count = 0;
        // Only a complete, length-prefixed token matches; include an exact EOF match.
        foreach (int offset in FindRecords(new byte[] { 2, 65, 66, 0, 65, 66, 2, 65, 66 }, "AB"))
        {
            if (offset != (count == 0 ? 3 : 9)) throw new Exception("Token boundary failure.");
            count++;
        }
        if (count != 2) throw new Exception("Token count failure.");
        Console.WriteLine("Synthetic token boundary tests passed.");
    }

    public static int Main(string[] args)
    {
        try
        {
            if (args.Length == 1 && args[0] == "--self-test") { SelfTest(); return 0; }
            if (args.Length != 2) throw new ArgumentException("Usage: WaveProbe.exe GAME_ROOT SCENE_ZPBN (or --self-test)");
            string root = Path.GetFullPath(args[0]);
            AppDomain.CurrentDomain.AssemblyResolve += delegate(object sender, ResolveEventArgs e)
            {
                string path = Path.Combine(root, new AssemblyName(e.Name).Name + ".dll");
                return File.Exists(path) ? Assembly.LoadFrom(path) : null;
            };
            Assembly engine = Assembly.LoadFrom(Path.Combine(root, "ParisEngine.dll"));
            Assembly game = Assembly.LoadFrom(Path.Combine(root, "TMNT.exe"));
            Assembly serializers = Assembly.LoadFrom(Path.Combine(root, "ParisSerializers.dll"));
            Type managerType = engine.GetType("Paris.Engine.BinaryContentManager", true);
            // Its normal constructor requires a running game context. Initialize only
            // the registry used by the native readers; no game startup is invoked.
            object manager = FormatterServices.GetUninitializedObject(managerType);
            FieldInfo registry = managerType.GetField("_readWriters", Flags);
            registry.SetValue(manager, Activator.CreateInstance(registry.FieldType));
            managerType.GetField("_singleton", Flags).SetValue(null, manager);
            managerType.GetField("_parisSerializerAssembly", Flags).SetValue(manager, serializers);
            MethodInfo add = managerType.GetMethod("AddAssemblyReadWriters", Flags);
            add.Invoke(manager, new object[] { engine });
            add.Invoke(manager, new object[] { serializers });

            // CameraBlockTrigger property setters consult EditorMode. This inert context
            // supplies false without constructing the game or any graphics device.
            Type context = engine.GetType("Paris.Engine.Context.ContextManager", true);
            context.GetField("_singleton", Flags).SetValue(null,FormatterServices.GetUninitializedObject(context));
            byte[] data = Inflate(args[1]);
            int found = 0;
            foreach (int offset in FindRecords(data, "ParisSerializer.CameraBlockTriggerReadWriter"))
            {
                using (var stream = new MemoryStream(data))
                using (var reader = new BinaryReader(stream))
                {
                    stream.Position = offset;
                    Type baseType = game.GetType("Paris.Game.Triggers.CameraBlockTrigger", true);
                    object obj = Activator.CreateInstance(baseType);
                    object readWriter = Activator.CreateInstance(serializers.GetType("ParisSerializer.CameraBlockTriggerReadWriter", true));
                    object[] values = { reader, obj };
                    readWriter.GetType().GetMethod("Read").Invoke(readWriter, values);
                    obj = values[1];
                    Console.WriteLine("Name={0}; Id={1}; Position={2}; InitialPosition={3}; StartActive={4}",
                        baseType.GetProperty("Name").GetValue(obj, null),
                        baseType.GetProperty("Id").GetValue(obj, null),
                        baseType.GetProperty("Position").GetValue(obj, null),
                        baseType.GetProperty("InitialPosition").GetValue(obj, null),
                        baseType.GetProperty("StartActive").GetValue(obj, null));
                    foreach (string prop in new string[] {"Waves", "WavesFor1Player", "IntroWave", "DelayStart", "TriggerPoint"}) {
                        object value=baseType.GetProperty(prop).GetValue(obj,null);
                        Console.WriteLine(prop+"="+value);
                        var list=value as System.Collections.IEnumerable;
                        if (list != null) {
                            int index = 0;
                            foreach (object wave in list) {
                                Console.WriteLine(" WaveIndex=" + index++);
                                foreach (PropertyInfo field in wave.GetType().GetProperties()) {
                                    object fieldValue = field.GetValue(wave, null);
                                    if (field.Name == "Group" && fieldValue != null)
                                        fieldValue = fieldValue.GetType().GetProperty("ID").GetValue(fieldValue, null);
                                    Console.WriteLine("  " + field.Name + "=" + fieldValue);
                                }
                            }
                        }
                    }
                    found++;
                }
            }
            Console.WriteLine("Parsed camera-block records: " + found);
            return found == 0 ? 1 : 0;
        }
        catch (Exception e) { Console.Error.WriteLine(e); return 1; }
    }
}







