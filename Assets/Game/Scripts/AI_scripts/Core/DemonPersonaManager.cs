using System.Collections.Generic;
using UnityEngine;

public static class DemonPersonaManager
{
    public static DeceptionMode RollDeceptionMode(DemonConfig config, int playerLevel = 1)
    {
        if (config == null)
        {
            Debug.LogError("[DemonPersonaManager] DemonConfig reference missing! Defaulting to TRUTH mode.");
            return DeceptionMode.TRUTH;
        }

        int total = config.TotalRatio;
        if (total <= 0) return DeceptionMode.TRUTH;

        int roll = Random.Range(0, total);
        if (roll < config.truthRatio) return DeceptionMode.TRUTH;
        roll -= config.truthRatio;
        if (roll < config.lieRatio) return DeceptionMode.LIE;
        roll -= config.lieRatio;
        if (roll < config.warningRatio) return DeceptionMode.WARNING;

        return DeceptionMode.CONFUSE;
    }

    public static void PopulateConsequences(DemonChatSession session, CardData card, bool choseRight)
    {
        ResourceChange trueChange = choseRight ? card.rightResourceChange : card.leftResourceChange;
        if (trueChange.values == null) return;

        foreach (ResourceValue rv in trueChange.values)
        {
            if (string.IsNullOrEmpty(rv.id)) continue;

            int claimedValue = session.demonMode switch
            {
                DeceptionMode.TRUTH   => rv.value,
                DeceptionMode.LIE     => -rv.value + Random.Range(-5, 6),
                DeceptionMode.WARNING => -Mathf.Abs(rv.value) * 3,
                DeceptionMode.CONFUSE => rv.value + Random.Range(-10, 11),
                _                     => rv.value
            };

            var c = new GameConsequence(rv.id, rv.value, claimedValue);
            session.trueConsequences.Add(c);
            session.demonClaimedConsequences.Add(c);
        }
    }
}