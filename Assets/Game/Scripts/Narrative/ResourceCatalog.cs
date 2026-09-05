using System;
using System.Collections.Generic;
using Game.Scripts.Definitions;
using UnityEngine;

namespace Game.Scripts.Narrative
{
    /// <summary>ScriptableObject listing all game resources.</summary>
    [CreateAssetMenu(fileName = "ResourceCatalog", menuName = "Weave/Resource Catalog")]
    public class ResourceCatalog : ScriptableObject
    {
        /// <summary>All resources tracked by the game.</summary>
        [Tooltip("All resources tracked by the game.")]
        public List<ResourceData> resources = new List<ResourceData>();

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
    }
}
