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
        /// <summary>Binds one resource asset to one text label.</summary>
        [Serializable]
        public struct LabelBinding
        {
            public ResourceData resource;
            public TMP_Text label;
        }

        [SerializeField] private ResourceState resourceState;
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
                if (binding.label == null || binding.resource == null)
                {
                    continue;
                }

                binding.label.text = $"{binding.resource.DisplayName}: {resourceState.Get(binding.resource)}";
            }
        }
    }
}
