using Verse;

namespace RimTelligence.Source.CustomEvents
{
    public static class ModLogger
    {
        private const string Prefix = "[RimTelligence] ";

        public static void Message(string msg)
        {
            Log.Message(Prefix + msg);
        }

        public static void Warning(string msg)
        {
            Log.Warning(Prefix + msg);
        }

        public static void Error(string msg)
        {
            Log.Error(Prefix + msg);
        }
    }
}