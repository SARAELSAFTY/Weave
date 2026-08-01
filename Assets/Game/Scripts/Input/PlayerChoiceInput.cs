using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Reads mouse, touch, and keyboard inputs for card swipe choices.
public class PlayerChoiceInput : MonoBehaviour
{
    private const float TargetFrameRate = 60f;
    private const float SnapThreshold = 0.25f;
    private const float MinimumCanvasScale = 0.0001f;

    [SerializeField, Tooltip("Main game manager reference.")] private GameManager gameManager;
    [SerializeField, Tooltip("Card UI component being dragged.")] private CardView cardView;
    [SerializeField, Tooltip("Root UI canvas for scale and event camera resolution.")] private Canvas rootCanvas;
    [SerializeField, Min(1f), Tooltip("Drag distance in pixels needed to confirm a swipe.")] private float swipeThreshold = 80f;
    [SerializeField, Range(0.05f, 1f), Tooltip("Smoothness factor for card drag motion.")] private float dragSmooth = 0.35f;

    private Vector2 dragStartScreenPosition;
    private float currentDragX;
    private float targetDragX;
    private bool isDragging;
    private Camera eventCamera;

    private void Awake()
    {
        if (gameManager == null)
        {
            Debug.LogError($"[PlayerChoiceInput] Missing required Inspector reference '{nameof(gameManager)}' on '{gameObject.name}'.", this);
        }

        if (cardView == null)
        {
            Debug.LogError($"[PlayerChoiceInput] Missing required Inspector reference '{nameof(cardView)}' on '{gameObject.name}'.", this);
        }

        if (rootCanvas == null)
        {
            Debug.LogError($"[PlayerChoiceInput] Missing required Inspector reference '{nameof(rootCanvas)}' on '{gameObject.name}'.", this);
        }

        if (gameManager == null || cardView == null || rootCanvas == null)
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
        if (IsPointerOverBlockingUi())
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
            cardView.ResetCardPosition();
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
            cardView.ResetCardPosition();
        }
    }

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

    private float ConvertScreenDeltaToCanvas(float screenDelta)
    {
        float scale = rootCanvas.scaleFactor;
        if (scale <= MinimumCanvasScale)
        {
            scale = 1f;
        }

        return screenDelta / scale;
    }

    private void CacheCanvas()
    {
        rootCanvas = rootCanvas.rootCanvas;
        eventCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : rootCanvas.worldCamera;
    }

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
            (touch.primaryTouch.press.isPressed
             || touch.primaryTouch.press.wasPressedThisFrame
             || touch.primaryTouch.press.wasReleasedThisFrame))
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

    private static bool IsPointerOverBlockingUi()
    {
        return IsTypingInInputField();
    }

    private static bool IsTypingInInputField()
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        GameObject selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null)
        {
            return false;
        }

        return selected.GetComponent<TMP_InputField>() != null
               || selected.GetComponent<InputField>() != null;
    }
}
