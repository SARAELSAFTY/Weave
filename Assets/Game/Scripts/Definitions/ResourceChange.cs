using System;

namespace Game.Scripts.Definitions
{
    /// <summary>Represents a value change for one resource ID.</summary>
    [Serializable]
    public struct ResourceValue
    {
        public string id;
        public int value;
    }

    /// <summary>Contains resource value changes for a card choice.</summary>
    [Serializable]
    public struct ResourceChange
    {
        public ResourceValue[] values;
    }
}
