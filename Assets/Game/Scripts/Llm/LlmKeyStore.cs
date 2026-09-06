using System;
using System.Text;
using UnityEngine;

namespace Game.Scripts.Llm
{
    /// <summary>Result of validating a candidate Groq API key against the models endpoint.</summary>
    public enum ApiKeyValidationResult
    {
        /// <summary>Groq accepted the key (HTTP 200).</summary>
        Valid,
        /// <summary>Groq rejected the key (HTTP 401).</summary>
        Invalid,
        /// <summary>The validation probe could not reach Groq.</summary>
        Unreachable
    }

    /// <summary>Stores the player's optional Groq API key locally and tracks session fallback state.</summary>
    /// <remarks>The key lives only in PlayerPrefs on the player's device and is sent only in direct
    /// requests to api.groq.com; it never transits the shared proxy.</remarks>
    public static class LlmKeyStore
    {
        private const string PrefKey = "groq.byok.key";
        private const string ChosenPrefKey = "groq.byok.chosen";

        /// <summary>True once the player picked a line (own key saved or shared confirmed); gates the first-launch API screen.</summary>
        public static bool ServiceChosen => PlayerPrefs.GetInt(ChosenPrefKey, 0) == 1;

        /// <summary>Marks the first-launch API screen as answered so it stops appearing; never un-set,
        /// removing a key later must not bring the onboarding back.</summary>
        public static void MarkServiceChosen()
        {
            if (ServiceChosen)
            {
                return;
            }

            PlayerPrefs.SetInt(ChosenPrefKey, 1);
            PlayerPrefs.Save();
        }

        /// <summary>Set when a live request got HTTP 401; makes the client use the shared proxy
        /// until restart without deleting the stored key.</summary>
        public static bool SessionDisabled { get; set; }

        /// <summary>Raised after a key is saved or cleared so UI indicators can update live.</summary>
        public static event Action KeyChanged;

        private static void RaiseKeyChanged() => KeyChanged?.Invoke();

        /// <summary>True when a key is stored and this session has not fallen back to the proxy.</summary>
        public static bool HasActiveKey => !SessionDisabled && GetKey() != null;

        /// <summary>Returns the stored key, or null when none is stored.</summary>
        public static string GetKey()
        {
            string key = SanitizeKey(PlayerPrefs.GetString(PrefKey));
            return key.Length == 0 ? null : key;
        }

        /// <summary>Stores a key persistently (survives restarts). Callers should validate it
        /// first via <see cref="LlmReactionClient.ValidateApiKey"/> so a typo cannot replace a working key.</summary>
        public static void SaveKey(string key)
        {
            PlayerPrefs.SetString(PrefKey, SanitizeKey(key));
            PlayerPrefs.Save();
            SessionDisabled = false;
            RaiseKeyChanged();
        }

        /// <summary>Returns the key with every character outside <c>[A-Za-z0-9_-]</c> removed.</summary>
        /// <remarks>Pastes into the RTL-capable input field can carry an interior space or newline,
        /// wrapping quotes, or bidi marks, and <c>UnityWebRequest.SetRequestHeader</c> throws on those.</remarks>
        public static string SanitizeKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder(key.Length);
            foreach (char character in key)
            {
                bool allowed = (character >= 'a' && character <= 'z')
                    || (character >= 'A' && character <= 'Z')
                    || (character >= '0' && character <= '9')
                    || character == '_'
                    || character == '-';

                if (allowed)
                {
                    builder.Append(character);
                }
            }

            return builder.ToString();
        }

        /// <summary>Deletes the stored key and clears the session fallback flag.</summary>
        public static void ClearKey()
        {
            PlayerPrefs.DeleteKey(PrefKey);
            PlayerPrefs.Save();
            SessionDisabled = false;
            RaiseKeyChanged();
        }
    }
}
