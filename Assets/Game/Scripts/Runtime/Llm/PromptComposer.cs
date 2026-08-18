using System.Text;

namespace Game.Scripts.Runtime.Llm
{
    /// <summary>Joins prompt sections in order, auto-spacing with one blank line between non-empty parts.</summary>
    public class PromptComposer
    {
        private readonly StringBuilder sb = new StringBuilder();

        /// <summary>Appends raw text with no [Label] wrapper (e.g. system instructions block).</summary>
        public PromptComposer AddRaw(string text)
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                sb.Append(text);
            }
            return this;
        }

        /// <summary>Appends a "[Label]\ncontent" block, skipped entirely if content is empty.</summary>
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

        public override string ToString() => sb.ToString();
    }
}
