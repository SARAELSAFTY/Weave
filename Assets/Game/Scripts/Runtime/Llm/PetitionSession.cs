using System;
using System.Collections.Generic;
using Game.Scripts.Definitions;
using Game.Scripts.Localization;

namespace Game.Scripts.Runtime.Llm
{
    /// <summary>
    /// Multi-turn conversation state for one petition audience: spam-dot budget, message history,
    /// and any proposal awaiting player confirmation. Create per shown petition card; discard on resolve.
    /// </summary>
    public class PetitionSession
    {
        private readonly List<GroqApiMessage> history = new List<GroqApiMessage>();
        private readonly List<string> transcript = new List<string>();
        private string pendingPlayerInput;

        public int DotBudget { get; }
        public int DotsRemaining { get; private set; }
        public bool DotsExhausted => DotsRemaining <= 0;

        public PetitionResolution LastProposal { get; private set; }
        public bool AwaitingConfirmation => LastProposal != null;

        public PetitionSession(int dotBudget)
        {
            DotBudget = dotBudget < 1 ? 1 : dotBudget;
            DotsRemaining = DotBudget;
        }

        /// <summary>
        /// Builds the prompt for the next petition turn.
        /// The player's message is buffered until <see cref="RecordReply"/> so a failed API call
        /// never pollutes history. Clears any pending proposal - confirmation must go through
        /// <see cref="LastProposal"/>, not this method.
        /// </summary>
        public List<GroqApiMessage> BuildMessagesForSubmission(
            string playerInput,
            SpeakerData speaker,
            string gameStateSnapshot,
            string situationalPrompt,
            IReadOnlyList<ResourceData> validResources,
            int clampMagnitude,
            LlmPromptTemplates templates,
            GameLanguage language = GameLanguage.English)
        {
            LastProposal = null;
            pendingPlayerInput = playerInput;

            string systemPrompt = SpeakerPromptBuilder.BuildPetitionTurnPrompt(
                speaker, gameStateSnapshot, situationalPrompt, validResources,
                clampMagnitude, templates, language);

            List<GroqApiMessage> messages = new List<GroqApiMessage>(history.Count + 2)
            {
                new GroqApiMessage { role = "system", content = systemPrompt }
            };
            messages.AddRange(history);
            messages.Add(new GroqApiMessage { role = "user", content = playerInput });
            return messages;
        }

        /// <summary>
        /// Commits the buffered player message and stores the assistant reply for continuity.
        /// Pass the cleaned raw response text so the model sees its prior turn verbatim on the next call.
        /// </summary>
        public void RecordReply(PetitionResolution resolution, string rawContent)
        {
            history.Add(new GroqApiMessage { role = "user", content = pendingPlayerInput });
            transcript.Add($"Ruler: {pendingPlayerInput}");
            pendingPlayerInput = null;

            history.Add(new GroqApiMessage { role = "assistant", content = rawContent });

            string reply = resolution != null && !string.IsNullOrWhiteSpace(resolution.reaction)
                ? resolution.reaction
                : null;
            if (reply != null)
            {
                transcript.Add($"Petitioner: {reply}");
            }

            if (resolution != null && resolution.isSpam)
            {
                DotsRemaining = Math.Max(0, DotsRemaining - 1);
            }

            if (resolution != null && resolution.IsProposal)
            {
                LastProposal = resolution;
            }
        }

        public IReadOnlyList<string> GetTranscript() => transcript;
    }
}
