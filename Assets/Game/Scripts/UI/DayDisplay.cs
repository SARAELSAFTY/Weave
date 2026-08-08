using TMPro;
using UnityEngine;

namespace Game.Scripts.UI
{
    /// <summary>Displays the current day text in the UI.</summary>
    public class DayDisplay : MonoBehaviour
    {
        [SerializeField, Tooltip("Text component displaying day number.")] private TMP_Text dayText;
        [SerializeField, Tooltip("Format string for day text.")] private string format = "Day {0}";

        private void Awake()
        {
            if (dayText == null)
            {
                Debug.LogError($"[DayDisplay] Missing required Inspector reference '{nameof(dayText)}' on '{gameObject.name}'.", this);
                enabled = false;
            }
        }

        /// <summary>Sets the day text using the configured format string.</summary>
        public void SetDay(int day)
        {
            dayText.text = string.Format(format, day);
        }
    }
}
