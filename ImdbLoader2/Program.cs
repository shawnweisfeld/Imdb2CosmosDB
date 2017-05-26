using ImdbLoader2.Util;
using PowerArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImdbLoader2
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Args.InvokeMain<MyArgs>(args);
            }
            catch (Exception ex)
            {
                ConsoleEx.WriteLineRed(ex.ToString());
            }
        }
    }
}
