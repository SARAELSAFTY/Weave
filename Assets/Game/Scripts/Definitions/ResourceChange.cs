using System;

namespace Game.Scripts.Definitions
{
    [Serializable]
    public struct ResourceValue
    {
        public ResourceData resource;
        public int value;
    }

    [Serializable]
    public struct ResourceChange
    {
        public ResourceValue[] values;
    }
}
