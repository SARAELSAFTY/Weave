using System;

namespace Game.Scripts.Runtime.Narrative
{
    public class NarrativeState
    {
        public int Day { get; private set; } = 1;
        public event Action DayChanged;

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
