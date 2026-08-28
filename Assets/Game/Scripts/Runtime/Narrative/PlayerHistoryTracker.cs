using System.Collections.Generic;
using System.Text;
using Game.Scripts.Definitions;
using Game.Scripts.Localization;

namespace Game.Scripts.Runtime.Narrative
{
    /// <summary>Records player choices and builds compact state snapshots for LLM prompts.</summary>
    public class PlayerHistoryTracker
    {
        private readonly ResourceState resourceState;
        private readonly NarrativeRunner narrativeRunner;
        private readonly ResourceCatalog catalog;
        private readonly List<string> fullChoiceHistory = new List<string>();
        private readonly List<string> narrativeHistoryEntries = new List<string>();
        private readonly List<string> completedPetitionTranscripts = new List<string>();

        public PlayerHistoryTracker(ResourceState resourceState, NarrativeRunner narrativeRunner, ResourceCatalog catalog)
        {
            this.resourceState = resourceState;
            this.narrativeRunner = narrativeRunner;
            this.catalog = catalog;
        }

        public void RecordChoice(string choiceText)
        {
            if (string.IsNullOrWhiteSpace(choiceText))
            {
                return;
            }

            string trimmedChoice = choiceText.Trim();
            fullChoiceHistory.Add(trimmedChoice);
            narrativeHistoryEntries.Add($"Player Decision: {trimmedChoice}");
        }

        public void RecordHistoryTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                return;
            }

            string trimmedTag = tag.Trim();
            narrativeHistoryEntries.Add($"Petition Outcome: {trimmedTag}");
        }

        public void RecordPetitionTranscript(IReadOnlyList<string> transcript)
        {
            if (transcript == null || transcript.Count == 0)
            {
                return;
            }

            RecordPetitionTranscript(string.Join("\n", transcript));
        }

        public void RecordPetitionTranscript(string transcript)
        {
            if (string.IsNullOrWhiteSpace(transcript))
            {
                return;
            }

            completedPetitionTranscripts.Add(transcript.Trim());
        }

        public string GetResourceSummary(GameLanguage language = GameLanguage.English)
        {
            StringBuilder resourceSummary = new StringBuilder();
            if (catalog != null && resourceState != null)
            {
                for (int i = 0; i < catalog.resources.Count; i++)
                {
                    ResourceData resourceDefinition = catalog.resources[i];
                    if (resourceDefinition == null) continue;

                    resourceSummary.Append($"{resourceDefinition.GetDisplayName(language)}: {resourceState.Get(resourceDefinition)}");
                    if (i < catalog.resources.Count - 1)
                    {
                        resourceSummary.Append(", ");
                    }
                }
            }
            return resourceSummary.ToString();
        }

        public string GetFullHistorySummary() =>
            fullChoiceHistory.Count > 0 ? string.Join("; ", fullChoiceHistory) : "No decisions were recorded.";

        public string GetSnapshot(int narrativeHistoryEntryCount, int petitionTranscriptCount, GameLanguage language = GameLanguage.English)
        {
            int day = narrativeRunner != null ? narrativeRunner.Day : 1;
            string resourceSummary = GetResourceSummary(language);

            return $"Current Day: {day}\n" +
                   $"Kingdom Resources -> {resourceSummary}\n" +
                   $"Recent Narrative History: {GetRecentEntries(narrativeHistoryEntries, narrativeHistoryEntryCount)}\n" +
                   $"Completed Petition Conversations: {GetRecentEntries(completedPetitionTranscripts, petitionTranscriptCount)}";
        }

        private static string GetRecentEntries(List<string> entries, int count)
        {
            if (count <= 0 || entries.Count == 0)
            {
                return "None";
            }

            int firstIndex = System.Math.Max(0, entries.Count - count);
            return string.Join("\n\n", entries.GetRange(firstIndex, entries.Count - firstIndex));
        }
    }
}
