using TMPro;
using UnityEngine;

public class ResourceDisplay : MonoBehaviour
{
    [SerializeField] private ResourceState resourceState;
    [SerializeField] private TMP_Text foodText;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text armyText;
    [SerializeField] private TMP_Text crownText;

    private void OnEnable()
    {
        resourceState.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        resourceState.Changed -= Refresh;
    }

    private void Refresh()
    {
        foodText.text = $"Food: {resourceState.Food}";
        goldText.text = $"Gold: {resourceState.Gold}";
        armyText.text = $"Army: {resourceState.Army}";
        crownText.text = $"Crown: {resourceState.Crown}";
    }
}
