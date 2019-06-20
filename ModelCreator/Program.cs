using Microsoft.ML;
using Microsoft.ML.Data;
using ModelCreator.Common;
using PdfPostprocessor;
using PdfPostprocessor.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static Microsoft.ML.DataOperationsCatalog;

namespace ModelCreator
{
    class Program
    {
        private static readonly string AppPath = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);

        private static readonly string CorpusPath = FileUtils.FindFolderInRoots("corpus", AppPath);

        private static readonly string ModelFileName = Path.GetFullPath(Path.Combine(CorpusPath, @"..\Models\PdfPostprocessModel.zip"));
        private static readonly string ModelDir = Path.GetDirectoryName(ModelFileName);


        // Program structure:
        // https://github.com/dotnet/machinelearning-samples/tree/master/samples/csharp/end-to-end-apps/MulticlassClassification-GitHubLabeler/GitHubLabeler/GitHubLabelerConsoleApp
        private static void Main(string[] args)
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
            var postprocessor = new Postprocessor(modelFileName);
            Console.WriteLine($"Model restored from '{modelFileName}'");

            Console.WriteLine();
            Console.WriteLine("Restored English text:");
            Console.WriteLine(postprocessor.RestoreText(EnText));
            Console.WriteLine();
            Console.WriteLine("Restored Russian text:");
            Console.WriteLine(postprocessor.RestoreText(RuText));
        }


        private static readonly string EnText = @"The rapid expansion of wireless services such as cellular voice, PCS
(Personal Communications Services), mobile data and wireless LANs
in recent years is an indication that signicant value is placed on accessibility
and portability as key features of telecommunication (Salkintzis and Mathiopoulos (Guest Ed.), 2000).
devices have maximum utility when they can be used any-
where at anytime"". One of the greatest limitations to that goal, how-
ever, is nite power supplies. Since batteries provide limited power, a
general constraint of wireless communication is the short continuous
operation time of mobile terminals. Therefore, power management is
y Corresponding Author: Dr.Krishna Sivalingam. Part of the research was
supported by Air Force Oce of Scientic Research grants F-49620-97-1-
0471 and F-49620-99-1-0125; by Telcordia Technologies and by Intel. Part of
the work was done while the rst author was at Washington State Univer-
sity.The authors' can be reached at cej@bbn.com, krishna@eecs.wsu.edu,
pagrawal @research.telcordia.com, jcchen @research.telcordia.com
c
2001 Kluwer Academic Publishers. Printed in the Netherlands.
Jones, Sivalingam, Agrawal and Chen
one of the most challenging problems in wireless communication, and
recent research has addressed this topic (Bambos, 1998). Examples include
a collection of papers available in (Zorzi (Guest Ed.), 1998) and
a recent conference tutorial (Srivastava, 2000), both devoted to energy
ecient design of wireless networks.
Studies show that the signicant consumers of power in a typical
laptop are the microprocessor (CPU), liquid crystal display (LCD),
hard disk, system memory (DRAM), keyboard/mouse, CDROM drive,
oppy drive, I/O subsystem, and the wireless network interface card
(Udani and Smith, 1996, Stemm and Katz, 1997). A typical example
from a Toshiba 410 CDT mobile computer demonstrates that nearly
36% of power consumed is by the display, 21% by the CPU/memory,
18% by the wireless interface, and 18% by the hard drive.Consequently,
energy conservation has been largely considered in the hardware design
of the mobile terminal (Chandrakasan and Brodersen, 1995) and in
components such as CPU, disks, displays, etc. Signicant additional
power savings may result by incorporating low-power strategies into
the design of network protocols used for data communication. This
paper addresses the incorporation of energy conservation at all layers
of the protocol stack for wireless networks.
The remainder of this paper is organized as follows. Section 2 introduces
the network architectures and wireless protocol stack considered
in this paper. Low-power design within the physical layer is brie
y
discussed in Section 2.3. Sources of power consumption within mobile
terminals and general guidelines for reducing the power consumed are
presented in Section 3. Section 4 describes work dealing with energy
ecient protocols within the MAC layer of wireless networks, and
power conserving protocols within the LLC layer are addressed in Section
5. Section 6 discusses power aware protocols within the network
layer. Opportunities for saving battery power within the transport
layer are discussed in Section 7. Section 8 presents techniques at the
OS/middleware and application layers for energy ecient operation.
Finally, Section 9 summarizes and concludes the paper.
2. Background
This section describes the wireless network architectures considered in
this paper. Also, a discussion of the wireless protocol stack is included
along with a brief description of each individual protocol layer. The
physical layer is further discussed. ";

        private static readonly string RuText = @"Метод опорных векторов предназначен для решения задач клас-
сификации путем поиска хороших решающих границ (рис. 1.10), 
разделяющих два набора точек, принадлежащих разным катего-
риям. Решающей границей может быть линия или поверхность, 
разделяющая выборку обучающих данных на пространства, при-
надлежащие двум категориям. Для классификации новых точек
достаточно только проверить, по какую сторону от границы они
находятся.
Поиск таких границ метод опорных векторов осуществляет в два
этапа:
1. Данные отображаются в новое пространство более высокой
размерности, где граница может быть представлена как гипер-
плоскость(если данные были двумерными, как на рис. 1.10,
гиперплоскость вырождается в линию).
2. Хорошая решающая граница (разделяющая гиперплоскость) вычисляется
путем максимизации расстояния от гиперплоскости до ближайших точек
каждого класса, этот этап называют максимизацией зазора. Это позволяет
обобщить классификацию новых образцов, не принадлежащих обучающему
набору данных.";
    }
}
