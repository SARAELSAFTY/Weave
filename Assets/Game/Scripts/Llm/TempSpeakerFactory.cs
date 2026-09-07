using Game.Scripts.Definitions;
using UnityEngine;

namespace Game.Scripts.Llm
{
    /// <summary>Creates runtime-only SpeakerData instances for petition and chat audiences.</summary>
    /// <remarks>Generated commoners start with a blank display name: the caller shows no speaker label until the
    /// model names the persona in its opening reply, so the hardcoded "A Common Subject" placeholder never appears.</remarks>
    public static class TempSpeakerFactory
    {
        /// <summary>Creates a temporary commoner speaker whose persona invents a name, trade, and grievance.</summary>
        /// <param name="templates">Prompt templates providing the default commoner persona; may be null.</param>
        /// <returns>A runtime-only SpeakerData; the caller must destroy it when the audience ends.</returns>
        public static SpeakerData CreateCommoner(LlmPromptTemplates templates)
        {
            SpeakerData commoner = ScriptableObject.CreateInstance<SpeakerData>();
            commoner.name = string.Empty;
            commoner.assetName = string.Empty;
            commoner.llmPersonaPrompt = templates != null ? templates.defaultCommonerPersona : string.Empty;
            return commoner;
        }

        /// <summary>Names a generated speaker from the model's opening reply and reports whether anything changed.</summary>
        /// <param name="speaker">The temporary speaker to name.</param>
        /// <param name="speakerName">The model-supplied name; blank values are ignored.</param>
        /// <returns>True when the speaker was given a new display name.</returns>
        public static bool TryApplyGeneratedName(SpeakerData speaker, string speakerName)
        {
            if (speaker == null || string.IsNullOrWhiteSpace(speakerName))
            {
                return false;
            }

            // Long or multi-line names are a model glitch; keep a short single-line label.
            string trimmed = speakerName.Trim();
            trimmed = System.Text.RegularExpressions.Regex.Replace(trimmed, @"\s+", " ");
            if (trimmed.Length > 48)
            {
                trimmed = trimmed.Substring(0, 48).Trim();
            }

            if (trimmed.Length == 0)
            {
                return false;
            }

            // The name arrives only in the session language; reuse it for both so a mid-audience
            // language switch keeps showing the same name.
            speaker.displayNameLocalized = new Game.Scripts.Localization.LocalizedText { english = trimmed, arabic = trimmed };
            return true;
        }
    }
}
