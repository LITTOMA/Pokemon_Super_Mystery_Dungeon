using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using msgtool.Properties;

namespace msgtool
{
    public class BinaryText
    {

        public BinaryTextHeader Header;
        public uint MessageCount;
        public uint HashTableStartOffset;
        public List<BinaryTextEntry> Entries = new List<BinaryTextEntry>();

        public BinaryText(string path)
        {
            using (Stream stream = File.OpenRead(path))
            {
                Read(stream);
            }
        }
        public BinaryText(Stream stream, int position)
        {
            stream.Position = position;
            Read(stream);
        }
        public BinaryText(Stream stream)
        {
            Read(stream);
        }
        public BinaryText(PlainText pt)
        {
            Entries = new List<BinaryTextEntry>();
            Header = new BinaryTextHeader();
            foreach (PlainTextEntry pte in pt.Entries)
            {
                Entries.Add(new BinaryTextEntry(pte));
            }
        }
        public void Import(PlainText pt)
        {
            Entries.Sort(new MessageOffsetComparer());
            for (int i = 0; i < MessageCount; i++)
            {
                var pte = pt.Entries.Find(e => e.MessageFlag == Entries[i].MessageFlag);
                if (pte != null)
                {
                    var e = new BinaryTextEntry(pte);
                    Entries[i].Message = e.Message;
                }
            }
        }
        private void Read(Stream stream)
        {
            long BasePosition = stream.Position;
            Header = new BinaryTextHeader(stream);

            BinaryReader reader = new BinaryReader(stream);

            stream.Position = BasePosition + Header.pMessageCount;
            MessageCount = reader.ReadUInt32();
            HashTableStartOffset = reader.ReadUInt32();

            stream.Position = BasePosition + HashTableStartOffset;
            for (int i = 0; i < MessageCount; i++)
            {
                Entries.Add(new BinaryTextEntry(stream));
            }

        }
        public void ToFile(string path)
        {
            Entries.Sort(new MessageOffsetComparer());
            using (Stream stream = File.Create(path))
            {
                using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8))
                {
                    writer.Write("SIR0".ToCharArray());
                    writer.Write(Header.pMessageCount);
                    writer.Write(Header.PatchTableOffset);
                    writer.Write((uint)0);
                    for (int i = 0; i < Entries.Count; i++)
                    {
                        Entries[i].TextOffset = (int)stream.Position;
                        writer.Write(Entries[i].Message);
                        writer.Write(new byte[] { 0, 0 });
                    }
                    while (stream.Position % 4 != 0)
                        writer.Write((byte)0);
                    HashTableStartOffset = (uint)stream.Position;
                    Entries.Sort(new MessageFlagComparer());
                    foreach (BinaryTextEntry bt in Entries)
                    {
                        writer.Write(bt.TextOffset);
                        writer.Write(bt.MessageFlag);
                        writer.Write(bt.Unknown);
                    }
                    Header.pMessageCount = (uint)stream.Position;
                    writer.Write(Entries.Count);
                    writer.Write(HashTableStartOffset);
                    while (stream.Position % 0x10 != 0)
                    {
                        writer.Write((byte)0);
                    }

                    Header.PatchTableOffset = (uint)stream.Position;
                    writer.Write(new byte[] { 4, 4 });

                    writer.Write(CalcPatchCode(HashTableStartOffset));

                    for (int i = 0; i < Entries.Count - 1; i++)
                    {
                        writer.Write((byte)0xC);
                    }
                    writer.Write((byte)0x10);

                    writer.Write((ushort)0);
                    while (stream.Position % 0x10 != 0)
                    {
                        writer.Write((byte)0);
                    }

                    stream.Position = 4;
                    writer.Write(Header.pMessageCount);
                    writer.Write(Header.PatchTableOffset);
                }
            }
        }
        public static byte[] CalcPatchCode(uint value)
        {
            //假设Patch表的值最大32位
            byte[] result = new byte[5];
            int j = 4;
            uint c = value - 8;
            //黑魔法，勿动
            while (true)
            {
                result[j] = (byte)(c & 0x7F);
                c = c >> 7;
                j--;
                if (c == 0)
                    break;
            }
            for (int i = 0; i < result.Length; i++)
            {
                if (result[i] > 0)
                {
                    if (i < result.Length - 1)
                        result[i] |= 0x80;
                }
            }
            return result.SkipWhile(b => b == 0).ToArray();
        }

        public class BinaryTextHeader
        {
            public string Magic;
            public uint pMessageCount;
            public uint PatchTableOffset;

            public BinaryTextHeader()
            {
                pMessageCount = 0;
                PatchTableOffset = 0;
            }
            public BinaryTextHeader(Stream stream, int position)
            {
                stream.Position = position;
                Read(stream);
            }
            public BinaryTextHeader(Stream stream)
            {
                Read(stream);
            }
            private void Read(Stream stream)
            {
                BinaryReader reader = new BinaryReader(stream);
                Magic = new string(reader.ReadChars(4));
                if (Magic != "SIR0")
                    throw new Exception("Not a Binary Text file.");
                pMessageCount = reader.ReadUInt32();
                PatchTableOffset = reader.ReadUInt32();
            }
        }
    }
    public class BinaryTextEntry
    {
        public int TextOffset;
        public int MessageFlag;
        public uint Unknown;
        public byte[] Message;

        public BinaryTextEntry() { }
        public BinaryTextEntry(PlainTextEntry pte, string ctrlFile = "")
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

            MessageFlag = pte.MessageFlag;
            Unknown = pte.Unknown;

            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(ms, Encoding.Unicode))
                {
                    bool ctrlStart = false;
                    string ctrl = "";
                    foreach (char c in pte.Message)
                    {
                        if (c == '<')
                        {
                            ctrlStart = true;
                            ctrl += c;
                            continue;
                        }
                        if (c == '>')
                        {
                            ctrlStart = false;
                            ctrl += c;
                            writer.Write(ByteCode.ParseByteCode(ctrl, codes));
                            ctrl = "";
                            continue;
                        }
                        if (ctrlStart)
                        {
                            ctrl += c;
                            continue;
                        }
                        else
                        {
                            writer.Write(c);
                        }
                    }
                    Message = ms.ToArray();
                }
            }
        }
        public BinaryTextEntry(Stream stream, int position)
        {
            stream.Position = position;
            Read(stream);
        }
        public BinaryTextEntry(Stream stream)
        {
            Read(stream);
        }
        private void Read(Stream stream)
        {
            BinaryReader reader = new BinaryReader(stream);

            using (MemoryStream ms = new MemoryStream())
            {
                TextOffset = reader.ReadInt32();
                MessageFlag = reader.ReadInt32();
                Unknown = reader.ReadUInt32();
                long Temp = stream.Position;
                stream.Position = TextOffset;
                byte[] c = reader.ReadBytes(2);
                while (!((c[0] == 0) && (c[1] == 0)))
                {
                    ms.Write(c, 0, 2);
                    c = reader.ReadBytes(2);
                }
                Message = ms.ToArray();
                stream.Position = Temp;
            }

        }
    }

    public class MessageFlagComparer : IComparer<BinaryTextEntry>
    {
        public int Compare(BinaryTextEntry a, BinaryTextEntry b)
        {
            if ((uint)a.MessageFlag > (uint)b.MessageFlag)
                return 1;
            else if ((uint)a.MessageFlag < (uint)b.MessageFlag)
                return -1;
            else
                return 0;
        }
    }
    public class MessageOffsetComparer : IComparer<BinaryTextEntry>
    {
        public int Compare(BinaryTextEntry x, BinaryTextEntry y)
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
