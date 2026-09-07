using System.Collections.Generic;

namespace Game.Scripts.Llm
{
    /// <summary>Shared conversation state for one LLM audience: the system prompt plus the Groq message history.</summary>
    /// <remarks>Subclasses add their audience-specific state on top (petition transcript, turn budget, proposals).
    /// The opening line is recorded as the first assistant turn so every later request sees how the audience began.</remarks>
    public abstract class AudienceSession
    {
        /// <summary>Full Groq conversation history (alternating user/assistant turns) sent with every submission.</summary>
        private readonly List<GroqApiMessage> history = new List<GroqApiMessage>();
        /// <summary>System prompt built once per audience; reused verbatim for every request.</summary>
        private string systemPrompt;

        /// <summary>True once the opening line has been recorded, so later requests include it in history.</summary>
        public bool HasOpening { get; private set; }

        /// <summary>Stores the system prompt for the whole audience; call once before building any messages.</summary>
        /// <param name="prompt">Complete system prompt assembled by <see cref="SpeakerPromptBuilder"/>.</param>
        public void Initialize(string prompt)
        {
            systemPrompt = prompt;
        }

        /// <summary>Builds the message list for the opening line request: the system prompt plus the seed as the first user turn.</summary>
        /// <param name="seed">Situational seed prompting the speaker to open the audience.</param>
        /// <returns>A message list ready for the <see cref="LlmReactionClient"/> request methods.</returns>
        public List<GroqApiMessage> BuildOpeningMessages(string seed)
        {
            return new List<GroqApiMessage>
            {
                new GroqApiMessage { role = "system", content = systemPrompt },
                new GroqApiMessage { role = "user", content = seed }
            };
        }

        /// <summary>Builds the complete message list for the next API call: stored system prompt, committed history, and the new ruler input.</summary>
        /// <param name="playerInput">The ruler's latest input for this turn.</param>
        /// <returns>A message list ready for the <see cref="LlmReactionClient"/> request methods.</returns>
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

        /// <summary>Commits the speaker's opening line as the first assistant turn so later requests know how the audience began.</summary>
        /// <param name="rawOpening">Raw assistant message content stored verbatim in conversation history.</param>
        protected void RecordOpeningInHistory(string rawOpening)
        {
            history.Add(new GroqApiMessage { role = "assistant", content = rawOpening });
            HasOpening = true;
        }

        /// <summary>Commits a completed audience turn to history as the next user/assistant pair.</summary>
        /// <param name="playerInput">The ruler's input for this turn.</param>
        /// <param name="assistantContent">Raw assistant message content stored verbatim in conversation history.</param>
        protected void RecordTurnInHistory(string playerInput, string assistantContent)
        {
            history.Add(new GroqApiMessage { role = "user", content = playerInput });
            history.Add(new GroqApiMessage { role = "assistant", content = assistantContent });
        }
    }
}
