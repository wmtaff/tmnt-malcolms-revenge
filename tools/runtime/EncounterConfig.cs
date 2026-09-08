using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
namespace Malcolm.Runtime {
internal sealed class ConfigVector3 {
    public float X { get; internal set; } public float Y { get; internal set; } public float Z { get; internal set; }
}
internal sealed class EncounterEnemy {
    public Guid Id { get; internal set; } public string Name { get; internal set; }
    public ConfigVector3 Original { get; internal set; } public ConfigVector3 Position { get; internal set; }
}
internal sealed class EncounterWave {
    public string Id { get; internal set; } public Guid BlockId { get; internal set; }
    public string BlockName { get; internal set; } public int WaveIndex { get; internal set; }
    public string GroupId { get; internal set; } public float ExpectedDelay { get; internal set; }
    public float Delay { get; internal set; } public ReadOnlyCollection<EncounterEnemy> Enemies { get; internal set; }
}
internal sealed class EncounterConfig {
    public string Id { get; private set; } public string Playfield { get; private set; }
    public ReadOnlyCollection<EncounterWave> Waves { get; private set; }
    private const int Limit=65536;
    internal static EncounterConfig Load(string path) {
        try {
            using(var input=File.OpenRead(path)) using(var output=new MemoryStream()) {
                byte[] buffer=new byte[4096];int n;
                while((n=input.Read(buffer,0,buffer.Length))!=0) {
                    if(output.Length+n>Limit)throw new InvalidDataException("Config exceeds 64 KiB.");
                    output.Write(buffer,0,n);
                }
                byte[] bytes=output.ToArray();int start=bytes.Length>=3&&bytes[0]==239&&bytes[1]==187&&bytes[2]==191?3:0;
                return Parse(new UTF8Encoding(false,true).GetString(bytes,start,bytes.Length-start));
            }
        } catch(DecoderFallbackException e){throw new InvalidDataException("Config must contain valid UTF8.",e);}
    }
    internal static EncounterConfig Parse(string json) {
        try {
            if(json==null||json.Length>Limit||new UTF8Encoding(false,true).GetByteCount(json)>Limit)throw new InvalidDataException("Config exceeds 64 KiB or is null.");
            var serializer=new JavaScriptSerializer{MaxJsonLength=Limit,RecursionLimit=16};
            new JsonGrammar(json,serializer).Validate();
            var root=Object(serializer.DeserializeObject(json),"schemaVersion","id","playfield","waves");
            if(Integer(root["schemaVersion"],1,1)!=1)throw Bad("Unsupported schemaVersion");
            var config=new EncounterConfig{Id=Label(root["id"]),Playfield=Text(root["playfield"],256).Replace('\\','/').ToLowerInvariant()};
            if(config.Playfield.StartsWith("/")||config.Playfield.IndexOf(':')>=0)throw Bad("Playfield must be a relative content identifier");
            foreach(string part in config.Playfield.Split('/'))if(part==""||part=="."||part=="..")throw Bad("Invalid playfield segment");
            object[] waves=Array(root["waves"],3,3);var result=new List<EncounterWave>();
            var waveIds=new HashSet<string>(StringComparer.Ordinal);var selectors=new HashSet<string>(StringComparer.Ordinal);
            var enemyIds=new HashSet<Guid>();
            foreach(object value in waves) {
                var item=Object(value,"id","blockId","blockName","waveIndex","groupId","expectedDelay","delay","enemies");
                var wave=new EncounterWave{Id=Label(item["id"]),BlockId=Identity(item["blockId"]),BlockName=Text(item["blockName"],128),WaveIndex=Integer(item["waveIndex"],0,63),GroupId=Text(item["groupId"],128),ExpectedDelay=Number(item["expectedDelay"],0,60),Delay=Number(item["delay"],0,60)};
                if(!waveIds.Add(wave.Id)||!selectors.Add(wave.BlockId.ToString("D")+":"+wave.WaveIndex))throw Bad("Duplicate wave id or block/index selector");
                if(wave.WaveIndex!=result.Count)throw Bad("Prototype waves must be ordered indexes 0, 1, 2");
                if(result.Count>0&&(wave.BlockId!=result[0].BlockId||wave.BlockName!=result[0].BlockName))throw Bad("Prototype waves must use the same camera block");
                var enemies=new List<EncounterEnemy>();
                foreach(object enemyValue in Array(item["enemies"],1,32)) {
                    var enemy=Object(enemyValue,"id","name","original","position");
                    var entry=new EncounterEnemy{Id=Identity(enemy["id"]),Name=Text(enemy["name"],128),Original=Vector(enemy["original"]),Position=Vector(enemy["position"])};
                    if(!enemyIds.Add(entry.Id))throw Bad("Duplicate enemy id");
                    enemies.Add(entry);
                }
                wave.Enemies=enemies.AsReadOnly();result.Add(wave);
            }
            config.Waves=result.AsReadOnly();return config;
        } catch(InvalidDataException){throw;} catch(ArgumentException e){throw new InvalidDataException("Invalid encounter JSON.",e);} catch(InvalidOperationException e){throw new InvalidDataException("Invalid encounter JSON.",e);} catch(OverflowException e){throw new InvalidDataException("Numeric overflow.",e);}
    }
    private static InvalidDataException Bad(string reason){return new InvalidDataException(reason);}
    private static Dictionary<string,object> Object(object value,params string[] keys) {
        var map=value as Dictionary<string,object>;
        if(map==null||map.Count!=keys.Length)throw Bad("Unexpected object fields");
        foreach(string key in keys)if(!map.ContainsKey(key))throw Bad("Missing field "+key);
        return map;
    }
    private static object[] Array(object value,int minimum,int maximum) {
        var array=value as object[];if(array==null||array.Length<minimum||array.Length>maximum)throw Bad("Invalid array size");return array;
    }
    private static string Text(object value,int max) {
        var text=value as string;if(String.IsNullOrWhiteSpace(text)||text.Length>max||text.Trim()!=text)throw Bad("Invalid string");
        foreach(char c in text)if(Char.IsControl(c))throw Bad("Control character in string");return text;
    }
    private static string Label(object value){string text=Text(value,64);if(!Regex.IsMatch(text,@"\A[a-zA-Z0-9][a-zA-Z0-9_-]*\z"))throw Bad("Invalid config label");return text;}
    private static Guid Identity(object value){Guid id;if(!Guid.TryParseExact(Text(value,36),"D",out id)||id==Guid.Empty)throw Bad("Invalid selector GUID");return id;}
    private static int Integer(object value,int min,int max){if(!(value is int))throw Bad("Integer required");int n=(int)value;if(n<min||n>max)throw Bad("Integer outside bounds");return n;}
    private static float Number(object value,float min,float max) {
        if(!(value is int)&&!(value is long)&&!(value is decimal)&&!(value is double))throw Bad("Number required");
        double n=Convert.ToDouble(value,System.Globalization.CultureInfo.InvariantCulture);
        if(Double.IsNaN(n)||Double.IsInfinity(n)||n<min||n>max)throw Bad("Number outside bounds");return (float)n;
    }
    private static ConfigVector3 Vector(object value){var v=Object(value,"x","y","z");return new ConfigVector3{X=Number(v["x"],-100000,100000),Y=Number(v["y"],-100000,100000),Z=Number(v["z"],-100000,100000)};}

    // JavaScriptSerializer accepts extensions and duplicate keys. Check the strict
    // JSON grammar and decoded key uniqueness before letting it construct objects.
    private sealed class JsonGrammar {
        private readonly string text;private readonly JavaScriptSerializer serializer;private int at;
        internal JsonGrammar(string value,JavaScriptSerializer reader){text=value;serializer=reader;}
        private void White(){while(at<text.Length&&(text[at]==' '||text[at]=='\t'||text[at]=='\r'||text[at]=='\n'))at++;}
        private bool Take(char c){White();if(at<text.Length&&text[at]==c){at++;return true;}return false;}
        internal void Validate(){Value(0);White();if(at!=text.Length)throw Bad("Trailing JSON data");}
        private string String() {
            White();int start=at;if(at>=text.Length||text[at++]!='"')throw Bad("JSON string required");
            while(at<text.Length) {
                char c=text[at++];if(c=='"')return serializer.Deserialize<string>(text.Substring(start,at-start));
                if(c<32)throw Bad("Unescaped string control");
                if(c!='\\')continue;
                if(at>=text.Length)throw Bad("Truncated escape");c=text[at++];
                if(c=='u'){for(int i=0;i<4;i++){if(at>=text.Length||!Uri.IsHexDigit(text[at++]))throw Bad("Invalid unicode escape");}}
                else if("\"\\/bfnrt".IndexOf(c)<0)throw Bad("Invalid string escape");
            }
            throw Bad("Unterminated string");
        }
        private void Value(int depth) {
            if(depth>12)throw Bad("JSON nesting exceeds limit");White();if(at>=text.Length)throw Bad("Missing JSON value");
            if(text[at]=='"'){String();return;}
            if(Take('{')) {
                var names=new HashSet<string>(StringComparer.Ordinal);if(Take('}'))return;
                do {if(!names.Add(String()))throw Bad("Duplicate JSON property");if(!Take(':'))throw Bad("Missing colon");Value(depth+1);if(Take('}'))return;}while(Take(','));
                throw Bad("Invalid JSON object");
            }
            if(Take('[')){if(Take(']'))return;do{Value(depth+1);if(Take(']'))return;}while(Take(','));throw Bad("Invalid JSON array");}
            foreach(string word in new[]{"true","false","null"})if(text.Length-at>=word.Length&&System.String.CompareOrdinal(text,at,word,0,word.Length)==0){at+=word.Length;return;}
            int start=at;while(at<text.Length&&"-+0123456789.eE".IndexOf(text[at])>=0)at++;
            if(!Regex.IsMatch(text.Substring(start,at-start),@"\A-?(0|[1-9][0-9]*)(\.[0-9]+)?([eE][+-]?[0-9]+)?\z"))throw Bad("Invalid JSON number");
        }
    }
}
}

