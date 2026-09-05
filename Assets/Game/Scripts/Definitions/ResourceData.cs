using Game.Scripts.Localization;
using UnityEngine;

namespace Game.Scripts.Definitions
{
    /// <summary>Defines a tracked gameplay resource with icon, starting value, collapse threshold, and low-value warning behavior.</summary>
    [CreateAssetMenu(fileName = "Res_NewResource", menuName = "Weave/Resource Data", order = 2)]
    public class ResourceData : NamedGameAsset
    {
        [Header("Display")]
        [Tooltip("Icon sprite representing this resource in the UI.")]
        public Sprite icon;

        [Header("Values")]
        [Tooltip("Initial value assigned to this resource at the start of a new game.")]
        public int defaultStartingValue = 50;

        [Tooltip("Value at or below which the resource is considered collapsed.")]
        public int collapseThreshold = 0;

        [Header("Low-Value Warning")]
        [Range(0, 100)]
        [Tooltip("Percentage of max at which a low-value warning triggers (0–100%).")]
        public int warningThresholdPercent = 30;

        [Min(0)]
        [Tooltip("Minimum number of cards between consecutive warnings for this resource.")]
        public int warningCooldownCards = 5;

        [Header("Speaker")]
        [UnityEngine.Serialization.FormerlySerializedAs("warningSpeaker")]
        [UnityEngine.Serialization.FormerlySerializedAs("visualSpeaker")]
        [Tooltip("The speaker associated with this resource. Provides the portrait for warning and collapse cards, as well as the LLM persona prompt.")]
        public SpeakerData speaker;

        [Header("Collapse Ending Fallback")]
        [Tooltip("Shown when LLM collapse generation fails or is unavailable (English).")]
        [TextArea(3, 6)]
        public string collapseEndingFallbackEnglish;

        [Tooltip("Shown when LLM collapse generation fails or is unavailable (Arabic).")]
        [TextArea(3, 6)]
        public string collapseEndingFallbackArabic;


        /// <summary>Returns the localized collapse fallback text for the given language.</summary>
        public string GetCollapseFallbackText(GameLanguage language)
        {
            if (language == GameLanguage.Arabic && !string.IsNullOrWhiteSpace(collapseEndingFallbackArabic))
                return collapseEndingFallbackArabic;
            return string.IsNullOrWhiteSpace(collapseEndingFallbackEnglish)
                ? string.Empty
                : collapseEndingFallbackEnglish;
        }
    }
}
