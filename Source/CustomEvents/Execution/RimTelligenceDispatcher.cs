using System;
using System.Collections.Concurrent;
using Verse;

namespace RimTelligence.CustomEvents.Execution
{
    public class RimTelligenceDispatcher : GameComponent
    {
        // Thread-safe queue for actions coming from the LLM
        public static readonly ConcurrentQueue<Action> ExecutionQueue = new ConcurrentQueue<Action>();

        public RimTelligenceDispatcher(Game game)
        {
            // Constructed for every new game and every load: drop anything left from the previous session.
            while (ExecutionQueue.TryDequeue(out _)) { }
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();
            //ModLogger.DrainQueue();

            // Process up to 5 actions per tick to avoid stuttering if multiple arrive
            int processed = 0;
            while (processed < 5 && ExecutionQueue.TryDequeue(out Action action))
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    ModLogger.Error($"Error executing queued action: {ex.Message}\n{ex.StackTrace}");
                }
                processed++;
            }
        }

        public override void GameComponentUpdate()
        {
            base.GameComponentUpdate();
            //ModLogger.DrainQueue();
        }
    }
}
