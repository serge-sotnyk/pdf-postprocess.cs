using System;
using System.Collections.Generic;
using System.Text;

namespace Common.PdfPostprocess
{
    public static class ConsoleHelper
    {
        public static void ConsolePressAnyKey()
        {
            var defaultColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(" ");
            Console.WriteLine("Press any key to finish.");
            Console.ReadKey();
        }
    }
}
