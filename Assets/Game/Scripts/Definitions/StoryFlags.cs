using System;

namespace Game.Scripts.Definitions
{
    /// <summary>Run-long story flags set by card choices and read by gates, alternate text, and the ending evaluator.</summary>
    [Flags]
    public enum StoryFlags
    {
        None = 0,
        BrotherKnown = 1 << 0,
        CircusIn = 1 << 1,
        ChancellorFallen = 1 << 2
    }
}
