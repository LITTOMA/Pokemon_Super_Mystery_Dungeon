using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;
using _3DSExplorer;

namespace msgtool
{
    public class PlainText
    {

        public List<PlainTextEntry> Entries = new List<PlainTextEntry>();

        //public PlainText(BinaryText bt)
        //{
        //    for (int i = 0; i < bt.Entries.Count; i++)
        //    {
        //        Entries.Add(new PlainTextEntry(bt.Entries[i]));
        //    }
        //}
        public PlainText() { }
        public PlainText(string path)
        {
            Entries = new List<PlainTextEntry>();
            //EntriesDic = new Dictionary<int, PlainTextEntry>();
            FromFile(path);
        }
        public void FromFile(string path)
        {
            Entries = new List<PlainTextEntry>();
            string[] lines = File.ReadAllLines(path);
            bool oldstarted = false;
            bool newstarted = false;
            string temp = "";
            PlainTextEntry entry = new PlainTextEntry();
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("MessageFlag") && lines[i].Contains("UnknownValue"))
                {
                    entry = new PlainTextEntry();
                    entry.MessageFlag = int.Parse(lines[i].Substring(0, lines[i].IndexOf(" | ")).Replace("MessageFlag: ", ""));
                    entry.Unknown = uint.Parse(lines[i].Substring(lines[i].LastIndexOf(": ") + 2));
                }
                if (!oldstarted && !newstarted && lines[i].Contains("－－－－－－－")) { oldstarted = true; continue; }
                if (oldstarted && !newstarted && lines[i].Contains("－－－－－－－")) { newstarted = true; oldstarted = false; continue; }
                if (newstarted && !lines[i].Contains("＝＝＝＝＝＝＝"))
                {
                    temp += lines[i];
                    if (!lines[i + 1].Contains("＝＝＝＝＝＝＝")) temp += '\n';
                }
                if (newstarted && lines[i].Contains("＝＝＝＝＝＝＝"))
                {
                    newstarted = false;
                    entry.Message = temp;
                    Entries.Add(entry);
                    //EntriesDic.Add(entry.MessageFlag, entry);
                    temp = "";
                }
            }
        }
        public void ToFile(string path)
        {
            FileStream fs = File.Create(path);
            using (StreamWriter writer = new StreamWriter(fs, Encoding.UTF8))
            {
                //Entries.Sort(new TextOffsetComparer());
                int no = 0;
                foreach (PlainTextEntry pte in Entries)
                {
                    if (string.IsNullOrEmpty(pte.Message))
                        continue;
                    no++;
                    writer.WriteLine("No.{0}  Talker: {1}", no, pte.Talker);
                    writer.WriteLine("MessageFlag: {0} | UnknownValue: {1}", pte.MessageFlag, pte.Unknown);
                    writer.WriteLine("－－－－－－－－－－－－－－－－－－－－");
                    writer.WriteLine(pte.Message.Replace("\n", "\r\n"));
                    writer.WriteLine("－－－－－－－－－－－－－－－－－－－－");
                    writer.WriteLine(pte.Message.Replace("\n", "\r\n"));
                    writer.WriteLine("＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝\r\n\r\n\r\n");
                }
            }
        }
        public static void AddTalkers(string inFilePath, string outFilePath, string pmNamePath, string sir0FilePath, string table2Path, string talkStructPath)
        {
            PlainText pt = new PlainText(inFilePath);

            string[] PM_Names = File.ReadAllLines(pmNamePath);
            ActorMessage[] actorMessages;
            Dictionary<int, TalkStruct> TalkStructDictionary = new Dictionary<int, TalkStruct>();
            #region dugeon
            //int[] FlagTable1, FlagTable2;
            //FlagTable1 = ReadFlagTable(table1Path); FlagTable2 = ReadFlagTable(table2Path);

            //using (FileStream talkStructFs = File.OpenRead(talkStructPath))
            //{
            //    while (talkStructFs.Position < talkStructFs.Length)
            //    {
            //        Talk t = new Talk();
            //        t.Talkstruct = MarshalUtil.ReadStruct<TalkStruct>(talkStructFs);
            //        t.Talker = PM_Names[t.Talkstruct.NameIndex];
            //        try
            //        {
            //            t.MessageFlag1 = FlagTable1[t.Talkstruct.FlagIndex];
            //        }
            //        catch { 
            //        }
            //        try
            //        {
            //            t.MessageFlag2 = FlagTable2[t.Talkstruct.FlagIndex];
            //        }
            //        catch { 
            //        }
            //        for (int i = 0; i < pt.Entries.Count; i++)
            //        {
            //            if ((pt.Entries[i].MessageFlag == t.MessageFlag1) || (pt.Entries[i].MessageFlag == t.MessageFlag2))
            //            {
            //                pt.Entries[i].Talker += t.Talker + ' ';
            //            }
            //        }
            //    }
            //}
            #endregion
            using (FileStream talkStructFs = File.OpenRead(talkStructPath))
            {
                while (talkStructFs.Position < talkStructFs.Length)
                {
                    TalkStruct ts = MarshalUtil.ReadStruct<TalkStruct>(talkStructFs);
                    TalkStructDictionary.Add(ts.FlagIndex, ts);
                }
            }
            using (FileStream sir0Fs = File.OpenRead(sir0FilePath))
            {
                Sir0Header sir0Header = new Sir0Header(sir0Fs);
                sir0Fs.Position = sir0Header.ContentInfoOffset;
                Sir0ContentInfo sir0ContentInfo = MarshalUtil.ReadStruct<Sir0ContentInfo>(sir0Fs);
                actorMessages = new ActorMessage[sir0ContentInfo.Sir0EntriesCount];
                sir0Fs.Position = sir0ContentInfo.Sir0InfoTableOffset;

                for (int i = 0; i < sir0ContentInfo.Sir0EntriesCount; i++)
                {
                    actorMessages[i] = MarshalUtil.ReadStruct<ActorMessage>(sir0Fs);
                }
            }
            foreach (ActorMessage am in actorMessages)
            {
                for (int i = 0; i < pt.Entries.Count; i++)
                {
                    if ((pt.Entries[i].MessageFlag == am.MessageFlag1) || (pt.Entries[i].MessageFlag == am.MessageFlag2))
                    {
                        pt.Entries[i].Talker = PM_Names[TalkStructDictionary[am.FlagIndex].NameIndex];
                    }
                }
            }

            pt.ToFile(outFilePath);
        }
        public static int[] ReadFlagTable(string path)
        {
            int[] FlagTable;
            using (FileStream fs = File.OpenRead(path))
            using (BinaryReader reader = new BinaryReader(fs))
            {
                FlagTable = new int[fs.Length / 4];
                for (int i = 0; i < FlagTable.Length; i++)
                {
                    FlagTable[i] = reader.ReadInt32();
                }
            }
            return FlagTable;
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
        public PlainTextEntry(int messageFlag, uint unknown, string message)
        {
            MessageFlag = messageFlag;
            Unknown = unknown;
            Message = message;
        }
        //public PlainTextEntry(BinaryTextEntry bte)
        //{
        //    TextOffset = bte.TextOffset;
        //    MessageFlag = bte.MessageFlag;
        //    Unknown = bte.Unknown;

        //    MemoryStream MessageStream = new MemoryStream();

        //    #region Handle Paras
        //    BinaryWriter writer = new BinaryWriter(MessageStream, Encoding.Unicode);
        //    using (BinaryReader reader = new BinaryReader(new MemoryStream(bte.Message)))
        //    {
        //        bool pron = false;
        //        int npron = 0;
        //        while (reader.BaseStream.Position < reader.BaseStream.Length - 1)
        //        {
        //            ushort c = reader.ReadUInt16();
        //            bool matched = false;

        //            //if (((c >> 8) > 0xA0) && ((c >> 8) < 0xFD))
        //            {
        //                if (c == 0xCF00)
        //                {
        //                    pron = true;
        //                    continue;
        //                }
        //                if (pron && ((c & 0xFF00) == 0xCF00) && (c != 0xCFFF))
        //                {
        //                    npron = c & 0xFF;
        //                    continue;
        //                }
        //                if (npron > 0)
        //                {
        //                    npron--;
        //                    continue;
        //                }
        //                if (c == 0xCFFF)
        //                    continue;
        //                for (int i = 0; i < Program.Paras.SingleConstPara.Count; i++)
        //                {
        //                    if (c == Program.Paras.SingleConstPara[i].Item1)
        //                    {
        //                        writer.Write((Program.Paras.SingleConstPara[i].Item2).ToCharArray());
        //                        matched = true;
        //                        break;
        //                    }
        //                }
        //                if (matched)
        //                    continue;
        //                for (int i = 0; i < Program.Paras.TriConstPara.Count; i++)
        //                {
        //                    if (c == Program.Paras.TriConstPara[i].Item1)
        //                    {
        //                        string HexX1 = string.Format("{0:X4}", reader.ReadUInt16());
        //                        string HexX2 = string.Format("{0:X4}", reader.ReadUInt16());
        //                        writer.Write((Program.Paras.TriConstPara[i].Item2.Replace("XX", "").Replace(">", HexX1 + HexX2 + ">")).ToCharArray());
        //                        matched = true;
        //                        break;
        //                    }
        //                }
        //                if (matched)
        //                    continue;
        //                for (int i = 0; i < Program.Paras.DoublePara.Count; i++)
        //                {
        //                    if ((c >> 8) == Program.Paras.DoublePara[i].Item1)
        //                    {
        //                        string HexX = string.Format("{0:X2}", (c & 0xFF));
        //                        string HexY = string.Format("{0:X4}", reader.ReadUInt16());
        //                        writer.Write((Program.Paras.DoublePara[i].Item2.Replace("XX", HexX).Replace("YYYY", HexY)).ToCharArray());
        //                        matched = true;
        //                        break;
        //                    }
        //                }
        //                if (matched)
        //                    continue;
        //                for (int i = 0; i < Program.Paras.TriVarPara.Count; i++)
        //                {
        //                    if ((c >> 8) == Program.Paras.TriVarPara[i].Item1)
        //                    {
        //                        string HexX = string.Format("{0:X2}", (c & 0xFF));
        //                        string HexY = string.Format("{0:X4}", reader.ReadUInt16());
        //                        string HexZ = string.Format("{0:X4}", reader.ReadUInt16());
        //                        writer.Write((Program.Paras.TriVarPara[i].Item2.Replace("XX", HexX).Replace("YYYY", HexY).Replace("ZZZZ", HexZ)).ToCharArray());
        //                        matched = true;
        //                        break;
        //                    }
        //                }
        //                if (matched)
        //                    continue;
        //                for (int i = 0; i < Program.Paras.SingleVarPara.Count; i++)
        //                {
        //                    if ((c >> 8) == Program.Paras.SingleVarPara[i].Item1)
        //                    {
        //                        string HexX = string.Format("{0:X2}", (c & 0xFF));
        //                        writer.Write((Program.Paras.SingleVarPara[i].Item2.Replace("XX", HexX)).ToCharArray());
        //                        matched = true;
        //                        break;
        //                    }
        //                }
        //                if (matched)
        //                    continue;


        //                //writer.Write(string.Format("<{0:X4}>", c));
        //                //continue;
        //            }
        //            writer.Write(c);
        //        }
        //    }
        //    #endregion

        //    StreamReader sr = new StreamReader(MessageStream, Encoding.Unicode);
        //    MessageStream.Position = 0;
        //    Message = sr.ReadToEnd();

        //    if (Program.Luas != null)
        //    {
        //        foreach (LuaEntry le in Program.Luas.Entries)
        //        {
        //            if (le.Flag == bte.MessageFlag)
        //                Talker = le.Talker;
        //        }
        //    }
        //}
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

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TalkStruct
    {
        public uint MagicCode;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x9C)]
        public byte[] Unknown1;
        public ushort NameIndex;
        public ushort FlagIndex;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x44)]
        public byte[] Unknown2;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x18)]
        public byte[] Name;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x50)]
        public byte[] Unknown3;
    }
    public class Talk
    {
        public TalkStruct Talkstruct;
        public string Talker;
        public int MessageFlag1;
        public int MessageFlag2;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ActorMessage
    {
        public int MessageFlag1;
        uint Unknown1;
        public int MessageFlag2;
        uint Unknown2;
        uint Unknown3;
        public int FlagIndex;
    }
    public class Sir0Header
    {
        public uint Magic;
        public uint ContentInfoOffset;
        public uint PatchTableOffset;

        public Sir0Header(Stream stream)
        {
            BinaryReader reader = new BinaryReader(stream);
            Magic = reader.ReadUInt32();
            if (Magic != 0x30524953) { return; }
            ContentInfoOffset = reader.ReadUInt32();
            PatchTableOffset = reader.ReadUInt32();
        }
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Sir0ContentInfo
    {
        public uint Sir0InfoTableOffset;
        public uint Sir0EntriesCount;
    }
}
