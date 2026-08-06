namespace RimTelligence.CustomEvents.EventSelector
{
    public enum DrawMode
    {
        Weighted,    // full heuristic, all categories, state modifiers active
        PureRandom,  // flat weights, no state modifiers, no category filter
        BadStreak,   // only Bad/Neutral, state modifiers active
        GoodStreak,  // only Good/Neutral, state modifiers active
        NeutralOnly,  // only Neutral, state modifiers active
    }
}