using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace BSCsharp
{
    class Utils
    {
        public static void Shuffle<T>(T[] arr)
        {
            var rg = new Random(11111);
            int n = arr.Length;
            while (n > 1)
            {
                n--;
                int k = rg.Next(n + 1);
                T value = arr[k];
                arr[k] = arr[n];
                arr[n] = value;
            }
        }

        public static double Similarity(string[] inputStringsWhole, int pieces)
        {
            var maxPieces = inputStringsWhole.Min(x => x.Length);
            if (pieces > maxPieces)
                pieces = maxPieces;
            var total = 0.0;
            for (int p = 0; p < pieces; p++)
            {
                var inputStrings = new string[inputStringsWhole.Length];
                for (int k = 0; k < inputStringsWhole.Length; k++)
                    if(k==inputStringsWhole.Length-1)
                        inputStrings[k] = inputStringsWhole[k].Substring(p * inputStringsWhole[k].Length / pieces);
                    else
                        inputStrings[k] = inputStringsWhole[k].Substring(p * inputStringsWhole[k].Length / pieces, inputStringsWhole[k].Length / pieces);
                //calculating similarity score
                var freqs = new Dictionary<char, double>[inputStrings.Length];
                var chars = new HashSet<char>();

                for (int k = 0; k < inputStrings.Length; k++)
                {
                    var freq = new Dictionary<char, double>();
                    foreach (var c in inputStrings[k])
                    {
                        chars.Add(c);
                        if (freq.ContainsKey(c))
                            freq[c]++;
                        else
                            freq[c] = 1;
                    }
                    for (int j = 0; j < freq.Keys.Count; j++)
                        freq[freq.Keys.ElementAt(j)] /= inputStrings[k].Length;
                    freqs[k] = freq;
                }
                var scalarProduct = 0.0;
                foreach (var c in chars)
                {
                    var prod = 1.0;
                    for (int k = 0; k < inputStrings.Length; k++)
                        if (!freqs[k].ContainsKey(c))
                        {
                            prod = 0;
                            break;
                        }
                        else
                            prod *= freqs[k][c];
                    scalarProduct += prod;
                }
                total+= scalarProduct;
            }
            total /= pieces;
            return total;
        }

        /* decompose  high  power (because of stability reasons in calculation when exponent is extremely high) */
        
        public static double StablePow(double v, int sigma, double k, int step)
        {
            var val = v;
            var exp = Math.Pow(sigma, step);
            while (k > step)
            {
                val = Math.Pow(val, exp);
                k -= step;
            }
            val = Math.Pow(val, Math.Pow(sigma, k));
            return val;
        }

        // Approximate l: l-t term of the sum; Sigma: alphabet size; pi: value of 1-E[Y_l] 
        public static double ApproxPow(double v, int sigma, double k)
        {
            double term = 1.0;
            if (v < 1E-250)
                return 0.0;

            double pxInterm = Math.Log(v) + k* Math.Log(sigma);
            if (pxInterm >= 300)
                term = 1.0;
            else
            { // in-between...
                double px = Math.Exp(pxInterm);
                double tildeP = px * (-1.0 - v / 2.0);
                if (Math.Abs(tildeP) < 1E-15)// 1-e^x ~ -x
                    return -tildeP;
                term = Math.Exp(tildeP);
            }
            return 1.0 - term;
        }

        public static double MyPow(double v, int sigma, double k)
        {
            if (v >= 1.0E-10)
                v = StablePow(v, sigma, k, 20);
            else
            {
                v = 1 - v;
                v = ApproxPow(v, sigma, k);
            }
            return v;
        }
        public static BigInteger Comb(int n, int k)
        {
            var t1 = Fact(n);
            var t2 = Fact(n-k);
            var t3 = Fact(k);
            return t1 / t2/t3;
        }

        public static BigInteger Fact(int n)
        {
            BigInteger fact = 1;
            for (int i = 1; i <= n; i++)
                fact *= i;
            return fact;
        }

        //given counts[i] characters i what is the number of possible sequences of length sum_i^n(counts)
        public static BigInteger DifferentSequences(int[] counts)
        {
            var seqNum = Utils.Fact(counts.Length);
            var places = counts.Sum();
            for (int ci = 0; ci < counts.Length; ci++)
                seqNum *= Utils.Comb(places, counts[ci]);
            return seqNum;
        }

        public static Dictionary<char, double> Frequencies(string[] strings, int[] startingPos)
        {
            var dict = new Dictionary<char, double>();
            var tot = 0;
            for (int i = 0; i < strings.Length; i++)
                for (int j = startingPos[i]; j < strings[i].Length; j++) {
                    var c = strings[i][j];
                    if (dict.ContainsKey(c))
                        dict[c] += 1;
                    else
                        dict[c] = 1;
                    tot++;
                }

            for (int i = 0; i < dict.Keys.Count; i++)
                dict[dict.ElementAt(i).Key] /= tot;
            return dict;
        }

        public static Dictionary<char, double> Frequencies(string str)
        {
            var dict = new Dictionary<char, double>();
            var tot = 0;
            foreach (var c in str)
            {
                if (dict.ContainsKey(c))
                    dict[c] += 1;
                else
                    dict[c] = 1;
                tot++;
            }

            for (int i = 0; i < dict.Keys.Count; i++)
                dict[dict.ElementAt(i).Key] /= tot;
            return dict;
        }

        public static Dictionary<char, double> Counts(string str)
        {
            var dict = new Dictionary<char, double>();
            foreach (var c in str)
            {
                if (dict.ContainsKey(c))
                    dict[c] += 1;
                else
                    dict[c] = 1;
            }
            return dict;
        }

        public static Dictionary<string, int> Counts(string str, int n)
        {
            var dict = new Dictionary<string, int>();
            for(int i=0; i<=str.Length-n; i++)
            {
                var ngram = str.Substring(i, n);
                if (dict.ContainsKey(ngram))
                    dict[ngram] += 1;
                else
                    dict[ngram] = 1;
            }
            return dict;
        }

        public static double Norm2(int[] vals)
        {
            var sum = 0.0;
            foreach (var v in vals)
                sum += (v * v);
            return Math.Sqrt(sum);
        }

        public static Dictionary<string, double> PositionDisturbance(string str)
        {
            var dict = new Dictionary<string, List<double>>();
            for(int i=0; i<str.Length; i++)
            {
                var c = str[i].ToString();
                if (!dict.ContainsKey(c))
                    dict[c] = new List<double>();
                dict[c].Add(i);
            }
            var disturb = new Dictionary<string, double>();
            foreach (var c in dict.Keys) {
                var cavg = dict[c].Average();
                var stdev = 0.0;
                foreach (var el in dict[c])
                    stdev += (el - cavg) * (el - cavg);
                stdev = Math.Sqrt(stdev / dict[c].Count);
                disturb.Add(c.ToString(), stdev);
            }
            return disturb;
        }

        public static Dictionary<string, double> PositionDistanceDisturbance(string str)
        {
            var dict = new Dictionary<string, List<double>>();
            for (int i = 0; i < str.Length; i++)
            {
                var c = str[i].ToString();
                if (!dict.ContainsKey(c))
                    dict[c] = new List<double>();
                dict[c].Add(i);
            }
            var disturb = new Dictionary<string, double>();
            foreach (var c in dict.Keys)
            {
                var distances = new List<double>();
                for (int i = 1; i < dict[c].Count; i++)
                    distances.Add(dict[c][i] - dict[c][i - 1]);
                if (distances.Count == 0)
                    continue;
                var cavg = distances.Average();
                var stdev = 0.0;
                foreach (var el in distances)
                    stdev += (el - cavg) * (el - cavg);
                stdev = Math.Sqrt(stdev / distances.Count);
                disturb.Add(c.ToString(), stdev);
            }
            return disturb;
        }

    }
}
