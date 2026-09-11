using System.Text;
using Game.Scripts.Localization;

namespace Game.Scripts.Definitions
{
    /// <summary>Formats authored resource deltas for the swipe UI, e.g. "+10 Gold / -5 Army".</summary>
    public static class ResourceChangeFormatter
    {
        /// <summary>Returns a compact signed tag list, or empty when the change has no non-zero values.</summary>
        public static string Format(ResourceChange change, GameLanguage language)
        {
            if (change.values == null || change.values.Length == 0)
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder();
            foreach (ResourceValue entry in change.values)
            {
                if (entry.resource == null || entry.value == 0)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append(" / ");
                }

                if (entry.value > 0)
                {
                    builder.Append('+');
                }

                builder.Append(entry.value);
                builder.Append(' ');
                builder.Append(entry.resource.GetDisplayName(language));
            }

            return builder.ToString();
        }

        /// <summary>Appends a resource tag line under a choice label when the change is non-empty.</summary>
        public static string WithTags(string choiceLabel, ResourceChange change, GameLanguage language)
        {
            string tags = Format(change, language);
            if (string.IsNullOrEmpty(tags))
            {
                return choiceLabel ?? string.Empty;
            }

            string label = choiceLabel ?? string.Empty;
            return string.IsNullOrEmpty(label) ? tags : label + "\n" + tags;
        }
    }
}
