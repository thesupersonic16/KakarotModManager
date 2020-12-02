using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using HedgeModManager.Serialization;
using Newtonsoft.Json;

namespace KakarotModManager
{
    public class ModInfo : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;

        public string RootDirectory;

        public bool Enabled { get; set; }

        public bool HasUpdates => !string.IsNullOrEmpty(UpdateServer);

        public bool Favorite { get; set; }

        // Main
        [IniField("Main")] public string UpdateServer { get; set; }

        // Desc
        [IniField("Desc")] public string Title { get; set; } = string.Empty;

        [IniField("Desc")] public string Description { get; set; } = string.Empty;

        [IniField("Desc")] public string Version { get; set; } = string.Empty;

        [IniField("Desc")] public string Date { get; set; } = string.Empty;

        [IniField("Desc")] public string Author { get; set; } = string.Empty;

        [IniField("Desc")] public string AuthorURL { get; set; } = string.Empty;

        public ModInfo()
        {

        }

        public ModInfo(string modPath)
        {
            RootDirectory = modPath;
            try
            {
                using (var stream = File.OpenRead(Path.Combine(modPath, "mod.ini")))
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
                }

                if (string.IsNullOrEmpty(Title))
                {
                    Title = string.IsNullOrEmpty(Title) ? Path.GetFileName(RootDirectory) : Title;
                    Version = string.IsNullOrEmpty(Version) ? "0.0" : Version;
                    Date = string.IsNullOrEmpty(Date) ? "Unknown" : Date;
                    Author = string.IsNullOrEmpty(Author) ? "Unknown" : Author;
                    Description = string.IsNullOrEmpty(Description) ? "None" : Description;
                    Save();
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
            {
                IniSerializer.Serialize(this, stream);
            }
        }
    }
}