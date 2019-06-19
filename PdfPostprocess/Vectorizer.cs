using System;
using System.Collections.Generic;
using System.Text;
using static PdfPostprocess.Common.StringUtils;
using static PdfPostprocess.Common.EnumerateUtils;
using static System.Math;
using static PdfPostprocess.Common.CloneUtils;

namespace PdfPostprocess
{
    public static class Vectorizer
    {
        public static IList<CorrectionData> FeaturizeTextWithAnnotation(string text)
        {
            var lines = text.Trim().SplitLines();
            var res = new List<CorrectionData>();
            foreach (var (i, line) in lines.Enumerate())
            {
                var txt_line = line.Substring(1);
                var features = LineToFeatures(txt_line, i, lines);
                var featuresWithLabels = ConstructAndFill<PdfFeatures, CorrectionData>(features);
                featuresWithLabels.GlueWithPrevious = line[0] == '+'; // True, if line should be glued with previous
                res.Add(featuresWithLabels);
            }
            return res;
        }

        private static char LastChar(string line) 
            => string.IsNullOrEmpty(line) ? ' ' : line[line.Length - 1];

        private static PdfFeatures LineToFeatures(string line, int i, string[] lines)
        {
            var thisLen = line.Length;
            var meanLen = MeanInWindow(lines, i);
            int prevLen = 0;
            char lastPrevChar = ' ';
            if (i > 0) {
                prevLen = lines[i - 1].Length - 1;
                lastPrevChar = LastChar(lines[i - 1]);
            }
            var features = new PdfFeatures
            {
                ThisLen = thisLen,
                MeanLen = meanLen,
                PrevLen = prevLen,
                FirstChars = FirstChars(line),
            };
            UpdateLastCharFeatures(features, lastPrevChar);
            return features;
        }

        private static void UpdateLastCharFeatures(PdfFeatures pdfFeatures, char lastChar)
        {
            pdfFeatures.PrevLastIsAlpha = char.IsLetter(lastChar);
            pdfFeatures.PrevLastIsDigit = char.IsDigit(lastChar);
            pdfFeatures.PrevLastIsLower = char.IsLower(lastChar);
            pdfFeatures.PrevLastIsPunct = char.IsPunctuation(lastChar);
        }

        private static string FirstChars(string line)
        {
            string chars;
            if (string.IsNullOrEmpty(line))
                chars = " ";
            else if (line.Length < 2)
                chars = line.Substring(0, 1);
            else
                chars = line.Substring(0, 2);
            var res = new StringBuilder();
            foreach (var c in chars)
            {
                if (char.IsDigit(c))
                    res.Append('0');
                else if (char.IsLetter(c))
                    res.Append(char.IsLower(c) ? 'a' : 'A');
                else
                    res.Append(c);
            }
            return res.ToString();
        }

        private static float MeanInWindow(string[] lines, int i)
        {
            var start = Max(i - 5, 0);
            var finish = Min(i + 5, lines.Length - 1);
            var sm = 0;
            var count = 0;
            for (int n = start; n < finish; ++n) {
                sm += lines[n].Length - 1; //  # minus one-char prefix
                count += 1;
            }
            return sm / Max(count, 1);
        }
    }
}
