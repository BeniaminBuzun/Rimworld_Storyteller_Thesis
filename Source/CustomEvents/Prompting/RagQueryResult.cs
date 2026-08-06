using RimTelligence.Memory;
using System.Collections.Generic;

namespace RimTelligence.CustomEvents.Prompting
{
    public class RagQueryResult
    {
        public List<RagResult> StaticResults { get; set; } = new List<RagResult>();
        public List<RagResult> DynamicResults { get; set; } = new List<RagResult>();
        public bool HasResults => StaticResults.Count > 0 || DynamicResults.Count > 0;
    }
}
