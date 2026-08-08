using System;
using System.Collections.Generic;
using Game.Scripts.Definitions;
using UnityEngine;

namespace Game.Scripts.Runtime.Narrative
{
    /// <summary>Tracks current resource values and notifies listeners when values change.</summary>
    public class ResourceState : MonoBehaviour
    {
        [SerializeField] private ResourceCatalog catalog;

        private readonly Dictionary<string, int> values = new Dictionary<string, int>();

        /// <summary>Raised after resource values are updated.</summary>
        public event Action Changed;

        private void Awake()
        {
            values.Clear();
            if (catalog == null)
            {
                Debug.LogError($"[ResourceState] Missing required Inspector field '{nameof(catalog)}' on '{gameObject.name}'.", this);
                enabled = false;
                return;
            }

            foreach (ResourceData resource in catalog.resources)
            {
                if (resource != null && !string.IsNullOrEmpty(resource.id))
                {
                    values[resource.id] = resource.defaultStartingValue;
                }
            }
        }

        /// <summary>Applies a resource change set and raises the change event.</summary>
        public void Apply(ResourceChange change)
        {
            if (change.values != null)
            {
                foreach (ResourceValue changeValue in change.values)
                {
                    if (!string.IsNullOrEmpty(changeValue.id))
                    {
                        values.TryGetValue(changeValue.id, out int currentValue);
                        values[changeValue.id] = currentValue + changeValue.value;
                    }
                }
            }

            Changed?.Invoke();
        }

        /// <summary>Gets the current value for a resource ID, or zero when not found.</summary>
        public int Get(string resourceId)
        {
            return !string.IsNullOrEmpty(resourceId) && values.TryGetValue(resourceId, out int value) ? value : 0;
        }
    }
}
