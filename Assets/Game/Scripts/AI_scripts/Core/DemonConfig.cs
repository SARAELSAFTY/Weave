using UnityEngine;

[CreateAssetMenu(fileName = "DemonConfig", menuName = "Weave/Demon Config", order = 10)]
public class DemonConfig : ScriptableObject
{
    [Header("Deception Ratios (Base)")]
    [Range(0, 100)] public int truthRatio = 50;
    [Range(0, 100)] public int lieRatio = 30;
    [Range(0, 100)] public int warningRatio = 10;
    [Range(0, 100)] public int confuseRatio = 10;

    [Header("Conversation Settings")]
    [Min(1)] public int minExchangesBeforeDismiss = 1;
    [Min(1)] public int maxExchanges = 4;
    [Min(50)] public int characterPerTurnLimit = 500;
    [Min(0f)] public float demonTypingDelay = 1.0f;

    [Header("Belief Dynamics")]
    public float beliefBonusForContradiction = 25f;

    [Header("Appearance")]
    public Sprite demonPortrait;
    public string demonName = "The Whisper";

    [Header("API Settings")]
    public string groqModel = "llama-3.3-70b-versatile";
    [Min(50)] public int maxTokensPerResponse = 150;
    [Min(1f)] public float apiTimeoutSeconds = 5f;

    public int TotalRatio => truthRatio + lieRatio + warningRatio + confuseRatio;
}