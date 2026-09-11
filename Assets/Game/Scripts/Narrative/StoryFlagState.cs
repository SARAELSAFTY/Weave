using Game.Scripts.Definitions;

namespace Game.Scripts.Narrative
{
    /// <summary>Mutable flag set for a single reign. Reset at run start.</summary>
    public class StoryFlagState
    {
        public StoryFlags Value { get; private set; }

        public void Reset()
        {
            Value = StoryFlags.None;
        }

        public void Set(StoryFlags flags)
        {
            Value |= flags;
        }

        public bool Has(StoryFlags flags)
        {
            return flags == StoryFlags.None || (Value & flags) == flags;
        }

        public bool HasAny()
        {
            return Value != StoryFlags.None;
        }
    }
}
