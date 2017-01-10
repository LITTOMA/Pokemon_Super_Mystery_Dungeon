using System.Collections.Generic;
using System.IO;

namespace msgtool
{
    public class SimpleLua
    {
        public List<LuaEntry> Entries = new List<LuaEntry>();
        public SimpleLua(string path)
        {
            ReadLua(path);
        }
        private void ReadLua(string path)
        {
            string[] lines = File.ReadAllLines(path);

            foreach (string line in lines) {
                if (line.Contains("WINDOW:Narration")) {
                    int Flag = int.Parse(line.Substring(line.LastIndexOf(", ") + 2, line.Length - line.LastIndexOf(", ") - 3));
                    Entries.Add(new LuaEntry("Narration", Flag));
                } else if (line.Contains("WINDOW:SysMsg")) {
                    int Flag = int.Parse(line.Substring(line.LastIndexOf('(') + 1, line.Length - line.LastIndexOf('(') - 2));
                    Entries.Add(new LuaEntry("SysMsg", Flag));
                } else if (line.Contains("WINDOW:Monologue")) {
                    int Flag = int.Parse(line.Substring(line.LastIndexOf('(') + 1, line.Length - line.LastIndexOf('(') - 2));
                    Entries.Add(new LuaEntry("Monologue", Flag));
                } else if (line.Contains("WINDOW:SwitchTalk")) {
                    int P0_Flag = int.Parse(line.Substring(line.IndexOf("= ") + 2, line.IndexOf(", ") - line.IndexOf("= ") - 2));
                    Entries.Add(new LuaEntry("PARTNER_0", P0_Flag));
                    int P1_Flag = int.Parse(line.Substring(line.LastIndexOf("= ") + 2, line.Length - line.LastIndexOf("= ") - 4));
                    Entries.Add(new LuaEntry("PARTNER_1", P1_Flag));
                } else if (line.Contains("WINDOW:Talk")) {
                    if ((!line.Contains("SymWord")) && (!line.Contains("SYSTEM:GetItemColorSymbol")) && line.Contains("\"")) {
                        string Talker = line.Substring(line.IndexOf("\""), line.LastIndexOf("\"") - line.IndexOf("\"") + 1);
                        int Flag = int.Parse(line.Substring(line.LastIndexOf(", ") + 2, line.Length - line.LastIndexOf(", ") - 3));
                        Entries.Add(new LuaEntry(Talker, Flag));
                    }
                }
            }
        }
    }
    public class LuaEntry
    {
        public string Talker;
        public int Flag;

        public LuaEntry(string talker, int flag)
        {
            Talker = talker;
            Flag = flag;
        }
    }
}
