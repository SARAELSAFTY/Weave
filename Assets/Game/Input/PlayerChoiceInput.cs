using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerChoiceInput : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField, Min(1f)] private float swipeThreshold = 80f;

    private Vector2 dragStartPosition;
    private bool isDragging;

    private void Update()
    {
        ReadKeyboard();
        ReadMouseSwipe();
    }

    private void ReadKeyboard()
    {
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

    private void ReadMouseSwipe()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        if (mouse.leftButton.wasPressedThisFrame)
        {
            dragStartPosition = mouse.position.ReadValue();
            isDragging = true;
        }

        if (!isDragging || !mouse.leftButton.wasReleasedThisFrame)
        {
            return;
        }

        isDragging = false;
        Vector2 drag = mouse.position.ReadValue() - dragStartPosition;

        if (Mathf.Abs(drag.x) >= swipeThreshold && Mathf.Abs(drag.x) > Mathf.Abs(drag.y))
        {
            SubmitChoice(drag.x > 0f);
        }
    }

    private void SubmitChoice(bool choseRight)
    {
        gameManager.TryChoose(choseRight);
    }
}
