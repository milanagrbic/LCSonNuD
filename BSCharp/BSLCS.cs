using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace BSCsharp
{
    class BSLCS
    {
        //beam things
        private int beamSize;
        private int minBeamSize = 100;
        private List<BSLCSItem> beam;
        private string heurName;
        private Heuristic heur;
        private int filterCount;
        private int maxTime;

        //data things
        private string[] inputStrings;
        private char[] alphabet;
        private double[][] subseqProbs;
        private double[][] subseqProbsPerString;
        private double[] exponents;
        private double[][][] countsPerStringPerPosition;
        private double[][][] logCountsPerStringPerPosition;

        //other things
        private Random rg = new Random(11111);
        private TimeSpan bsElapsedTime;
        private DateTime tstart = DateTime.Now;
        private int iteration = 0;
        private int heurCalls = 0;
        private double maxLB = 0;
        private string outputFilePath;
        private string cmdArg;
        private string statString;
        private double gmFract = 0.5; //in gmpsum heur default importance of gm is 50%

        //dynamic beam adjustment things
        private double lastIterationTime;
        private int remainingIterationsEstimate;

        public BSLCS(int beamSize, string[] inputStrings, string heurName, int filterCount, int maxTime, string outputFilePath, string cmdArg, double[][] subseqProbs = null, double[][] subseqProbsPerString = null)
        {
            this.beamSize = beamSize;
            this.maxTime = maxTime;
            this.inputStrings = inputStrings;
            this.filterCount = filterCount;
            this.cmdArg = cmdArg;
            this.outputFilePath = outputFilePath;
            this.subseqProbs = subseqProbs;
            this.subseqProbsPerString = subseqProbsPerString;
            var alphabet = new SortedSet<char>();
            foreach (var str in inputStrings)
                foreach (var c in str)
                    alphabet.Add(c);
            this.alphabet = alphabet.ToArray();
            CalculateCounts();
            CalculateStats();
            this.heurName = heurName;
            if (heurName == "RND")
                heur = HeuristicRandom;
            else if (heurName == "UB")
                heur = HeuristicUB;
            else if (heurName == "PSUM")
                heur = HeuristicPSUM;
            else if (heurName == "GM")
                heur = HeuristicGM;
            else if (heurName.StartsWith("GMPSUM"))
            {
                if (heurName != "GMPSUM")
                {
                    int perc;
                    var succ = Int32.TryParse(heurName.Replace("GMPSUM", ""), out perc);
                    if (succ)
                        gmFract = perc / 100.0;
                    else
                        throw new Exception("When using GMPSUM heuristic, last characters should form a number that represents GM percentage importance.");
                }
                heur = HeuristicGMPSUM;
            }
            else if (heurName == "EX")
            {
                heur = HeuristicEX;
                if (subseqProbs == null && subseqProbsPerString == null)
                    throw new Exception("When using EX heuristic, you must supply non nullable subseqProbs or subseqProbsPerString argument!");
                //var freq = Utils.Frequencies(inputStrings, new int[inputStrings.Length]).Values.ToArray();
                //exponents = ExponentsFrequencies(freq);
                exponents = ExponentsSimple();
            }
            else
                throw new Exception("Heuristic " + heurName + " is not defined!");
        }

        public double MaxLB
        {
            get { return maxLB; }
        }

        public string HeurName
        {
            set { this.heurName = value; }
        }
        private double[] ExponentsSimple()
        {
            var maxLCS = inputStrings.Min(x => x.Length);
            exponents = new double[maxLCS + 1];
            for (int k = 0; k <= maxLCS; k++)
                exponents[k] = Math.Pow(alphabet.Length, k);
            return exponents;
        }
        private void CalculateCounts()
        {
            countsPerStringPerPosition = new double[inputStrings.Length][][];
            logCountsPerStringPerPosition = new double[inputStrings.Length][][];
            for (int i = 0; i < inputStrings.Length; i++)
            {
                var str = inputStrings[i];
                countsPerStringPerPosition[i] = new double[inputStrings[i].Length][];
                logCountsPerStringPerPosition[i] = new double[inputStrings[i].Length][];
                for (int j = 0; j < str.Length; j++)
                {
                    var substr = str.Substring(j);
                    var counts = Utils.Counts(substr);
                    countsPerStringPerPosition[i][j] = new double[alphabet.Length];
                    logCountsPerStringPerPosition[i][j] = new double[alphabet.Length];
                    for (int k = 0; k < alphabet.Length; k++)
                    {
                        var c = alphabet[k];
                        if (!counts.ContainsKey(c))
                        {
                            countsPerStringPerPosition[i][j][k] = 0;
                            logCountsPerStringPerPosition[i][j][k] = -1;
                        }
                        else
                        {
                            countsPerStringPerPosition[i][j][k] = counts[c];
                            logCountsPerStringPerPosition[i][j][k] = Math.Log(counts[c]);
                        }
                    }
                }
            }
        }
        private void CalculateStats()
        {
            var sb = new StringBuilder();
            var sim = 0.0;
            for (int k = 0; k < alphabet.Length; k++)
            {
                var geomMean = 0.0;
                var geomSD = 0.0;
                var undefined = false;
                var minc = Double.MaxValue;
                for (int i = 0; i < inputStrings.Length; i++)
                {
                    var logCnt = logCountsPerStringPerPosition[i][0][k];
                    if (logCnt == -1)
                    {
                        undefined = true;
                        minc = 0;
                        break;
                    }
                    var cnt = countsPerStringPerPosition[i][0][k];
                    if (cnt < minc)
                        minc = cnt;

                    geomMean += logCnt;
                }

                if (!undefined)
                {
                    geomMean = Math.Exp(geomMean / inputStrings.Length);
                    geomSD = 0.0;
                    var logGeomMean = Math.Log(geomMean);
                    for (int i = 0; i < inputStrings.Length; i++)
                    {
                        var logCnt = logCountsPerStringPerPosition[i][0][k];
                        logCnt -= logGeomMean;
                        geomSD += logCnt * logCnt;
                    }
                    geomSD = Math.Exp(geomSD / inputStrings.Length);
                    var adjustedGM = geomMean / Math.Pow(geomSD, 1);
                    sim += adjustedGM;
                }
                sb.Append(String.Format("s({0})=({1} {2:0.00} {3:0.00}) ", alphabet[k], minc, geomMean, geomSD));
            }
            sb.Append(String.Format("Sim={0:0.00}", sim));
            statString = sb.ToString();
        }
        public void EvaluateSolution(string currSol)
        {
            var nextPos = new int[inputStrings.Length];
            foreach (var c in currSol)
            {
                for (int i = 0; i < inputStrings.Length; i++)
                {
                    var found = false;
                    for (int p = nextPos[i]; p < inputStrings[i].Length; p++)
                        if (inputStrings[i][p] == c)
                        {
                            found = true;
                            nextPos[i] = p + 1;
                            break;
                        }
                    if (!found)
                        throw new Exception("Infeasible current solution w.r.t. string " + inputStrings[i]);
                }
            }
            var heurValue = this.heur(currSol, nextPos);
            Console.WriteLine(new BSLCSItem(currSol, heurValue, nextPos));
        }
        private int[] FindNextPos(BSLCSItem item, char c)
        {
            var newNextPos = new int[inputStrings.Length];
            for (int i = 0; i < inputStrings.Length; i++)
            {
                var str = inputStrings[i];
                //looking for the first occurence of character c starting from the first free position
                var foundChar = false;
                for (int p = item.NextPos[i]; p < str.Length; p++)
                    if (str[p] == c)
                    {
                        newNextPos[i] = p + 1;
                        foundChar = true;
                        break;
                    }
                if (!foundChar)
                    return null;
            }
            return newNextPos;
        }
        internal string DoBS()
        {
            var sw = new Stopwatch();
            sw.Start();
            beam = new List<BSLCSItem>();
            //first solution 
            var sol = "";
            //starting from the position 0 in all input strings
            var nextPos = new int[inputStrings.Length];
            var initHeur = heur(sol, nextPos);

            Console.WriteLine("Initial heur is {0}", initHeur);
            beam.Add(new BSLCSItem(sol, 0, nextPos));
            var filtered = 0;
            var filteredTried = 0;
            //main loop
            while (true)
            {
                iteration++;
                var tIterationStart = DateTime.Now;
                var newBeam = new List<BSLCSItem>();
                foreach (var item in beam)
                {
                    //extending by each character
                    foreach (var c in alphabet)
                    {

                        var newNextPos = FindNextPos(item, c);
                        //if all input strings are extended, then it is ok, oterwise no extension for this character
                        if (newNextPos != null)
                        {
                            var newSol = item.Sol + c;
                            var heurVal = heur(newSol, newNextPos);
                            var lb = LB(newSol, newNextPos) + newSol.Length;
                            if (lb > maxLB)
                                maxLB = lb;
                            var newItem = new BSLCSItem(newSol, heurVal, newNextPos);
                            newBeam.Add(newItem);

                            heurCalls++;
                            if (heurCalls % 10000 == 0)
                            {
                                var ub = UB(newSol, newNextPos);
                                Console.WriteLine("{0}. T={1:0}  LEN={2}  H1={3:0} MAXLB={4}  UB={5}", heurCalls, (DateTime.Now - tstart).TotalSeconds, newSol.Length, heurVal, maxLB, ub + newSol.Length);
                            }
                        }
                    }
                }

                if (newBeam.Count == 0)
                    break;
                newBeam.Sort();
                //now filtering dominated extensions

                if (filterCount > 0)
                {
                    for (int i = (int)Math.Min(newBeam.Count - 1, beamSize); i >= 0; i--)
                    {
                        filteredTried++;
                        for (int k = 0; k < Math.Min(filterCount, newBeam.Count); k++)
                        {
                            if (i == k)
                                continue;
                            if (newBeam[k].Dominates(newBeam[i]))
                            {
                                filtered++;
                                newBeam.RemoveAt(i);
                                break;
                            }
                        }
                    }
                }
                lastIterationTime = (DateTime.Now - tIterationStart).TotalSeconds;
                RecalculateBeamSize();
                beam = newBeam.Take(beamSize).ToList();
            }

            beam.Sort();
            var finalSol = beam[0].Sol;

            Console.Out.WriteLine("Filtered {0:0.00}% ({1})", filtered * 100.0 / (filteredTried + 1), filtered);
            sw.Stop();
            bsElapsedTime = sw.Elapsed;
            return finalSol;
        }

        private void RecalculateBeamSize()
        {
            if (maxTime == -1 || beam.Count!=beamSize)
                return;
            remainingIterationsEstimate = (int)maxLB - iteration;
            var remainingIterationsTimeEstimate = remainingIterationsEstimate * lastIterationTime;
            var remainingTime = (tstart.AddSeconds(maxTime) - DateTime.Now).TotalSeconds;
            Console.WriteLine("Remaining time is {0} while remaining time estimate for finishing iterations is {1}. ", remainingTime, remainingIterationsTimeEstimate);
            if (remainingIterationsTimeEstimate > remainingTime * 1.1)
                beamSize = (int)(beamSize/1.2);
            else if (remainingIterationsTimeEstimate < remainingTime * 0.9)
                beamSize = (int)(beamSize * 1.2);
            if (beamSize < minBeamSize)
                beamSize = minBeamSize;
            Console.WriteLine("Beam size for the next iteration is {0}.", beamSize);
        }
        public string Search()
        {
            var solution = DoBS();
            ValidateAndPrint(solution);
            //File.WriteAllText("tmp",String.Join(Environment.NewLine, remainingLengthsMap));
            return solution;
        }

        internal void ValidateAndPrint(string solution)
        {
            foreach (var str in inputStrings)
                if (!IsSub(solution, str))
                    throw new Exception("Error, final item is not substring of all input strings!");
            var output = cmdArg + Environment.NewLine + solution.Length + Environment.NewLine + solution.ToString() + Environment.NewLine + "BS elapsed time (s) " + bsElapsedTime.TotalSeconds + Environment.NewLine + "Stat: " + statString;
            Console.WriteLine(output);
            var fileOutput = cmdArg + "," + heurName + "," + inputStrings.Length + "," + inputStrings.Average(x => x.Length) + "," + alphabet.Length +/*","+ statString+*/"," + solution.Length + "," + bsElapsedTime.TotalSeconds + "," + solution + Environment.NewLine;
            File.AppendAllText(outputFilePath, fileOutput);
        }
        private bool IsSub(string sub, string str)
        {
            int k = 0;
            for (int i = 0; i < str.Length && k < sub.Length; i++)
                if (sub[k] == str[i])
                    k++;
            return k == sub.Length;
        }
        public double UB(string sol, int[] pos)
        {
            var mincSum = 0.0;
            for (int k = 0; k < alphabet.Length; k++)
            {
                var minc = Double.MaxValue;
                for (int i = 0; i < inputStrings.Length; i++)
                {
                    if (pos[i] >= inputStrings[i].Length)
                        break;
                    else
                    {
                        var cnt = countsPerStringPerPosition[i][pos[i]][k];
                        minc = Math.Min(minc, cnt);
                        if (minc == 0)
                            break;
                    }
                }
                if (minc == 0)
                    continue;
                mincSum += minc;
            }
            var ub = mincSum;
            return ub;
        }
        public double LB(string sol, int[] pos)
        {
            var maxMinc = 0.0;

            for (int k = 0; k < alphabet.Length; k++)
            {
                var minc = Double.MaxValue;
                for (int i = 0; i < inputStrings.Length; i++)
                {
                    if (pos[i] >= inputStrings[i].Length)
                    {
                        minc = 0;
                        break;
                    }
                    else
                    {
                        var cnt = countsPerStringPerPosition[i][pos[i]][k];
                        minc = Math.Min(minc, cnt);
                        if (minc == 0)
                            break;
                    }
                }
                if (minc > maxMinc)
                    maxMinc = minc;
            }
            var lb = maxMinc;
            return lb;
        }

        public delegate double Heuristic(string sol, int[] pos);
        public double HeuristicRandom(string sol, int[] pos)
        {
            return rg.NextDouble();
        }

        public double HeuristicUB(string sol, int[] pos)
        {
            return UB(sol, pos);
        }
        public double HeuristicPSUM(string sol, int[] pos)
        {

            var lmax = pos.Zip(inputStrings).Min(x => x.Second.Length - x.First);
            double psum = 0;

            for (int k = 1; k <= lmax; k++)
            {
                var prod = 1.0;
                //var controlProd = 0.0;
                if (subseqProbsPerString != null)
                {
                    var kSubseqProbs = subseqProbsPerString[k];
                    for (int i = 0; i < inputStrings.Length; i++)
                        prod *= kSubseqProbs[(inputStrings[i].Length - pos[i]) * inputStrings.Length + i];
                }
                else
                {
                    var kSubseqProbs = subseqProbs[k];
                    for (var i = 0; i < inputStrings.Length; i++)
                    {
                        prod *= kSubseqProbs[inputStrings[i].Length - pos[i]];
                        //    controlProd += Math.Log(subseqProbs[k][inputStrings[i].Length - pos[i]]);
                    }
                    //controlProd = Math.Exp(controlProd);
                    //if (Math.Abs(prod - controlProd) > 0.000001)
                    //   throw new Exception("GRESKAAAAAAA");
                }
                if (prod == 0) //once product becomes 0 it cannot increase since it is non-increasing sequence
                    break; //Console.WriteLine(k);
                psum += prod;
            }
            var ex = psum;

            return ex;
        }
        public double HeuristicGM(string sol, int[] pos)
        {
            var sim = 0.0;
            var mincSum = 0.0;
            for (int k = 0; k < alphabet.Length; k++)
            {
                var geomMean = 0.0;
                var undefined = false;
                var minc = Double.MaxValue;
                for (int i = 0; i < inputStrings.Length && pos[i] < inputStrings[i].Length; i++)
                {
                    var logCnt = logCountsPerStringPerPosition[i][pos[i]][k];
                    if (logCnt == -1)
                    {
                        undefined = true;
                        minc = 0;
                        break;
                    }
                    var cnt = countsPerStringPerPosition[i][pos[i]][k];
                    if (cnt < minc)
                        minc = cnt;

                    geomMean += logCnt;
                }

                if (undefined)
                    continue;

                geomMean = Math.Exp(geomMean / inputStrings.Length);
                var geomSD = 0.0;
                var logGeomMean = Math.Log(geomMean);
                for (int i = 0; i < inputStrings.Length && pos[i] < inputStrings[i].Length; i++)
                {
                    var logCnt = logCountsPerStringPerPosition[i][pos[i]][k];
                    logCnt -= logGeomMean;
                    geomSD += logCnt * logCnt;
                }
                geomSD = Math.Exp(geomSD / inputStrings.Length);
                var adjustedGM = geomMean / geomSD;
                sim += adjustedGM * minc;
                mincSum += minc;
            }
            sim /= mincSum;
            return sim;
        }

        public double HeuristicGMPSUM(string sol, int[] pos)
        {
            double gm = 0;
            double psum = 0;
            if(gmFract>0)
                gm = HeuristicGM(sol, pos);
            if(gmFract<1)
                psum = HeuristicPSUM(sol, pos);
            var heur = gmFract * gm + (1 - gmFract) * psum;
            return heur;
        }
        public double HeuristicEX(string sol, int[] pos)
        {
            heurCalls++;
            var lmax = pos.Zip(inputStrings).Min(x => x.Second.Length - x.First);
            double ex = lmax;
            double prod;
            double prevProd = -1;
            for (var k = 1; k <= lmax; k++)
            {
                //once prod becomes 0, it cannot be increased, since it is a non decreasing sequence of prod terms
                if (prevProd == 0)
                    prod = 0;
                else
                {
                    prod = 1;
                    if (subseqProbsPerString != null)
                    {
                        var kSubseqProbs = subseqProbsPerString[k];
                        for (int i = 0; i < inputStrings.Length; i++)
                        {
                            //Console.WriteLine("{0:000} {1:0000} {2:0000}", k, i, inputStrings[i].Length - pos[i]);
                            prod *= kSubseqProbs[(inputStrings[i].Length - pos[i]) * inputStrings.Length + i];
                        }
                    }
                    else
                    {
                        var kSubseqProbs = subseqProbs[k];
                        for (var i = 0; i < inputStrings.Length; i++)
                            prod *= kSubseqProbs[inputStrings[i].Length - pos[i]];
                    }
                }
                var kterm = Math.Pow(1 - prod, exponents[k]);
                ex = ex - kterm;
                prevProd = prod;
            }
            if (heurCalls % 10000 == 0)
            {
                var ub = UB(sol, pos);
                Console.WriteLine("{0}. {1} {2}({3}) {4:0.00}", heurCalls, ub, ex, sol.Length + ex, ex / ub);
            }
            return ex;
        }
    }
}
