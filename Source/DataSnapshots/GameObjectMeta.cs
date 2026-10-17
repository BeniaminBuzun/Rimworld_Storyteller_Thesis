using RimWorld;
using System;

namespace RimTelligence.DataSnapshots
{
    public class GameObjectMeta
    {
        public string DefName;
        public string Label;
        public string Description;
        public float MarketValue;
        public TechLevel TechLevel;

        public override string ToString()
        {
            bool hasDef = !string.IsNullOrEmpty(DefName);
            bool hasLabel = !string.IsNullOrEmpty(Label);

            if (!hasDef && !hasLabel)
                return "unknown";

            if (!hasLabel || Label == DefName)
                return DefName;

            if (!hasDef)
                return Label;

            return DefName + " (" + Label + ")";
        }

        public override bool Equals(object obj)
        {
            GameObjectMeta other = obj as GameObjectMeta;
            if (other == null)
                return false;

            return string.Equals(DefName, other.DefName, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            return DefName != null ? DefName.GetHashCode() : 0;
        }
    }

    // JSON-friendly wrapper: Dictionary<GameObjectMeta, int> serializes its key
    // via ToString(), losing all fields. A list of these keeps every field.
    public class CountedMeta
    {
        public GameObjectMeta meta;
        public int count;
    }
}
