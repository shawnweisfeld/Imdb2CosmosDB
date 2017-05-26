using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImdbLoader2.Util
{
    public static class ConsoleEx
    {
        public static void WriteLineRed(String message)
        {
            ConsoleEx.WriteLine(message, ConsoleColor.Red);
        }

        public static void WriteLineGreen(String message)
        {
            ConsoleEx.WriteLine(message, ConsoleColor.Green);
        }

        public static void WriteLineYellow(String message)
        {
            ConsoleEx.WriteLine(message, ConsoleColor.Yellow);
        }

        public static void WriteLine(string message, ConsoleColor color)
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ForegroundColor = oldColor;
        }
    }
}
