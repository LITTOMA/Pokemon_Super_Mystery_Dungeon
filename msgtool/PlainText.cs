using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using msgtool.Properties;

namespace msgtool
{
    public class PlainText
    {
        public List<PlainTextEntry> Entries = new List<PlainTextEntry>();

        public PlainText(BinaryText bt, string scriptPath, string ctrlFile = "")
        {
            ByteCode codes;
            string[] ps = new string[0];
            if (File.Exists(ctrlFile))
            {
                ps = File.ReadAllLines(ctrlFile);
            }
            else
            {
                ps = Resources.ctrl.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            }
            codes = new ByteCode();
            codes.SetCtrls(ps);
            SimpleLua Scripts = null;
            if (!string.IsNullOrEmpty(scriptPath))
                if (File.Exists(scriptPath))
                    Scripts = new SimpleLua(scriptPath);
            for (int i = 0; i < bt.Entries.Count; i++)
            {
                Entries.Add(new PlainTextEntry(bt.Entries[i], codes, Scripts));
            }
        }
        public PlainText(string path)
        {
            Entries = new List<PlainTextEntry>();
            FromFile(path);
        }
        public void FromFile(string path)
        {
            Regex regex = new Regex(@"No\..+?Talker: (.*?)\r\nMessageFlag: (.+?) \| UnknownValue: (.+?)\r\n－+\r\n([\s|\S]*?)\r\n－+\r\n([\s|\S]*?)\r\n＝+\r\n");
            MatchCollection mc = regex.Matches(File.ReadAllText(path, Encoding.Unicode));
            foreach (Match m in mc)
            {
                PlainTextEntry pte = new PlainTextEntry();
                pte.Talker = m.Groups[1].Value;
                pte.MessageFlag = int.Parse(m.Groups[2].Value);
                pte.Unknown = uint.Parse(m.Groups[3].Value);
                pte.Message = m.Groups[5].Value.Replace("\r\n", "\n").Replace("<PAGE>\n", "<PAGE>");
                Entries.Add(pte);
            }
        }

        public void ToFile(string path, bool keepEmpty = false)
        {
            FileStream fs = File.Create(path);
            using (StreamWriter writer = new StreamWriter(fs, Encoding.UTF8))
            {
                Entries.Sort(new TextOffsetComparer());
                int no = 0;
                foreach (PlainTextEntry pte in Entries)
                {
                    if (string.IsNullOrEmpty(pte.Message) && !keepEmpty)
                        continue;
                    no++;
                    writer.WriteLine("No.{0}  Talker: {1}", no, pte.Talker);
                    writer.WriteLine(string.Format("MessageFlag: {0} | UnknownValue: {1}", pte.MessageFlag, pte.Unknown));
                    writer.WriteLine("－－－－－－－－－－－－－－－－－－－－");
                    writer.WriteLine(pte.Message.Replace("\n", "\r\n"));
                    writer.WriteLine("－－－－－－－－－－－－－－－－－－－－");
                    writer.WriteLine(pte.Message.Replace("\n", "\r\n"));
                    writer.WriteLine("＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝\r\n\r\n");
                }
            }
        }
    }

    public class PlainTextEntry
    {
        public int TextOffset;
        public int MessageFlag;
        public uint Unknown;
        public string Talker = "";
        public string Message;

        public PlainTextEntry()
        {

        }
        public PlainTextEntry(int messageflag, uint unknown, string message)
        {
            MessageFlag = messageflag;
            Unknown = unknown;
            Message = message;
        }
        public PlainTextEntry(BinaryTextEntry bte, ByteCode codes, SimpleLua script)
        {
            TextOffset = bte.TextOffset;
            MessageFlag = bte.MessageFlag;
            Unknown = bte.Unknown;

            MemoryStream MessageStream = new MemoryStream();

            #region Handle ByteCodes
            BinaryWriter writer = new BinaryWriter(MessageStream, Encoding.Unicode);
            using (BinaryReader reader = new BinaryReader(new MemoryStream(bte.Message)))
            {
                bool pron = false;
                int npron = 0;
                while (reader.BaseStream.Position < reader.BaseStream.Length - 1)
                {
                    ushort c = reader.ReadUInt16();
                    bool matched = false;

                    //if (((c >> 8) > 0xA0) && ((c >> 8) < 0xFD))
                    //try
                    {
                        if (c == 0xCF00)
                        {
                            pron = true;
                            continue;
                        }
                        if (pron && ((c & 0xFF00) == 0xCF00) && (c != 0xCFFF))
                        {
                            npron = c & 0xFF;
                            continue;
                        }
                        if (npron > 0)
                        {
                            npron--;
                            continue;
                        }
                        if (c == 0xCFFF)
                            continue;
                        for (int i = 0; i < codes.SingleConstCodes.Count; i++)
                        {
                            if (c == codes.SingleConstCodes[i].Item1)
                            {
                                writer.Write((codes.SingleConstCodes[i].Item2).ToCharArray());
                                matched = true;
                                break;
                            }
                        }
                        if (matched)
                            continue;
                        for (int i = 0; i < codes.TriConstCodes.Count; i++)
                        {
                            if (c == codes.TriConstCodes[i].Item1)
                            {
                                string HexX1 = string.Format("{0:X4}", reader.ReadUInt16());
                                string HexX2 = string.Format("{0:X4}", reader.ReadUInt16());
                                writer.Write((codes.TriConstCodes[i].Item2.Replace("XX", "").Replace(">", HexX1 + HexX2 + ">")).ToCharArray());
                                matched = true;
                                break;
                            }
                        }
                        if (matched)
                            continue;
                        for (int i = 0; i < codes.DoubleCodes.Count; i++)
                        {
                            if ((c >> 8) == codes.DoubleCodes[i].Item1)
                            {
                                string HexX = string.Format("{0:X2}", (c & 0xFF));
                                string HexY = string.Format("{0:X4}", reader.ReadUInt16());
                                writer.Write((codes.DoubleCodes[i].Item2.Replace("XX", HexX).Replace("YYYY", HexY)).ToCharArray());
                                matched = true;
                                break;
                            }
                        }
                        if (matched)
                            continue;
                        for (int i = 0; i < codes.TriVarCodes.Count; i++)
                        {
                            if ((c >> 8) == codes.TriVarCodes[i].Item1)
                            {
                                string HexX = string.Format("{0:X2}", (c & 0xFF));
                                string HexY = string.Format("{0:X4}", reader.ReadUInt16());
                                string HexZ = string.Format("{0:X4}", reader.ReadUInt16());
                                writer.Write((codes.TriVarCodes[i].Item2.Replace("XX", HexX).Replace("YYYY", HexY).Replace("ZZZZ", HexZ)).ToCharArray());
                                matched = true;
                                break;
                            }
                        }
                        if (matched)
                            continue;
                        for (int i = 0; i < codes.SingleVarCodes.Count; i++)
                        {
                            if ((c >> 8) == codes.SingleVarCodes[i].Item1)
                            {
                                string HexX = string.Format("{0:X2}", (c & 0xFF));
                                writer.Write((codes.SingleVarCodes[i].Item2.Replace("XX", HexX)).ToCharArray());
                                matched = true;
                                break;
                            }
                        }
                        if (matched)
                            continue;
                        //writer.Write(string.Format("<{0:X4}>", c));
                        //continue;
                    }
                    //catch
                    //{

                    //}
                    writer.Write(c);
                }
            }
            #endregion

            StreamReader sr = new StreamReader(MessageStream, Encoding.Unicode);
            MessageStream.Position = 0;
            Message = sr.ReadToEnd();

            if (script != null)
            {
                foreach (LuaEntry le in script.Entries)
                {
                    if (le.Flag == bte.MessageFlag)
                        Talker = le.Talker;
                }
            }
        }
    }
    public class TextOffsetComparer : IComparer<PlainTextEntry>
    {
        public int Compare(PlainTextEntry x, PlainTextEntry y)
        {
            if (x.TextOffset > y.TextOffset)
                return 1;
            else if (x.TextOffset < y.TextOffset)
                return -1;
            else
                return 0;
        }
    }
}
