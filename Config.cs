using System.IO;
using System.Text.Json;

namespace LegacyQuestBSModdingTool
{
    public class Config
    {
        public bool manualADB { get; set; } = false;

        public static Config LoadConfig()
        {
            if (!File.Exists(PublicVars.exe + "config.json")) return new Config();
            return JsonSerializer.Deserialize<Config>(File.ReadAllText(PublicVars.exe + "config.json"));
        }

        public void Save()
        {
            File.WriteAllText(PublicVars.exe + "config.json", JsonSerializer.Serialize(this));
        }
    }
}