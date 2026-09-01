using System;
using System.Collections.Generic;
using Game.Scripts.Definitions;
using UnityEngine;

namespace Game.Scripts.Runtime.Narrative
{
    /// <summary>Holds the mutable integer values for every resource in the catalog and broadcasts changes.</summary>
    /// <remarks>Initializes all resources to their defaultStartingValue in Awake. Disables itself if the catalog reference is missing.</remarks>
    public class ResourceState : MonoBehaviour
    {
        [Tooltip("The resource catalog defining which resources exist and their starting values.")]
        [SerializeField] private ResourceCatalog catalog;

        private readonly Dictionary<ResourceData, int> values = new Dictionary<ResourceData, int>();

        /// <summary>Raised after any call to <see cref="Apply"/> completes, regardless of whether values actually changed.</summary>
        public event Action Changed;

        /// <summary>Initializes resource values from the catalog; disables the component if the catalog is unassigned.</summary>
        private void Awake()
        {
            values.Clear();
            if (InspectorValidation.RequireField(catalog, nameof(catalog), nameof(ResourceState), this))
            {
                enabled = false;
                return;
            }

            foreach (ResourceData resource in catalog.resources)
            {
                if (resource != null)
                {
                    values[resource] = resource.defaultStartingValue;
                }
            }
        }

        /// <summary>Adds each delta in the change to the corresponding resource value and raises <see cref="Changed"/>.</summary>
        /// <param name="change">The set of resource deltas to apply; null value arrays are safely skipped.</param>
        public void Apply(ResourceChange change)
        {
            if (change.values != null)
            {
                foreach (ResourceValue changeValue in change.values)
                {
                    if (changeValue.resource != null)
                    {
                        values.TryGetValue(changeValue.resource, out int currentValue);
                        values[changeValue.resource] = currentValue + changeValue.value;
                    }
                }
            }

            Changed?.Invoke();
        }

        /// <summary>Returns the current value for the given resource, or 0 if the resource is null or untracked.</summary>
        /// <param name="resource">The resource to query.</param>
        /// <returns>The current integer value.</returns>
        public int Get(ResourceData resource)
        {
            return resource != null && values.TryGetValue(resource, out int value) ? value : 0;
        }
    }
}
