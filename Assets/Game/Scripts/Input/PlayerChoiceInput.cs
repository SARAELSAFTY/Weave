using Game.Scripts;
using Game.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Game.Scripts.Input
{
    /// <summary>Reads keyboard and pointer input for card-swipe choices and forwards them to GameManager.</summary>
    /// <remarks>
    /// Converts screen-space drag deltas to canvas-space units so swipe thresholds remain consistent
    /// across resolutions. Uses frame-rate-independent exponential smoothing for drag visuals.
    /// </remarks>
    public class PlayerChoiceInput : MonoBehaviour
    {
        // Reference frame rate used to normalize the smoothing factor across different update rates.
        private const float TargetFrameRate = 60f;

        // Drag delta below this threshold (canvas units) snaps immediately to target, avoiding micro-jitter.
        private const float SnapThreshold = 0.25f;

        // Guard against division by zero when the canvas has not yet been laid out.
        private const float MinimumCanvasScale = 0.0001f;

        [Tooltip("GameManager that receives the final left/right choice decision.")]
        [SerializeField] private GameManager gameManager;

        [Tooltip("CardView whose visual position tracks the player's drag gesture.")]
        [SerializeField] private CardView cardView;

        [Tooltip("Root Canvas used to convert screen-space drag distances to canvas units.")]
        [SerializeField] private Canvas rootCanvas;

        [Tooltip("Minimum drag distance in screen pixels required to register as a deliberate swipe.")]
        [SerializeField, Min(1f)] private float swipeThreshold = 80f;

        [Tooltip("Smoothing factor for drag interpolation (lower = more lag, higher = snappier).")]
        [SerializeField, Range(0.05f, 1f)] private float dragSmooth = 0.35f;

        private Vector2 dragStartScreenPosition;
        private float currentDragX;
        private float targetDragX;
        private bool isDragging;
        private Camera eventCamera;

        private void Awake()
        {
            bool missingReference =
                InspectorValidation.RequireField(gameManager, nameof(gameManager), nameof(PlayerChoiceInput), this) |
                InspectorValidation.RequireField(cardView, nameof(cardView), nameof(PlayerChoiceInput), this) |
                InspectorValidation.RequireField(rootCanvas, nameof(rootCanvas), nameof(PlayerChoiceInput), this);

            if (missingReference)
            {
                enabled = false;
                return;
            }

            CacheCanvas();
        }

        private void Update()
        {
            if (!CanReadChoiceInput())
            {
                CancelDrag(resetVisual: true);
                return;
            }

            ReadKeyboard();
            ReadPointerSwipe();
            ApplySmoothedDrag();
        }

        // Cancel any in-progress drag when the app loses focus to prevent stuck state on return.
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                CancelDrag(resetVisual: true);
            }
        }

        private bool CanReadChoiceInput()
        {
            return gameManager.AcceptsChoiceInput && cardView.AcceptsDrag;
        }

        private void ReadKeyboard()
        {
            if (isDragging || IsTypingInInputField())
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame)
            {
                SubmitChoice(false);
            }
            else if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)
            {
                SubmitChoice(true);
            }
        }

        private void ReadPointerSwipe()
        {
            if (!TryGetPointer(out Vector2 screenPosition, out bool pressedThisFrame, out bool isPressed, out bool releasedThisFrame))
            {
                if (isDragging)
                {
                    EndDrag(targetDragX);
                }

                return;
            }

            if (pressedThisFrame && !isDragging)
            {
                TryBeginDrag(screenPosition);
            }

            if (isDragging)
            {
                if (!isPressed || releasedThisFrame)
                {
                    EndDrag(targetDragX);
                    return;
                }

                Vector2 screenDelta = screenPosition - dragStartScreenPosition;
                targetDragX = ConvertScreenDeltaToCanvas(screenDelta.x);
            }
        }

        private void TryBeginDrag(Vector2 screenPosition)
        {
            if (IsTypingInInputField())
            {
                return;
            }

            if (!cardView.ContainsScreenPoint(screenPosition, eventCamera))
            {
                return;
            }

            dragStartScreenPosition = screenPosition;
            currentDragX = 0f;
            targetDragX = 0f;
            isDragging = true;
        }

        private void EndDrag(float dragX)
        {
            isDragging = false;
            currentDragX = 0f;
            targetDragX = 0f;

            float thresholdInCanvas = ConvertScreenDeltaToCanvas(swipeThreshold);
            if (Mathf.Abs(dragX) >= thresholdInCanvas)
            {
                SubmitChoice(dragX > 0f);
            }
            else
            {
                cardView.ResetCardPosition(easeChoiceCards: true);
            }
        }

        private void CancelDrag(bool resetVisual)
        {
            if (!isDragging)
            {
                return;
            }

            isDragging = false;
            currentDragX = 0f;
            targetDragX = 0f;

            if (resetVisual)
            {
                cardView.ResetCardPosition(easeChoiceCards: true);
            }
        }

        // Frame-rate-independent exponential smoothing: converts the per-frame blend into
        // an equivalent rate at TargetFrameRate so feel stays consistent at any FPS.
        private void ApplySmoothedDrag()
        {
            if (!isDragging)
            {
                return;
            }

            float blendAmount = 1f - Mathf.Pow(1f - dragSmooth, Time.unscaledDeltaTime * TargetFrameRate);
            currentDragX = Mathf.Lerp(currentDragX, targetDragX, blendAmount);
            if (Mathf.Abs(currentDragX - targetDragX) < SnapThreshold)
            {
                currentDragX = targetDragX;
            }

            float thresholdInCanvas = ConvertScreenDeltaToCanvas(swipeThreshold);
            cardView.SetDragProgress(currentDragX, thresholdInCanvas);
        }

        private void SubmitChoice(bool choseRight)
        {
            CancelDrag(resetVisual: false);
            gameManager.ChooseSide(choseRight);
        }

        // Divides screen pixels by the canvas scale factor to get canvas-unit distance,
        // keeping swipe thresholds resolution-independent.
        private float ConvertScreenDeltaToCanvas(float screenDelta)
        {
            float scale = rootCanvas.scaleFactor;
            if (scale <= MinimumCanvasScale)
            {
                scale = 1f;
            }

            return screenDelta / scale;
        }

        // Resolves the actual root canvas and caches the event camera needed for hit-testing.
        private void CacheCanvas()
        {
            rootCanvas = rootCanvas.rootCanvas;
            eventCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : rootCanvas.worldCamera;
        }

        // Reads mouse first, then touchscreen; returns false when neither device has active input.
        private static bool TryGetPointer(out Vector2 screenPosition, out bool pressedThisFrame, out bool isPressed, out bool releasedThisFrame)
        {
            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                screenPosition = mouse.position.ReadValue();
                pressedThisFrame = mouse.leftButton.wasPressedThisFrame;
                isPressed = mouse.leftButton.isPressed;
                releasedThisFrame = mouse.leftButton.wasReleasedThisFrame;
                return true;
            }

            Touchscreen touch = Touchscreen.current;
            if (touch != null &&
                (touch.primaryTouch.press.isPressed ||
                 touch.primaryTouch.press.wasPressedThisFrame ||
                 touch.primaryTouch.press.wasReleasedThisFrame))
            {
                screenPosition = touch.primaryTouch.position.ReadValue();
                pressedThisFrame = touch.primaryTouch.press.wasPressedThisFrame;
                isPressed = touch.primaryTouch.press.isPressed;
                releasedThisFrame = touch.primaryTouch.press.wasReleasedThisFrame;
                return true;
            }

            screenPosition = default;
            pressedThisFrame = false;
            isPressed = false;
            releasedThisFrame = false;
            return false;
        }

        // Prevents keyboard shortcuts from firing while the player is typing in a petition input field.
        private static bool IsTypingInInputField()
        {
            if (EventSystem.current == null)
            {
                return false;
            }

            GameObject selected = EventSystem.current.currentSelectedGameObject;
            return selected != null && selected.TryGetComponent<TMP_InputField>(out _);
        }
    }
}
