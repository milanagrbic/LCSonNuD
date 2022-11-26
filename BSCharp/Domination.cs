using System;
using System.Collections.Generic;
using System.Text;

namespace BSCsharp
{
    class Domination
    {
        private TNode[] trees;
        private List<int[]> nextPosInOrder;
        public Domination(int inputStringCount)
        {
            trees = new TNode[inputStringCount];
            nextPosInOrder = new List<int[]>();
        }
        internal bool Dominates(int[] nextPos1, int[] nextPos2)
        {
            for (int i = 0; i < nextPos1.Length; i++)
                if (nextPos1[i] > nextPos2[i])
                    return false;
            return true;
        }

        internal bool IsDominatedSlow(int[] nextPos)
        {
            if (trees[0] == null)
                return false;
            var dominatingIdsList = new List<int>();
            trees[0].FindLessOrEqualElementIds(nextPos[0], dominatingIdsList);
            var dominatingIds = new HashSet<int>(dominatingIdsList);
            //find in trees bs indices that are greater or equal than the candidate and reduce the set of those that satisfy conditions successively
            for (int i=1; i<nextPos.Length; i++)
            {
                var newIdsList = new List<int>();
                trees[i].FindLessOrEqualElementIds(nextPos[i],newIdsList);
                var intersection = new List<int>();
                foreach (var id in newIdsList)
                    if (dominatingIds.Contains(id))
                        intersection.Add(id);
                dominatingIds = new HashSet<int>(intersection);
                if (dominatingIds.Count == 0)
                    return false;
               // Console.WriteLine(i+". "+String.Join(',',dominatingIds));
            }
            return dominatingIds.Count > 0;
        }

        internal bool IsDominated(int[] nextPos)
        {
            int maxPartSize = 100;
            int parts = (int)Math.Ceiling(nextPosInOrder.Count*1.0 / maxPartSize);
            for (int p = 0; p < parts; p++)
            {
                var mini = p * maxPartSize;
                var maxi = Math.Min(nextPosInOrder.Count, (p + 1) * maxPartSize);
                var candidateIndices = new List<int>();
                for (int i = mini; i < maxi; i++)
                    if (nextPosInOrder[i][0] <= nextPos[0])
                        candidateIndices.Add(i);

                for (int i = 1; i < nextPos.Length; i++)
                {
                    var newCandidates = new List<int>();
                    for (int j = candidateIndices.Count - 1; j >= 0; j--)
                    {
                        var cid = candidateIndices[j];
                        //the candidate cannot dominate nextPos
                        if (nextPosInOrder[cid][i] <= nextPos[i])
                            newCandidates.Add(cid);
                    }
                    candidateIndices = newCandidates;
                    if (candidateIndices.Count == 0)
                        break;
                }
                if (candidateIndices.Count > 0)
                    return true;
            }
            return false;
        }

        //adding to list of BST (one BST for each input string)
        internal void Add(int[] nextPos, int bsIndex)
        {
            /*for(int i=0; i<nextPos.Length; i++)
            {
                if (trees[i] == null)
                    trees[i] = new TNode(nextPos[i],bsIndex, null, null);
                else
                    trees[i].Add(nextPos[i], bsIndex);
            }*/
            nextPosInOrder.Add(nextPos);
        }
    }
    class TNode
    {
        private int v;
        private int id;
        private TNode l;
        private TNode r;

        internal TNode(int v, int id, TNode l, TNode r)
        {
            this.v = v;
            this.id = id;
            this.l = l;
            this.r = r;
        }

        internal void Add(int v, int id)
        {
            if (v > this.v)
            {
                if (this.r == null)
                    this.r = new TNode(v,id, null, null);
                else
                    this.r.Add(v, id);
            }else
            {
                if (this.l == null)
                    this.l = new TNode(v,id, null, null);
                else
                    this.l.Add(v,id);
            }
        }
        internal List<int> FindLessOrEqualElementIds(int v, List<int> els)
        {
            if (this.v <= v) {
                els.Add(this.id);
                if (this.l != null)
                    this.l.FindLessOrEqualElementIds(v,els);
                if (this.r != null)
                    this.r.FindLessOrEqualElementIds(v,els);
            }
            else if(this.l!=null)
               this.l.FindLessOrEqualElementIds(v,els);
            return els;
        }
    }
}
