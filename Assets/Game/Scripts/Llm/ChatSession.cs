using System.Collections.Generic;

namespace Game.Scripts.Llm
{
    /// <summary>Tracks one free chat audience on a chat card: system prompt and plain-text conversation history.</summary>
    /// <remarks>Chat is talk only - no JSON contract, no turn budget, no resource or history effects. The opening
    /// line is recorded as the first assistant turn so every later request sees the full conversation.</remarks>
    public class ChatSession : AudienceSession
    {
        /// <summary>Builds the complete message list for the next chat API call: stored system prompt, committed history, and the new ruler input.</summary>
        /// <param name="playerInput">The ruler's latest input for this turn.</param>
        /// <returns>A message list ready for <see cref="LlmReactionClient.RequestChatTurn"/>.</returns>
        public List<GroqApiMessage> BuildTurnMessages(string playerInput)
        {
            return BuildNextTurnMessages(playerInput);
        }

        /// <summary>Commits the speaker's opening line as the first assistant turn so later requests know how the audience began.</summary>
        /// <param name="rawOpening">Sanitized opening line stored verbatim in conversation history.</param>
        public void RecordOpening(string rawOpening)
        {
            RecordOpeningInHistory(rawOpening);
        }

        /// <summary>Commits a completed chat turn to history.</summary>
        /// <param name="playerInput">The ruler's input for this turn.</param>
        /// <param name="reply">The sanitized speaker reply.</param>
        public void RecordTurn(string playerInput, string reply)
        {
            RecordTurnInHistory(playerInput, reply);
        }
    }
}
