using System;

namespace Game.Scripts.Runtime.Narrative
{
    /// <summary>Stores mutable narrative progression state.</summary>
    public class NarrativeState
    {
        /// <summary>Gets the current in-game day.</summary>
        public int Day { get; private set; } = 1;

        /// <summary>Raised after the day value changes.</summary>
        public event Action DayChanged;

        /// <summary>Advances the in-game day by a positive amount.</summary>
        public void Advance(int dayAdvance)
        {
            if (dayAdvance <= 0)
            {
                return;
            }

            Day += dayAdvance;
            DayChanged?.Invoke();
        }
    }
}
