using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

// Listens to resource changes and updates resource UI text labels.
public class ResourceDisplay : MonoBehaviour
{
    private const string FoodId = "food";
    private const string GoldId = "gold";
    private const string ArmyId = "army";
    private const string FavorId = "favor";

    [SerializeField, Tooltip("Source of active resource values.")] private ResourceState resourceState;
    [SerializeField, Tooltip("UI text for food level.")] private TMP_Text foodText;
    [SerializeField, Tooltip("UI text for gold level.")] private TMP_Text goldText;
    [SerializeField, Tooltip("UI text for army level.")] private TMP_Text armyText;
    [SerializeField, FormerlySerializedAs("crownText"), Tooltip("UI text for favor level.")] private TMP_Text favorText;

    private void Awake()
    {
        if (resourceState == null)
        {
            Debug.LogError($"[ResourceDisplay] Missing required Inspector reference '{nameof(resourceState)}' on '{gameObject.name}'.", this);
        }

        if (foodText == null)
        {
            Debug.LogError($"[ResourceDisplay] Missing required Inspector reference '{nameof(foodText)}' on '{gameObject.name}'.", this);
        }

        if (goldText == null)
        {
            Debug.LogError($"[ResourceDisplay] Missing required Inspector reference '{nameof(goldText)}' on '{gameObject.name}'.", this);
        }

        if (armyText == null)
        {
            Debug.LogError($"[ResourceDisplay] Missing required Inspector reference '{nameof(armyText)}' on '{gameObject.name}'.", this);
        }

        if (favorText == null)
        {
            Debug.LogError($"[ResourceDisplay] Missing required Inspector reference '{nameof(favorText)}' on '{gameObject.name}'.", this);
        }

        if (resourceState == null || foodText == null || goldText == null || armyText == null || favorText == null)
        {
            enabled = false;
        }
    }

    private void OnEnable()
    {
        if (!enabled)
        {
            return;
        }

        resourceState.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (resourceState != null)
        {
            resourceState.Changed -= Refresh;
        }
    }

    private void Refresh()
    {
        foodText.text = $"Food: {resourceState.Get(FoodId)}";
        goldText.text = $"Gold: {resourceState.Get(GoldId)}";
        armyText.text = $"Army: {resourceState.Get(ArmyId)}";
        favorText.text = $"Favor: {resourceState.Get(FavorId)}";
    }
}
