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
        private readonly List<string> fullChoiceHistory = new List<string>();

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
            fullChoiceHistory.Clear();
        }

        /// <summary>Adds one choice text item to recent history.</summary>
        public void RecordChoice(string choiceText)
        {
            if (string.IsNullOrWhiteSpace(choiceText))
            {
                return;
            }

            fullChoiceHistory.Add(choiceText);

            recentChoices.Add(choiceText);
            if (recentChoices.Count > MaxChoiceHistory)
            {
                recentChoices.RemoveAt(0);
            }
        }

        /// <summary>Builds a summary string of all kingdom resource values.</summary>
        public string GetResourceSummary()
        {
            StringBuilder resourceSummary = new StringBuilder();
            if (catalog != null && resourceState != null)
            {
                for (int i = 0; i < catalog.resources.Count; i++)
                {
                    ResourceData resourceDefinition = catalog.resources[i];
                    if (resourceDefinition == null) continue;

                    resourceSummary.Append($"{resourceDefinition.DisplayName}: {resourceState.Get(resourceDefinition)}");
                    if (i < catalog.resources.Count - 1)
                    {
                        resourceSummary.Append(", ");
                    }
                }
            }
            return resourceSummary.ToString();
        }

        /// <summary>Builds a string of all choices made throughout the full run.</summary>
        public string GetFullHistorySummary() =>
            fullChoiceHistory.Count > 0 ? string.Join("; ", fullChoiceHistory) : "No decisions were recorded.";

        /// <summary>Builds a compact state snapshot used in LLM prompts.</summary>
        public string GetSnapshot()
        {
            int day = narrativeRunner != null ? narrativeRunner.Day : 1;
            string resourceSummary = GetResourceSummary();

            string choicesSummary = recentChoices.Count > 0
                ? string.Join("; ", recentChoices)
                : "None";

            return $"Current Day: {day}\n" +
                   $"Kingdom Resources -> {resourceSummary}\n" +
                   $"Recent Player Decisions: {choicesSummary}";
        }
    }
}
