using System;

namespace Game.Scripts.Definitions
{
    /// <summary>A single resource paired with an integer delta or absolute value.</summary>
    [Serializable]
    public struct ResourceValue
    {
        /// <summary>The resource this entry modifies.</summary>
        public ResourceData resource;
        /// <summary>The numeric change applied to the resource.</summary>
        public int value;
    }

    /// <summary>A collection of resource deltas applied atomically when a card choice is selected.</summary>
    [Serializable]
    public struct ResourceChange
    {
        /// <summary>Individual resource-value pairs in this change set.</summary>
        public ResourceValue[] values;
    }
}
