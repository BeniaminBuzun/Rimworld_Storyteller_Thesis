using Verse;

namespace RimTelligence
{
    public static class ModLogger
    {
        private const string Prefix = "[RimTelligence] ";

        public static void Debug(string msg)
        {
#if DEBUG
            Log.Message(Prefix + "[Debug] " + msg);
#endif
        }

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