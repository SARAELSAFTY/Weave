using System.Text;

namespace Game.Scripts.Runtime.Llm
{
    /// <summary>Fluent builder that assembles LLM system prompts from labeled sections separated by double newlines.</summary>
    public class PromptComposer
    {
        private readonly StringBuilder sb = new StringBuilder();

        /// <summary>Appends raw text to the prompt without section formatting; skips blank input.</summary>
        /// <param name="text">Raw text to append (typically system instructions).</param>
        /// <returns>This composer for method chaining.</returns>
        public PromptComposer AddRaw(string text)
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                sb.Append(text);
            }
            return this;
        }

        /// <summary>Appends a labeled section in [Label]\nContent format, preceded by a double newline if not first.</summary>
        /// <param name="label">Section header displayed in square brackets.</param>
        /// <param name="content">Section body text; the entire section is skipped if content is null or whitespace.</param>
        /// <returns>This composer for method chaining.</returns>
        public PromptComposer AddSection(string label, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return this;
            }

            if (sb.Length > 0)
            {
                sb.Append("\n\n");
            }

            sb.Append('[').Append(label).Append("]\n").Append(content.Trim());
            return this;
        }

        /// <summary>Returns the fully assembled prompt string.</summary>
        public override string ToString() => sb.ToString();
    }
}
