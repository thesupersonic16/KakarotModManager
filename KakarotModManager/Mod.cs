using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HedgeModManager;
using HedgeModManager.Serialization;

namespace KakarotModManager
{
    public class Mod
    {
        public string RootDirectory;

        public bool Enabled { get; set; }
        public bool CanUpdate => ItemId != 0;
        public string CanUpdateText => CanUpdate ? "GB" : "N/A";

        public List<string> Paks = new List<string>();
        [IniField("Main", "Sound")] public List<string> Sounds = new List<string>();
        public bool AudioMod;

        // Main
        [IniField("Main")] public int FormatVersion { get; set; }


        // GameBanana
        [IniField("GameBanana")] public string ItemType { get; set; }
        [IniField("GameBanana")] public int ItemId { get; set; }

        // Desc
        [IniField("Desc")] public string Title { get; set; }
        [IniField("Desc")] public string Description { get; set; }
        [IniField("Desc")] public string Version { get; set; }
        [IniField("Desc")] public string Date { get; set; }
        [IniField("Desc")] public string Author { get; set; }
        [IniField("Desc")] public string AuthorURL { get; set; }


        public Mod()
        {

        }

        public Mod(string rootDirectory)
        {
            RootDirectory = rootDirectory;
            try
            {
                using (var stream = File.OpenRead(Path.Combine(RootDirectory, "mod.ini")))
                {
                    if (!Read(stream))
                    {
                        // Close the file so we can write a valid file back
                        stream.Close();

                        Title = Path.GetFileName(RootDirectory);
                        Version = "0.0";
                        Date = "Unknown";
                        Author = "Unknown";
                        Description = "None";
                        Save();
                    }

                    var soundFolder = new DirectoryInfo(Path.Combine(RootDirectory, "Sound"));

                    Directory.GetFiles(RootDirectory, "*.pak.disabled", SearchOption.AllDirectories).ToList().ForEach(t => File.Move(t, t.Replace(".pak.disabled", ".disabled")));
                    Paks = new List<string>();
                    Paks.AddRange(Directory.GetFiles(RootDirectory, "*.pak", SearchOption.AllDirectories));
                    Paks.AddRange(Directory.GetFiles(RootDirectory, "*.disabled", SearchOption.AllDirectories).Select(t => t.Replace(".disabled", ".pak")).ToList());
                    AudioMod = Directory.Exists(Path.Combine(RootDirectory, "Sound"));
                }
            }
            catch
            {
                Title = Path.GetFileName(RootDirectory);
            }
        }

        public bool Read(Stream stream)
        {
            try
            {
                IniSerializer.Deserialize(this, stream);
            }
            catch
            {
                return false;
            }
            Description = Description.Replace("\\n", "\n");
            return true;
        }

        public void Save()
        {
            using (var stream = File.Create(Path.Combine(RootDirectory, "mod.ini")))
                IniSerializer.Serialize(this, stream);
        }
    }
}
