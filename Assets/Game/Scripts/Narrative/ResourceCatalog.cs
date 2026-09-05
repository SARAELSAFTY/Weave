using System;
using System.Collections.Generic;
using Game.Scripts.Definitions;
using UnityEngine;

namespace Game.Scripts.Narrative
{
    /// <summary>Pairs a resource with the ending card shown when that resource collapses.</summary>
    [Serializable]
    public class ResourceCollapseEnding
    {
        /// <summary>The resource whose collapse triggers this ending.</summary>
        [Tooltip("The resource whose collapse triggers this ending.")]
        public ResourceData resource;

        /// <summary>The ending card displayed when the paired resource collapses.</summary>
        [Tooltip("The ending card displayed when the paired resource collapses.")]
        public CardData endingCard;
    }

    /// <summary>ScriptableObject listing all game resources and their associated collapse-ending cards.</summary>
    [CreateAssetMenu(fileName = "ResourceCatalog", menuName = "Weave/Resource Catalog")]
    public class ResourceCatalog : ScriptableObject
    {
        /// <summary>All resources tracked by the game.</summary>
        [Tooltip("All resources tracked by the game.")]
        public List<ResourceData> resources = new List<ResourceData>();

        /// <summary>Maps each resource to the ending card shown when it reaches its collapse threshold.</summary>
        [Tooltip("Maps each resource to the ending card shown when it reaches its collapse threshold.")]
        public List<ResourceCollapseEnding> collapseEndings = new List<ResourceCollapseEnding>();

        /// <summary>Finds a resource by its asset name using case-insensitive comparison.</summary>
        /// <param name="assetName">The asset name to search for.</param>
        /// <returns>The matching <see cref="ResourceData"/>, or null if not found.</returns>
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

        /// <summary>Returns the collapse-ending card mapped to the given resource, or null if no mapping exists.</summary>
        /// <param name="resource">The resource to look up in <see cref="collapseEndings"/>.</param>
        /// <returns>The associated ending card, or null.</returns>
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
