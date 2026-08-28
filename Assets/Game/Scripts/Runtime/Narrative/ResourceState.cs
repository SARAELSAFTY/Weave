using System;
using System.Collections.Generic;
using Game.Scripts.Definitions;
using UnityEngine;

namespace Game.Scripts.Runtime.Narrative
{
    public class ResourceState : MonoBehaviour
    {
        [SerializeField] private ResourceCatalog catalog;

        private readonly Dictionary<ResourceData, int> values = new Dictionary<ResourceData, int>();

        public event Action Changed;

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

        public int Get(ResourceData resource)
        {
            return resource != null && values.TryGetValue(resource, out int value) ? value : 0;
        }
    }
}
