using System.Text;

namespace Game.Scripts.Runtime.Llm
{
    /// <summary>Builds a system prompt for generating a concise epilogue at run end.</summary>
    public static class EpiloguePromptBuilder
    {
        public static string Build(int dayCount, string collapsedResourceName, string finalResourceSummary, string fullChoiceHistory, string systemInstructionsTemplate = null)
        {
            StringBuilder promptBuilder = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(systemInstructionsTemplate))
            {
                promptBuilder.Append(systemInstructionsTemplate);
            }
            else
            {
                promptBuilder.Append("[SYSTEM INSTRUCTIONS]\n")
                    .Append("You are a Royal Chronicler writing a concise epilogue for a narrative strategy game.\n")
                    .Append("RULES:\n")
                    .Append("1. Write a short 2 to 3 sentence historical epilogue summarizing how this ruler's reign ended.\n")
                    .Append("2. Mention the length of the reign, the collapsed resource (or cause of collapse), and reference their actual decisions.\n")
                    .Append("3. Output ONLY the spoken or written epilogue text. No titles, headers, quotation marks, parentheses (), or asterisks * *.\n")
                    .Append("4. Keep the tone calm, sober, and direct.\n")
                    .Append("5. Hard limit: no more than 60 words total. Do not pad.\n\n");
            }

            promptBuilder.Append("[REIGN RECORD]\n")
                .Append($"Reign Length: {dayCount} day{(dayCount == 1 ? "" : "s")}\n")
                .Append($"Cause of Collapse: {collapsedResourceName} fell to zero\n")
                .Append($"Final Kingdom State: {finalResourceSummary}\n")
                .Append($"Full Decision History (in order): {fullChoiceHistory}");

            return promptBuilder.ToString();
        }
    }
}
