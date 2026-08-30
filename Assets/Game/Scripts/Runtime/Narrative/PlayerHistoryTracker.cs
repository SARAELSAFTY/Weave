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
            if (catalog == null || resourceState == null)
            {
                return string.Empty;
            }

            List<string> entries = new List<string>();
            foreach (ResourceData resourceDefinition in catalog.resources)
            {
                if (resourceDefinition == null) continue;
                entries.Add($"{resourceDefinition.GetDisplayName(language)}: {resourceState.Get(resourceDefinition)}");
            }

            return string.Join(", ", entries);
        }

        public string GetFullHistorySummary() =>
            fullChoiceHistory.Count > 0 ? string.Join("; ", fullChoiceHistory) : "No decisions were recorded.";

        public string GetSnapshot(int narrativeHistoryEntryCount, int petitionTranscriptCount, GameLanguage language = GameLanguage.English)
        {
            int day = narrativeRunner != null ? narrativeRunner.Day : 1;
            string resourceSummary = GetResourceSummary(language);

            StringBuilder snapshot = new StringBuilder();
            snapshot.Append($"Current Day: {day}\nKingdom Resources -> {resourceSummary}");

            string recentHistory = GetRecentEntries(narrativeHistoryEntries, narrativeHistoryEntryCount);
            if (recentHistory != null)
            {
                snapshot.Append($"\nRecent Narrative History: {recentHistory}");
            }

            string petitions = GetRecentEntries(completedPetitionTranscripts, petitionTranscriptCount);
            if (petitions != null)
            {
                snapshot.Append($"\nCompleted Petition Conversations: {petitions}");
            }

            return snapshot.ToString();
        }

        private static string GetRecentEntries(List<string> entries, int count)
        {
            if (count <= 0 || entries.Count == 0)
            {
                return null;
            }

            int firstIndex = System.Math.Max(0, entries.Count - count);
            return string.Join("\n\n", entries.GetRange(firstIndex, entries.Count - firstIndex));
        }
    }
}
