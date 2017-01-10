using System;
using System.Collections.Generic;

namespace msgtool
{
    public class ByteCode
    {
        public List<Tuple<ushort, string>> SingleConstCodes = new List<Tuple<ushort, string>>();
        public List<Tuple<ushort, string>> TriConstCodes = new List<Tuple<ushort, string>>();
        public List<Tuple<byte, string>> SingleVarCodes = new List<Tuple<byte, string>>();
        public List<Tuple<byte, string>> DoubleCodes = new List<Tuple<byte, string>>();
        public List<Tuple<byte, string>> TriVarCodes = new List<Tuple<byte, string>>();

        public void SetCtrls(string[] lines)
        {
            foreach (string line in lines) {
                string[] s = line.Split(new string[] { ": " }, StringSplitOptions.None);
                if ((!s[0].Contains("X")) && (!s[0].Contains("Y")) && (!s[0].Contains("Z"))) {
                    if (s[1] == "<PAGE>")
                        s[1] += "\n";
                    SingleConstCodes.Add(new Tuple<ushort, string>(Convert.ToUInt16(s[0], 16), s[1]));
                } else if ((s[0].Contains("XX")) && (!s[0].Contains("Y")) && (!s[0].Contains("Z"))) {
                    if (s[0].Length == 4) {
                        s[0] = s[0].Replace("XX", "");
                        SingleVarCodes.Add(new Tuple<byte, string>(Convert.ToByte(s[0], 16), s[1]));
                    }
                    if (s[0].Length == 14) {
                        s[0] = s[0].Replace("X", "").Replace(" ", "");
                        TriConstCodes.Add(new Tuple<ushort, string>(Convert.ToUInt16(s[0], 16), s[1]));
                    }
                } else if ((s[0].Contains("XX")) && (s[0].Contains("Y")) && (!s[0].Contains("Z"))) {
                    s[0] = s[0].Replace("X", "").Replace("Y", "").Replace(" ", "");
                    DoubleCodes.Add(new Tuple<byte, string>(Convert.ToByte(s[0], 16), s[1]));
                } else if ((s[0].Contains("XX")) && (s[0].Contains("Y")) && (s[0].Contains("Z"))) {
                    s[0] = s[0].Replace("X", "").Replace("Y", "").Replace("Z", "").Replace(" ", "");
                    TriVarCodes.Add(new Tuple<byte, string>(Convert.ToByte(s[0], 16), s[1]));
                }
            }
        }
        public static byte[] ParseByteCode(string codeString, ByteCode codes)
        {
            #region Parse unknown single
            if ((!codeString.Contains(":")) && (codeString.Length == 6)) {
                foreach (var s in codes.SingleVarCodes) {
                    if (s.Item2.Substring(0, 3) == codeString.Substring(0, 3)) {
                        string numstr = codeString.Replace("<", "").Replace(">", "");
                        foreach (char c in numstr) {
                            if (!((c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F') || (c >= '0' && c <= '9')))  //检查是否16进制字符串
                                goto label_1;
                        }
                        return BitConverter.GetBytes(Convert.ToUInt16(numstr, 16));
                    }
                }
            }
        #endregion
        label_1:
            #region Parse single const
            foreach (var s in codes.SingleConstCodes) {
                if (codeString == s.Item2.Replace("\n", ""))
                    return BitConverter.GetBytes(s.Item1);
            }
            #endregion
            #region Parse tri const
            foreach (var s in codes.TriConstCodes) {
                if (GetControlName(codeString) == GetControlName(s.Item2)) {
                    string numstr = codeString.Replace(GetControlName(codeString), "").Replace("<", "").Replace(":", "").Replace(">", "");
                    ushort HexX1 = Convert.ToUInt16(numstr.Substring(0, 4), 16);
                    ushort HexX2 = Convert.ToUInt16(numstr.Substring(4, 4), 16);
                    var result = new byte[6];
                    Array.Copy(BitConverter.GetBytes(s.Item1), 0, result, 0, 2);
                    Array.Copy(BitConverter.GetBytes(HexX1), 0, result, 2, 2);
                    Array.Copy(BitConverter.GetBytes(HexX2), 0, result, 4, 2);
                    return result;
                }
            }
            #endregion
            #region Parse tri var
            foreach (var s in codes.TriVarCodes) {
                if (GetControlName(codeString) == GetControlName(s.Item2)) {
                    string numstr = codeString.Replace(GetControlName(codeString), "").Replace("<", "").Replace(":", "").Replace(">", "");
                    ushort HexY = Convert.ToUInt16(numstr.Substring(0, 4), 16);
                    ushort HexZ = Convert.ToUInt16(numstr.Substring(4, 4), 16);
                    ushort HexX = (ushort)(s.Item1 << 8 | ((HexY - 1) & 0xFF));
                    var result = new byte[6];
                    Array.Copy(BitConverter.GetBytes(HexX), 0, result, 0, 2);
                    Array.Copy(BitConverter.GetBytes(HexY), 0, result, 2, 2);
                    Array.Copy(BitConverter.GetBytes(HexZ), 0, result, 4, 2);
                    return result;
                }
            }
            #endregion
            #region Parse double
            foreach (var s in codes.DoubleCodes) {
                if (GetControlName(codeString).Contains("SHEET_TITLE_") && GetControlName(s.Item2).Contains("SHEET_TITLE_")) {
                    ushort HexX = (ushort)(s.Item1 << 8 | Convert.ToUInt16(GetControlName(codeString).Replace("SHEET_TITLE_", ""), 16));
                    string numstr = codeString.Replace(GetControlName(codeString), "").Replace("<", "").Replace(":", "").Replace(">", "");
                    ushort HexY = Convert.ToUInt16(numstr, 16);
                    var result = new byte[4];
                    Array.Copy(BitConverter.GetBytes(HexX), 0, result, 0, 2);
                    Array.Copy(BitConverter.GetBytes(HexY), 0, result, 2, 2);
                    return result;
                } else if (GetControlName(codeString) == GetControlName(s.Item2)) {
                    string numstr = codeString.Replace(GetControlName(codeString), "").Replace("<", "").Replace(":", "").Replace(">", "");
                    ushort HexY = Convert.ToUInt16(numstr, 16);
                    ushort HexX = (ushort)(s.Item1 << 8 | HexY - 1 & 0xFF);
                    var result = new byte[4];
                    Array.Copy(BitConverter.GetBytes(HexX), 0, result, 0, 2);
                    Array.Copy(BitConverter.GetBytes(HexY), 0, result, 2, 2);
                    return result;
                }
            }
            #endregion
            #region Parse single var
            foreach (var s in codes.SingleVarCodes) {
                if (s.Item2.Contains(":")) {
                    if (GetControlName(codeString) == GetControlName(s.Item2)) {
                        if (s.Item2.Contains("Mark")) {
                            ushort HexX = (ushort)(Convert.ToUInt16(codeString.Substring(6, 3), 16) | 0xA000);
                            return BitConverter.GetBytes(HexX);
                        } else {
                            string numstr = codeString.Replace(GetControlName(codeString), "").Replace("<", "").Replace(":", "").Replace(">", "");
                            ushort HexX = (ushort)(Convert.ToUInt16(numstr, 16) | s.Item1 << 8);
                            return BitConverter.GetBytes(HexX);
                        }
                    }
                }
            }
            #endregion

            return new byte[0];
        }
        public static string GetControlName(string fullpara)
        {
            return fullpara.Substring(0, fullpara.IndexOf(':')).Replace("<", "");
        }
    }
}
