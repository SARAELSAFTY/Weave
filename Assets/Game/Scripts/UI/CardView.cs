using System;
using System.Collections;
using Game.Scripts.Definitions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI
{
    /// <summary>Controls card UI content, drag visuals, and exit animation.</summary>
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

        /// <summary>Raised when restart is requested from the ending screen.</summary>
        public event Action RestartRequested;

        /// <summary>Gets whether this card can be dragged for a choice.</summary>
        public bool AcceptsDrag => !isEndingCard;

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

            if (descriptionText == null || leftChoiceText == null || rightChoiceText == null || canvasGroup == null || speakerPortrait == null || speakerNameText == null || restartButton == null)
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
        }

        /// <summary>Shows a standard narrative card.</summary>
        public void Show(CardData cardData, CouncilMemberData speaker)
        {
            isEndingCard = false;
            ResetCardPosition();
            restartButton.gameObject.SetActive(false);

            if (cardData != null)
            {
                descriptionText.text = cardData.description;
                leftChoiceText.text = cardData.leftChoiceText;
                rightChoiceText.text = cardData.rightChoiceText;
            }

            ApplySpeaker(speaker);
        }

        /// <summary>Shows an LLM reaction card before generated text is returned.</summary>
        public void ShowLlmReaction(CardData cardData, CouncilMemberData speaker)
        {
            isEndingCard = false;
            ResetCardPosition();
            restartButton.gameObject.SetActive(false);

            if (cardData != null)
            {
                descriptionText.text = string.Empty;
                leftChoiceText.text = cardData.leftChoiceText;
                rightChoiceText.text = cardData.rightChoiceText;
            }

            ApplySpeaker(speaker);
        }

        /// <summary>Sets the main description text value.</summary>
        public void SetDescriptionText(string text)
        {
            if (descriptionText != null)
            {
                descriptionText.text = text ?? string.Empty;
            }
        }

        /// <summary>Shows ending card UI and enables restart.</summary>
        public void ShowEnding(CardData cardData, CouncilMemberData speaker)
        {
            isEndingCard = true;
            ResetCardPosition();

            descriptionText.text = cardData != null ? cardData.description : "The run has ended.";
            leftChoiceText.text = string.Empty;
            rightChoiceText.text = string.Empty;

            ApplySpeaker(speaker);
            restartButton.gameObject.SetActive(true);
        }

        /// <summary>Updates card position and choice highlight from drag input.</summary>
        public void SetDragProgress(float horizontalDrag, float swipeThreshold)
        {
            if (isEndingCard)
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

        /// <summary>Resets card transform and visuals to default.</summary>
        public void ResetCardPosition()
        {
            cardRectTransform.anchoredPosition = homePosition;
            cardRectTransform.localRotation = homeRotation;
            cardRectTransform.localScale = Vector3.one;
            canvasGroup.alpha = 1f;
            SetChoiceVisibility(0f);
        }

        /// <summary>Checks whether a screen point is inside the card rectangle.</summary>
        public bool ContainsScreenPoint(Vector2 screenPoint, Camera eventCamera)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(cardRectTransform, screenPoint, eventCamera);
        }

        /// <summary>Animates card exit in the chosen direction.</summary>
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

        private void RequestRestart()
        {
            RestartRequested?.Invoke();
        }

        private void ApplySpeaker(CouncilMemberData speaker)
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

        private static string FormatSpeakerLabel(CouncilMemberData speaker)
        {
            if (speaker == null)
            {
                return string.Empty;
            }

            return !string.IsNullOrEmpty(speaker.displayName)
                ? speaker.displayName.Trim()
                : speaker.title ?? string.Empty;
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
    }
}
