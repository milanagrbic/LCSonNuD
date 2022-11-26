using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;

namespace BSCsharp
{
    class Program
    {
        static void Main(string[] args)
        {

            if (args.Length != 7)
            {
                Console.WriteLine("Usage [beamSize] [filterCount] [heuristic RND/UB/EX/PSUM/GM/GMPSUMXX] [probs mous/smplN/specN] [inputFilePath] [outputFilePath] [maxTimeSeconds]");
                System.Environment.Exit(1);
            }
            var beamSize = Convert.ToInt32(args[0]);
            var filterCount = Convert.ToInt32(args[1]);
            var heurName = args[2];
            var probs = args[3];
            var inputFilePath = args[4];
            var outputFilePath = args[5];
            var maxTime = Convert.ToInt32(args[6]);

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var inputStrings = File.ReadAllLines(inputFilePath, Encoding.GetEncoding("Windows-1252")).Skip(1).Select(x => x.Split()[1]).ToArray();
            var matrixGenerator = new ProbMatrixGenerator(inputStrings, 11111);
            double[][] subseqProbs = null;
            double[][] subseqProbsPerString = null;
            if (probs == "mous")
                subseqProbs = matrixGenerator.CalculateMousaviMatrix();
            else if (probs.StartsWith("smpl"))
            {
                var N = Convert.ToInt32(probs.Replace("smpl", ""));
                subseqProbs = matrixGenerator.CalculateProbMatrixGram(N, false);
            }
            else if (probs.StartsWith("spec"))
            {
                var N = Convert.ToInt32(probs.Replace("spec", ""));
                subseqProbsPerString = matrixGenerator.CalculateSpecificProbMatricesGram(N, false);
            }
            else
                throw new Exception("Probability matrix generation with name " + probs + " is not supported!");

            var bclcs = new BSLCS(beamSize, inputStrings, heurName, filterCount,maxTime, outputFilePath, String.Join(' ', args), subseqProbs: subseqProbs, subseqProbsPerString: subseqProbsPerString);
            bclcs.Search();
        }
    }
}
