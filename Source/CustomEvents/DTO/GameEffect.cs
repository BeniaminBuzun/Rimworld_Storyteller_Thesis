namespace RimTelligence.Source.CustomEvents
{
    public class GameEffect
    {
        public string type;
        public string defName;
        public string defDescription;
        public int count;
        public SeverityTypes value;
        public string targetPawn;
    }

    public enum SeverityTypes
    {
        None = 0,
        Low = 1,
        Medium = 2,
        High = 3
    }
}