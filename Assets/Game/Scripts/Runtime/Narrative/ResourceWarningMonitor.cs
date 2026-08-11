using System.Collections.Generic;
using Game.Scripts.Definitions;
using UnityEngine;

namespace Game.Scripts.Runtime.Narrative
{
    /// <summary>
    /// Fires at most one warning per resource while it stays below threshold, with a cooldown
    /// so the same resource does not re-alert every card.
    /// </summary>
    public class ResourceWarningMonitor
    {
        private readonly ResourceCatalog catalog;
        private readonly ResourceState resourceState;
        private readonly HashSet<ResourceData> warnedResources = new HashSet<ResourceData>();
        private readonly Dictionary<ResourceData, int> cooldownCardsRemaining = new Dictionary<ResourceData, int>();

        public ResourceWarningMonitor(ResourceCatalog catalog, ResourceState resourceState)
        {
            this.catalog = catalog;
            this.resourceState = resourceState;
        }

        public void Reset()
        {
            warnedResources.Clear();
            cooldownCardsRemaining.Clear();
        }

        public void Tick()
        {
            List<ResourceData> keys = new List<ResourceData>(cooldownCardsRemaining.Keys);
            foreach (ResourceData key in keys)
            {
                if (cooldownCardsRemaining[key] > 0)
                {
                    cooldownCardsRemaining[key]--;
                }
            }
        }

        public void OnWarningShown(ResourceData resource)
        {
            if (resource == null)
            {
                return;
            }

            cooldownCardsRemaining[resource] = Mathf.Max(0, resource.warningCooldownCards);
        }

        public bool TryGetTriggeredWarning(out ResourceData triggeredResource)
        {
            triggeredResource = null;

            if (catalog == null || catalog.resources == null || resourceState == null)
            {
                return false;
            }

            foreach (ResourceData resource in catalog.resources)
            {
                if (resource == null)
                {
                    continue;
                }

                float thresholdValue = resource.defaultStartingValue * (resource.warningThresholdPercent / 100f);
                int currentValue = resourceState.Get(resource);

                if (currentValue > thresholdValue)
                {
                    // Recovered above threshold — may warn again the next time it dips.
                    warnedResources.Remove(resource);
                    continue;
                }

                cooldownCardsRemaining.TryGetValue(resource, out int cooldown);
                if (warnedResources.Contains(resource) || cooldown > 0 || resource.warningSpeaker == null)
                {
                    continue;
                }

                warnedResources.Add(resource);
                triggeredResource = resource;
                return true;
            }

            return false;
        }
    }
}
