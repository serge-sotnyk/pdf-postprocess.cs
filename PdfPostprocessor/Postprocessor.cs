using Microsoft.ML;
using Microsoft.ML.Data;
using PdfPostprocessor.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace PdfPostprocessor
{
    public class Postprocessor
    {
        private MLContext _mlContext;
        private ITransformer _model;


        public Postprocessor()
        {
            // To aid in debugging assembly.GetManifestResourceNames() will return the names of all the embedded resources in an assembly.
            // https://sedders123.me/2017/08/30/read-an-embedded-resource-dotnet-standard-20/
            var assembly = typeof(Postprocessor).GetTypeInfo().Assembly;
            using (var stream = assembly.GetManifestResourceStream("PdfPostprocessor.PdfPostprocessModel.zip"))
            {
                InitModel(stream);
            }
        }

        public Postprocessor(Stream modelStream)
        {
            InitModel(modelStream);
        }

        public Postprocessor(string modelFilename)
        {
            using (var stream = File.Open(modelFilename, FileMode.Open))
                InitModel(stream);
        }

        private void InitModel(Stream modelStream)
        {
            _mlContext = new MLContext(seed: 1);
            _model = _mlContext.Model.Load(modelStream, out var inputSchema);
        }

        public string RestoreText(string text)
        {
            var (features, lines) = Vectorizer.FeaturizeTextWoAnnotation(text);
            var dataView = _mlContext.Data.LoadFromEnumerable(features);
            var predictions = _model.Transform(dataView).GetColumn<bool>("PredictedLabel").ToList();
            var restored = Restore(lines, predictions);
            return restored;
        }

        private static string Restore(IReadOnlyList<string> lines, IReadOnlyList<bool> predictions)
        {
            var res = new StringBuilder();
            for (var lineNum = 0; lineNum < lines.Count; ++lineNum)
            {
                var line = lines[lineNum];
                if (lineNum > 0)
                    if (!predictions[lineNum])
                        res.Append(Environment.NewLine);
                if (lineNum + 1 < lines.Count)
                {
                    if (predictions[lineNum + 1])
                        line = PrepareLineEndToGluing(line);
                }
                res.Append(line);
            }
            return res.ToString();
        }

        private static string PrepareLineEndToGluing(string line)
        {
            if (line.Length > 0)
            {
                if (StringUtils.HYPHEN_CHARS.Contains(line[line.Length - 1]))
                    line = line.Substring(0, line.Length - 1);
                else
                    line += ' ';
            }
            return line;
        }
    }
}
