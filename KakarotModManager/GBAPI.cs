using HedgeModManager;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Runtime.Serialization;
using GameBananaAPI;

namespace KakarotModManager
{
    public class GBAPI
    {
        /// <summary>
        /// Installs the GameBanana one-click install handler
        /// </summary>
        /// <returns></returns>
        public static bool InstallGBHandler(Game game)
        {
            string protocolName = $"KakarotModManager for {game.GameName}";
            try
            {
                var reg = Registry.CurrentUser.CreateSubKey($"Software\\Classes\\{game.GBProtocol}");
                reg.SetValue("", $"URL:{protocolName}");
                reg.SetValue("URL Protocol", "");
                reg = reg.CreateSubKey("shell\\open\\command");
                reg.SetValue("", $"\"{App.AppPath}\" -gb \"%1\"");
                reg.Close();
            }
            catch
            {
                new ExceptionWindow(new Exception("Error installing GameBanana handler.")).ShowDialog();
            }

            return true;
        }

        public static void ParseCommandLine(string line)
        {
            string[] split = line.Split(',');
            if (split.Length < 3)
                return;

            string itemType = split[1];
            var protocal = split[0].Substring(0, split[0].IndexOf(':'));
            string itemDLURL = split[0].Substring(protocal.Length + 1, split[0].Length - (protocal.Length + 1));

            if (!int.TryParse(split[2], out int itemID))
            {
                App.CreateOKMessageBox("Error", $"Invalid Gamebanana item id {itemID}").ShowDialog();
                return;
            }

            var item = new GBAPIItemDataBasic(itemType, itemID);
            if (!GameBananaAPI.GBAPI.RequestItemData(ref item))
            {
                App.CreateOKMessageBox("Error", "Invalid Gamebanana item").ShowDialog();
                return;
            }
            var game = Games.Unknown;
            foreach (var gam in Games.GetSupportedGames())
            {
                if (gam.GBProtocol == protocal)
                {
                    game = gam;
                    break;
                }
            }
            if (game == Games.Unknown)
                return;

            new GBModWindow(item, itemDLURL, game).ShowDialog();
            return;
        }
    }
}