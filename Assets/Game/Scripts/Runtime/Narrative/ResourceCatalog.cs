using System.Collections.Generic;
using Game.Scripts.Definitions;
using UnityEngine;

namespace Game.Scripts.Runtime.Narrative
{
    /// <summary>Stores the list of resource definitions used by a narrative database.</summary>
    [CreateAssetMenu(fileName = "ResourceCatalog", menuName = "Weave/Resource Catalog")]
    public class ResourceCatalog : ScriptableObject
    {
        public List<ResourceData> resources = new List<ResourceData>();
    }
}
