using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSCsharp
{
    class ProbMatrixGenerator
    {
        private string[] inputStrings;
        private int alphabetCount;
        private Dictionary<char, double> alphabetFreq;
        private int size;
        private Random rg;
        private int[,] L;
        public ProbMatrixGenerator(string[] inputStrings, int randomSeed)
        {
            this.inputStrings = inputStrings;
            this.rg = new Random(randomSeed);
            this.size = inputStrings.Max(x => x.Length);
            this.alphabetFreq = new Dictionary<char, double>();
            var total = 0.0;
            foreach (var s in inputStrings)
                foreach (var c in s)
                {
                    total += 1;
                    if (alphabetFreq.ContainsKey(c))
                        alphabetFreq[c]++;
                    else
                        alphabetFreq[c] = 1;
                }
            for (int i = 0; i < alphabetFreq.Keys.Count; i++)
                alphabetFreq[alphabetFreq.Keys.ElementAt(i)] /= total;
            this.alphabetCount = alphabetFreq.Keys.Count;
            this.L = new int[size + 1, size + 1];
        }

        //TODO: napraviti onoliko matrica koliko je ulaznih stringova
        //succProb ce biti skalarni proizvod frekvencija karaktera unutar svake od niski i globalne frekvencije posto nju ocekujemo da bude u okviru stringa koji je resenje
        public double[][] CalculateMousaviMatrix()
        {
            var mousMatrix = new double[size + 1][];
            for (int i = 0; i <= size; i++)
                mousMatrix[i] = new double[size + 1];
            var sigma = alphabetCount;
            var succProb = 1.0 / sigma;
            for (int k = 0; k <= size; k++)
                for (int q = 0; q <= size; q++)
                {
                    if (k == 0)
                        mousMatrix[k][q] = 1;
                    else if (k > q)
                        mousMatrix[k][q] = 0;
                    else
                        mousMatrix[k][q] = succProb * mousMatrix[k - 1][q - 1] + (1-succProb) * mousMatrix[k][q - 1];
                }
            for (int k = 0; k <= size && k < 20; k++)
            {
                for (int q = 0; q <= size && q < 20; q++)
                    Console.Write("{0:0.000}\t", mousMatrix[k][q]);
                Console.WriteLine();
            }
            return mousMatrix;
        }

        private int Overlap(string s1, string s2)
        {
            if (s1.Length != s2.Length)
                throw new Exception("Strings should have the same length!");
            var overlap = 0;
            for (int i = 0; i < s1.Length; i++)
                if (s1[i] == s2[i])
                    overlap++;
            return overlap;
        }

        public double[][] CalculateProbMatrixGram(int N, bool density)
        {

            var matrix = new double[size + 1][];
            for (int k = 0; k <= size; k++)
                matrix[k] = new double[size + 1];
            var succProbs = new double[size+1,N + 1, N + 1];
            for (int q = 1; q <= size; q++)
            {
                var joinedInputStrings = String.Join("", inputStrings.Select(x=>x.Substring(Math.Max(x.Length-q,0))));
                for (int n = 1; n <= N; n++)
                {
                    var freqn = NgramFrequencies(joinedInputStrings, n);
                    foreach (var x in freqn.Keys)
                        foreach (var y in freqn.Keys)
                        {
                            var overlap = Overlap(x, y);
                            succProbs[q,n, overlap] += freqn[x] * freqn[y];
                        }
                    var totProb = 0.0;
                    for (int succCnt = 0; succCnt <= n; succCnt++)
                        totProb += succProbs[q,n, succCnt];
                    if (Math.Abs(totProb - 1.0) > 0.00001)
                        throw new Exception("Probabilities do not sum to 1!");
                }
            }

            for (int k = 0; k <= size; k++)
            {
                for (int q = k; q <= size; q++)
                {
                    if (k == 0)
                        matrix[k][q] = 1;
                    else if (k > q)
                        matrix[k][q] = 0;
                    else
                    {
                        var lowerNUsed = false;
                        for (int n = 1; n < N; n++)
                        {
                            if (q <= n || k <= n)
                            {
                                for (int succCnt = n; succCnt >= 0; succCnt--)
                                    matrix[k][q] += succProbs[q,n, succCnt] * matrix[k - succCnt][(q - n)];
                                lowerNUsed = true;
                                break;
                            }
                        }
                        if (!lowerNUsed)
                            for (int succCnt = N; succCnt >= 0; succCnt--)
                                matrix[k][q] += succProbs[q,N, succCnt] * matrix[k - succCnt][(q - N)];

                        //Console.WriteLine("{0} {1} {2:0.000000}", k, q, matrices[k][q * inputStrings.Length + i]);
                    }
                }
            }

            if (density)
            {
                for (int q = 1; q <= size; q++)
                    for (int k = 1; k < q; k++)
                        matrix[k][q] = matrix[k][q] - matrix[k + 1][q];
                for (int q = 1; q <= size; q++)
                {
                    var control = 0.0;
                    for (int k = 1; k <= q; k++)
                        control += matrix[k][q];
                    if (control > 1.000001)
                        throw new Exception("Probability larger than 1!");
                }
            }

            //print matrix
            for (int k = 0; k <= size && k < 20; k++)
            {
                for (int q = 0; q <= size && q < 20; q++)
                    Console.Write("{0:0.000}\t", matrix[k][q]);
                Console.WriteLine();
            }
            return matrix;
        }

        public double[][] CalculateSpecificProbMatricesGram(int N, bool density)
        {

            var matrices = new double[size + 1][];
            for (int k = 0; k <= size; k++)
                matrices[k] = new double[inputStrings.Length * (size + 1)];
            for (int i = 0; i < inputStrings.Length; i++)
            {
                Console.WriteLine("String {0}", i);
                var succProbs = new double[size+1,N+1,N+1];

                for (int q = 1; q <= size; q++)
                {
                    for (int n = 1; n <= N; n++)
                    {
                        var freqn = NgramFrequencies(inputStrings[i].Substring(Math.Max(inputStrings[i].Length-q,0)), n);
                        foreach (var x in freqn.Keys)
                            foreach (var y in freqn.Keys)
                            {
                                var overlap = Overlap(x, y);
                                succProbs[q,n, overlap] += freqn[x] * freqn[y];
                            }
                        var totProb = 0.0;
                        for (int succCnt = 0; succCnt <= n; succCnt++)
                            totProb += succProbs[q,n, succCnt];
                        if (Math.Abs(totProb - 1.0) > 0.00001)
                            throw new Exception("Probabilities do not sum to 1!");
                    }

                }


                for (int k = 0; k <= size; k++)
                {
                    for (int q = k; q <= size; q++)
                    {
                        if (k == 0)
                            matrices[k][q * inputStrings.Length + i] = 1;
                        else if (k > q)
                            matrices[k][q * inputStrings.Length + i] = 0;
                        else
                        {
                            var lowerNUsed = false;
                            for(int n=1; n<N; n++)
                            {
                                if(q<=n || k <= n)
                                {
                                    for(int succCnt=n; succCnt>=0; succCnt--)
                                        matrices[k][q*inputStrings.Length+i]+= succProbs[q,n, succCnt] * matrices[k - succCnt][(q - n) * inputStrings.Length + i];
                                    lowerNUsed = true;
                                    break;
                                }
                            }
                            if (!lowerNUsed)
                                for (int succCnt = N; succCnt >= 0; succCnt--)
                                    matrices[k][q * inputStrings.Length + i] += succProbs[q,N, succCnt] * matrices[k - succCnt][(q - N) * inputStrings.Length + i];
                           
                            //Console.WriteLine("{0} {1} {2:0.000000}", k, q, matrices[k][q * inputStrings.Length + i]);

                        }
                    }
                }

                if (density)
                {
                    for (int q = 1; q <= size; q++)
                        for (int k=1; k <q; k++)
                            matrices[k][q * inputStrings.Length + i] = matrices[k][q * inputStrings.Length + i] - matrices[k+1][q * inputStrings.Length + i];
                    for(int q=1; q<=size; q++)
                    {
                        var control = 0.0;
                        for (int k = 1; k <=q; k++)
                            control += matrices[k][q * inputStrings.Length + i];
                        if (control>1.000001)
                            throw new Exception("Probability larger than 1!");
                    }
                }

                //print matrix
                for (int k = 0; k <= size && k < 20; k++)
                {
                    for (int q = 0; q <= size && q < 20; q++)
                        Console.Write("{0:0.000}\t", matrices[k][q * inputStrings.Length + i]);
                    Console.WriteLine();
                }
            }
            return matrices;
        }

        Dictionary<string, double> NgramFrequencies(string str, int n)
        {
            var freq = new Dictionary<string, double>();
            var total = 0.0;
            for (int i = 0; i <= str.Length - n; i++)
            {
                var ng = str.Substring(i, n);
                if (freq.ContainsKey(ng))
                    freq[ng] += 1;
                else
                    freq[ng] = 1;
                total += 1;
            }
            for (int i = 0; i < freq.Keys.Count; i++)
                freq[freq.Keys.ElementAt(i)] /= total;
            return freq;
        }

        public double[,] CalculateCustomMatrix(int sampleCnt, int mcMatrixSize)
        {
            var probMatrix = new double[size + 1, size + 1];
            string stringSample = null;
            string subStringSample = null;
            double avgFullLCS = 0;
            for (int s = 0; s < sampleCnt; s++)
            {
                stringSample = NewSampleMixed(mcMatrixSize);
                subStringSample = NewSampleMixed(mcMatrixSize);
             
                if (s % 1000 == 0)
                    Console.WriteLine("{0} samples so far. {1} in {2} AvgFullLCS({3},{4}={5:0.00000000000000000000000000000}", s, subStringSample, stringSample, subStringSample.Length, stringSample.Length, avgFullLCS / (s + 1));
                var lcsTable = LcsTable(subStringSample, stringSample, subStringSample.Length, stringSample.Length);
                avgFullLCS += lcsTable[mcMatrixSize, mcMatrixSize] == mcMatrixSize ? 1 : 0;
                for (int k = 0; k <= mcMatrixSize; k++)
                    for (int q = 0; q <= mcMatrixSize; q++)
                    {
                        if (k == 0)
                            probMatrix[k, q] = 1;
                        else if (k > q)
                            probMatrix[k, q] = 0;
                        else
                        {
                            var isSubNew = lcsTable[k, q] == k;
                            if (isSubNew)
                                probMatrix[k, q] += 1;
                        }
                    }
            }
            for (int k = 0; k <= mcMatrixSize; k++)
                for (int q = 0; q <= size; q++)
                    if (k <= q && k != 0)
                        probMatrix[k, q] /= sampleCnt;


            for (int k = 0; k <= size; k++)
                for (int q = 0; q <= size; q++)
                {
                    if (k <= mcMatrixSize && q <= mcMatrixSize)
                        continue;
                    if (k == 0)
                        probMatrix[k, q] = 1;
                    else if (k > q)
                        probMatrix[k, q] = 0;
                    else
                        probMatrix[k, q] = probMatrix[1, 1] * probMatrix[k - 1, q - 1] + (1 - probMatrix[1, 1]) * probMatrix[k, q - 1];
                }
            return probMatrix;
        }

        private string NewSample(int mcMatrixSize)
        {
            //simply selecting the random input string and then random sample of size maxSize from it
            var randString = inputStrings[rg.Next(inputStrings.Length)];
            var selectedIndices = new SortedSet<int>();
            int i = 0;
            while (i < mcMatrixSize)
            {
                var ri = rg.Next(randString.Length);
                if (!selectedIndices.Contains(ri))
                {
                    selectedIndices.Add(ri);
                    i++;
                }
            }
            var sb = new StringBuilder();
            foreach (var ind in selectedIndices)
                sb.Append(randString[ind]);
            return sb.ToString();
        }

        private string NewSampleMixed(int mcMatrixSize)
        {
            var selectedPos = new SortedSet<int>();
            while (selectedPos.Count < mcMatrixSize)
            {
                var p = rg.Next(size);
                selectedPos.Add(p);
            }
            var sb = new StringBuilder();
            foreach (var p in selectedPos)
                sb.Append(inputStrings[rg.Next(inputStrings.Length)][p]);
            return sb.ToString();
        }

        private int[,] LcsTable(string X, string Y, int m, int n)
        {
            for (int i = 0; i <= m; i++)
            {
                for (int j = 0; j <= n; j++)
                {
                    if (i == 0 || j == 0)
                        L[i, j] = 0;
                    else if (X[i - 1] == Y[j - 1])
                        L[i, j] = L[i - 1, j - 1] + 1;
                    else
                        L[i, j] = max(L[i - 1, j], L[i, j - 1]);
                }
            }
            return L;
        }

        private int max(int a, int b)
        {
            return (a > b) ? a : b;
        }
    }
}
