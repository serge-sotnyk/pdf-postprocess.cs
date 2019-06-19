using System;
using System.Collections.Generic;
using System.Text;

namespace PdfPostprocess.Common
{
    public static class StringUtils
    {
        private static string[] LineDelimiters = new[] { "\r\n", "\r", "\n" };

        public static string[] SplitLines(this string text)
        {
            return text.Split(LineDelimiters, StringSplitOptions.None);
        }
    }
}
