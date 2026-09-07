using System;
using System.Collections.Generic;
using Game.Scripts.Definitions;
using Game.Scripts.Localization;
using UnityEngine;

namespace Game.Scripts.Llm
{
    /// <summary>Resolves player-visible fallback lines from <see cref="LlmPromptTemplates"/>, falling back to
    /// the hardcoded <see cref="FallbackStrings"/> copies only when the asset or a field is missing, and sends
    /// single-turn speaker-line requests so callers handle one success path instead of three failure modes.</summary>
    public static class LlmFallbackText
    {
        /// <summary>Description text when a speaker reaction request fails.</summary>
        public static string Reaction(LlmPromptTemplates templates, GameLanguage language) =>
            GetLocalized(templates, language, t => t.reactionFallback, FallbackStrings.ReactionUnavailable);

        /// <summary>Description text when a low-resource warning reaction request fails.</summary>
        public static string Warning(LlmPromptTemplates templates, GameLanguage language) =>
            GetLocalized(templates, language, t => t.warningFallback, FallbackStrings.WarningUnavailable);

        /// <summary>Opening line when the petitioner's first line request fails.</summary>
        public static string PetitionOpening(LlmPromptTemplates templates, GameLanguage language) =>
            GetLocalized(templates, language, t => t.petitionOpeningFallback, FallbackStrings.PetitionOpeningUnavailable);

        /// <summary>Closing line when the audience is exhausted and the closing-line request fails.</summary>
        public static string PetitionClosing(LlmPromptTemplates templates, GameLanguage language) =>
            GetLocalized(templates, language, t => t.petitionClosingFallback, FallbackStrings.PetitionClosingLine);

        /// <summary>Petition error message when a submission was rate limited.</summary>
        public static string PetitionRateLimited(LlmPromptTemplates templates, GameLanguage language) =>
            GetLocalized(templates, language, t => t.petitionRateLimitedMessage, FallbackStrings.PetitionRateLimited);

        /// <summary>Petition error message when a submission fails for any other reason.</summary>
        public static string PetitionSendFailed(LlmPromptTemplates templates, GameLanguage language) =>
            GetLocalized(templates, language, t => t.petitionSendFailedMessage, FallbackStrings.PetitionSendFailed);

        /// <summary>Label on the petition confirm button while a proposal awaits confirmation.</summary>
        public static string PetitionConfirmLabel(LlmPromptTemplates templates, GameLanguage language) =>
            GetLocalized(templates, language, t => t.petitionConfirmButtonLabel, FallbackStrings.Confirm);

        /// <summary>Label on the confirm button while a chat card is open, ending the audience.</summary>
        public static string ChatEndLabel(LlmPromptTemplates templates, GameLanguage language) =>
            GetLocalized(templates, language, t => t.chatEndButtonLabel, FallbackStrings.EndAudience);

        // Prefers the editable template line; only a missing asset or blank field drops to the hardcoded copy.
        private static string GetLocalized(LlmPromptTemplates templates, GameLanguage language,
            Func<LlmPromptTemplates, LocalizedText> selector, Func<GameLanguage, string> hardFallback)
        {
            if (templates != null)
            {
                LocalizedText text = selector(templates);
                if (!text.IsEmpty)
                {
                    return text.Get(language);
                }
            }

            return hardFallback(language);
        }

        /// <summary>Builds the persona system prompt and sends a single-turn reaction request, collapsing every
        /// failure mode (missing client, missing templates, HTTP error, empty response) into one callback with
        /// the given fallback line.</summary>
        /// <param name="client">LLM client; may be null, in which case the fallback line is delivered immediately.</param>
        /// <param name="templates">Prompt templates; may be null.</param>
        /// <param name="resources">Resource list for the prompt's terminology section; may be null.</param>
        /// <param name="speaker">Speaker providing the persona section.</param>
        /// <param name="gameStateSnapshot">Serialized kingdom state injected as the State section.</param>
        /// <param name="seed">Situational prompt injected as the Situation section.</param>
        /// <param name="language">Target language.</param>
        /// <param name="fallbackText">Line to deliver instead of a model response whenever generation is unavailable.</param>
        /// <param name="onLine">Called with either the sanitized model text or the fallback line; never both paths skipped.</param>
        public static void RequestSpeakerLine(LlmReactionClient client, LlmPromptTemplates templates,
            IReadOnlyList<ResourceData> resources, SpeakerData speaker, string gameStateSnapshot,
            string seed, GameLanguage language, string fallbackText, Action<string> onLine)
        {
            if (client == null)
            {
                Debug.LogWarning("[LlmFallbackText] LLM client is missing; showing fallback line.");
                onLine?.Invoke(fallbackText);
                return;
            }

            string fullSystemPrompt = SpeakerPromptBuilder.BuildPersonaPrompt(
                speaker, gameStateSnapshot, seed, templates, language, resources);
            string userMessage = templates != null ? templates.singleTurnUserMessage : null;

            client.RequestReaction(fullSystemPrompt, userMessage, language, onLine, error =>
            {
                Debug.LogWarning($"[LlmFallbackText] Speaker line request failed: {error}");
                onLine?.Invoke(fallbackText);
            });
        }
    }
}
