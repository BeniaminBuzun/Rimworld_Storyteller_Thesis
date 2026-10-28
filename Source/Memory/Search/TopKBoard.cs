using System;
using System.Collections.Generic;

namespace RimTelligence.Source.Memory.Search
{
    /// <summary>
    /// Keeps only the K best-scoring results while scanning, so a query never has to
    /// materialize and sort every row. Ties keep insertion order.
    /// </summary>
    public sealed class TopKBoard
    {
        private readonly int k;
        private readonly List<RagResult> items;

        public TopKBoard(int k)
        {
            this.k = Math.Max(0, k);
            items = new List<RagResult>(this.k + 1);
        }

        /// <summary>True if a result with this score would make the board (lets callers skip building it).</summary>
        public bool Qualifies(float score)
        {
            return k > 0 && (items.Count < k || score > items[items.Count - 1].Score);
        }

        public void Add(RagResult result)
        {
            int index = items.Count;
            while (index > 0 && items[index - 1].Score < result.Score) index--;
            items.Insert(index, result);
            if (items.Count > k) items.RemoveAt(items.Count - 1);
        }

        public List<RagResult> ToList()
        {
            return new List<RagResult>(items);
        }
    }
}
