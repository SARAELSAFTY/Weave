using System;
using System.Collections;
using Game.Scripts.Definitions;
using Game.Scripts.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI
{
    /// <summary>Drives card presentation including static cards, petition input, LLM reaction display, endings, drag preview, and exit animation.</summary>
    /// <remarks>Subscribes to LanguageManager for live language switching. Choice sub-cards are animated by paired <see cref="ChoiceCardAnimator"/> components.</remarks>
    public class CardView : MonoBehaviour
    {
        [Header("Card Content")]
        [Tooltip("Text component displaying the card's main description or dialogue body.")]
        [SerializeField] private TMP_Text descriptionText;

        [Tooltip("Text component for the left choice label shown during drag or on a normal card.")]
        [SerializeField] private TMP_Text leftChoiceText;

        [Tooltip("Text component for the right choice label shown during drag or on a normal card.")]
        [SerializeField] private TMP_Text rightChoiceText;

        [Tooltip("Canvas group controlling overall card opacity during exit animations.")]
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Speaker & Art")]
        [Tooltip("Image displaying the speaker's portrait when the card uses SpeakerPortrait art mode.")]
        [SerializeField] private Image speakerPortrait;

        [Tooltip("Text component showing the current speaker's localized display name.")]
        [SerializeField] private TMP_Text speakerNameText;

        [Tooltip("Background image overridden per-card by the visual template; falls back to the default sprite.")]
        [SerializeField] private Image cardBackgroundImage;

        [Tooltip("Border image overridden per-card by the visual template; falls back to the default sprite.")]
        [SerializeField] private Image cardBorderImage;

        [Tooltip("Illustration image shown only when the card uses EventImage art mode and has a cardImage assigned.")]
        [SerializeField] private Image cardIllustrationImage;

        [Tooltip("Fallback background sprite used when no visual template provides one.")]
        [SerializeField] private Sprite defaultCardBackground;

        [Tooltip("Fallback border sprite used when no visual template provides one.")]
        [SerializeField] private Sprite defaultCardBorder;

        [Header("Ending UI")]
        [Tooltip("Button shown only on ending cards that triggers a run restart.")]
        [SerializeField] private Button restartButton;

        [Header("Petition Input")]
        [Tooltip("Root GameObject containing the petition text input and submit button; toggled active during petition cards.")]
        [SerializeField] private GameObject petitionInputRoot;

        [Tooltip("Text input field where the player types petition commands.")]
        [SerializeField] private TMP_InputField petitionInputField;

        [Tooltip("Button that submits the current petition input text.")]
        [SerializeField] private Button petitionSubmitButton;

        [Tooltip("Optional button shown after an LLM proposal to confirm the petition action before proceeding.")]
        [SerializeField] private Button petitionConfirmButton;

        [Tooltip("Array of dot images indicating remaining petition patience; alpha dims for exhausted dots.")]
        [SerializeField] private Image[] petitionPatienceDots;

        [Header("Exit Animation")]
        [Tooltip("Horizontal distance in pixels the card travels during its exit animation.")]
        [SerializeField] private float exitDistance = 1400f;

        [Tooltip("Rotation in degrees applied to the card at full exit displacement.")]
        [SerializeField] private float exitRotationDegrees = 18f;

        [Tooltip("Curve controlling the easing of position, rotation, and fade during the card exit animation.")]
        [SerializeField] private AnimationCurve exitEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Choice Sub-Cards")]
        [Tooltip("Animator driving the left choice sub-card reveal and dismiss.")]
        [SerializeField] private ChoiceCardAnimator leftChoiceCardAnimator;

        [Tooltip("Animator driving the right choice sub-card reveal and dismiss.")]
        [SerializeField] private ChoiceCardAnimator rightChoiceCardAnimator;

        private RectTransform cardRectTransform;
        private Vector2 homePosition;
        private Quaternion homeRotation;
        private bool isEndingCard;
        private bool isPetitionCard;
        private bool isLlmReactionPresentation;
        private CardData currentCardData;
        private SpeakerData currentSpeaker;
        private string currentDynamicDescription;

        /// <summary>Raised when the player clicks the restart button on an ending card.</summary>
        public event Action RestartRequested;

        /// <summary>Raised when the player submits petition input text; carries the trimmed input string.</summary>
        public event Action<string> PetitionCommandSubmitted;

        /// <summary>Raised when the player clicks the petition confirm button after an LLM proposal.</summary>
        public event Action PetitionConfirmRequested;

        /// <summary>True when the card accepts drag gestures (not an ending or petition card).</summary>
        public bool AcceptsDrag => !isEndingCard && !isPetitionCard;

        private GameLanguage CurrentLanguage => LanguageManager.Instance != null
            ? LanguageManager.Instance.CurrentLanguage
            : GameLanguage.English;

        private void Awake()
        {
            bool missingReference =
                InspectorValidation.RequireField(descriptionText, nameof(descriptionText), nameof(CardView), this) |
                InspectorValidation.RequireField(leftChoiceText, nameof(leftChoiceText), nameof(CardView), this) |
                InspectorValidation.RequireField(rightChoiceText, nameof(rightChoiceText), nameof(CardView), this) |
                InspectorValidation.RequireField(canvasGroup, nameof(canvasGroup), nameof(CardView), this) |
                InspectorValidation.RequireField(speakerPortrait, nameof(speakerPortrait), nameof(CardView), this) |
                InspectorValidation.RequireField(speakerNameText, nameof(speakerNameText), nameof(CardView), this) |
                InspectorValidation.RequireField(restartButton, nameof(restartButton), nameof(CardView), this) |
                InspectorValidation.RequireField(petitionInputRoot, nameof(petitionInputRoot), nameof(CardView), this) |
                InspectorValidation.RequireField(petitionInputField, nameof(petitionInputField), nameof(CardView), this) |
                InspectorValidation.RequireField(petitionSubmitButton, nameof(petitionSubmitButton), nameof(CardView), this);

            if (missingReference)
            {
                enabled = false;
                return;
            }

            cardRectTransform = (RectTransform)transform;
            homePosition = cardRectTransform.anchoredPosition;
            homeRotation = cardRectTransform.localRotation;

            if (defaultCardBackground == null && cardBackgroundImage != null)
            {
                defaultCardBackground = cardBackgroundImage.sprite;
            }

            if (defaultCardBorder == null && cardBorderImage != null)
            {
                defaultCardBorder = cardBorderImage.sprite;
            }

            RtlTextHelper.Configure(descriptionText);
            RtlTextHelper.Configure(leftChoiceText);
            RtlTextHelper.Configure(rightChoiceText);
            RtlTextHelper.Configure(speakerNameText);
            if (petitionInputField.textComponent != null)
            {
                RtlTextHelper.EnsureRtlInputText(petitionInputField);
            }

            restartButton.gameObject.SetActive(false);
            restartButton.onClick.AddListener(RequestRestart);

            petitionInputRoot.SetActive(false);
            petitionSubmitButton.onClick.AddListener(RequestPetitionSubmit);
            petitionInputField.onValueChanged.AddListener(OnPetitionInputChanged);
            petitionInputField.onSubmit.AddListener(_ => RequestPetitionSubmit());

            if (petitionConfirmButton == null)
            {
                Debug.LogWarning($"[CardView] Optional Inspector reference '{nameof(petitionConfirmButton)}' is missing on '{gameObject.name}'. Petitions will have no confirm step.", this);
            }

            if (petitionConfirmButton != null)
            {
                petitionConfirmButton.gameObject.SetActive(false);
                petitionConfirmButton.onClick.AddListener(() => PetitionConfirmRequested?.Invoke());
            }
        }

        private void OnEnable()
        {
            if (LanguageManager.Instance != null)
            {
                LanguageManager.Instance.LanguageChanged += RefreshLanguage;
            }
        }

        private void OnDisable()
        {
            if (LanguageManager.HasInstance)
            {
                LanguageManager.Instance.LanguageChanged -= RefreshLanguage;
            }
        }

        /// <summary>Presents a normal choice card with static text, speaker, and visuals; resets position and hides petition UI.</summary>
        /// <param name="cardData">The card data providing text, art, and branch configuration.</param>
        /// <param name="speaker">The speaker whose portrait and name are displayed; may be null.</param>
        public void Show(CardData cardData, SpeakerData speaker)
        {
            isEndingCard = false;
            isPetitionCard = false;
            isLlmReactionPresentation = false;
            currentDynamicDescription = null;
            currentCardData = cardData;
            currentSpeaker = speaker;
            ResetCardPosition();
            restartButton.gameObject.SetActive(false);
            HidePetitionInput();

            ApplySpeaker(speaker, cardData);
            ApplyCardVisuals(cardData);
            ApplyStaticCardText(cardData, useReactionFallbacks: false);
        }

        /// <summary>Presents a card showing an LLM reaction with empty description (populated later) and reaction-fallback choice labels.</summary>
        /// <param name="cardData">The card data providing choice text fallbacks and visuals.</param>
        /// <param name="speaker">The speaker whose portrait and name are displayed; may be null.</param>
        public void ShowLlmReaction(CardData cardData, SpeakerData speaker)
        {
            isEndingCard = false;
            isPetitionCard = false;
            isLlmReactionPresentation = true;
            currentDynamicDescription = string.Empty;
            currentCardData = cardData;
            currentSpeaker = speaker;
            ResetCardPosition();
            restartButton.gameObject.SetActive(false);
            HidePetitionInput();

            ApplySpeaker(speaker, cardData);
            ApplyCardVisuals(cardData);
            SetLabel(descriptionText, string.Empty);
            ApplyChoiceTexts(cardData, useReactionFallbacks: true);
        }

        /// <summary>Presents a petition card with empty text fields, ready for the petition input UI to be activated separately.</summary>
        /// <param name="cardData">The card data providing speaker and visual configuration.</param>
        /// <param name="speaker">The speaker whose portrait and name are displayed; may be null.</param>
        public void ShowPetition(CardData cardData, SpeakerData speaker)
        {
            isEndingCard = false;
            isPetitionCard = true;
            isLlmReactionPresentation = false;
            currentDynamicDescription = string.Empty;
            currentCardData = cardData;
            currentSpeaker = speaker;
            ResetCardPosition();
            restartButton.gameObject.SetActive(false);
            HidePetitionInput();

            SetLabel(descriptionText, string.Empty);
            SetLabel(leftChoiceText, string.Empty);
            SetLabel(rightChoiceText, string.Empty);

            ApplySpeaker(speaker, cardData);
            ApplyCardVisuals(cardData);
        }

        /// <summary>Converts an active petition card into a normal choice card displaying the final LLM reaction text and reaction-fallback labels.</summary>
        /// <param name="cardData">The card data providing choice text fallbacks.</param>
        /// <param name="finalReactionText">The LLM-generated reaction text displayed as the card description.</param>
        public void ConvertPetitionToNormalChoices(CardData cardData, string finalReactionText)
        {
            isPetitionCard = false;
            isLlmReactionPresentation = false;
            currentCardData = cardData;
            currentDynamicDescription = finalReactionText ?? string.Empty;
            HidePetitionInput();

            SetLabel(descriptionText, currentDynamicDescription);
            ApplyChoiceTexts(cardData, useReactionFallbacks: true);

            ResetCardPosition();
        }

        /// <summary>Activates the petition input field and submit button, clearing any previous text.</summary>
        public void ShowPetitionInput()
        {
            if (petitionInputField != null)
            {
                petitionInputField.text = string.Empty;
            }

            if (petitionInputRoot != null)
            {
                petitionInputRoot.SetActive(true);
            }

            SetPetitionSubmitting(false);
        }

        /// <summary>Hides the petition input root, preventing further player input for this petition.</summary>
        public void DisablePetitionFurtherInput()
        {
            if (petitionInputRoot != null)
            {
                petitionInputRoot.SetActive(false);
            }
        }

        /// <summary>Replaces the card description with dynamically generated text (e.g., an LLM response).</summary>
        /// <param name="text">The new description text; null is treated as empty.</param>
        public void SetDescriptionText(string text)
        {
            currentDynamicDescription = text ?? string.Empty;
            SetLabel(descriptionText, currentDynamicDescription);
        }

        /// <summary>Presents an ending card with its description, hides choice labels, and shows the restart button.</summary>
        /// <param name="cardData">The ending card data; null displays a fallback run-ended message.</param>
        /// <param name="speaker">The speaker whose portrait and name are displayed; may be null.</param>
        public void ShowEnding(CardData cardData, SpeakerData speaker)
        {
            isEndingCard = true;
            isPetitionCard = false;
            isLlmReactionPresentation = false;
            currentCardData = cardData;
            currentSpeaker = speaker;
            currentDynamicDescription = null;
            ResetCardPosition();
            HidePetitionInput();

            string description = cardData != null
                ? cardData.GetDescription(CurrentLanguage)
                : FallbackStrings.RunEnded(CurrentLanguage);
            SetLabel(descriptionText, description);
            SetLabel(leftChoiceText, string.Empty);
            SetLabel(rightChoiceText, string.Empty);

            ApplySpeaker(speaker, cardData);
            ApplyCardVisuals(cardData);
            restartButton.gameObject.SetActive(true);
        }

        /// <summary>Updates card position, rotation, and choice sub-card reveal based on horizontal drag progress.</summary>
        /// <param name="horizontalDrag">Current horizontal drag displacement in pixels from center.</param>
        /// <param name="swipeThreshold">The pixel distance at which drag is considered fully committed; clamped to minimum 1.</param>
        public void SetDragProgress(float horizontalDrag, float swipeThreshold)
        {
            if (isEndingCard || isPetitionCard)
            {
                return;
            }

            float safeThreshold = Mathf.Max(1f, swipeThreshold);
            float progress = Mathf.Clamp(horizontalDrag / safeThreshold, -1f, 1f);
            Vector2 visualDragOffset = new Vector2(horizontalDrag, 0f);

            cardRectTransform.anchoredPosition = homePosition + visualDragOffset;
            cardRectTransform.localRotation = Quaternion.Euler(0f, 0f, progress * -exitRotationDegrees);

            leftChoiceCardAnimator?.SetRevealProgress(-progress);
            rightChoiceCardAnimator?.SetRevealProgress(progress);
        }

        /// <summary>Snap-resets the card to its home position and optionally eases choice sub-cards back to their park positions.</summary>
        /// <param name="easeChoiceCards">When true, choice sub-cards animate back; when false, they snap immediately (unless currently dismissing).</param>
        public void ResetCardPosition(bool easeChoiceCards = false)
        {
            cardRectTransform.anchoredPosition = homePosition;
            cardRectTransform.localRotation = homeRotation;
            cardRectTransform.localScale = Vector3.one;
            canvasGroup.alpha = 1f;

            if (easeChoiceCards)
            {
                leftChoiceCardAnimator?.EaseBackToPark();
                rightChoiceCardAnimator?.EaseBackToPark();
            }
            else
            {
                if (leftChoiceCardAnimator != null && !leftChoiceCardAnimator.IsDismissing)
                {
                    leftChoiceCardAnimator.SnapToPark();
                }

                if (rightChoiceCardAnimator != null && !rightChoiceCardAnimator.IsDismissing)
                {
                    rightChoiceCardAnimator.SnapToPark();
                }
            }
        }

        /// <summary>Coroutine that yields each frame until both choice sub-card animators finish their dismiss flights.</summary>
        public IEnumerator WaitForChoiceFlight()
        {
            while ((leftChoiceCardAnimator != null && leftChoiceCardAnimator.IsDismissing) ||
                   (rightChoiceCardAnimator != null && rightChoiceCardAnimator.IsDismissing))
            {
                yield return null;
            }
        }

        /// <summary>Triggers the confirm-dismiss animation on the chosen side and eases the unchosen side back to park.</summary>
        /// <param name="choseRight">True to confirm the right choice card, false for the left.</param>
        public void PlayConfirmAnimation(bool choseRight)
        {
            ChoiceCardAnimator chosen = choseRight ? rightChoiceCardAnimator : leftChoiceCardAnimator;
            ChoiceCardAnimator other = choseRight ? leftChoiceCardAnimator : rightChoiceCardAnimator;
            chosen?.PlayConfirm();
            other?.EaseBackToPark();
        }

        /// <summary>Returns true if the given screen point lies within this card's rectangle.</summary>
        /// <param name="screenPoint">The screen-space point to test.</param>
        /// <param name="eventCamera">The camera used for the screen-to-rect conversion.</param>
        public bool ContainsScreenPoint(Vector2 screenPoint, Camera eventCamera)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(cardRectTransform, screenPoint, eventCamera);
        }

        /// <summary>Coroutine animating the card offscreen in the chosen direction using <see cref="exitEase"/> for position, rotation, and alpha.</summary>
        /// <param name="choseRight">True to fly right, false to fly left.</param>
        /// <param name="duration">Total animation duration in seconds.</param>
        public IEnumerator AnimateCardExit(bool choseRight, float duration)
        {
            Vector2 startPosition = cardRectTransform.anchoredPosition;
            float direction = choseRight ? 1f : -1f;
            Vector2 targetPosition = startPosition + new Vector2(direction * exitDistance, 0f);
            Quaternion targetRotation = Quaternion.Euler(0f, 0f, direction * -exitRotationDegrees);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float easedProgress = exitEase.Evaluate(progress);

                cardRectTransform.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, easedProgress);
                cardRectTransform.localRotation = Quaternion.Lerp(homeRotation, targetRotation, easedProgress);
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, easedProgress);

                yield return null;
            }
        }

        private void HidePetitionInput()
        {
            if (petitionInputRoot != null)
            {
                petitionInputRoot.SetActive(false);
            }

            if (petitionConfirmButton != null)
            {
                petitionConfirmButton.gameObject.SetActive(false);
            }

            SetPatienceDotsActive(false);
        }

        private void SetPatienceDotsActive(bool active)
        {
            if (petitionPatienceDots == null) return;
            foreach (Image dot in petitionPatienceDots)
            {
                if (dot != null) dot.gameObject.SetActive(active);
            }
        }

        /// <summary>Toggles petition input and button interactability based on whether a submission is in progress.</summary>
        /// <param name="isSubmitting">True to disable all petition controls; false to re-enable them (submit also requires non-empty text).</param>
        public void SetPetitionSubmitting(bool isSubmitting)
        {
            if (petitionInputField != null)
            {
                petitionInputField.interactable = !isSubmitting;
            }

            if (petitionSubmitButton != null)
            {
                petitionSubmitButton.interactable = !isSubmitting && petitionInputField != null && !string.IsNullOrWhiteSpace(petitionInputField.text);
            }

            if (petitionConfirmButton != null)
            {
                petitionConfirmButton.interactable = !isSubmitting;
            }
        }

        private void RequestPetitionSubmit()
        {
            if (petitionInputField == null || string.IsNullOrWhiteSpace(petitionInputField.text))
            {
                return;
            }

            if (petitionConfirmButton != null)
            {
                petitionConfirmButton.gameObject.SetActive(false);
            }

            PetitionCommandSubmitted?.Invoke(petitionInputField.text.Trim());
        }

        private void OnPetitionInputChanged(string text)
        {
            if (petitionSubmitButton != null)
            {
                petitionSubmitButton.interactable = !string.IsNullOrWhiteSpace(text);
            }
        }

        private void RequestRestart()
        {
            RestartRequested?.Invoke();
        }

        private void ApplySpeaker(SpeakerData speaker, CardData card)
        {
            CardArtMode mode = card != null ? card.artMode : CardArtMode.SpeakerPortrait;
            bool hasPortrait = mode == CardArtMode.SpeakerPortrait && speaker != null && speaker.portrait != null;

            speakerPortrait.gameObject.SetActive(hasPortrait);
            speakerPortrait.sprite = hasPortrait ? speaker.portrait : null;
            speakerPortrait.enabled = hasPortrait;

            if (speaker != null)
            {
                speakerNameText.gameObject.SetActive(true);
                SetLabel(speakerNameText, speaker.GetDisplayName(CurrentLanguage), TextFontCategory.SpeakerName);
            }
            else
            {
                SetLabel(speakerNameText, string.Empty, TextFontCategory.SpeakerName);
                speakerNameText.gameObject.SetActive(false);
            }
        }

        private void ApplyCardVisuals(CardData card)
        {
            CardVisualTemplate template = card != null ? card.visualTemplate : null;

            if (cardBackgroundImage != null)
            {
                cardBackgroundImage.sprite = template != null && template.background != null
                    ? template.background
                    : defaultCardBackground;
            }

            if (cardBorderImage != null)
            {
                Sprite border = template != null ? template.border : defaultCardBorder;
                cardBorderImage.sprite = border;
                cardBorderImage.enabled = border != null;
            }

            if (cardIllustrationImage != null)
            {
                bool showImage = card != null && card.artMode == CardArtMode.EventImage && card.cardImage != null;
                cardIllustrationImage.sprite = showImage ? card.cardImage : null;
                cardIllustrationImage.enabled = showImage;
            }
        }

        /// <summary>Displays an LLM deliberation response in the description and reopens petition input without a confirm button.</summary>
        /// <param name="reactionText">The LLM-generated deliberation text.</param>
        public void ShowPetitionDeliberation(string reactionText)
        {
            DisplayPetitionResponse(reactionText, showConfirm: false);
        }

        /// <summary>Displays an LLM proposal response in the description and reopens petition input with the confirm button visible.</summary>
        /// <param name="reactionText">The LLM-generated proposal text.</param>
        public void ShowPetitionProposal(string reactionText)
        {
            DisplayPetitionResponse(reactionText, showConfirm: true);
        }

        private void DisplayPetitionResponse(string reactionText, bool showConfirm)
        {
            currentDynamicDescription = reactionText ?? string.Empty;
            SetLabel(descriptionText, currentDynamicDescription);
            ShowPetitionInput();

            if (petitionConfirmButton != null)
            {
                petitionConfirmButton.gameObject.SetActive(showConfirm);
            }
        }

        /// <summary>Updates petition patience dots: fully opaque for remaining count, dimmed (alpha 0.25) for exhausted ones.</summary>
        /// <param name="remainingDots">Number of patience dots that should appear active; clamped to array length.</param>
        public void UpdatePetitionDots(int remainingDots)
        {
            if (petitionPatienceDots == null || petitionPatienceDots.Length == 0) return;

            SetPatienceDotsActive(true);
            int remaining = Mathf.Clamp(remainingDots, 0, petitionPatienceDots.Length);
            for (int i = 0; i < petitionPatienceDots.Length; i++)
            {
                if (petitionPatienceDots[i] == null) continue;
                Color c = petitionPatienceDots[i].color;
                c.a = i < remaining ? 1f : 0.25f;
                petitionPatienceDots[i].color = c;
            }
        }

        public void ShowPetitionSubmitFailed(string message)
        {
            if (descriptionText != null && !string.IsNullOrEmpty(message))
            {
                currentDynamicDescription = message;
                SetLabel(descriptionText, message);
            }

            SetPetitionSubmitting(false);
        }

        private void RefreshLanguage()
        {
            if (petitionInputField != null && petitionInputField.textComponent != null)
            {
                RtlTextHelper.Apply(petitionInputField.textComponent, CurrentLanguage);
            }

            ApplySpeaker(currentSpeaker, currentCardData);

            if (isPetitionCard)
            {
                if (currentDynamicDescription != null)
                {
                    SetLabel(descriptionText, currentDynamicDescription);
                }

                return;
            }

            if (isEndingCard)
            {
                string description = currentCardData != null
                    ? currentCardData.GetDescription(CurrentLanguage)
                    : FallbackStrings.RunEnded(CurrentLanguage);
                SetLabel(descriptionText, description);
                SetLabel(leftChoiceText, string.Empty);
                SetLabel(rightChoiceText, string.Empty);
                return;
            }

            if (!string.IsNullOrEmpty(currentDynamicDescription))
            {
                SetLabel(descriptionText, currentDynamicDescription);
            }
            else if (currentCardData != null && !isLlmReactionPresentation)
            {
                SetLabel(descriptionText, currentCardData.GetDescription(CurrentLanguage));
            }

            ApplyChoiceTexts(currentCardData, useReactionFallbacks: isLlmReactionPresentation);
        }

        private void ApplyStaticCardText(CardData cardData, bool useReactionFallbacks)
        {
            if (cardData != null)
            {
                SetLabel(descriptionText, cardData.GetDescription(CurrentLanguage));
            }

            ApplyChoiceTexts(cardData, useReactionFallbacks);
        }

        private void ApplyChoiceTexts(CardData cardData, bool useReactionFallbacks)
        {
            if (cardData == null)
            {
                return;
            }

            string left = cardData.GetLeftChoice(CurrentLanguage);
            string right = cardData.GetRightChoice(CurrentLanguage);
            if (useReactionFallbacks)
            {
                if (string.IsNullOrWhiteSpace(left))
                {
                    left = FallbackStrings.Dismiss(CurrentLanguage);
                }

                if (string.IsNullOrWhiteSpace(right))
                {
                    right = FallbackStrings.Continue(CurrentLanguage);
                }
            }

            SetLabel(leftChoiceText, left, TextFontCategory.Choice);
            SetLabel(rightChoiceText, right, TextFontCategory.Choice);
        }

        private void SetLabel(TMP_Text label, string value, TextFontCategory category = TextFontCategory.Default)
        {
            if (category == TextFontCategory.Default && label == descriptionText)
            {
                category = TextFontCategory.DialogueBody;
            }

            RtlTextHelper.SetText(label, value ?? string.Empty, CurrentLanguage, category);
        }
    }
}
