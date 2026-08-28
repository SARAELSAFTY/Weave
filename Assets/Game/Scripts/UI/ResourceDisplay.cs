using System;
using Game.Scripts.Definitions;
using Game.Scripts.Localization;
using Game.Scripts.Runtime.Narrative;
using TMPro;
using UnityEngine;

namespace Game.Scripts.UI
{
    public class ResourceDisplay : LocalizedDisplay
    {
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
            if (InspectorValidation.RequireField(resourceState, nameof(resourceState), nameof(ResourceDisplay), this))
            {
                enabled = false;
                return;
            }

            if (labels == null || labels.Length == 0)
            {
                Debug.LogError($"[ResourceDisplay] No label bindings assigned on '{gameObject.name}'.", this);
                enabled = false;
                return;
            }

            foreach (LabelBinding binding in labels)
            {
                RtlTextHelper.Configure(binding.label);
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (resourceState != null)
            {
                resourceState.Changed += Refresh;
            }
        }

        protected override void OnDisable()
        {
            if (resourceState != null)
            {
                resourceState.Changed -= Refresh;
            }

            base.OnDisable();
        }

        protected override void RefreshContent(GameLanguage language)
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

                RtlTextHelper.SetText(
                    binding.label,
                    $"{binding.resource.GetDisplayName(language)}: {resourceState.Get(binding.resource)}",
                    language);
            }
        }
    }
}
