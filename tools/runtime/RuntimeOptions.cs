using System;
namespace Malcolm.Runtime {
internal sealed class RuntimeOptions {
    public string GameDirectory;
    public string LogPath;
    public string EncounterPath;
    public string ResidentialDirectory;
    public string CharacterManifestPath;
    public bool Baseline;
    public static RuntimeOptions Parse(string[] args) {
        const string usage = "Usage: Malcolm.Runtime.exe <game-dir> <fresh-log-file> (--baseline | --encounter <json-file> | --residential <art-directory>) [--character <manifest.json>] or --self-test";
        if (args == null || args.Length < 3 || args.Length > 6 || String.IsNullOrWhiteSpace(args[0]) || String.IsNullOrWhiteSpace(args[1])) throw new ArgumentException(usage);
        var result = new RuntimeOptions { GameDirectory=args[0], LogPath=args[1] };
        bool primary = false;
        for (int i = 2; i < args.Length; i++) {
            string option = args[i];
            if (option == "--baseline") {
                if (primary) throw new ArgumentException(usage);
                result.Baseline = true; primary = true; continue;
            }
            if (option != "--encounter" && option != "--residential" && option != "--character") throw new ArgumentException(usage);
            if (++i >= args.Length || String.IsNullOrWhiteSpace(args[i]) || args[i].StartsWith("--", StringComparison.Ordinal)) throw new ArgumentException(usage);
            if (option == "--character") {
                if (result.CharacterManifestPath != null) throw new ArgumentException(usage);
                result.CharacterManifestPath = args[i];
            } else {
                if (primary) throw new ArgumentException(usage);
                primary = true;
                if (option == "--encounter") result.EncounterPath = args[i]; else result.ResidentialDirectory = args[i];
            }
        }
        if (!primary) throw new ArgumentException(usage);
        return result;
    }
}
}
