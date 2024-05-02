using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GearsetHelper
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

    }
}
