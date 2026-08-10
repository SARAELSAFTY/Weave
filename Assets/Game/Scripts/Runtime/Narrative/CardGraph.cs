using System;
using Game.Scripts.Definitions;

namespace Game.Scripts.Runtime.Narrative
{
    /// <summary>Provides shared validation helpers for narrative cards.</summary>
    public static class CardGraph
    {
        /// <summary>Validates database integrity, logging any issues found.</summary>
        public static bool ValidateDatabase(NarrativeDatabase database, Action<string> onIssue = null)
        {
            if (database == null)
            {
                onIssue?.Invoke("NarrativeDatabase is null.");
                return false;
            }

            bool valid = true;
            if (database.cards != null)
            {
                foreach (CardData card in database.cards)
                {
                    if (card == null)
                    {
                        onIssue?.Invoke("Database contains a missing card reference.");
                        valid = false;
                        continue;
                    }

                    if (card.speaker == null)
                    {
                        onIssue?.Invoke($"Card '{card.name}' has no speaker assigned.");
                        valid = false;
                    }
                }
            }

            return valid;
        }
    }
}