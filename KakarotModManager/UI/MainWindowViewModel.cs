using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KakarotModManager
{
    public class MainWindowViewModel
    {
        public ModsDB ModsDB { get; set; }

        public MainWindowViewModel()
        {
            ModsDB = new ModsDB();
        }
    }
}
