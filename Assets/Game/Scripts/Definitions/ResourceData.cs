using UnityEngine;

namespace Game.Scripts.Definitions
{
    [CreateAssetMenu(fileName = "Res_NewResource", menuName = "Weave/Resource Data", order = 2)]
    public class ResourceData : NamedGameAsset
    {
        [Tooltip("Resource icon image.")]
        public Sprite icon;

        [Tooltip("Starting value for this resource at the beginning of a run.")]
        public int defaultStartingValue = 50;

        [Header("Collapse Threshold")]
        [Tooltip("Resource triggers its collapse ending at or below this value.")]
        public int collapseThreshold = 0;

        [Header("Warning Threshold")]
        [Range(0, 100), Tooltip("Resource triggers a Warning reaction at or below this percent of its starting value.")]
        public int warningThresholdPercent = 30;

        [Header("Warning Alert Speaker")]
        [Tooltip("Speaker who reacts when this resource crosses the Warning threshold.")]
        public SpeakerData warningSpeaker;

        [Min(0), Tooltip("Minimum story cards that must pass after a warning before this resource can warn again.")]
        public int warningCooldownCards = 5;

    }
}
