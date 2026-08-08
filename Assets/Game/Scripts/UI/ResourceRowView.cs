using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI
{
    /// <summary>
    /// Displays one resource row with icon and value text.
    /// </summary>
    public class ResourceRowView : MonoBehaviour
    {
        public Image icon;
        public TMP_Text label;

        /// <summary>
        /// Sets the row text from resource display name and value.
        /// </summary>
        public void SetValue(string displayName, int value)
        {
            if (label != null)
            {
                label.text = $"{displayName}: {value}";
            }
        }

        /// <summary>
        /// Sets the row icon and toggles its visibility.
        /// </summary>
        public void SetIcon(Sprite sprite)
        {
            if (icon != null)
            {
                icon.sprite = sprite;
                icon.gameObject.SetActive(sprite != null);
            }
        }
    }
}
