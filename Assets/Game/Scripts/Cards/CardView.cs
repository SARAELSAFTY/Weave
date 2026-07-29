using System.Collections;
using TMPro;
using UnityEngine;

public class CardView : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text leftChoiceText;
    [SerializeField] private TMP_Text rightChoiceText;

    private RectTransform cardRectTransform;
    private Vector2 homePosition;
    private Quaternion homeRotation;

    private void Awake()
    {
        cardRectTransform = (RectTransform)transform;
        homePosition = cardRectTransform.anchoredPosition;
        homeRotation = cardRectTransform.localRotation;
    }

    public void Show(CardData cardData)
    {
        cardRectTransform.anchoredPosition = homePosition;
        cardRectTransform.localRotation = homeRotation;
        titleText.text = cardData.cardTitle;
        descriptionText.text = cardData.description;
        leftChoiceText.text = cardData.leftChoiceText;
        rightChoiceText.text = cardData.rightChoiceText;
    }

    public IEnumerator AnimateAway(bool choseRight, float duration)
    {
        Vector2 startPosition = cardRectTransform.anchoredPosition;
        float direction = choseRight ? 1f : -1f;
        Vector2 targetPosition = startPosition + new Vector2(direction * 1400f, 0f);
        Quaternion targetRotation = Quaternion.Euler(0f, 0f, direction * -12f);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            cardRectTransform.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, progress);
            cardRectTransform.localRotation = Quaternion.Lerp(homeRotation, targetRotation, progress);
            yield return null;
        }
    }
}
