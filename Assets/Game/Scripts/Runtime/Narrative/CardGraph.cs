using System;
using System.Collections.Generic;
using Game.Scripts.Definitions;

namespace Game.Scripts.Runtime.Narrative
{
    /// <summary>Provides shared lookup and link validation helpers for narrative cards.</summary>
    public static class CardGraph
    {
        /// <summary>Builds a lookup table from card ID to card data.</summary>
        public static Dictionary<string, CardData> BuildLookup(NarrativeDatabase database, Action<string> onIssue = null)
        {
            Dictionary<string, CardData> lookup = new Dictionary<string, CardData>();
            if (database == null || database.cards == null)
            {
                return lookup;
            }

            foreach (CardData card in database.cards)
            {
                if (card == null)
                {
                    onIssue?.Invoke("Database contains a missing card reference.");
                    continue;
                }

                if (string.IsNullOrEmpty(card.cardId))
                {
                    onIssue?.Invoke($"'{card.name}' has no Card ID.");
                    continue;
                }

                if (!lookup.TryAdd(card.cardId, card))
                {
                    onIssue?.Invoke($"Duplicate Card ID '{card.cardId}' used by both '{lookup[card.cardId].name}' and '{card.name}'.");
                }
            }

            return lookup;
        }

        /// <summary>Builds a lookup table from speaker ID to speaker data.</summary>
        public static Dictionary<string, CouncilMemberData> BuildSpeakerLookup(NarrativeDatabase database, Action<string> onIssue = null)
        {
            Dictionary<string, CouncilMemberData> lookup = new Dictionary<string, CouncilMemberData>();
            if (database == null || database.speakers == null)
            {
                return lookup;
            }

            foreach (CouncilMemberData speaker in database.speakers)
            {
                if (speaker == null || string.IsNullOrEmpty(speaker.memberId))
                {
                    continue;
                }

                if (!lookup.TryAdd(speaker.memberId, speaker))
                {
                    onIssue?.Invoke($"Duplicate speaker ID '{speaker.memberId}' used by both '{lookup[speaker.memberId].name}' and '{speaker.name}'.");
                }
            }

            return lookup;
        }

        /// <summary>Returns true when a target card ID is set but not found in the lookup.</summary>
        public static bool IsBrokenLink(string targetCardId, Dictionary<string, CardData> lookup)
        {
            return !string.IsNullOrEmpty(targetCardId) && (lookup == null || !lookup.ContainsKey(targetCardId));
        }

        /// <summary>Returns true when the speaker is configured as an LLM speaker.</summary>
        public static bool IsLlmSpeaker(NarrativeDatabase database, CouncilMemberData speaker)
        {
            return speaker != null && speaker.isLlmSpeaker;
        }
    }
}