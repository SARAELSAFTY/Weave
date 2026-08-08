using System.Collections.Generic;
using System.Text;
using Game.Scripts.Definitions;

namespace Game.Scripts.Runtime.Narrative
{
    /// <summary>Stores recent player choices and builds snapshot text for LLM prompts.</summary>
    public class PlayerHistoryTracker
    {
        private const int MaxChoiceHistory = 3;

        private readonly ResourceState resourceState;
        private readonly NarrativeRunner narrativeRunner;
        private readonly ResourceCatalog catalog;
        private readonly List<string> recentChoices = new List<string>();

        /// <summary>Creates a new history tracker for one active run.</summary>
        public PlayerHistoryTracker(ResourceState resourceState, NarrativeRunner narrativeRunner, ResourceCatalog catalog)
        {
            this.resourceState = resourceState;
            this.narrativeRunner = narrativeRunner;
            this.catalog = catalog;
        }

        /// <summary>Clears all recorded choices.</summary>
        public void Reset()
        {
            recentChoices.Clear();
        }

        /// <summary>Adds one choice text item to recent history.</summary>
        public void RecordChoice(string choiceText)
        {
            if (string.IsNullOrWhiteSpace(choiceText))
            {
                return;
            }

            recentChoices.Add(choiceText);
            if (recentChoices.Count > MaxChoiceHistory)
            {
                recentChoices.RemoveAt(0);
            }
        }

        /// <summary>Builds a compact state snapshot used in LLM prompts.</summary>
        public string GetSnapshot()
        {
            int day = narrativeRunner != null ? narrativeRunner.Day : 1;

            StringBuilder resourceSummary = new StringBuilder();
            if (catalog != null && resourceState != null)
            {
                for (int i = 0; i < catalog.resources.Count; i++)
                {
                    ResourceData resourceDefinition = catalog.resources[i];
                    resourceSummary.Append($"{resourceDefinition.displayName}: {resourceState.Get(resourceDefinition.id)}");
                    if (i < catalog.resources.Count - 1)
                    {
                        resourceSummary.Append(", ");
                    }
                }
            }

            string choicesSummary = recentChoices.Count > 0
                ? string.Join("; ", recentChoices)
                : "None";

            return $"Current Day: {day}\n" +
                   $"Kingdom Resources -> {resourceSummary}\n" +
                   $"Recent Player Decisions: {choicesSummary}";
        }
    }
}
