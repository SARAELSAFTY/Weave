using System;
using System.Collections.Generic;

namespace Game.Scripts.Llm
{
    /// <summary>Conversation state for one LLM audience: system prompt, message history, turn budget, proposals, and transcript.</summary>
    /// <remarks>Petition audiences use the budget, proposal, and transcript state; chat audiences run with
    /// unlimited turns and plain-text turns only. The opening line is recorded as the first assistant
    /// turn so every later request sees how the audience began.</remarks>
    public class AudienceSession
    {
        private readonly List<GroqApiMessage> history = new List<GroqApiMessage>();
        private string systemPrompt;
        private readonly List<string> transcript = new List<string>();
        private string pendingPlayerInput;

        public bool HasOpening { get; private set; }

        /// <summary>True when turns never run out; chat audiences set this, petition audiences do not.</summary>
        public bool UnlimitedTurns { get; }
        /// <summary>True when the audience ends with a proposal confirmation; false for plain-chat audiences.</summary>
        public bool ExpectsProposal { get; }
        public int TurnsRemaining { get; private set; }
        public bool TurnsExhausted => !UnlimitedTurns && TurnsRemaining <= 0;

        public PetitionResolution LastProposal { get; private set; }
        public bool AwaitingConfirmation => LastProposal != null;

        public AudienceSession(int turnBudget, bool unlimitedTurns, bool expectsProposal)
        {
            TurnsRemaining = turnBudget < 1 ? 1 : turnBudget;
            UnlimitedTurns = unlimitedTurns;
            ExpectsProposal = expectsProposal;
        }

        public void Initialize(string prompt)
        {
            systemPrompt = prompt;
        }

        public List<GroqApiMessage> BuildOpeningMessages(string seed)
        {
            return new List<GroqApiMessage>
            {
                new GroqApiMessage { role = "system", content = systemPrompt },
                new GroqApiMessage { role = "user", content = seed }
            };
        }

        protected List<GroqApiMessage> BuildNextTurnMessages(string playerInput)
        {
            List<GroqApiMessage> messages = new List<GroqApiMessage>(history.Count + 2)
            {
                new GroqApiMessage { role = "system", content = systemPrompt }
            };
            messages.AddRange(history);
            messages.Add(new GroqApiMessage { role = "user", content = playerInput });
            return messages;
        }

        protected void RecordOpeningInHistory(string rawOpening)
        {
            history.Add(new GroqApiMessage { role = "assistant", content = rawOpening });
            HasOpening = true;
        }

        protected void RecordTurnInHistory(string playerInput, string assistantContent)
        {
            history.Add(new GroqApiMessage { role = "user", content = playerInput });
            history.Add(new GroqApiMessage { role = "assistant", content = assistantContent });
        }

        public void RecordOpening(string rawOpening, string openingReaction)
        {
            RecordOpeningInHistory(rawOpening);
            if (!string.IsNullOrWhiteSpace(openingReaction))
            {
                transcript.Add($"Petitioner: {openingReaction.Trim()}");
            }
        }

        public void RecordOpening(string rawOpening)
        {
            RecordOpeningInHistory(rawOpening);
        }

        public List<GroqApiMessage> BuildMessagesForSubmission(string playerInput)
        {
            LastProposal = null;
            pendingPlayerInput = playerInput;
            return BuildNextTurnMessages(playerInput);
        }

        public List<GroqApiMessage> BuildTurnMessages(string playerInput)
        {
            return BuildNextTurnMessages(playerInput);
        }

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

            bool shouldDeduct = !UnlimitedTurns && (!detectSpam || (resolution != null && resolution.isSpam));
            if (shouldDeduct)
            {
                TurnsRemaining = Math.Max(0, TurnsRemaining - 1);
            }

            if (resolution != null && resolution.IsProposal)
            {
                LastProposal = resolution;
            }
        }

        public void RecordTurn(string playerInput, string reply)
        {
            RecordTurnInHistory(playerInput, reply);
        }

        public IReadOnlyList<string> GetTranscript() => transcript;
    }
}
