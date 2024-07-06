using Dalamud.Game.ClientState.JobGauge.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MitView
{
    internal class Utils
    {
        public static unsafe string ReadString(byte* ptr)
        {
            var stringBuilder = new StringBuilder();

            for (var i = 0; ptr[i] != 0; ++i)
                stringBuilder.Append(Convert.ToChar(ptr[i]));

            return stringBuilder.ToString();
        }
        public static unsafe int ByteLength(byte* ptr)
        {
            int length = 0;
            for (; ptr[length] != 0; ++length) ;
            return length;
        }
        public static unsafe byte[] BytePtrtoArray(byte* ptr)
        {
            int length = ByteLength(ptr);
            byte[] arr = new byte[length];
            Marshal.Copy((IntPtr)ptr, arr, 0, length);
            return arr;
        }
    }
}
