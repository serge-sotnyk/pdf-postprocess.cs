using System;
using System.Collections.Generic;
using System.Text;

namespace PdfPostprocessor.Common
{
    public static class StringUtils
    {
        private static string[] LineDelimiters = new[] { "\r\n", "\r", "\n" };

        public static ISet<char> HYPHEN_CHARS = new HashSet<char>{
            '\u002D',  // HYPHEN-MINUS
            '\u00AD',  // SOFT HYPHEN
            '\u2010',  // HYPHEN
            '\u2011',  // NON-BREAKING HYPHEN
        };

        public static string[] SplitLines(this string text)
        {
            return text.Split(LineDelimiters, StringSplitOptions.None);
        }
    }
}
