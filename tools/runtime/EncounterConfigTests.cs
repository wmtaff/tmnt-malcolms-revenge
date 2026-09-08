using System;
using System.IO;
namespace Malcolm.Runtime {
internal static class EncounterConfigTests {
    private static string Wave(int n) {
        return "{\"id\":\"wave"+n+"\",\"blockId\":\"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa\",\"blockName\":\"SyntheticBlock\",\"waveIndex\":"+n+",\"groupId\":\"SyntheticGroup"+n+"\",\"expectedDelay\":0,\"delay\":1.5,\"enemies\":[{\"id\":\"00000000-0000-0000-0000-00000000000"+(n+1)+"\",\"name\":\"SyntheticEnemy"+n+"\",\"original\":{\"x\":10,\"y\":20,\"z\":0},\"position\":{\"x\":30,\"y\":20,\"z\":0}}]}";
    }
    internal static string Fixture() {
        return "{\"schemaVersion\":1,\"id\":\"synthetic-test\",\"playfield\":\"2d/level/playfield/synthetic\",\"waves\":["+Wave(0)+","+Wave(1)+","+Wave(2)+"]}";
    }
    private static void Reject(string json) {
        try { EncounterConfig.Parse(json); } catch (InvalidDataException) { return; }
        throw new Exception("Invalid encounter config accepted: " + json.Substring(0, Math.Min(100, json.Length)));
    }
    internal static void Run() {
        string json=Fixture();
        EncounterConfig config=EncounterConfig.Parse(json);
        if(config.Waves.Count!=3 || config.Waves[0].Delay!=1.5f || config.Waves[0].Enemies[0].Position.X!=30f) throw new Exception("Valid fixture lost values");
        Reject(json.Replace("\"schemaVersion\":1", "\"schemaVersion\":\"1\""));
        Reject(json.Replace("\"schemaVersion\":1", "\"schemaVersion\":2"));
        Reject(json.Replace("\"schemaVersion\":1", "\"schemaVersion\":1,\"unknown\":0"));
        Reject(json.Replace("\"schemaVersion\":1", "\"schemaVersion\":1,\"schemaVersion\":1"));
        Reject(json.Replace("\"schemaVersion\":1", "\"schemaVersion\":1,\"schema\\u0056ersion\":1"));
        Reject(json.Replace("\"x\":10", "\"x\":\"10\""));
        Reject(json.Replace("\"x\":10", "\"x\":true"));
        Reject(json.Replace("\"x\":10", "\"x\":1e999"));
        Reject(json.Replace("\"x\":10", "\"x\":100001"));
        Reject(json.Replace("\"x\":10", "\"x\":NaN"));
        Reject(json.Replace("\"delay\":1.5", "\"delay\":-1"));
        Reject(json.Replace("\"delay\":1.5", "\"delay\":61"));
        Reject(json.Replace("\"waveIndex\":1", "\"waveIndex\":1.5"));
        Reject(json.Replace("wave1", "wave0"));
        Reject(json.Replace("000000000002", "000000000001"));
        Reject(json.Replace("\"waveIndex\":1", "\"waveIndex\":0"));
        Reject(json.Replace("\"waveIndex\":2", "\"waveIndex\":3"));
        Reject(json.Replace("\"id\":\"wave1\",\"blockId\":\"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa\"", "\"id\":\"wave1\",\"blockId\":\"bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb\""));
        Reject(json.Replace("\"waveIndex\":1", "\"waveIndex\":0").Replace("\"waveIndex\":2", "\"waveIndex\":1"));
        Reject(json.Replace("\"groupId\":\"SyntheticGroup0\"", "\"groupId\":null"));
        Reject(json.Replace("\"z\":0", "\"z\":0,\"extra\":1"));
        Reject(json.Replace("\"z\":0", "\"z\":0,"));
        Reject(json.Replace("\"x\":10", "\"x\":01"));
        Reject(json.Replace('"', '\''));
        Reject(json+" false");
        Reject(new string(' ',65537));
        Reject(json.Replace(","+Wave(2), ""));
        string path=Path.Combine(Path.GetTempPath(), "malcolm-config-"+Guid.NewGuid().ToString("N")+".json");
        try {
            File.WriteAllText(path,json,new System.Text.UTF8Encoding(false));
            if(EncounterConfig.Load(path).Id!="synthetic-test") throw new Exception("File load failed");
            File.WriteAllBytes(path,new byte[]{0xff,0xfe,0xfd});
            bool invalid=false;try{EncounterConfig.Load(path);}catch(InvalidDataException){invalid=true;}
            if(!invalid)throw new Exception("Malformed UTF8 accepted");
        } finally {File.Delete(path);}
        Console.WriteLine("ENCOUNTER_CONFIG_TEST_PASS");
    }
}
}

