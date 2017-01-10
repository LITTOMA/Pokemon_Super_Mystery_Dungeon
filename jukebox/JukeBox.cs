using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using CEED.Utils;

namespace jukebox
{
    class JukeBox
    {
        public JukeBoxHeader Header;
        public JukeBoxContentInfo ContentInfo;
        public List<Box> Entries;

        public static JukeBox FromBinary(string path)
        {
            Stream stream = File.OpenRead(path);
            return FromBinary(stream);
        }
        public static JukeBox FromBinary(Stream stream)
        {
            JukeBox jukeBox = new JukeBox();
            jukeBox.Header = new JukeBoxHeader(stream);
            stream.Position = jukeBox.Header.ContentInfoOffset;
            jukeBox.ContentInfo = StructReader.ReadStruct<JukeBoxContentInfo>(stream);
            jukeBox.Entries = new List<Box>();

            stream.Position = jukeBox.ContentInfo.JukeBoxInfoTableOffset;
            for (int i = 0; i < jukeBox.ContentInfo.JukeBoxEntriesCount; i++)
            {
                jukeBox.Entries.Add(Box.FromBinary(stream));
            }
            return jukeBox;
        }
        public void ToText(string path)
        {
            FileStream stream = File.Create(path);
            ToText(stream);
        }
        public void ToText(FileStream stream)
        {
            StreamWriter writer = new StreamWriter(stream, Encoding.UTF8);
            foreach (Box box in Entries)
            {
                //box.ToText(stream);
                writer.WriteLine("No.{0}", box.Info.ID/*, box.Info.MagicNumber*/);
                writer.WriteLine("Name: {1}\r\nStage: {0}", box.StageName, box.MessageName);
                writer.WriteLine(box.MessageInfo);
                writer.WriteLine("－－－－－－－－－－－－－－－－－－－－");
                writer.WriteLine(box.MessageText.Replace("\n", "\r\n"));
                writer.WriteLine("－－－－－－－－－－－－－－－－－－－－");
                writer.WriteLine(box.MessageText.Replace("\n", "\r\n"));
                writer.WriteLine("＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝\r\n\r\n\r\n");
            }
            writer.Close();
        }
        public void Import(string path)
        {
            Regex regex = new Regex(@"No\.(.+)\r\nName: (.+)\r\nStage: (.*)\r\n(.*)\r\n－+?\r\n[\s|\S]+?\r\n－+?\r\n([\s|\S]+?)\r\n＝+?\r\n");
            string text = File.ReadAllText(path, Encoding.Unicode);
            MatchCollection mc = regex.Matches(text);
            foreach(Match m in mc)
            {
                int index = Entries.FindIndex(e => e.Info.ID == uint.Parse(m.Groups[1].Value));
                Entries[index].MessageText = m.Groups[5].Value;
            }
        }
        public void ToBinary(string path)
        {
            FileStream fs = File.Create(path);
            Header.Write(fs);
            BinaryWriter writer = new BinaryWriter(fs, Encoding.Unicode);
            for (int i = 0; i < Entries.Count; i++)
            {
                Entries[i].Info.MessageNameOffset = (uint)fs.Position;
                writer.Write(Entries[i].MessageName.ToCharArray());
                writer.Write('\0');
                writer.Flush();
            }
            Align(writer);
            for (int i = 0; i < Entries.Count; i++)
            {
                Entries[i].Info.MessageTextOffset = (uint)fs.Position;
                writer.Write(Entries[i].MessageText.ToCharArray());
                writer.Write('\0');
                writer.Flush();
            }
            Align(writer);
            for (int i = 0; i < Entries.Count; i++)
            {
                Entries[i].Info.MessageInfoOffset = (uint)fs.Position;
                writer.Write(Entries[i].MessageInfo.ToCharArray());
                writer.Write('\0');
                writer.Flush();
            }
            Align(writer);
            for (int i = 0; i < Entries.Count; i++)
            {
                Entries[i].Info.StageNameOffset = (uint)fs.Position;
                writer.Write(Entries[i].StageName.ToCharArray());
                writer.Write('\0');
                writer.Flush();
            }
            Align(writer);
            for (int i = 0; i < Entries.Count; i++)
            {
                Entries[i].Info.ReservedOffset = (uint)fs.Position;
                writer.Write(Entries[i].Reserved.ToCharArray());
                writer.Write('\0');
                writer.Flush();
            }
            Align(writer);
            ContentInfo.JukeBoxEntriesCount = (uint)Entries.Count;
            ContentInfo.JukeBoxInfoTableOffset = (uint)fs.Position;
            foreach (var e in Entries)
            {
                StructWriter.WriteStruct(fs, e.Info);
            }
            Align(writer);

            Header.ContentInfoOffset = (uint)fs.Position;
            StructWriter.WriteStruct(fs, ContentInfo);
            Align(writer);
            Header.PatchTableOffset = (uint)fs.Position;

            writer.Write(new byte[] { 4, 4 });

            uint c = ContentInfo.JukeBoxInfoTableOffset;
            writer.Write(CalcPatchCode(c));

            byte[] code = { 4, 4, 4, 4, 0x14 };
            for (int i = 0; i < ContentInfo.JukeBoxEntriesCount; i++)
            {
                writer.Write(code);
                writer.Flush();
            }
            fs.Seek(-1, SeekOrigin.Current);
            writer.Write((byte)0x18);
            Align(writer);

            fs.Position = 0;
            Header.Write(fs);

            writer.Close();
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
        private static void Align(BinaryWriter writer)
        {
            while (!(writer.BaseStream.Position % 0x10 == 0))
            {
                writer.Write((byte)0);
            }
            writer.Flush();
        }

        public class JukeBoxHeader
        {
            public uint Magic;
            public uint ContentInfoOffset;
            public uint PatchTableOffset;

            public JukeBoxHeader(Stream stream)
            {
                BinaryReader reader = new BinaryReader(stream);
                Magic = reader.ReadUInt32();
                if (Magic != 0x30524953) { return; }
                ContentInfoOffset = reader.ReadUInt32();
                PatchTableOffset = reader.ReadUInt32();
            }
            public void Write(Stream stream)
            {
                BinaryWriter writer = new BinaryWriter(stream);
                writer.Write("SIR0".ToCharArray());
                writer.Write(ContentInfoOffset);
                writer.Write(PatchTableOffset);
                writer.Write((uint)0);
            }
        }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct JukeBoxContentInfo
        {
            public uint JukeBoxInfoTableOffset;
            public uint JukeBoxEntriesCount;
        }
    }
    public class Box
    {
        public JukeBoxInfo Info;
        public string MessageName;
        public string MessageText;
        public string MessageInfo;
        public string StageName;
        public string Reserved;

        public Box() { }
        public static Box FromBinary(Stream stream)
        {
            Box box = new Box();

            box.Info = StructReader.ReadStruct<JukeBoxInfo>(stream);
            long orginOffset = stream.Position;

            stream.Position = box.Info.MessageNameOffset;
            box.MessageName = ReadString(stream);

            stream.Position = box.Info.MessageTextOffset;
            box.MessageText = ReadString(stream);

            stream.Position = box.Info.MessageInfoOffset;
            box.MessageInfo = ReadString(stream);

            stream.Position = box.Info.StageNameOffset;
            box.StageName = ReadString(stream);

            stream.Position = box.Info.ReservedOffset;
            box.Reserved = ReadString(stream);

            stream.Position = orginOffset;
            return box;
        }
        public static Box FromText(string[] texts)
        {
            return new Box();
        }
        public void ToBinary(FileStream stream)
        {

        }
        public void ToText(FileStream stream)
        {
            //StreamWriter writer = new StreamWriter(stream, Encoding.UTF8);
            //foreach (uint id in Info.ID)
            //{
            //    if (id > 0)
            //    {
            //        writer.Write("No.{0} ", id);
            //    }
            //}
            //writer.WriteLine();
            //writer.WriteLine("Chapter:{0}, Name:{1}", ChapterName, MessageName);
            //writer.WriteLine("Info:{0}", MessageInfo);
            //writer.WriteLine("－－－－－－－－－－－－－－－－－－－－");
            //writer.WriteLine(MessageText.Replace("\n", "\r\n"));
            //writer.WriteLine("－－－－－－－－－－－－－－－－－－－－");
            //writer.WriteLine(MessageText.Replace("\n", "\r\n"));
            //writer.WriteLine("＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝\r\n\r\n\r\n");
        }

        public static string ReadString(Stream stream)
        {
            StringBuilder sb = new StringBuilder();
            BinaryReader reader = new BinaryReader(stream, Encoding.Unicode);
            char c = reader.ReadChar();
            while (c != 0)
            {
                sb.Append(c);
                c = reader.ReadChar();
            }
            return sb.ToString();
        }
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct JukeBoxInfo
    {
        public uint MessageNameOffset;
        public uint MessageTextOffset;
        public uint MessageInfoOffset;
        public uint StageNameOffset;
        public uint ReservedOffset;
        public uint MagicNumber;
        public uint ID;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public uint[] Unknown;
    }
}
