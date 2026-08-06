using RimTelligence.CustomEvents.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimTelligence.CustomEvents.EventSelector
{
    public class EventHistory : GameComponent
    {
        private static EventHistory _instance;
        public static EventHistory Current => _instance;

        private List<string> _recentTypes = new List<string>();
        private List<EventAlignment> _recentAlignments = new List<EventAlignment>();

        private const int TypeWindow = 20;
        private const int AlignmentWindow = 10;

        public EventHistory(Game game) { _instance = this; }

        public int ConsecutiveBadBatches => ConsecutiveAlignments(EventAlignment.Bad);
        public int ConsecutiveGoodBatches => ConsecutiveAlignments(EventAlignment.Good);

        // How many of the last batches had this alignment (no other alignment in between)
        private int ConsecutiveAlignments(EventAlignment alignment)
        {
            int count = 0;
            for (int index = _recentAlignments.Count - 1; index >= 0; index--)
            {
                if (_recentAlignments[index] == alignment) count++;
                else break;
            }
            return count;
        }

        // Returns a 0-1 penalty for how recently this type was seen
        // 1.0 = seen last event (max penalty), 0.0 = not seen in window
        public float RecencyPenalty(string type)
        {
            int index = _recentTypes.LastIndexOf(type);
            if (index < 0) return 0f;

            int stepsAgo = _recentTypes.Count - 1 - index;
            // linear decay: 0 steps ago → 1.0 penalty, TypeWindow steps ago → 0.0
            return Math.Max(0f, 1f - (float)stepsAgo / TypeWindow);
        }

        public void Record(List<SelectedEventIntent> events)
        {
            foreach (var evt in events)
            {
                _recentTypes.Add(evt.type);
                if (_recentTypes.Count > TypeWindow)
                    _recentTypes.RemoveAt(0);
            }

            bool hasBad = events.Any(evt => GetAlignment(evt.type) == EventAlignment.Bad);
            bool hasGood = events.Any(evt => GetAlignment(evt.type) == EventAlignment.Good);

            EventAlignment batchAlign =
                hasBad ? EventAlignment.Bad :
                hasGood ? EventAlignment.Good :
                EventAlignment.Neutral;

            _recentAlignments.Add(batchAlign);
            if (_recentAlignments.Count > AlignmentWindow)
                _recentAlignments.RemoveAt(0);
        }

        private static EventAlignment GetAlignment(string type)
        {
            var handler = EffectHandlerRegistry.GetHandler(type);
            return handler?.Alignment ?? EventAlignment.Neutral;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref _recentTypes, "recentTypes", LookMode.Value);
            Scribe_Collections.Look(ref _recentAlignments, "recentAlignments", LookMode.Value);

            // Guard against null after load
            if (_recentTypes == null) _recentTypes = new List<string>();
            if (_recentAlignments == null) _recentAlignments = new List<EventAlignment>();
        }
    }
}
