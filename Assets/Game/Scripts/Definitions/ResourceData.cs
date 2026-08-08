using UnityEngine;

namespace Game.Scripts.Definitions
{
    /// <summary>Defines display and default values for one game resource.</summary>
    [CreateAssetMenu(fileName = "NewResource", menuName = "Weave/Resource Data")]
    public class ResourceData : ScriptableObject
    {
        public string id;
        public string displayName;
        public Sprite icon;
        public int defaultStartingValue;
    }
}
