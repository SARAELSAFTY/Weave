using UnityEngine;

namespace Game.Scripts.Definitions
{
    /// <summary>Data-driven visual template providing background and border sprites for individual cards.</summary>
    [CreateAssetMenu(fileName = "CardTemplate_Slug", menuName = "Weave/Card Visual Template", order = 1)]
    public class CardVisualTemplate : ScriptableObject
    {
        [Tooltip("Background sprite rendered behind the card content.")]
        public Sprite background;

        [Tooltip("Border sprite overlaid on top of the card background.")]
        public Sprite border;
    }
}
