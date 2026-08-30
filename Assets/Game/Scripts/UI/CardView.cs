using System;
using System.Collections;
using Game.Scripts.Definitions;
using Game.Scripts.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI
{
    public class CardView : MonoBehaviour
    {
        [SerializeField, Tooltip("Main card story text.")] private TMP_Text descriptionText;
        [SerializeField, Tooltip("Left choice text label.")] private TMP_Text leftChoiceText;
        [SerializeField, Tooltip("Right choice text label.")] private TMP_Text rightChoiceText;
        [SerializeField, Tooltip("Canvas group for card transparency.")] private CanvasGroup canvasGroup;
        [SerializeField, Tooltip("Character portrait image.")] private Image speakerPortrait;
        [SerializeField, Tooltip("Speaker character name text.")] private TMP_Text speakerNameText;

        [Header("Ending")]
        [SerializeField, Tooltip("Restart button shown on game ending screen.")] private Button restartButton;

        [Header("Petition")]
        [SerializeField, Tooltip("Root object containing the petition input field and submit button.")] private GameObject petitionInputRoot;
        [SerializeField, Tooltip("Text input field for petition commands.")] private TMP_InputField petitionInputField;
        [SerializeField, Tooltip("Submit button for petition commands.")] private Button petitionSubmitButton;
        [SerializeField, Tooltip("Confirm button shown once the AI has proposed a resolution.")] private Button petitionConfirmButton;
        [SerializeField, Tooltip("Dot meter showing remaining petition turns. Optional.")] private Image[] petitionPatienceDots;

        [Header("Exit Feel")]
        [SerializeField, Tooltip("Horizontal distance card moves offscreen on exit.")] private float exitDistance = 1400f;
        [SerializeField, Tooltip("Maximum rotation angle during exit.")] private float exitRotationDegrees = 18f;
        [SerializeField, Tooltip("Easing curve for exit animation.")] private AnimationCurve exitEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Choice Card Feel")]
        [SerializeField, Tooltip("Animator driver on the left choice card object.")] private ChoiceCardAnimator leftChoiceCardAnimator;
        [SerializeField, Tooltip("Animator driver on the right choice card object.")] private ChoiceCardAnimator rightChoiceCardAnimator;

        private RectTransform cardRectTransform;
        private Vector2 homePosition;
        private Quaternion homeRotation;
        private bool isEndingCard;
        private bool isPetitionCard;
        private bool isLlmReactionPresentation;
        private CardData currentCardData;
        private SpeakerData currentSpeaker;
        private string currentDynamicDescription;

        public event Action RestartRequested;
        public event Action<string> PetitionCommandSubmitted;
        public event Action PetitionConfirmRequested;

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

            ApplySpeaker(speaker);
            ApplyStaticCardText(cardData, useReactionFallbacks: false);
        }

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

            ApplySpeaker(speaker);
            SetLabel(descriptionText, string.Empty);
            ApplyChoiceTexts(cardData, useReactionFallbacks: true);
        }

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

            ApplySpeaker(speaker);
        }

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

        public void SetDescriptionText(string text)
        {
            currentDynamicDescription = text ?? string.Empty;
            SetLabel(descriptionText, currentDynamicDescription);
        }

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

            ApplySpeaker(speaker);
            restartButton.gameObject.SetActive(true);
        }

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
                leftChoiceCardAnimator?.SnapToPark();
                rightChoiceCardAnimator?.SnapToPark();
            }
        }

        public void PlayConfirmAnimation(bool choseRight)
        {
            ChoiceCardAnimator chosen = choseRight ? rightChoiceCardAnimator : leftChoiceCardAnimator;
            ChoiceCardAnimator other = choseRight ? leftChoiceCardAnimator : rightChoiceCardAnimator;
            chosen?.PlayConfirm();
            other?.EaseBackToPark();
        }

        public bool ContainsScreenPoint(Vector2 screenPoint, Camera eventCamera)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(cardRectTransform, screenPoint, eventCamera);
        }

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

        private void ApplySpeaker(SpeakerData speaker)
        {
            bool hasSpeaker = speaker != null;

            speakerPortrait.gameObject.SetActive(hasSpeaker);
            speakerPortrait.sprite = hasSpeaker ? speaker.portrait : null;
            speakerPortrait.enabled = hasSpeaker && speaker.portrait != null;

            if (hasSpeaker)
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

        public void ShowPetitionDeliberation(string reactionText)
        {
            DisplayPetitionResponse(reactionText, showConfirm: false);
        }

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

            ApplySpeaker(currentSpeaker);

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
