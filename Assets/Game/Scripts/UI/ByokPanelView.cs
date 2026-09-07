using System;
using Game.Scripts.Llm;
using Game.Scripts.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI
{
    /// <summary>Single-screen panel for choosing the court's AI line: our shared service or the player's own Groq key.</summary>
    /// <remarks>Shown automatically on first launch (until the player commits a choice) and afterwards
    /// from the AI Voice button on the start and pause screens. One screen holds the explanation, the
    /// two-option line choice, the key box with its check button, and Continue. A candidate key is
    /// validated against Groq's free models endpoint before being stored; the stored key persists in
    /// PlayerPrefs and is only ever sent directly to api.groq.com by <see cref="LlmReactionClient"/>,
    /// never through the shared proxy. The status line doubles as the shared-line indicator: opening the
    /// panel probes that service, and its result is shown while the shared option is selected.</remarks>
    public class ByokPanelView : LocalizedDisplay
    {
        [Tooltip("Title of the panel.")]
        [SerializeField] private TMP_Text titleText;

        [Tooltip("Concise, non-technical explanation of the AI voice and the two lines.")]
        [SerializeField] private TMP_Text infoText;

        [Tooltip("Option button selecting our shared line.")]
        [SerializeField] private Button sharedOptionButton;

        [Tooltip("Option button selecting the player's own key.")]
        [SerializeField] private Button ownKeyOptionButton;

        [Tooltip("Group shown while the own-key option is selected: input, check button, status.")]
        [SerializeField] private GameObject ownKeyGroup;

        [Tooltip("Input field for pasting the key.")]
        [SerializeField] private TMP_InputField keyInputField;

        [Tooltip("Button validating and storing the pasted key.")]
        [SerializeField] private Button checkButton;

        [Tooltip("Optional button opening the Groq key page in the browser.")]
        [SerializeField] private Button openGroqButton;

        [Tooltip("Optional button pasting the clipboard into the input; hidden on WebGL where clipboard reads are unavailable.")]
        [SerializeField] private Button pasteButton;

        [Tooltip("Status line for validation results and the saved-key confirmation.")]
        [SerializeField] private TMP_Text statusText;

        [Tooltip("Button committing the chosen line and closing the panel.")]
        [SerializeField] private Button continueButton;

        [Tooltip("Client used to validate a candidate key against Groq.")]
        [SerializeField] private LlmReactionClient llmClient;

        private const int KeyCharacterLimit = 64;
        private const string ConsoleKeysUrl = "https://console.groq.com/keys";
        private const string KeyPrefix = "gsk_";

        // Clipboard reads are unavailable in browsers; the paste button hides itself there.
        private static bool IsWebGl => Application.platform == RuntimePlatform.WebGLPlayer;

        /// <summary>True when the own-key option is selected; false for the shared line.</summary>
        private bool ownMode;

        /// <summary>True while the panel is showing; lets GameManager route Escape to closing it.</summary>
        public bool IsVisible => gameObject.activeInHierarchy;

        /// <summary>Shows the panel reflecting the stored key, if any, and refreshes the shared-line status.</summary>
        public void Show()
        {
            gameObject.SetActive(true);
            ownMode = !string.IsNullOrEmpty(LlmKeyStore.GetKey());
            keyInputField.text = LlmKeyStore.GetKey() ?? string.Empty;
            llmClient?.ProbeSharedService();
            RefreshMode();
        }

        /// <summary>Hides the panel.</summary>
        public void Hide() => gameObject.SetActive(false);

        private void Awake()
        {
            bool missingReference =
                InspectorValidation.RequireField(titleText, nameof(titleText), nameof(ByokPanelView), this) |
                InspectorValidation.RequireField(infoText, nameof(infoText), nameof(ByokPanelView), this) |
                InspectorValidation.RequireField(sharedOptionButton, nameof(sharedOptionButton), nameof(ByokPanelView), this) |
                InspectorValidation.RequireField(ownKeyOptionButton, nameof(ownKeyOptionButton), nameof(ByokPanelView), this) |
                InspectorValidation.RequireField(ownKeyGroup, nameof(ownKeyGroup), nameof(ByokPanelView), this) |
                InspectorValidation.RequireField(keyInputField, nameof(keyInputField), nameof(ByokPanelView), this) |
                InspectorValidation.RequireField(checkButton, nameof(checkButton), nameof(ByokPanelView), this) |
                InspectorValidation.RequireField(statusText, nameof(statusText), nameof(ByokPanelView), this) |
                InspectorValidation.RequireField(continueButton, nameof(continueButton), nameof(ByokPanelView), this);

            if (missingReference)
            {
                enabled = false;
                return;
            }

            keyInputField.characterLimit = KeyCharacterLimit;
            RtlTextHelper.EnsureRtlInputText(keyInputField);

            sharedOptionButton.onClick.AddListener(HandleSharedSelected);
            ownKeyOptionButton.onClick.AddListener(HandleOwnSelected);
            checkButton.onClick.AddListener(HandleCheckClicked);
            continueButton.onClick.AddListener(HandleContinueClicked);

            if (openGroqButton != null)
            {
                openGroqButton.onClick.AddListener(HandleOpenGroqClicked);
            }

            if (pasteButton != null)
            {
                pasteButton.onClick.AddListener(HandlePasteClicked);
                pasteButton.gameObject.SetActive(!IsWebGl);
            }
        }

        // Subscribed only while the panel is showing: Show() and the base refresh already re-render the
        // status line from LlmReactionClient.SharedService, so nothing is missed while hidden.
        protected override void OnEnable()
        {
            base.OnEnable();

            if (llmClient != null)
            {
                llmClient.SharedServiceChanged += HandleSharedServiceChanged;
            }
        }

        protected override void OnDisable()
        {
            if (llmClient != null)
            {
                llmClient.SharedServiceChanged -= HandleSharedServiceChanged;
            }

            base.OnDisable();
        }

        private void OnDestroy()
        {
            if (sharedOptionButton != null)
            {
                sharedOptionButton.onClick.RemoveListener(HandleSharedSelected);
            }

            if (ownKeyOptionButton != null)
            {
                ownKeyOptionButton.onClick.RemoveListener(HandleOwnSelected);
            }

            if (checkButton != null)
            {
                checkButton.onClick.RemoveListener(HandleCheckClicked);
            }

            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(HandleContinueClicked);
            }

            if (openGroqButton != null)
            {
                openGroqButton.onClick.RemoveListener(HandleOpenGroqClicked);
            }

            if (pasteButton != null)
            {
                pasteButton.onClick.RemoveListener(HandlePasteClicked);
            }
        }

        private void HandleOpenGroqClicked() => Application.OpenURL(ConsoleKeysUrl);

        private void HandlePasteClicked() => TryCaptureClipboard(announceEmpty: true);

        // When the player returns from the Groq page with a fresh key copied, pre-fill the field
        // from the clipboard so they only have to press Check. Desktop only; reads nothing while
        // the panel is closed.
        private void OnApplicationFocus(bool focused)
        {
            if (focused && !IsWebGl)
            {
                TryCaptureClipboard(announceEmpty: false);
            }
        }

        private void TryCaptureClipboard(bool announceEmpty)
        {
            if (!IsVisible || IsWebGl)
            {
                return;
            }

            string candidate = LlmKeyStore.SanitizeKey(GUIUtility.systemCopyBuffer);
            if (!candidate.StartsWith(KeyPrefix, StringComparison.Ordinal))
            {
                if (announceEmpty)
                {
                    SetStatus(FallbackStrings.ByokClipboardEmpty(LanguageManager.CurrentLanguageOrDefault));
                }

                return;
            }

            keyInputField.text = candidate;
            SetStatus(FallbackStrings.ByokClipboardFound(LanguageManager.CurrentLanguageOrDefault));
        }

        private void HandleSharedSelected() => SetMode(false);

        private void HandleOwnSelected() => SetMode(true);

        private void SetMode(bool own)
        {
            ownMode = own;
            RefreshMode();
        }

        // Reflects the current mode: option emphasis, own-key group visibility, and status line.
        private void RefreshMode()
        {
            SetOptionEmphasis(sharedOptionButton, !ownMode);
            SetOptionEmphasis(ownKeyOptionButton, ownMode);
            ownKeyGroup.SetActive(ownMode);

            GameLanguage language = LanguageManager.CurrentLanguageOrDefault;
            string status;
            if (ownMode)
            {
                status = string.IsNullOrEmpty(LlmKeyStore.GetKey()) ? string.Empty : FallbackStrings.ByokStatusSaved(language);
            }
            else
            {
                SharedServiceState state = llmClient != null ? llmClient.SharedService : SharedServiceState.Unknown;
                status = SharedStatusLine(state, language);
            }

            SetStatus(status);
        }

        // The status line doubles as the shared-line indicator, so a finished probe updates it live;
        // it must not clobber a key-validation message the player is reading in own-key mode.
        private void HandleSharedServiceChanged(SharedServiceState state)
        {
            if (IsVisible && !ownMode)
            {
                SetStatus(SharedStatusLine(state, LanguageManager.CurrentLanguageOrDefault));
            }
        }

        private static string SharedStatusLine(SharedServiceState state, GameLanguage language) => state switch
        {
            SharedServiceState.Checking => FallbackStrings.ByokSharedChecking(language),
            SharedServiceState.Online => FallbackStrings.ByokSharedOnline(language),
            SharedServiceState.Unavailable => FallbackStrings.ByokSharedOffline(language),
            _ => string.Empty
        };

        // Dims the inactive option by fading the button image; the label keeps its own color.
        private static void SetOptionEmphasis(Button button, bool selected)
        {
            if (button != null && button.image != null)
            {
                Color color = button.image.color;
                color.a = selected ? 1f : 0.45f;
                button.image.color = color;
            }
        }

        // Validates against Groq's free models endpoint before storing so a typo can never
        // silently replace a working key with a dead one.
        private void HandleCheckClicked()
        {
            string candidate = (keyInputField.text ?? string.Empty).Trim();
            if (candidate.Length == 0)
            {
                return;
            }

            checkButton.interactable = false;
            SetStatus(FallbackStrings.ByokChecking(LanguageManager.CurrentLanguageOrDefault));

            if (llmClient == null)
            {
                Debug.LogWarning($"[{nameof(ByokPanelView)}] LlmReactionClient reference is missing; cannot validate the key.", this);
                FinishValidation(ApiKeyValidationResult.Unreachable);
                return;
            }

            llmClient.ValidateApiKey(candidate, FinishValidation);
        }

        private void FinishValidation(ApiKeyValidationResult result)
        {
            checkButton.interactable = true;
            GameLanguage language = LanguageManager.CurrentLanguageOrDefault;

            switch (result)
            {
                case ApiKeyValidationResult.Valid:
                    LlmKeyStore.SaveKey(keyInputField.text);
                    ownMode = true;
                    RefreshMode();
                    break;
                case ApiKeyValidationResult.Invalid:
                    SetStatus(FallbackStrings.ByokRejectedKey(language));
                    break;
                default:
                    SetStatus(FallbackStrings.ByokUnreachable(language));
                    break;
            }
        }

        private void HandleContinueClicked()
        {
            GameLanguage language = LanguageManager.CurrentLanguageOrDefault;
            if (ownMode && string.IsNullOrEmpty(LlmKeyStore.GetKey()))
            {
                SetStatus(FallbackStrings.ByokNag(language));
                return;
            }

            if (!ownMode)
            {
                LlmKeyStore.ClearKey();
            }

            LlmKeyStore.MarkServiceChosen();
            Hide();
        }

        protected override void RefreshContent(GameLanguage language)
        {
            RtlTextHelper.SetText(titleText, FallbackStrings.ByokPanelTitle(language), language);
            RtlTextHelper.SetText(infoText, FallbackStrings.ByokPanelInfo(language), language);
            SetButtonLabel(sharedOptionButton, FallbackStrings.ByokSharedOption(language), language);
            SetButtonLabel(ownKeyOptionButton, FallbackStrings.ByokOwnOption(language), language);
            SetButtonLabel(checkButton, FallbackStrings.ByokCheckButton(language), language);
            SetButtonLabel(continueButton, FallbackStrings.Continue(language), language);
            SetButtonLabel(openGroqButton, FallbackStrings.ByokOpenGroq(language), language);
            SetButtonLabel(pasteButton, FallbackStrings.ByokPaste(language), language);

            // The placeholder is scene-authored text; localize it here so it follows the language.
            if (keyInputField.placeholder is TMP_Text placeholderText)
            {
                RtlTextHelper.SetText(placeholderText, FallbackStrings.ByokPlaceholder(language), language);
            }

            RefreshMode();
        }

        private static void SetButtonLabel(Button button, string label, GameLanguage language)
        {
            TMP_Text text = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
            RtlTextHelper.SetText(text, label, language);
        }

        private void SetStatus(string message) =>
            RtlTextHelper.SetText(statusText, message, LanguageManager.CurrentLanguageOrDefault);
    }
}
