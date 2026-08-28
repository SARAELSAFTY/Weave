using System;
using System.Collections.Generic;
using Game.Scripts.Definitions;
using Game.Scripts.Runtime.Narrative;
using UnityEngine;

// Keep this schema in sync with LlmPromptTemplates.petitionSystemInstructions.
namespace Game.Scripts.Runtime.Llm
{
    [Serializable]
    public class PetitionResourceDelta
    {
        public string resource; // must match ResourceData.AssetName
        public int delta;
    }

    [Serializable]
    public class PetitionResolution
    {
        /// <summary>Expected: "deliberating" or "proposal". Compared case-insensitively.</summary>
        public string phase;
        public string reaction;
        public PetitionResourceDelta[] resourceChanges;
        public string historyTag;
        public bool isSpam;

        /// <summary>
        /// True only for an explicit "proposal" phase. Missing/malformed phase is treated as
        /// deliberation so resources are never applied by accident.
        /// </summary>
        public bool IsProposal => string.Equals(phase, "proposal", StringComparison.OrdinalIgnoreCase);
    }

    public readonly struct PetitionApplyResult
    {
        public readonly ResourceChange? resourceChange;
        public readonly string historyTag;

        public PetitionApplyResult(ResourceChange? resourceChange, string historyTag)
        {
            this.resourceChange = resourceChange;
            this.historyTag = historyTag;
        }
    }

    public static class PetitionResolutionApplier
    {
        public static PetitionApplyResult Apply(PetitionResolution resolution, ResourceCatalog catalog, int clampMagnitude)
        {
            // Fail closed: deliberating or malformed phase must never move resources.
            // Models have been observed to ignore the JSON contract on this field.
            if (resolution == null || !resolution.IsProposal)
            {
                return new PetitionApplyResult(null, null);
            }

            List<ResourceValue> changeList = new List<ResourceValue>();

            if (resolution.resourceChanges != null && catalog != null)
            {
                foreach (PetitionResourceDelta delta in resolution.resourceChanges)
                {
                    if (delta == null || string.IsNullOrWhiteSpace(delta.resource))
                    {
                        continue;
                    }

                    ResourceData matchedResource = catalog.FindByAssetName(delta.resource);
                    if (matchedResource == null)
                    {
                        continue;
                    }

                    int clampedValue = Mathf.Clamp(delta.delta, -clampMagnitude, clampMagnitude);
                    changeList.Add(new ResourceValue { resource = matchedResource, value = clampedValue });
                }
            }

            ResourceChange? change = changeList.Count > 0 ? new ResourceChange { values = changeList.ToArray() } : (ResourceChange?)null;
            string historyTag = !string.IsNullOrWhiteSpace(resolution.historyTag)
                ? resolution.historyTag.Trim()
                : null;

            return new PetitionApplyResult(change, historyTag);
        }
    }
}
