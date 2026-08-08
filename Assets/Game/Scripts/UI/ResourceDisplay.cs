using System;
using Game.Scripts.Definitions;
using Game.Scripts.Runtime.Narrative;
using TMPro;
using UnityEngine;

namespace Game.Scripts.UI
{
    /// <summary>Updates resource labels when resource values change.</summary>
    public class ResourceDisplay : MonoBehaviour
    {
        /// <summary>Binds one resource ID to one text label.</summary>
        [Serializable]
        public struct LabelBinding
        {
            public string resourceId;
            public TMP_Text label;
        }

        [SerializeField] private ResourceState resourceState;
        [SerializeField] private ResourceCatalog catalog;
        [SerializeField] private LabelBinding[] labels;

        private void Awake()
        {
            if (resourceState == null)
            {
                Debug.LogError($"[ResourceDisplay] Missing required Inspector reference '{nameof(resourceState)}' on '{gameObject.name}'.", this);
                enabled = false;
                return;
            }

            if (labels == null || labels.Length == 0)
            {
                Debug.LogError($"[ResourceDisplay] No label bindings assigned on '{gameObject.name}'.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (resourceState != null)
            {
                resourceState.Changed += Refresh;
                Refresh();
            }
        }

        private void OnDisable()
        {
            if (resourceState != null)
            {
                resourceState.Changed -= Refresh;
            }
        }

        private void Refresh()
        {
            if (labels == null)
            {
                return;
            }

            foreach (LabelBinding binding in labels)
            {
                if (binding.label == null || string.IsNullOrEmpty(binding.resourceId))
                {
                    continue;
                }

                string displayName = ResolveDisplayName(binding.resourceId);
                binding.label.text = $"{displayName}: {resourceState.Get(binding.resourceId)}";
            }
        }

        private string ResolveDisplayName(string resourceId)
        {
            if (catalog != null && catalog.resources != null)
            {
                foreach (ResourceData definition in catalog.resources)
                {
                    if (definition != null && definition.id == resourceId && !string.IsNullOrEmpty(definition.displayName))
                    {
                        return definition.displayName;
                    }
                }
            }

            return resourceId;
        }
    }
}
