using System;
using System.Collections.Generic;
using System.Text;

namespace SolutionValidator
{
    public static class AnsiColourExtensions
    {
        public static string Red(this string text)
        {
            return "\u001b[31;1m" + text + "\u001b[37;1m";
        }
        public static string Green(this string text)
        {
            return "\u001b[32;1m" + text + "\u001b[37;1m";
        }
    }
}
