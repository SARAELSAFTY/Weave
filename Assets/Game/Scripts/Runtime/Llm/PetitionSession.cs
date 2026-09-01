using System;
using System.Collections.Generic;
using Game.Scripts.Definitions;
using Game.Scripts.Localization;

namespace Game.Scripts.Runtime.Llm
{
    /// <summary>Tracks the state of a single multi-turn petition audience including turn budget, conversation history, and resolution status.</summary>
    /// <remarks>Owns the Groq message list sent to <see cref="LlmReactionClient.RequestPetitionTurn"/> and a human-readable transcript for UI display.</remarks>
    public class PetitionSession
    {
        /// <summary>Full Groq conversation history (system + alternating user/assistant turns) rebuilt each submission.</summary>
        private readonly List<GroqApiMessage> history = new List<GroqApiMessage>();
        /// <summary>Human-readable log of ruler inputs and petitioner reactions for UI transcript display.</summary>
        private readonly List<string> transcript = new List<string>();
        /// <summary>Holds the current player input until RecordReply commits it to history and transcript.</summary>
        private string pendingPlayerInput;

        /// <summary>Total number of turns allocated for this audience at construction time.</summary>
        public int TurnBudget { get; }
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

        /// <summary>Builds the complete message list for the next petition API call, including system prompt and history.</summary>
        /// <param name="playerInput">The ruler's latest input for this turn.</param>
        /// <param name="speaker">Speaker data providing persona and identity context.</param>
        /// <param name="gameStateSnapshot">Serialized kingdom state injected into the system prompt.</param>
        /// <param name="situationalPrompt">Optional situational context appended to the system prompt.</param>
        /// <param name="validResources">Resources the model may reference in resourceChanges deltas.</param>
        /// <param name="clampMagnitude">Maximum absolute delta value communicated to the model.</param>
        /// <param name="templates">Prompt templates providing system instructions and language strings.</param>
        /// <param name="language">Target language for language instruction selection.</param>
        /// <returns>A message list ready for <see cref="LlmReactionClient.RequestPetitionTurn"/>.</returns>
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

        /// <summary>Commits a completed petition turn to history and transcript, decrementing the turn budget.</summary>
        /// <param name="resolution">The parsed resolution from this turn; may be null on parse failure.</param>
        /// <param name="rawContent">Raw assistant message content stored verbatim in conversation history.</param>
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

            TurnsRemaining = Math.Max(0, TurnsRemaining - 1);

            if (resolution != null && resolution.IsProposal)
            {
                LastProposal = resolution;
            }
        }

        /// <summary>Returns the human-readable transcript of all ruler/petitioner exchanges so far.</summary>
        public IReadOnlyList<string> GetTranscript() => transcript;
    }
}
