using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ImdbLoader2.Util
{
    public static class StringEx
    {
        public static string Clean(this string value)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var letter in value.ToCharArray())
            {
                if (char.IsLetterOrDigit(letter) || char.IsWhiteSpace(letter))
                {
                    sb.Append(letter);
                }
            }

            var str = sb.ToString().Trim();

            str = str.Replace("\t", " ");

            while (str.IndexOf("  ") > -1)
            {
                str = str.Replace("  ", " ");
            }

            return str;
        }

        public static string TrimBoth(this string input, int length = 1)
        {
            if (input.Length < (length * 2))
                return input;

            return input.Substring(length, input.Length - (length * 2));
        }

        public static string Find(this string input, string pattern)
        {
            foreach (Match match in Regex.Matches(input, pattern))
            {
                return match.Value.Trim();
            }

            return string.Empty;
        }

    }
}
