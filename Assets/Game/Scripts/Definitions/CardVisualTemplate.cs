using UnityEngine;

namespace Game.Scripts.Definitions
{
    /// <summary>A complete card look: one authoring pick that defines the card's background and border.</summary>
    [CreateAssetMenu(fileName = "CardTemplate_Slug", menuName = "Weave/Card Visual Template", order = 1)]
    public class CardVisualTemplate : ScriptableObject
    {
        [Tooltip("Card background pattern for this template.")]
        public Sprite background;

        [Tooltip("Card border frame for this template. Leave empty for no border.")]
        public Sprite border;
    }
}
