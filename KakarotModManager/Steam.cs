using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HedgeModManager;
using Microsoft.Win32;

namespace KakarotModManager
{
    public static class Steam
    {
        public static string SteamLocation;

        public static void Init()
        {
            // Gets Steam's Registry Key
            var key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Valve\\Steam");
            // If null then try get it from the 64-bit Registry
            if (key == null)
                key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                    .OpenSubKey("SOFTWARE\\Wow6432Node\\Valve\\Steam");
            // Checks if the Key and Value exists.
            if (key != null && key.GetValue("InstallPath") is string steamPath)
                SteamLocation = steamPath;
        }

        public static List<SteamGame> SearchForGames(string preference = null)
        {
            var paths = new List<string>();
            var games = new List<SteamGame>();

            if (string.IsNullOrEmpty(SteamLocation))
                return new List<SteamGame>();

            string vdfLocation = Path.Combine(SteamLocation, "steamapps\\libraryfolders.vdf");
            Dictionary<string, object> vdf = null;
            try
            {
                vdf = SteamVDF.Load(vdfLocation);
            }
            catch (Exception ex)
            {
                return new List<SteamGame>();
            }

            // Default Common Path
            paths.Add(Path.Combine(SteamLocation, "steamapps\\common"));

            // Adds all the custom libraries
            foreach (var library in SteamVDF.GetContainer(vdf, "LibraryFolders"))
                if (int.TryParse(library.Key, out int index))
                    paths.Add(Path.Combine(library.Value as string, "steamapps\\common"));

            foreach (string path in paths)
            {
                if (Directory.Exists(path))
                {
                    string atPath = Path.Combine(path, "DRAGON BALL Z KAKAROT\\AT.exe");
                    if (File.Exists(atPath))
                        games.Add(new SteamGame("Dragon Ball Z Kakarot", atPath, "851850"));
                }
            }

            if (preference != null)
                return games.OrderBy(x => x.GameName != preference).ToList();
            else return games;
        }

    }
}
