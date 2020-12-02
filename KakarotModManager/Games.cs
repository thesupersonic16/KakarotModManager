using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KakarotModManager.Properties;

namespace KakarotModManager
{
    public static class Games
    {
        public static Game Unknown = new Game();
        public static Game DBZKakarot = new Game()
        {
            GameName = "Dragon Ball Z Kakarot",
            ExecuteableName = "AT.exe",
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

    public class Game
    {
        public string GameName = "Unnamed Game";
        public string ExecuteableName = string.Empty;
        public uint DirectXVersion = uint.MaxValue;
        public string AppID = "0";
        public string GBProtocol;
        public bool Is64Bit = false;

        public override string ToString()
        {
            return GameName;
        }
    }
}
