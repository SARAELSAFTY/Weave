using System.Collections.Generic;
using Game.Scripts.Definitions;
using UnityEngine;

namespace Game.Scripts.Narrative
{
    /// <summary>Detects when a resource drops below its warning threshold and manages per-resource cooldowns so warnings don't repeat too frequently.</summary>
    /// <remarks>Non-MonoBehaviour; call <see cref="Tick"/> once per card resolution to decrement cooldowns, then <see cref="TryGetTriggeredWarning"/> to check for new warnings.</remarks>
    public class ResourceWarningMonitor
    {
        private readonly ResourceCatalog catalog;
        private readonly ResourceState resourceState;
        private readonly HashSet<ResourceData> warnedResources = new HashSet<ResourceData>();
        private readonly Dictionary<ResourceData, int> cooldownCardsRemaining = new Dictionary<ResourceData, int>();

        /// <summary>Creates a monitor bound to the given catalog and resource state.</summary>
        /// <param name="catalog">Provides the list of resources and their warning configuration.</param>
        /// <param name="resourceState">Provides current resource values for threshold checks.</param>
        public ResourceWarningMonitor(ResourceCatalog catalog, ResourceState resourceState)
        {
            this.catalog = catalog;
            this.resourceState = resourceState;
        }

        /// <summary>Clears all warned flags and cooldown counters, restoring the monitor to its initial state.</summary>
        public void Reset()
        {
            warnedResources.Clear();
            cooldownCardsRemaining.Clear();
        }

        /// <summary>Decrements all active cooldown counters by one; call once per card resolution.</summary>
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

        /// <summary>Sets the cooldown counter for a resource after its warning has been shown, using the resource's configured warningCooldownCards value.</summary>
        /// <param name="resource">The resource whose warning was just displayed.</param>
        public void OnWarningShown(ResourceData resource)
        {
            if (resource == null)
            {
                return;
            }

            cooldownCardsRemaining[resource] = Mathf.Max(0, resource.warningCooldownCards);
        }

        /// <summary>Checks all resources for a warning trigger: value at or below threshold, not already warned, cooldown expired, and speaker assigned.</summary>
        /// <param name="triggeredResource">Receives the first resource that meets all warning conditions, or null if none.</param>
        /// <returns>True if a resource triggered a new warning this call.</returns>
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
                    warnedResources.Remove(resource);
                    continue;
                }

                cooldownCardsRemaining.TryGetValue(resource, out int cooldown);
                if (warnedResources.Contains(resource) || cooldown > 0 || resource.speaker == null)
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
