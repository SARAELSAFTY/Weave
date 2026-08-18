using System;
using System.Collections;
using Game.Scripts.Definitions;
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

        private RectTransform cardRectTransform;
        private Vector2 homePosition;
        private Quaternion homeRotation;
        private float leftChoiceBaseAlpha;
        private float rightChoiceBaseAlpha;
        private bool isEndingCard;
        private bool isPetitionCard;

        public event Action RestartRequested;
        public event Action<string> PetitionCommandSubmitted;
        public event Action PetitionConfirmRequested;

        public bool AcceptsDrag => !isEndingCard && !isPetitionCard;

        private void Awake()
        {
            if (descriptionText == null)
            {
                Debug.LogError($"[CardView] Missing required Inspector reference '{nameof(descriptionText)}' on '{gameObject.name}'.", this);
            }

            if (leftChoiceText == null)
            {
                Debug.LogError($"[CardView] Missing required Inspector reference '{nameof(leftChoiceText)}' on '{gameObject.name}'.", this);
            }

            if (rightChoiceText == null)
            {
                Debug.LogError($"[CardView] Missing required Inspector reference '{nameof(rightChoiceText)}' on '{gameObject.name}'.", this);
            }

            if (canvasGroup == null)
            {
                Debug.LogError($"[CardView] Missing required Inspector reference '{nameof(canvasGroup)}' on '{gameObject.name}'.", this);
            }

            if (speakerPortrait == null)
            {
                Debug.LogError($"[CardView] Missing required Inspector reference '{nameof(speakerPortrait)}' on '{gameObject.name}'.", this);
            }

            if (speakerNameText == null)
            {
                Debug.LogError($"[CardView] Missing required Inspector reference '{nameof(speakerNameText)}' on '{gameObject.name}'.", this);
            }

            if (restartButton == null)
            {
                Debug.LogError($"[CardView] Missing required Inspector reference '{nameof(restartButton)}' on '{gameObject.name}'.", this);
            }

            if (petitionInputRoot == null)
            {
                Debug.LogError($"[CardView] Missing required Inspector reference '{nameof(petitionInputRoot)}' on '{gameObject.name}'.", this);
            }

            if (petitionInputField == null)
            {
                Debug.LogError($"[CardView] Missing required Inspector reference '{nameof(petitionInputField)}' on '{gameObject.name}'.", this);
            }

            if (petitionSubmitButton == null)
            {
                Debug.LogError($"[CardView] Missing required Inspector reference '{nameof(petitionSubmitButton)}' on '{gameObject.name}'.", this);
            }

            if (descriptionText == null || leftChoiceText == null || rightChoiceText == null || canvasGroup == null || speakerPortrait == null || speakerNameText == null || restartButton == null || petitionInputRoot == null || petitionInputField == null || petitionSubmitButton == null)
            {
                enabled = false;
                return;
            }

            cardRectTransform = (RectTransform)transform;
            homePosition = cardRectTransform.anchoredPosition;
            homeRotation = cardRectTransform.localRotation;
            leftChoiceBaseAlpha = leftChoiceText.color.a;
            rightChoiceBaseAlpha = rightChoiceText.color.a;

            restartButton.gameObject.SetActive(false);
            restartButton.onClick.AddListener(RequestRestart);

            petitionInputRoot.SetActive(false);
            petitionSubmitButton.onClick.AddListener(RequestPetitionSubmit);
            petitionInputField.onValueChanged.AddListener(OnPetitionInputChanged);

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

        public void Show(CardData cardData, SpeakerData speaker)
        {
            isEndingCard = false;
            isPetitionCard = false;
            ResetCardPosition();
            restartButton.gameObject.SetActive(false);
            HidePetitionInput();

            if (cardData != null)
            {
                descriptionText.text = cardData.description;
                leftChoiceText.text = cardData.leftChoiceText;
                rightChoiceText.text = cardData.rightChoiceText;
            }

            ApplySpeaker(speaker);
        }

        public void ShowLlmReaction(CardData cardData, SpeakerData speaker)
        {
            isEndingCard = false;
            isPetitionCard = false;
            ResetCardPosition();
            restartButton.gameObject.SetActive(false);
            HidePetitionInput();

            if (cardData != null)
            {
                descriptionText.text = string.Empty;
                leftChoiceText.text = !string.IsNullOrWhiteSpace(cardData.leftChoiceText) ? cardData.leftChoiceText : "Dismiss";
                rightChoiceText.text = !string.IsNullOrWhiteSpace(cardData.rightChoiceText) ? cardData.rightChoiceText : "Continue";
            }

            ApplySpeaker(speaker);
        }

        public void ShowPetition(CardData cardData, SpeakerData speaker)
        {
            isEndingCard = false;
            isPetitionCard = true;
            ResetCardPosition();
            restartButton.gameObject.SetActive(false);
            HidePetitionInput();

            descriptionText.text = string.Empty;
            leftChoiceText.text = string.Empty;
            rightChoiceText.text = string.Empty;

            ApplySpeaker(speaker);
        }

        public void RevealPetitionInput()
        {
            ShowPetitionInput();
        }

        public void SetDescriptionText(string text)
        {
            if (descriptionText != null)
            {
                descriptionText.text = text ?? string.Empty;
            }
        }

        public void ShowEnding(CardData cardData, SpeakerData speaker)
        {
            isEndingCard = true;
            isPetitionCard = false;
            ResetCardPosition();
            HidePetitionInput();

            descriptionText.text = cardData != null ? cardData.description : "The run has ended.";
            leftChoiceText.text = string.Empty;
            rightChoiceText.text = string.Empty;

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
            SetChoiceVisibility(progress);
        }

        public void ResetCardPosition()
        {
            cardRectTransform.anchoredPosition = homePosition;
            cardRectTransform.localRotation = homeRotation;
            cardRectTransform.localScale = Vector3.one;
            canvasGroup.alpha = 1f;
            SetChoiceVisibility(0f);
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

        private void ShowPetitionInput()
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
                speakerNameText.text = FormatSpeakerLabel(speaker);
            }
            else
            {
                speakerNameText.text = string.Empty;
                speakerNameText.gameObject.SetActive(false);
            }
        }

        private static string FormatSpeakerLabel(SpeakerData speaker)
        {
            return speaker != null ? speaker.DisplayName : string.Empty;
        }

        private void SetChoiceVisibility(float progress)
        {
            SetTextAlpha(leftChoiceText, leftChoiceBaseAlpha, Mathf.Clamp01(-progress));
            SetTextAlpha(rightChoiceText, rightChoiceBaseAlpha, Mathf.Clamp01(progress));
        }

        private static void SetTextAlpha(TMP_Text text, float baseAlpha, float alpha)
        {
            Color color = text.color;
            color.a = baseAlpha * alpha;
            text.color = color;
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
            descriptionText.text = reactionText ?? string.Empty;
            ShowPetitionInput();

            if (petitionConfirmButton != null)
            {
                petitionConfirmButton.gameObject.SetActive(showConfirm);
            }
        }

        /// <summary>Filled dots represent turns remaining, not turns used.</summary>
        public void UpdatePetitionPatience(int turnsUsed, int maxTurns)
        {
            if (petitionPatienceDots == null || petitionPatienceDots.Length == 0) return;

            SetPatienceDotsActive(true);
            int remaining = Mathf.Clamp(maxTurns - turnsUsed, 0, petitionPatienceDots.Length);

            for (int i = 0; i < petitionPatienceDots.Length; i++)
            {
                if (petitionPatienceDots[i] == null) continue;
                Color c = petitionPatienceDots[i].color;
                c.a = i < remaining ? 1f : 0.25f;
                petitionPatienceDots[i].color = c;
            }
        }
    }
}
