using RimTelligence.Source.CustomEvents.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimTelligence.Source.CustomEvents.EventSelector
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

        // How many of the last N batches were Bad (no Good/Neutral break)
        public int ConsecutiveBadBatches
        {
            get
            {
                int count = 0;
                for (int i = _recentAlignments.Count - 1; i >= 0; i--)
                {
                    if (_recentAlignments[i] == EventAlignment.Bad) count++;
                    else break;
                }
                return count;
            }
        }

        // How many of the last N batches were Good (no Bad/Neutral break)
        public int ConsecutiveGoodBatches
        {
            get
            {
                int count = 0;
                for (int i = _recentAlignments.Count - 1; i >= 0; i--)
                {
                    if (_recentAlignments[i] == EventAlignment.Good) count++;
                    else break;
                }
                return count;
            }
        }

        // Returns a 0-1 penalty for how recently this type was seen
        // 1.0 = seen last event (max penalty), 0.0 = not seen in window
        public float RecencyPenalty(string type)
        {
            int idx = _recentTypes.LastIndexOf(type);
            if (idx < 0) return 0f;

            int stepsAgo = _recentTypes.Count - 1 - idx;
            // linear decay: 0 steps ago → 1.0 penalty, TypeWindow steps ago → 0.0
            return Math.Max(0f, 1f - (float)stepsAgo / TypeWindow);
        }

        public float FamilyRecencyPenalty(string family)
        {
            // find the most recent type in this family
            float worst = 0f;
            for (int i = 0; i < _recentTypes.Count; i++)
            {
                string t = _recentTypes[i];
                string f = t.Contains('_') ? t.Split('_')[0] : t;
                if (string.Equals(f, family, StringComparison.OrdinalIgnoreCase))
                {
                    int stepsAgo = _recentTypes.Count - 1 - i;
                    float p = Math.Max(0f, 1f - (float)stepsAgo / TypeWindow);
                    if (p > worst) worst = p;
                }
            }
            return worst;
        }

        public void Record(List<SelectedEventIntent> events)
        {
            foreach (var e in events)
            {
                _recentTypes.Add(e.type);
                if (_recentTypes.Count > TypeWindow)
                    _recentTypes.RemoveAt(0);
            }

            bool hasBad = events.Any(e => GetAlignment(e.type) == EventAlignment.Bad);
            bool hasGood = events.Any(e => GetAlignment(e.type) == EventAlignment.Good);

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
