using System;
using System.Collections.Generic;
using Game.Scripts.Definitions;
using UnityEngine;

namespace Game.Scripts.Runtime.Narrative
{
    /// <summary>Maps a resource to the ending card shown when it reaches zero.</summary>
    [Serializable]
    public class ResourceCollapseEnding
    {
        public ResourceData resource;
        public CardData endingCard;
    }

    [CreateAssetMenu(fileName = "ResourceCatalog", menuName = "Weave/Resource Catalog")]
    public class ResourceCatalog : ScriptableObject
    {
        public List<ResourceData> resources = new List<ResourceData>();

        [Tooltip("Per-resource ending card shown when that resource collapses to zero. Every resource " +
                 "that should trigger a distinct lose ending needs an entry here.")]
        public List<ResourceCollapseEnding> collapseEndings = new List<ResourceCollapseEnding>();

        public ResourceData FindByAssetName(string assetName)
        {
            if (string.IsNullOrWhiteSpace(assetName) || resources == null)
            {
                return null;
            }

            foreach (ResourceData resource in resources)
            {
                if (resource != null && string.Equals(resource.AssetName, assetName, StringComparison.OrdinalIgnoreCase))
                {
                    return resource;
                }
            }

            return null;
        }

        public CardData GetCollapseEndingCard(ResourceData resource)
        {
            if (resource == null || collapseEndings == null)
            {
                return null;
            }

            foreach (ResourceCollapseEnding entry in collapseEndings)
            {
                if (entry != null && entry.resource == resource)
                {
                    return entry.endingCard;
                }
            }

            return null;
        }
    }
}
