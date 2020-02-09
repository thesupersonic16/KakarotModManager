using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HedgeModManager;
using KakarotModManager.Properties;

namespace KakarotModManager
{
    class Games
    {
        public static Game Unknown = new Game();
        public static Game DBZKakarot = new Game()
        {
            GameName = "Dragon Ball Z Kakarot",
            ExecuteableName = "AT.exe",
            HasCustomLoader = false,
            SupportsCPKREDIR = false,
            AppID = "851850",
            DirectXVersion = 12,
            GBProtocol = "kmmkakarot",
            Is64Bit = true
        };

        public static IEnumerable<Game> GetSupportedGames()
        {
            yield return DBZKakarot;
        }
    }
}
