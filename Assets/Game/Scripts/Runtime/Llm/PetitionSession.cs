using System.Collections.Generic;
using Game.Scripts.Definitions;

namespace Game.Scripts.Runtime.Llm
{
    /// <summary>
    /// Multi-turn conversation state for one petition audience: turn budget, message history,
    /// and any proposal awaiting player confirmation. Create per shown petition card; discard on resolve.
    /// </summary>
    public class PetitionSession
    {
        private readonly List<GroqApiMessage> history = new List<GroqApiMessage>();

        public int MaxTurns { get; }
        public int TurnsUsed { get; private set; }

        /// <summary>
        /// True if the next player submission would be the final allowed turn.
        /// Read before calling <see cref="BuildMessagesForSubmission"/>, which increments <see cref="TurnsUsed"/>.
        /// </summary>
        public bool NextTurnIsFinal => TurnsUsed + 1 >= MaxTurns;

        public PetitionResolution LastProposal { get; private set; }
        public bool AwaitingConfirmation => LastProposal != null;

        /// <summary>True when the turn budget is spent with no confirmed proposal left to apply.</summary>
        public bool BudgetExhausted => TurnsUsed >= MaxTurns && !AwaitingConfirmation;

        public PetitionSession(int maxTurns)
        {
            MaxTurns = maxTurns < 1 ? 1 : maxTurns;
        }

        /// <summary>
        /// Builds the ordered message list for the next submission: a fresh system prompt
        /// (so turn/final-turn framing stays current), prior history, then the new user turn.
        /// Increments the turn counter and clears any pending proposal — confirmation must
        /// go through <see cref="LastProposal"/>, not this method.
        /// </summary>
        public List<GroqApiMessage> BuildMessagesForSubmission(
            string playerInput,
            SpeakerData speaker,
            string gameStateSnapshot,
            string situationalPrompt,
            IReadOnlyList<ResourceData> validResources,
            int clampMagnitude,
            string systemInstructionsTemplate)
        {
            LastProposal = null;
            TurnsUsed++;
            bool isFinalTurn = TurnsUsed >= MaxTurns;

            string systemPrompt = PetitionPromptBuilder.Build(
                speaker, gameStateSnapshot, situationalPrompt, validResources,
                clampMagnitude, systemInstructionsTemplate, TurnsUsed, MaxTurns, isFinalTurn);

            history.Add(new GroqApiMessage { role = "user", content = playerInput });

            List<GroqApiMessage> messages = new List<GroqApiMessage>(history.Count + 1)
            {
                new GroqApiMessage { role = "system", content = systemPrompt }
            };
            messages.AddRange(history);

            return messages;
        }

        /// <summary>
        /// Stores the assistant reply for continuity. Pass the cleaned raw response text so the
        /// model sees its prior turn verbatim on the next call.
        /// </summary>
        public void RecordReply(PetitionResolution resolution, string rawContent)
        {
            history.Add(new GroqApiMessage { role = "assistant", content = rawContent });

            if (resolution != null && resolution.IsProposal)
            {
                LastProposal = resolution;
            }
        }
    }
}
