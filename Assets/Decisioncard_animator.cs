using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Game.Scripts.UI;

public class DecisionCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IDragHandler, IEndDragHandler
{
    [Header("References")]
    public Image leftCard;      // Your Left_card
    public Image rightCard;     // Your Right_card
    public Animator animator;
    public CardView cardView;

    [Header("Swipe Mechanics")]
    public float dragThreshold = 50f;
    public float choiceRevealThreshold = 100f;
    public float confirmThreshold = 150f;
    
    private RectTransform rectTransform;
    private Vector3 startPosition;
    private Vector2 dragStartPosition;
    private float currentDragDistance = 0f;
    private bool isDragging = false;
    private bool choiceConfirmed = false;
    private string chosenSide = "";

    private CanvasGroup cardCanvasGroup;
    private CanvasGroup leftCardCanvasGroup;
    private CanvasGroup rightCardCanvasGroup;

    private bool CanInteract => cardView == null || cardView.AcceptsDrag;

    void Awake()
    {
        if (cardView == null)
            cardView = GetComponent<CardView>();
    }

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        cardCanvasGroup = GetComponent<CanvasGroup>();
        
        if (cardCanvasGroup == null)
            cardCanvasGroup = gameObject.AddComponent<CanvasGroup>();

        startPosition = rectTransform.anchoredPosition;

        // Get animator if not assigned
        if (animator == null)
            animator = GetComponent<Animator>();

        // Setup CanvasGroups for left/right cards
        if (leftCard != null)
        {
            leftCardCanvasGroup = leftCard.GetComponent<CanvasGroup>();
            if (leftCardCanvasGroup == null)
                leftCardCanvasGroup = leftCard.gameObject.AddComponent<CanvasGroup>();
            leftCardCanvasGroup.alpha = 0f;
        }

        if (rightCard != null)
        {
            rightCardCanvasGroup = rightCard.GetComponent<CanvasGroup>();
            if (rightCardCanvasGroup == null)
                rightCardCanvasGroup = rightCard.gameObject.AddComponent<CanvasGroup>();
            rightCardCanvasGroup.alpha = 0f;
        }
    }

    // ========== HOVER ANIMATIONS ==========
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (choiceConfirmed || !CanInteract) return;
        if (animator != null)
            animator.SetTrigger("Hover_enter");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (choiceConfirmed || isDragging || !CanInteract) return;
        if (animator != null)
            animator.SetTrigger("Hover_exist");
    }

    // ========== DRAG/SWIPE MECHANICS ==========
    public void OnDrag(PointerEventData eventData)
    {
        if (choiceConfirmed || !CanInteract)
        {
            CancelDragVisuals();
            return;
        }

        if (!isDragging)
        {
            dragStartPosition = eventData.position;
            isDragging = true;
        }

        float dragDelta = eventData.position.x - dragStartPosition.x;

        if (Mathf.Abs(dragDelta) > dragThreshold)
        {
            float moveAmount = Mathf.Abs(dragDelta) - dragThreshold;
            
            if (dragDelta > 0) // Dragging RIGHT
            {
                // DON'T MOVE CardView - just fade
                float revealProgress = Mathf.Clamp01(moveAmount / choiceRevealThreshold);
                if (rightCardCanvasGroup != null) rightCardCanvasGroup.alpha = revealProgress;
                if (leftCardCanvasGroup != null) leftCardCanvasGroup.alpha = 0f;
                chosenSide = "right";
            }
            else // Dragging LEFT
            {
                float revealProgress = Mathf.Clamp01(moveAmount / choiceRevealThreshold);
                if (leftCardCanvasGroup != null) leftCardCanvasGroup.alpha = revealProgress;
                if (rightCardCanvasGroup != null) rightCardCanvasGroup.alpha = 0f;
                chosenSide = "left";
            }

            // Dim main card
            if (cardCanvasGroup != null)
                cardCanvasGroup.alpha = Mathf.Lerp(1f, 0.7f, Mathf.Clamp01(moveAmount / choiceRevealThreshold));
        }
        else
        {
            CancelDragVisuals();
        }

        currentDragDistance = dragDelta;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging || !CanInteract)
        {
            CancelDragVisuals();
            return;
        }

        isDragging = false;
        float dragDelta = currentDragDistance;
        float moveAmount = Mathf.Abs(dragDelta) - dragThreshold;

        if (moveAmount > confirmThreshold)
        {
            ConfirmChoice(chosenSide);
        }
        else
        {
            SnapBack();
        }
    }

    public void CancelDragVisuals()
    {
        if (leftCardCanvasGroup != null) leftCardCanvasGroup.alpha = 0f;
        if (rightCardCanvasGroup != null) rightCardCanvasGroup.alpha = 0f;
        if (cardCanvasGroup != null) cardCanvasGroup.alpha = 1f;
    }

    void SnapBack()
    {
        StartCoroutine(SnapBackAnimation());
    }

    System.Collections.IEnumerator SnapBackAnimation()
    {
        float duration = 0.3f;
        float elapsed = 0f;
        
        float currentAlpha = cardCanvasGroup != null ? cardCanvasGroup.alpha : 1f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            if (cardCanvasGroup != null) cardCanvasGroup.alpha = Mathf.Lerp(currentAlpha, 1f, t);
            if (leftCardCanvasGroup != null) leftCardCanvasGroup.alpha = Mathf.Lerp(leftCardCanvasGroup.alpha, 0f, t);
            if (rightCardCanvasGroup != null) rightCardCanvasGroup.alpha = Mathf.Lerp(rightCardCanvasGroup.alpha, 0f, t);

            yield return null;
        }

        CancelDragVisuals();
    }

    void ConfirmChoice(string side)
    {
        choiceConfirmed = true;
        Debug.Log($"User chose: {side}");

        if (animator != null)
        {
            if (side == "left")
                animator.SetBool("ConfirmLeft", true);
            else
                animator.SetBool("ConfirmRight", true);
        }

        // No Exit animation clip to attach an Animation Event to, so we fall
        // back to a timed Invoke. Set this delay to roughly match how long
        // your ConfirmLeft/ConfirmRight clip takes to play (e.g. its length
        // in seconds). If ConfirmLeft/Right clips are ~0.5s long, 0.5f is fine.
        Invoke(nameof(OnChoiceConfirmed), 0.5f);
    }

    public void OnChoiceConfirmed()
    {
        Debug.Log($"Decision made: {chosenSide}");
        ResetCard();
    }

    public void ResetCard()
    {
        choiceConfirmed = false;
        isDragging = false;
        currentDragDistance = 0f;
        chosenSide = "";

        if (animator != null)
        {
            animator.SetBool("ConfirmLeft", false);
            animator.SetBool("ConfirmRight", false);
        }

        if (rectTransform != null)
            rectTransform.anchoredPosition = startPosition;

        CancelDragVisuals();
    }
}