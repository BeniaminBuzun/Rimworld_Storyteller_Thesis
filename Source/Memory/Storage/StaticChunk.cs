namespace RimTelligence.Source.Memory.Storage
{
    public class StaticChunk
    {
        public string Category;
        public string DefName;
        public string Text;
        public float[] Embedding;      // null = not embedded yet
        public string MetadataJson;
    }
}
