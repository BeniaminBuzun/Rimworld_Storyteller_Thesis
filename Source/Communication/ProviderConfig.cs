using Verse;

namespace RimTelligence.Llm
{
    public class ProviderConfig : IExposable
    {
        public string providerId = "";
        public string apiKey = "";
        public string chatModel = "";
        public string embeddingModel = "";
        public string baseUrl = "";

        public void ExposeData()
        {
            Scribe_Values.Look(ref providerId, "providerId", "");
            Scribe_Values.Look(ref apiKey, "apiKey", "");
            Scribe_Values.Look(ref chatModel, "chatModel", "");
            Scribe_Values.Look(ref embeddingModel, "embeddingModel", "");
            Scribe_Values.Look(ref baseUrl, "baseUrl", "");
        }
    }
}
