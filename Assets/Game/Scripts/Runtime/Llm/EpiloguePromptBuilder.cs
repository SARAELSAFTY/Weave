using System.Text;

namespace Game.Scripts.Runtime.Llm
{
    public static class EpiloguePromptBuilder
    {
        public static string Build(int dayCount, string collapsedResourceName, string finalResourceSummary, string fullChoiceHistory, string systemInstructionsTemplate = null)
        {
            StringBuilder promptBuilder = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(systemInstructionsTemplate))
            {
                promptBuilder.Append(systemInstructionsTemplate);
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
