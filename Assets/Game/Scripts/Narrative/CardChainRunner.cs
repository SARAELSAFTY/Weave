using System.Collections.Generic;
using UnityEngine;

// Manages side-story chains, checking triggers, probabilities, and cooldowns.
public class CardChainRunner
{
    private readonly ResourceState resourceState;
    private readonly List<CardChainData> chains;
    private readonly HashSet<string> firedOneTimeChains = new HashSet<string>();
    private readonly Dictionary<string, int> cooldownUntilTurn = new Dictionary<string, int>();
    private readonly Dictionary<string, int> zoneStreak = new Dictionary<string, int>();

    private string returnCardId;
    private CardChainData activeChain;
    private int turnCount;

    public CardChainRunner(ResourceState resourceState, List<CardChainData> chains)
    {
        this.resourceState = resourceState;
        this.chains = chains ?? new List<CardChainData>();
    }

    // Resolves the next card ID, redirecting to a triggered chain or returning to main story after a chain completes.
    public string ResolveNextCardId(CardData resolvedCard, string selectedNextCardId)
    {
        turnCount++;
        if (resolvedCard == null)
        {
            return selectedNextCardId;
        }

        string nextCardId = activeChain != null
            ? ResolveActiveChainCard(resolvedCard, selectedNextCardId)
            : StartTriggeredChain(selectedNextCardId);

        UpdateZoneStreaks();
        return nextCardId;
    }

    private string ResolveActiveChainCard(CardData resolvedCard, string selectedNextCardId)
    {
        if (!resolvedCard.returnToMainCard || string.IsNullOrEmpty(returnCardId))
        {
            return selectedNextCardId;
        }

        string nextCardId = returnCardId;
        StartCooldown();
        ClearActiveChain();
        return nextCardId;
    }

    private string StartTriggeredChain(string selectedNextCardId)
    {
        if (!TryFindTriggeredChain(out CardChainData triggeredChain))
        {
            return selectedNextCardId;
        }

        returnCardId = selectedNextCardId;
        activeChain = triggeredChain;

        if (triggeredChain.oneTimeOnly)
        {
            firedOneTimeChains.Add(triggeredChain.chainId);
        }

        if (!string.IsNullOrEmpty(triggeredChain.startCardId))
        {
            return triggeredChain.startCardId;
        }

        Debug.LogWarning($"Chain '{triggeredChain.chainId}' has no start card. Continuing normally.");
        ClearActiveChain();
        return selectedNextCardId;
    }

    private bool TryFindTriggeredChain(out CardChainData triggeredChain)
    {
        triggeredChain = null;

        foreach (CardChainData chain in chains)
        {
            if (chain == null || string.IsNullOrEmpty(chain.chainId))
            {
                continue;
            }

            if (chain.oneTimeOnly && firedOneTimeChains.Contains(chain.chainId))
            {
                continue;
            }

            if (cooldownUntilTurn.TryGetValue(chain.chainId, out int readyTurn) && turnCount < readyTurn)
            {
                continue;
            }

            if (!IsTriggered(chain))
            {
                continue;
            }

            triggeredChain = chain;
            return true;
        }

        return false;
    }

    private bool IsTriggered(CardChainData chain)
    {
        if (!IsInZone(chain) || !NarrativeConditionEvaluator.AreConditionsMet(chain.conditions, resourceState))
        {
            return false;
        }

        if (!chain.isRandom)
        {
            return true;
        }

        int streak = zoneStreak.TryGetValue(chain.chainId, out int streakCount) ? streakCount : 0;
        float chance = Mathf.Min(chain.baseChance + chain.chanceStep * streak, chain.maxChance);
        return UnityEngine.Random.value <= chance;
    }

    private bool IsInZone(CardChainData chain)
    {
        if (resourceState == null || chain.conditions == null || chain.conditions.Length == 0)
        {
            return false;
        }

        NarrativeCondition primary = chain.conditions[0];
        return NarrativeConditionEvaluator.Compare(resourceState.Get(primary.resourceId), primary.comparison, primary.value);
    }

    private void UpdateZoneStreaks()
    {
        foreach (CardChainData chain in chains)
        {
            if (chain == null || string.IsNullOrEmpty(chain.chainId))
            {
                continue;
            }

            if (IsInZone(chain))
            {
                zoneStreak[chain.chainId] = zoneStreak.TryGetValue(chain.chainId, out int current) ? current + 1 : 1;
            }
            else
            {
                zoneStreak[chain.chainId] = 0;
            }
        }
    }

    private void StartCooldown()
    {
        if (activeChain == null)
        {
            return;
        }

        int cooldown = Mathf.Max(0, activeChain.cooldownTurns);
        cooldownUntilTurn[activeChain.chainId] = turnCount + cooldown;
    }

    private void ClearActiveChain()
    {
        activeChain = null;
        returnCardId = null;
    }
}
