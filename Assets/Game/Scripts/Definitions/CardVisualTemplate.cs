using UnityEngine;

namespace Game.Scripts.Definitions
{
    /// <summary>Data-driven visual template providing per-part fill and border sprites for individual cards.</summary>
    [CreateAssetMenu(fileName = "CardTemplate_Slug", menuName = "Weave/Card Visual Template", order = 1)]
    public class CardVisualTemplate : ScriptableObject
    {
        [Tooltip("Fill sprite for the main card face behind both panels.")]
        public Sprite background;

        [Tooltip("Optional border sprite overlaid on the card background.")]
        public Sprite backgroundBorder;

        [Tooltip("Fill sprite for the top panel holding the speaker portrait or event illustration.")]
        public Sprite topPanel;

        [Tooltip("Optional border sprite overlaid on the top panel fill.")]
        public Sprite topPanelBorder;

        [Tooltip("Fill sprite for the bottom panel holding the card text.")]
        public Sprite bottomPanel;

        [Tooltip("Optional border sprite overlaid on the bottom panel fill.")]
        public Sprite bottomPanelBorder;
    }
}
