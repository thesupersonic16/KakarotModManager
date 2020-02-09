using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using GameBananaAPI;
using HedgeModManager;
using HedgeModManager.Serialization;
using Microsoft.Win32;

namespace KakarotModManager
{
    public class ModsDB
    {

        public ObservableCollection<Mod> Mods { get; set; } = new ObservableCollection<Mod>();
        public string ModsDBPath;

        [IniField("Main", "ActiveMod")] public List<string> ActiveMods = new List<string>();
        [IniField("Main")] public string ActiveAudioMod;

        public ModsDB()
        {
            ModsDBPath = Path.Combine(GetModsDirectory(), "modsDB.ini");
            if (File.Exists(ModsDBPath))
            {
                using (var stream = File.OpenRead(ModsDBPath))
                {
                    Read(stream);
                }
            }
            Scan();
        }

        public void UpdateModStatus()
        {
            ActiveMods.Clear();
            // Disable Mods
            foreach (var mod in Mods)
            {
                if (!mod.Enabled)
                {
                    foreach (string path in mod.Paks)
                    {
                        if (File.Exists(path))
                            File.Move(path, path + ".disabled");
                    }

                    if (mod.AudioMod && mod.Title == ActiveAudioMod)
                    {
                        RestoreAudioMod(mod);
                        ActiveAudioMod = "";
                    }
                }
            }
            foreach (var mod in Mods)
            {
                if (mod.AudioMod && mod.Enabled)
                {
                    if (!string.IsNullOrEmpty(ActiveAudioMod) && mod.Title != ActiveAudioMod)
                    {
                        mod.Enabled = false;
                        HedgeModManager.App.CreateOKMessageBox("Audio Mod Warning",
                            "Currently only one audio mod can be enabled at a time due to how mods are written.\n\n" +
                            $"The mod \"{mod.Title}\" has been disabled to prevent conflicts with \"{ActiveAudioMod}\".").ShowDialog();
                        continue;
                    }
                    ActiveAudioMod = mod.Title;
                }

                if (mod.Enabled)
                {
                    foreach (string path in mod.Paks)
                    {
                        if (File.Exists(path + ".disabled"))
                            File.Move(path + ".disabled", path);
                    }

                    if (mod.AudioMod)
                    {
                        foreach (string path in mod.Sounds)
                        {
                            string modsoundpath = Path.Combine(mod.RootDirectory, "Sound", path);
                            string gamesoundpath = Path.Combine(App.StartDirectory, "AT\\Content\\Sound", path);
                            if (File.Exists(modsoundpath))
                            {
                                if (File.Exists(gamesoundpath))
                                    File.Move(gamesoundpath, gamesoundpath + ".backup");
                                File.Move(modsoundpath, gamesoundpath);
                            }

                        }
                    }

                    ActiveMods.Add(mod.Title);
                }
            }
        }

        protected void RestoreAudioMod(Mod mod)
        {
            foreach (string path in mod.Sounds)
            {
                string modsoundpath = Path.Combine(mod.RootDirectory, "Sound", path);
                string gamesoundpath = Path.Combine(App.StartDirectory, "AT\\Content\\Sound", path);

                if (File.Exists(gamesoundpath + ".backup"))
                {
                    File.Move(gamesoundpath, modsoundpath);
                    File.Move(gamesoundpath + ".backup", gamesoundpath);
                }
            }

        }

        /// <summary>
        /// Generates dummy information for mods without a mod.ini
        /// </summary>
        /// <param name="path">Path to .pak file</param>
        public static void InstallMod(string path, GBAPIItemDataBasic gbmod = null, bool move = false)
        {
            var box = new HedgeMessageBox("Installing non-KMM mod",
                $"The mod {Path.GetFileName(path)} does not contain\n" +
                $"any information about itself, KMM can generate this information\n" +
                $"for the time being but if you are the developer, \n" +
                $"manually creating a mod.ini file is recommended\n\n" +
                $"Do you want KMM to try and install this mod?\n\n");

            box.AddButton("No", () => box.Close());
            box.AddButton("Yes", () =>
            {
                string modPath = Path.Combine(GetModsDirectory(), Path.GetFileNameWithoutExtension(path));
                Directory.CreateDirectory(modPath);
                if (move)
                    File.Move(path, Path.Combine(modPath, Path.GetFileName(path)));
                else
                    File.Copy(path, Path.Combine(modPath, Path.GetFileName(path)));
                var mod = new Mod();
                mod.RootDirectory = modPath;
                // Main
                mod.FormatVersion = 1;

                // Desc
                mod.Title = gbmod?.ModName ?? Path.GetFileNameWithoutExtension(path);
                mod.Description = gbmod?.Subtitle ?? "Auto generated description.";
                mod.Version = "1";
                mod.Date = DateTime.UtcNow.ToString("yyyy-MM-dd");
                mod.Author = gbmod?.OwnerName ?? "Unknown";
                if (gbmod != null)
                {
                    mod.AuthorURL = "https://gamebanana.com/members/" + gbmod.OwnerID;
                    mod.ItemType = gbmod.ItemType;
                    mod.ItemId = gbmod.ItemID;
                }
                mod.Save();
                box.Close();
            });
            box.ShowDialog();
        }

        /// <summary>
        /// Installs KMM mod
        /// </summary>
        /// <param name="path">Path to archive or directory</param>
        public static bool InstallKMMMod(string path, GBAPIItemDataBasic mod = null)
        {
            if (File.Exists(path))
                return InstallModArchive(path, mod);
            if (Directory.Exists(path))
                return InstallModDirectory(path, mod);
            return false;
        }

        public static bool InstallModArchive(string path, GBAPIItemDataBasic mod = null)
        {
            if (Path.GetExtension(path) == ".zip")
                return InstallModArchiveUsingZipFile(path, mod);
            if (!InstallModArchiveUsing7Zip(path, mod))
                if (!InstallModArchiveUsingWinRAR(path, mod))
                {
                    var box = new HedgeMessageBox("ERROR", "Failed to install mods using 7-Zip and WinRAR!\n" +
                        "Make sure you have either one installed on your system.");
                    box.AddButton("  Close  ", () => box.Close());
                    box.ShowDialog();
                }
            return false;
        }

        public static bool InstallModArchiveUsingZipFile(string path, GBAPIItemDataBasic mod = null)
        {
            // Path to the install temp folder
            string tempFolder = Path.Combine(App.StartDirectory, "temp_install");

            // Deletes the temp Directory if it exists
            if (Directory.Exists(tempFolder))
                Directory.Delete(tempFolder, true);

            // Extracts all contents inside of the zip file
            ZipFile.ExtractToDirectory(path, tempFolder);

            // Install mods from the temp folder
            bool result = InstallModDirectory(tempFolder, mod);

            // Deletes the temp folder with all of its contents
            Directory.Delete(tempFolder, true);
            return result;
        }

        public static bool InstallModArchiveUsing7Zip(string path, GBAPIItemDataBasic mod = null)
        {
            // Gets 7-Zip's Registry Key
            var key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\7-Zip");
            // If null then try get it from the 64-bit Registry
            if (key == null)
                key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                    .OpenSubKey("SOFTWARE\\7-Zip");
            // Checks if 7-Zip is installed by checking if the key and path value exists
            if (key != null && key.GetValue("Path") is string exePath)
            {
                // Path to 7z.exe
                string exe = Path.Combine(exePath, "7z.exe");

                // Path to the install temp directory
                string tempDirectory = Path.Combine(App.StartDirectory, "temp_install");

                // Deletes the temp directory if it exists
                if (Directory.Exists(tempDirectory))
                    Directory.Delete(tempDirectory, true);

                // Creates the temp directory
                Directory.CreateDirectory(tempDirectory);

                // Extracts the archive to the temp directory
                var psi = new ProcessStartInfo(exe, $"x \"{path}\" -o\"{tempDirectory}\" -y");
                Process.Start(psi).WaitForExit(1000 * 60 * 5);

                // Search and install mods from the temp directory
                InstallModDirectory(tempDirectory, mod);

                // Deletes the temp directory with all of its contents
                Directory.Delete(tempDirectory, true);
                key.Close();
                return true;
            }
            // 7-Zip is not installed
            return false;
        }

        // TODO: Add WinRAR x86 support
        // TODO: Needs Testing
        public static bool InstallModArchiveUsingWinRAR(string path, GBAPIItemDataBasic mod = null)
        {
            // Gets WinRAR's Registry Key
            var key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\WinRAR");
            // If null then try get it from the 64-bit Registry
            if (key == null)
                key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                    .OpenSubKey("SOFTWARE\\WinRAR");
            // Checks if WinRAR is installed by checking if the key and path value exists
            if (key != null && key.GetValue("exe64") is string exePath)
            {
                // Path to the install temp directory
                string tempDirectory = Path.Combine(App.StartDirectory, "temp_install");

                // Deletes the temp directory if it exists
                if (Directory.Exists(tempDirectory))
                    Directory.Delete(tempDirectory, true);

                // Creates the temp directory
                Directory.CreateDirectory(tempDirectory);

                // Extracts the archive to the temp directory
                var psi = new ProcessStartInfo(exePath, $"x \"{path}\" \"{tempDirectory}\"");
                Process.Start(psi).WaitForExit(1000 * 60 * 5);

                // Search and install mods from the temp directory
                InstallModDirectory(tempDirectory, mod);

                // Deletes the temp directory with all of its contents
                Directory.Delete(tempDirectory, true);
                key.Close();
                return true;
            }
            // WinRAR is not installed
            return false;
        }

        public static bool InstallModDirectory(string path, GBAPIItemDataBasic gbmod = null)
        {
            // A list of folders that have mod.ini in them
            var directories = new List<string>();

            // Looks though all the folders for mods
            directories.AddRange(Directory.GetDirectories(path, "*", SearchOption.AllDirectories)
                .Where(t => File.Exists(Path.Combine(t, "mod.ini"))));

            // Checks if there is a file called "mod.ini" inside the selected folder
            if (File.Exists(Path.Combine(path, "mod.ini")))
                directories.Add(path);

            // Check if there is any mods
            if (directories.Count > 0)
            {
                foreach (string folder in directories)
                {
                    string directoryName = Path.GetFileName(folder);

                    // If it doesn't know the name of the mod its installing
                    if (directoryName == "temp_install")
                    {
                        var mod = new ModInfo(Path.Combine(folder, "mod.ini"));
                        directoryName = new string(mod.Title.Where(x => !Path.GetInvalidFileNameChars()
                            .Contains(x)).ToArray());
                    }

                    // Creates all of the directories.
                    Directory.CreateDirectory(Path.Combine(GetModsDirectory(), Path.GetFileName(folder)));
                    foreach (string dirPath in Directory.GetDirectories(folder, "*", SearchOption.AllDirectories))
                        Directory.CreateDirectory(dirPath.Replace(folder, Path.Combine(GetModsDirectory(), directoryName)));

                    // Copies all the files from the Directories.
                    foreach (string filePath in Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories))
                        File.Copy(filePath, filePath.Replace(folder, Path.Combine(GetModsDirectory(), directoryName)), true);
                }
                return true;
            }

            var paks = Directory.GetFiles(path, "*.pak", SearchOption.AllDirectories);
            if (paks.Length != 0)
            {
                paks.ToList().ForEach(t => InstallMod(t, gbmod));
                return true;
            }
            return false;
        }

        public void DeleteMod(Mod mod)
        {
            Mods.Remove(mod);
            Directory.Delete(mod.RootDirectory, true);
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
            return true;
        }

        public void Save()
        {
            UpdateModStatus();
            using (var stream = File.Create(ModsDBPath))
                IniSerializer.Serialize(this, stream);
        }

        public void Scan()
        {
            if (!Directory.Exists(GetModsDirectory()))
            {
                Directory.CreateDirectory(GetModsDirectory());
            }
            // Check if the mods folder is clean
            if (Directory.GetFiles(GetModsDirectory(), "*.pak", SearchOption.TopDirectoryOnly).Any())
            {
                var box = new HedgeMessageBox("Active non-KMM mods detected",
                    "One or more mods has been detected in your mods folder,\n" +
                    "KakarotModManager can not handle mods installed in this way\n" +
                    "but can be converted to work. Would you like to perform the conversion?\n\n" +
                    "Note: Selecting No will disable all mod(s)!");
                box.AddButton("No", () =>
                {
                    foreach (string path in Directory.GetFiles(GetModsDirectory(), "*.pak", SearchOption.TopDirectoryOnly))
                    {
                        File.Move(path, path + ".disabled");
                    }
                    box.Close();
                });
                box.AddButton("Yes", () =>
                {
                    foreach (string path in Directory.GetFiles(GetModsDirectory(), "*.pak", SearchOption.TopDirectoryOnly))
                    {
                        InstallMod(path, null, true);
                    }
                    foreach (string path in Directory.GetFiles(GetModsDirectory(), "*.pak", SearchOption.TopDirectoryOnly))
                    {
                        File.Move(path, path + ".disabled");
                    }
                    box.Close();
                });
                box.ShowDialog();
            }

            Mods.Clear();
            foreach (string path in Directory.GetDirectories(GetModsDirectory())
                .Where(t => File.Exists(Path.Combine(t, "mod.ini"))))
            {
                var mod = new Mod(path);
                mod.Enabled = ActiveMods.Any(t => t == mod.Title);
                Mods.Add(mod);
            }
        }

        public static string GetModsDirectory() => Path.Combine(App.StartDirectory, "AT\\Content\\Paks\\~Mods\\");
    }
}
