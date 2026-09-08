using System;
namespace Malcolm.Runtime {
internal sealed class RuntimeOptions {
    public string GameDirectory;
    public string LogPath;
    public string EncounterPath;
    public bool Baseline;
    public static RuntimeOptions Parse(string[] args) {
        const string usage = "Usage: Malcolm.Runtime.exe <game-dir> <fresh-log-file> (--baseline | --encounter <json-file>) or --self-test";
        if (args == null || args.Length < 3 || args.Length > 4 || String.IsNullOrWhiteSpace(args[0]) || String.IsNullOrWhiteSpace(args[1])) throw new ArgumentException(usage);
        var result = new RuntimeOptions { GameDirectory=args[0], LogPath=args[1] };
        if (args.Length == 3 && args[2] == "--baseline") result.Baseline = true;
        else if (args.Length == 4 && args[2] == "--encounter" && !String.IsNullOrWhiteSpace(args[3])) result.EncounterPath = args[3];
        else throw new ArgumentException(usage);
        return result;
    }
}
}
