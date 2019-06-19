using Common.PdfPostprocess;
using Microsoft.ML;
using PdfPostprocess.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using static Microsoft.ML.TrainCatalogBase;

namespace PdfPostprocess
{
    class Program
    {
        private static string AppPath => Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);

        private static string CorpusPath = FileUtils.FindFolderInRoots("corpus", AppPath);

        private static string BaseModelsRelativePath = @"../../../../MLModels";
        private static string ModelRelativePath = $"{BaseModelsRelativePath}/GitHubLabelerModel.zip";
        private static string ModelPath = GetAbsolutePath(ModelRelativePath);

        public enum MyTrainerStrategy : int { SdcaMultiClassTrainer = 1, OVAAveragedPerceptronTrainer = 2 };

        // Program structure:
        // https://github.com/dotnet/machinelearning-samples/tree/master/samples/csharp/end-to-end-apps/MulticlassClassification-GitHubLabeler/GitHubLabeler/GitHubLabelerConsoleApp
        private static async Task Main(string[] args)
        {
            //1. ChainedBuilderExtensions and Train the model
            BuildAndTrainModel();
            /*
            //2. Try/test to predict a label for a single hard-coded Issue
            TestSingleLabelPrediction(ModelPath);
            */
            ConsoleHelper.ConsolePressAnyKey();
        }

        public static void BuildAndTrainModel()
        {
            // Create MLContext to be shared across the model creation workflow objects 
            // Set a random seed for repeatable/deterministic results across multiple trainings.
            var mlContext = new MLContext(seed: 1);
            var trainingDataView = mlContext.Data.LoadFromEnumerable(LoadCorpus());
            var dataProcessPipeline = //mlContext.Transforms.CopyColumns(outputColumnName: "Label", inputColumnName: nameof(CorrectionData.GlueWithPrevious))
                mlContext.Transforms.Conversion
                .MapValueToKey(outputColumnName: "Label", inputColumnName: nameof(CorrectionData.GlueWithPrevious))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding(nameof(PdfFeatures.FirstChars)))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding(nameof(PdfFeatures.PrevLastIsAlpha)))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding(nameof(PdfFeatures.PrevLastIsDigit)))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding(nameof(PdfFeatures.PrevLastIsLower)))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding(nameof(PdfFeatures.PrevLastIsPunct)))
                .Append(mlContext.Transforms.Concatenate("Features", nameof(PdfFeatures.ThisLen), nameof(PdfFeatures.MeanLen), nameof(PdfFeatures.PrevLen), 
                            nameof(PdfFeatures.FirstChars), nameof(PdfFeatures.PrevLastIsAlpha), nameof(PdfFeatures.PrevLastIsDigit), 
                            nameof(PdfFeatures.PrevLastIsLower), nameof(PdfFeatures.PrevLastIsPunct)))
                .AppendCacheCheckpoint(mlContext);
            ConsoleHelper.PeekDataViewInConsole(mlContext, trainingDataView, dataProcessPipeline, 2);

            IEstimator<ITransformer> trainer = mlContext.BinaryClassification.Trainers.LbfgsLogisticRegression();

            var trainingPipeline = dataProcessPipeline.Append(trainer);
                //.Append(mlContext.Transforms.Conversion.MapKeyToVector("PredictedLabel"));

            Console.WriteLine("=============== Cross-validating to get model's accuracy metrics ===============");
            var crossValidationResults = 
                mlContext.MulticlassClassification.CrossValidate(
                    data: trainingDataView, 
                    estimator: trainingPipeline, 
                    numberOfFolds: 6, 
                    labelColumnName: nameof(CorrectionData.GlueWithPrevious));

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

        /*
        public static void BuildAndTrainModelOriginal(string DataSetLocation, string ModelPath, MyTrainerStrategy selectedStrategy)
        {
            // Create MLContext to be shared across the model creation workflow objects 
            // Set a random seed for repeatable/deterministic results across multiple trainings.
            var mlContext = new MLContext(seed: 1);

            // STEP 1: Common data loading configuration
            var trainingDataView = mlContext.Data.LoadFromTextFile<GitHubIssue>(DataSetLocation, hasHeader: true, separatorChar: '\t', allowSparse: false);

            // STEP 2: Common data process configuration with pipeline data transformations
            var dataProcessPipeline = mlContext.Transforms.Conversion.MapValueToKey(outputColumnName: "Label", inputColumnName: nameof(GitHubIssue.Area))
                            .Append(mlContext.Transforms.Text.FeaturizeText(outputColumnName: "TitleFeaturized", inputColumnName: nameof(GitHubIssue.Title)))
                            .Append(mlContext.Transforms.Text.FeaturizeText(outputColumnName: "DescriptionFeaturized", inputColumnName: nameof(GitHubIssue.Description)))
                            .Append(mlContext.Transforms.Concatenate(outputColumnName: "Features", "TitleFeaturized", "DescriptionFeaturized"))
                            .AppendCacheCheckpoint(mlContext);
            // Use in-memory cache for small/medium datasets to lower training time. 
            // Do NOT use it (remove .AppendCacheCheckpoint()) when handling very large datasets.

            // (OPTIONAL) Peek data (such as 2 records) in training DataView after applying the ProcessPipeline's transformations into "Features" 
            Common.ConsoleHelper.PeekDataViewInConsole(mlContext, trainingDataView, dataProcessPipeline, 2);

            // STEP 3: Create the selected training algorithm/trainer
            IEstimator<ITransformer> trainer = null;
            switch (selectedStrategy)
            {
                case MyTrainerStrategy.SdcaMultiClassTrainer:
                    trainer = mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy("Label", "Features");
                    break;
                case MyTrainerStrategy.OVAAveragedPerceptronTrainer:
                    {
                        // Create a binary classification trainer.
                        var averagedPerceptronBinaryTrainer = mlContext.BinaryClassification.Trainers.AveragedPerceptron("Label", "Features", numberOfIterations: 10);
                        // Compose an OVA (One-Versus-All) trainer with the BinaryTrainer.
                        // In this strategy, a binary classification algorithm is used to train one classifier for each class, "
                        // which distinguishes that class from all other classes. Prediction is then performed by running these binary classifiers, "
                        // and choosing the prediction with the highest confidence score.
                        trainer = mlContext.MulticlassClassification.Trainers.OneVersusAll(averagedPerceptronBinaryTrainer);

                        break;
                    }
                default:
                    break;
            }

            //Set the trainer/algorithm and map label to value (original readable state)
            var trainingPipeline = dataProcessPipeline.Append(trainer)
                    .Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

            // STEP 4: Cross-Validate with single dataset (since we don't have two datasets, one for training and for evaluate)
            // in order to evaluate and get the model's accuracy metrics

            Console.WriteLine("=============== Cross-validating to get model's accuracy metrics ===============");
            var crossValidationResults = mlContext.MulticlassClassification.CrossValidate(data: trainingDataView, estimator: trainingPipeline, numberOfFolds: 6, labelColumnName: "Label");

            ConsoleHelper.PrintMulticlassClassificationFoldsAverageMetrics(trainer.ToString(), crossValidationResults);

            // STEP 5: Train the model fitting to the DataSet
            Console.WriteLine("=============== Training the model ===============");
            var trainedModel = trainingPipeline.Fit(trainingDataView);

            // (OPTIONAL) Try/test a single prediction with the "just-trained model" (Before saving the model)
            GitHubIssue issue = new GitHubIssue() { ID = "Any-ID", Title = "WebSockets communication is slow in my machine", Description = "The WebSockets communication used under the covers by SignalR looks like is going slow in my development machine.." };
            // Create prediction engine related to the loaded trained model
            var predEngine = mlContext.Model.CreatePredictionEngine<GitHubIssue, GitHubIssuePrediction>(trainedModel);
            //Score
            var prediction = predEngine.Predict(issue);
            Console.WriteLine($"=============== Single Prediction just-trained-model - Result: {prediction.Area} ===============");
            //

            // STEP 6: Save/persist the trained model to a .ZIP file
            Console.WriteLine("=============== Saving the model to a file ===============");
            mlContext.Model.Save(trainedModel, trainingDataView.Schema, ModelPath);

            Common.ConsoleHelper.ConsoleWriteHeader("Training process finalized");
        }*/

        public static string GetAbsolutePath(string relativePath)
        {
            FileInfo _dataRoot = new FileInfo(typeof(Program).Assembly.Location);
            string assemblyFolderPath = _dataRoot.Directory.FullName;

            string fullPath = Path.Combine(assemblyFolderPath, relativePath);

            return fullPath;
        }
    }
}
