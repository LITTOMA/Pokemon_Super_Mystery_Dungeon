using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;

namespace CEED.Utils
{
    class StructWriter
    {
        public static void WriteStruct(Stream fs,object structure)
        {
            var buffer = new byte[Marshal.SizeOf(structure)];
            var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            Marshal.StructureToPtr(structure, handle.AddrOfPinnedObject(), true);
            fs.Write(buffer, 0, buffer.Length);
        }
    }
}
