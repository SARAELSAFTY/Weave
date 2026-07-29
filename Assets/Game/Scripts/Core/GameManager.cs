using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private CardView cardView;
    [SerializeField] private ResourceState resourceState;
    [SerializeField] private List<CardData> cards = new();
    [SerializeField, Min(0.05f)] private float cardExitDuration = 0.25f;

    private int currentCardIndex;
    private bool canChoose;

    private void Start()
    {
        if (cardView == null || resourceState == null || cards.Count == 0)
        {
            Debug.LogError("GameManager is missing its required Inspector references.", this);
            enabled = false;
            return;
        }

        ShowCurrentCard();
    }

    public void TryChoose(bool choseRight)
    {
        if (!canChoose)
        {
            return;
        }

        StartCoroutine(ChooseRoutine(choseRight));
    }

    private IEnumerator ChooseRoutine(bool choseRight)
    {
        canChoose = false;

        CardData chosenCard = cards[currentCardIndex];
        ResourceChange change = choseRight
            ? chosenCard.rightResourceChange
            : chosenCard.leftResourceChange;

        resourceState.Apply(change);
        yield return cardView.AnimateAway(choseRight, cardExitDuration);

        currentCardIndex = (currentCardIndex + 1) % cards.Count;
        ShowCurrentCard();
    }

    private void ShowCurrentCard()
    {
        cardView.Show(cards[currentCardIndex]);
        canChoose = true;
    }
}
