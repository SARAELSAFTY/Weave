using System.Collections.Generic;
using System.Text;
using Game.Scripts.Definitions;
using Game.Scripts.Localization;

namespace Game.Scripts.Narrative
{
    /// <summary>Accumulates player choices, petition transcripts, and resource snapshots into a text summary consumed by LLM prompts.</summary>
    /// <remarks>Non-MonoBehaviour; constructed with <see cref="ResourceState"/>, <see cref="NarrativeRunner"/>, and <see cref="ResourceCatalog"/>.</remarks>
    public class PlayerHistoryTracker
    {
        private readonly ResourceState resourceState;
        private readonly NarrativeRunner narrativeRunner;
        private readonly ResourceCatalog catalog;
        private readonly List<string> fullChoiceHistory = new List<string>();
        private readonly List<string> narrativeHistoryEntries = new List<string>();
        private readonly List<string> completedPetitionTranscripts = new List<string>();

        /// <summary>Creates a tracker bound to the given resource state, narrative runner, and resource catalog.</summary>
        /// <param name="resourceState">Provides current resource values for snapshot summaries.</param>
        /// <param name="narrativeRunner">Provides the current day number for snapshot summaries.</param>
        /// <param name="catalog">Enumerates resources for display-name lookups in summaries.</param>
        public PlayerHistoryTracker(ResourceState resourceState, NarrativeRunner narrativeRunner, ResourceCatalog catalog)
        {
            this.resourceState = resourceState;
            this.narrativeRunner = narrativeRunner;
            this.catalog = catalog;
        }

        /// <summary>Records a player choice text into both the full history and the narrative history entries.</summary>
        /// <param name="choiceText">The display text of the choice the player made; blank values are ignored.</param>
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

        /// <summary>Records a petition outcome tag into the narrative history entries.</summary>
        /// <param name="tag">A short label describing the petition outcome; blank values are ignored.</param>
        public void RecordHistoryTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                return;
            }

            string trimmedTag = tag.Trim();
            narrativeHistoryEntries.Add($"Petition Outcome: {trimmedTag}");
        }

        /// <summary>Records a petition transcript from a list of lines, joining them with newlines before storing.</summary>
        /// <param name="transcript">The individual transcript lines; null or empty lists are ignored.</param>
        public void RecordPetitionTranscript(IReadOnlyList<string> transcript)
        {
            if (transcript == null || transcript.Count == 0)
            {
                return;
            }

            RecordPetitionTranscript(string.Join("\n", transcript));
        }

        /// <summary>Records a completed petition transcript as a single string.</summary>
        /// <param name="transcript">The full transcript text; blank values are ignored.</param>
        public void RecordPetitionTranscript(string transcript)
        {
            if (string.IsNullOrWhiteSpace(transcript))
            {
                return;
            }

            completedPetitionTranscripts.Add(transcript.Trim());
        }

        /// <summary>Returns a comma-separated string of all resource display names and their current values.</summary>
        /// <param name="language">The language used for resource display names.</param>
        /// <returns>A formatted resource summary, or empty if the catalog or state is null.</returns>
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

        /// <summary>Returns all recorded player choices joined by semicolons, or a fallback message if none exist.</summary>
        /// <param name="language">Language used for the fallback message when no choices were recorded.</param>
        public string GetFullHistorySummary(GameLanguage language = GameLanguage.English) =>
            fullChoiceHistory.Count > 0 ? string.Join("; ", fullChoiceHistory) : FallbackStrings.FullHistoryUnavailable(language);

        /// <summary>Builds a multi-line prompt snapshot containing the current day, resources, recent narrative history, and recent petition transcripts.</summary>
        /// <param name="narrativeHistoryEntryCount">Maximum number of recent narrative history entries to include.</param>
        /// <param name="petitionTranscriptCount">Maximum number of recent petition transcripts to include.</param>
        /// <param name="language">The language used for resource display names.</param>
        /// <returns>A formatted snapshot string suitable for inclusion in an LLM prompt.</returns>
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
