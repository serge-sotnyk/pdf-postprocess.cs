using System.Collections.Generic;

namespace PdfPostprocessor.Common
{
    public static class EnumerateUtils
    {
        public static IEnumerable<(int, T)> Enumerate<T>(this IEnumerable<T> sequence)
        {
            int counter = 0;
            foreach(var item in sequence)
            {
                yield return (counter, item);
                ++counter;
            }
        }
    }
}
