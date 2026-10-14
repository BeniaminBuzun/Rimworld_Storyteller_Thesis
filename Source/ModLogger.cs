using System;
using Verse;

namespace RimTelligence
{
    public static class ModLogger
    {
        private const string Prefix = "[RimTelligence] ";

        /// <summary>
        /// Optional out-of-game sink. When set (for example by the benchmark console),
        /// log lines are routed here instead of <see cref="Verse.Log"/>, which cannot
        /// run outside the Unity player.
        /// </summary>
        public static Action<string> Sink { get; set; }

        public static void Debug(string msg)
        {
#if DEBUG
            Message("[Debug] " + msg);
#endif
        }

        public static void Message(string msg)
        {
            Action<string> sink = Sink;
            if (sink != null)
            {
                sink(Prefix + msg);
                return;
            }

            Log.Message(Prefix + msg);
        }

        public static void Warning(string msg)
        {
            Action<string> sink = Sink;
            if (sink != null)
            {
                sink(Prefix + "[Warning] " + msg);
                return;
            }

            Log.Warning(Prefix + msg);
        }

        public static void Error(string msg)
        {
            Action<string> sink = Sink;
            if (sink != null)
            {
                sink(Prefix + "[Error] " + msg);
                return;
            }

            Log.Error(Prefix + msg);
        }
    }
}
