using UnityEngine;

// Data asset for a multi-card story chain triggered by resource conditions.
// The first entry in conditions[] is the primary trigger — it drives streak tracking
// and the probability ramp. Additional entries are extra AND-gate conditions with no streak tracking.
[CreateAssetMenu(
    fileName = "NewChain",
    menuName = "Weave/Card Chain",
    order = 2)]
public class CardChainData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Unique ID for this story chain.")]
    public string chainId;

    [Header("Trigger")]
    [Tooltip("Trigger conditions. The first entry is the primary trigger (streak tracking + probability ramp). Additional entries are AND-gated with no streak tracking.")]
    public NarrativeCondition[] conditions;

    [Header("Probability")]
    [Tooltip("Use random chance calculation when triggering.")]
    public bool isRandom = true;
    [Range(0f, 1f), Tooltip("Starting trigger chance (0 to 1).")] public float baseChance = 0.25f;
    [Range(0f, 1f), Tooltip("Extra chance added per turn condition stays met.")] public float chanceStep = 0.15f;
    [Range(0f, 1f), Tooltip("Maximum trigger chance limit.")] public float maxChance = 0.75f;

    [Header("Behavior")]
    [Tooltip("First card ID in this chain.")]
    public string startCardId;
    [Tooltip("Turns before this chain can trigger again.")]
    public int cooldownTurns = 5;
    [Tooltip("Only trigger once per game session.")]
    public bool oneTimeOnly = false;
}
