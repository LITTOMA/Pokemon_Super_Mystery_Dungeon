using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jukebox
{
    class Program
    {
        static void Main(string[] args)
        {
            JukeBox jukeBox = JukeBox.FromBinary(args[0]);
            //jukeBox.ToText(args[1]);
            jukeBox.Import(args[1]);
            jukeBox.ToBinary(args[2]);
        }
    }
}
