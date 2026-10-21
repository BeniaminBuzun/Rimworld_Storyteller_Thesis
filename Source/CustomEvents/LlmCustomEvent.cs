using System.Collections.Generic;

namespace RimTelligence.CustomEvents
{
    public class LlmCustomEvent
    {
        public List<GameEffect> effects;
        public string letterTitle;
        public string letterText;
    }

    public class GameEffect
    {
        public string EffectName;
        public string[] SelectedDefName;
        public SeverityTypes Severity;
        public string TargetPawn;

        public string DefNameText
        {
            get
            {
                if (SelectedDefName == null || SelectedDefName.Length == 0)
                {
                    return "";
                }
                return string.Join(", ", SelectedDefName);
            }
        }
    }

    public enum SeverityTypes
    {
        None = 0,
        Low = 1,
        Medium = 2,
        High = 3,
        Extreme = 4,
    }
}