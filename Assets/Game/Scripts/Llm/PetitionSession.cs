using System;
using System.Collections.Generic;

namespace Game.Scripts.Llm
{
    /// <summary>Tracks the state of a single multi-turn petition audience including turn budget, conversation history, and resolution status.</summary>
    /// <remarks>Owns the Groq message list sent to <see cref="LlmReactionClient.RequestPetitionTurn"/> and a human-readable transcript for UI display.
    /// The petitioner's opening line is recorded as the first assistant turn so every later request sees the full conversation.</remarks>
    public class PetitionSession : AudienceSession
    {
        /// <summary>Human-readable log of ruler inputs and petitioner reactions for UI transcript display.</summary>
        private readonly List<string> transcript = new List<string>();
        /// <summary>Holds the current player input until RecordReply commits it to history and transcript.</summary>
        private string pendingPlayerInput;

        /// <summary>Total number of turns allocated for this audience at construction time.</summary>
        private int TurnBudget { get; }
        /// <summary>Number of turns still available before the audience ends.</summary>
        public int TurnsRemaining { get; private set; }
        /// <summary>True when no turns remain and the petitioner should deliver a closing statement.</summary>
        public bool TurnsExhausted => TurnsRemaining <= 0;

        /// <summary>The most recent proposal-phase resolution, or null if the petitioner has not yet proposed.</summary>
        public PetitionResolution LastProposal { get; private set; }
        /// <summary>True when a proposal has been received and the game is waiting for ruler confirmation.</summary>
        public bool AwaitingConfirmation => LastProposal != null;

        /// <summary>Creates a new petition session with the given turn budget.</summary>
        /// <param name="turnBudget">Number of turns for this audience; clamped to a minimum of 1.</param>
        public PetitionSession(int turnBudget)
        {
            TurnBudget = turnBudget < 1 ? 1 : turnBudget;
            TurnsRemaining = TurnBudget;
        }

        /// <summary>Commits the petitioner's opening line as the first assistant turn so later requests know how the audience began.</summary>
        /// <param name="rawOpening">Raw assistant message content stored verbatim in conversation history.</param>
        /// <param name="openingReaction">Human-readable opening line for the UI transcript.</param>
        public void RecordOpening(string rawOpening, string openingReaction)
        {
            RecordOpeningInHistory(rawOpening);
            if (!string.IsNullOrWhiteSpace(openingReaction))
            {
                transcript.Add($"Petitioner: {openingReaction.Trim()}");
            }
        }

        /// <summary>Builds the complete message list for the next petition API call: stored system prompt, committed history, and the new ruler input.</summary>
        /// <param name="playerInput">The ruler's latest input for this turn.</param>
        /// <returns>A message list ready for <see cref="LlmReactionClient.RequestPetitionTurn"/>.</returns>
        public List<GroqApiMessage> BuildMessagesForSubmission(string playerInput)
        {
            LastProposal = null;
            pendingPlayerInput = playerInput;
            return BuildNextTurnMessages(playerInput);
        }

        /// <summary>Commits a completed petition turn to history and transcript, decrementing turn budget when appropriate.</summary>
        /// <param name="resolution">The parsed resolution from this turn; may be null on parse failure.</param>
        /// <param name="rawContent">Raw assistant message content stored verbatim in conversation history.</param>
        /// <param name="detectSpam">When true, only deducts a turn if the resolution is classified as spam.</param>
        public void RecordReply(PetitionResolution resolution, string rawContent, bool detectSpam = false)
        {
            RecordTurnInHistory(pendingPlayerInput, rawContent);
            transcript.Add($"Ruler: {pendingPlayerInput}");
            pendingPlayerInput = null;

            string reply = resolution != null && !string.IsNullOrWhiteSpace(resolution.reaction)
                ? resolution.reaction
                : null;
            if (reply != null)
            {
                transcript.Add($"Petitioner: {reply}");
            }

            bool shouldDeduct = !detectSpam || (resolution != null && resolution.isSpam);
            if (shouldDeduct)
            {
                TurnsRemaining = Math.Max(0, TurnsRemaining - 1);
            }

            if (resolution != null && resolution.IsProposal)
            {
                LastProposal = resolution;
            }
        }

        /// <summary>Returns the human-readable transcript of all ruler/petitioner exchanges so far.</summary>
        public IReadOnlyList<string> GetTranscript() => transcript;
    }
}
