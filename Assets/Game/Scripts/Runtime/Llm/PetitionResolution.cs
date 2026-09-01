using System;
using System.Collections.Generic;
using Game.Scripts.Definitions;
using Game.Scripts.Runtime.Narrative;
using UnityEngine;

namespace Game.Scripts.Runtime.Llm
{
    /// <summary>A single resource delta entry within a <see cref="PetitionResolution"/>.</summary>
    [Serializable]
    public class PetitionResourceDelta
    {
        /// <summary>Asset name of the resource being modified (must match a ResourceData asset name exactly).</summary>
        public string resource;
        /// <summary>Signed integer change applied to the resource; clamped at application time.</summary>
        public int delta;
    }

    /// <summary>Structured JSON response from a petition turn, deserialized from the model's output.</summary>
    /// <remarks>The JSON keys (phase, reaction, resourceChanges, historyTag) must stay in sync with the
    /// contract defined in <see cref="LlmPromptTemplates.petitionSystemInstructions"/>. Changing either side
    /// without updating the other will silently break petition parsing.</remarks>
    [Serializable]
    public class PetitionResolution
    {
        /// <summary>"deliberating" while the matter is open; "proposal" once the ruler gives a clear command.</summary>
        public string phase;
        /// <summary>1-2 in-character sentences expressing the petitioner's reaction.</summary>
        public string reaction;
        /// <summary>Resource changes to apply when <see cref="IsProposal"/> is true; null during deliberation.</summary>
        public PetitionResourceDelta[] resourceChanges;
        /// <summary>Short snake_case tag recorded in kingdom history when the petition resolves.</summary>
        public string historyTag;

        /// <summary>True when the petitioner has accepted the ruler's command and the petition can be finalized.</summary>
        public bool IsProposal => string.Equals(phase, "proposal", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Immutable result of applying a <see cref="PetitionResolution"/> to the resource catalog.</summary>
    public readonly struct PetitionApplyResult
    {
        /// <summary>The resolved resource change, or null if the resolution was not a proposal or had no valid deltas.</summary>
        public readonly ResourceChange? resourceChange;
        /// <summary>The trimmed history tag, or null if none was provided.</summary>
        public readonly string historyTag;

        /// <summary>Constructs a new apply result with the given resource change and history tag.</summary>
        public PetitionApplyResult(ResourceChange? resourceChange, string historyTag)
        {
            this.resourceChange = resourceChange;
            this.historyTag = historyTag;
        }
    }

    /// <summary>Validates and applies a <see cref="PetitionResolution"/> against the live resource catalog.</summary>
    public static class PetitionResolutionApplier
    {
        /// <summary>Resolves resource deltas against the catalog, clamps values, and returns the apply result.</summary>
        /// <param name="resolution">The parsed petition resolution; ignored if null or not a proposal.</param>
        /// <param name="catalog">Resource catalog used to look up resources by asset name.</param>
        /// <param name="clampMagnitude">Maximum absolute value each delta is clamped to.</param>
        /// <returns>A <see cref="PetitionApplyResult"/> containing the validated change and history tag.</returns>
        public static PetitionApplyResult Apply(PetitionResolution resolution, ResourceCatalog catalog, int clampMagnitude)
        {
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
