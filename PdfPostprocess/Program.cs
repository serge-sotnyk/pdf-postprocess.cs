using Common.PdfPostprocess;
using Microsoft.ML;
using Microsoft.ML.Data;
using PdfPostprocess.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using static Microsoft.ML.DataOperationsCatalog;
using static Microsoft.ML.TrainCatalogBase;

namespace PdfPostprocess
{
    class Program
    {
        private static readonly string AppPath = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);

        private static readonly string CorpusPath = FileUtils.FindFolderInRoots("corpus", AppPath);

        private static readonly string ModelFileName = Path.GetFullPath(Path.Combine(CorpusPath, @"..\Models\PdfPostprocessModel.zip"));
        private static readonly string ModelDir = Path.GetDirectoryName(ModelFileName);


        // Program structure:
        // https://github.com/dotnet/machinelearning-samples/tree/master/samples/csharp/end-to-end-apps/MulticlassClassification-GitHubLabeler/GitHubLabeler/GitHubLabelerConsoleApp
        private static async Task Main(string[] args)
        {
            //1. ChainedBuilderExtensions and Train the model
            BuildAndTrainModel();
            
            //2. Try/test to predict a label for a single hard-coded Issue
            LoadAndTestModel(ModelFileName);
            
            ConsoleHelper.ConsolePressAnyKey();
        }

        public static void BuildAndTrainModel()
        {
            // Create MLContext to be shared across the model creation workflow objects 
            // Set a random seed for repeatable/deterministic results across multiple trainings.
            var mlContext = new MLContext(seed: 1);
            var dataView = mlContext.Data.LoadFromEnumerable(LoadCorpus());
            TrainTestData trainTestSplit = mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);
            IDataView trainingData = trainTestSplit.TrainSet;
            IDataView testData = trainTestSplit.TestSet;

            var dataProcessPipeline = mlContext.Transforms.CopyColumns(outputColumnName: "Label", inputColumnName: nameof(CorrectionData.GlueWithPrevious))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding(nameof(PdfFeatures.FirstChars)))
                .Append(mlContext.Transforms.Conversion.ConvertType(new[]
                {
                    new InputOutputColumnPair(nameof(PdfFeatures.PrevLastIsAlpha)),
                    new InputOutputColumnPair(nameof(PdfFeatures.PrevLastIsDigit)),
                    new InputOutputColumnPair(nameof(PdfFeatures.PrevLastIsLower)),
                    new InputOutputColumnPair(nameof(PdfFeatures.PrevLastIsPunct)),
                }, DataKind.Single))
                .Append(mlContext.Transforms.Concatenate("Features", nameof(PdfFeatures.ThisLen), nameof(PdfFeatures.MeanLen), nameof(PdfFeatures.PrevLen), 
                            nameof(PdfFeatures.FirstChars), nameof(PdfFeatures.PrevLastIsAlpha), nameof(PdfFeatures.PrevLastIsDigit), 
                            nameof(PdfFeatures.PrevLastIsLower), nameof(PdfFeatures.PrevLastIsPunct)))
                .AppendCacheCheckpoint(mlContext);
            ConsoleHelper.PeekDataViewInConsole(mlContext, trainingData, dataProcessPipeline, 2);

            IEstimator<ITransformer> trainer = mlContext.BinaryClassification.Trainers.LbfgsLogisticRegression();

            var trainingPipeline = dataProcessPipeline.Append(trainer);
            //.Append(mlContext.Transforms.Conversion.MapKeyToVector("PredictedLabel"));

            // STEP 4: Train the model fitting to the DataSet
            ITransformer trainedModel = trainingPipeline.Fit(trainingData);

            // STEP 5: Evaluate the model and show accuracy stats
            var predictions = trainedModel.Transform(testData);
            var metrics = mlContext.BinaryClassification.Evaluate(data: predictions, labelColumnName: "Label", scoreColumnName: "Score");
            ConsoleHelper.PrintBinaryClassificationMetrics(trainer.ToString(), metrics);

            // STEP 6: Save/persist the trained model to a .ZIP file
            if (!Directory.Exists(ModelDir))
                Directory.CreateDirectory(ModelDir);
            mlContext.Model.Save(trainedModel, trainingData.Schema, ModelFileName);
            Console.WriteLine($"Model has been written into '{ModelFileName}'");
        }

        private static IEnumerable<CorrectionData> LoadCorpus()
        {
            var res = new List<CorrectionData>();
            foreach(string fn in Directory.EnumerateFiles(CorpusPath, "*.txt"))
            {
                var lines = File.ReadAllText(fn);
                var firstChar = lines[0];
                if (firstChar=='*' || firstChar == '+')
                {
                    Console.WriteLine($"File '{fn}' has annotations, process it.");
                    var featurizedText = Vectorizer.FeaturizeTextWithAnnotation(lines);
                    res.AddRange(featurizedText);
                }
                else
                {
                    Console.WriteLine($"File '{fn}' doesn't have annotations, skipped.");
                }
            }
            return res;
        }

        private static void LoadAndTestModel(string modelFileName)
        {
            throw new NotImplementedException();
        }
    }
}
