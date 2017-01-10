using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using msgtool;

namespace Add_Talker
{
    class Program
    {
        static void Main(string[] args)
        {
            PlainText.AddTalkers(args[0], args[1], args[2], args[3], args[4], args[5]);
        }
    }
}
