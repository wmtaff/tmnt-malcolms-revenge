// Separate pure/synthetic art test entry point. No game assemblies or launcher execution.
using System;
namespace Malcolm.Runtime {
    internal static class CharacterArtRuntimeTests {
        public static int Main() {
            try { CharacterArtRuntime.SelfTest(); return 0; }
            catch (Exception error) { Console.Error.WriteLine(error); return 1; }
            finally { CharacterArtRuntime.Dispose(); }
        }
    }
}
