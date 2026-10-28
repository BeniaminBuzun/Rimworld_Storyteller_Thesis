using System;
using System.Collections.Concurrent;
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

        private enum LogLevel
        {
            Message,
            Warning,
            Error
        }

        private struct QueuedLog
        {
            public LogLevel Level;
            public string Text;
        }

        private static readonly ConcurrentQueue<QueuedLog> _queue = new ConcurrentQueue<QueuedLog>();

        private static bool IsMainThread()
        {
            try
            {
                return UnityData.IsInMainThread;
            }
            catch
            {
                return false;
            }
        }

        public static void DrainQueue()
        {
            if (!IsMainThread()) return;

            while (_queue.TryDequeue(out QueuedLog item))
            {
                EmitDirect(item.Level, item.Text);
            }
        }

        private static void EmitDirect(LogLevel level, string fullMsg)
        {
            switch (level)
            {
                case LogLevel.Warning:
                    Log.Warning(fullMsg);
                    break;
                case LogLevel.Error:
                    Log.Error(fullMsg);
                    break;
                default:
                    Log.Message(fullMsg);
                    break;
            }
        }

        private static void LogInternal(LogLevel level, string msg)
        {
            Action<string> sink = Sink;
            if (sink != null)
            {
                string tag = level == LogLevel.Warning ? "[Warning] " : level == LogLevel.Error ? "[Error] " : "";
                sink(Prefix + tag + msg);
                return;
            }

            string full = Prefix + msg;

            if (IsMainThread())
            {
                // Flush any earlier pending background messages first to maintain chronological order
                DrainQueue();
                EmitDirect(level, full);
            }
            else
            {
                _queue.Enqueue(new QueuedLog { Level = level, Text = full });
            }
        }

        public static void Debug(string msg)
        {
#if DEBUG
            Message("[Debug] " + msg);
#endif
        }

        public static void Message(string msg) => LogInternal(LogLevel.Message, msg);
        public static void Warning(string msg) => LogInternal(LogLevel.Warning, msg);
        public static void Error(string msg) => LogInternal(LogLevel.Error, msg);
    }
}
