using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace BSCsharp
{
    class BSLCSItem : IComparable<BSLCSItem>
    {
        private string sol;
        private double heurValue;
        private int[] nextPos;
        public BSLCSItem(string sol, double heurValue, int[] nextPos)
        {
            this.sol = sol;
            this.heurValue = heurValue;
            this.nextPos = nextPos;
        }

        public string Sol
        {
            get { return sol; }
        }

        public int[] NextPos
        {
            get { return nextPos; }
        }

        public double HeurValue
        {
            get { return heurValue; }
        }

        public bool Dominates(BSLCSItem item)
        {
            for (int i = 0; i < nextPos.Length; i++)
                if (nextPos[i] > item.nextPos[i])
                    return false;
            return true;
        }

        public int CompareTo([AllowNull] BSLCSItem other)
        {
            return other.heurValue.CompareTo(this.heurValue);
        }

        public double PosStdev()
        {
            var posAvg = nextPos.Average();
            var posStdev = 0.0;
            foreach (var p in nextPos)
                posStdev += (posAvg - p) * (posAvg - p);
            posStdev = Math.Sqrt(posStdev / nextPos.Length);
            return posStdev;
        }

        public double PosAvgOverStdev()
        {
            var score= nextPos.Average() / PosStdev();
            return score;
        }
        public override string ToString()
        {
            var posStdev = PosStdev();
            return String.Format("{0}\t{1}\t{2}",heurValue,sol.Length, sol);
        }


    }
}
