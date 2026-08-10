using System.Collections.Generic;
using Game.Scripts.Definitions;
using UnityEngine;

namespace Game.Scripts.Runtime.Narrative
{
    /// <summary>Monitors resource state against a single Warning threshold per resource and
    /// manages per-resource cooldowns so a warning doesn't re-fire every card.</summary>
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

        /// <summary>Clears all tracked warning and cooldown state for a new run.</summary>
        public void Reset()
        {
            warnedResources.Clear();
            cooldownCardsRemaining.Clear();
        }

        /// <summary>Decrements cooldown counters for all resources by 1 (floor at 0).</summary>
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

        /// <summary>Sets the cooldown counter when a warning is shown to the player.</summary>
        public void OnWarningShown(ResourceData resource)
        {
            if (resource == null)
            {
                return;
            }

            cooldownCardsRemaining[resource] = Mathf.Max(0, resource.warningCooldownCards);
        }

        /// <summary>Returns the first resource (in catalog order) currently eligible to warn.</summary>
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
                    // Recovered above threshold — allow this resource to warn again next time it dips.
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
