using System;

// Resource amount paired with a resource ID string.
[Serializable]
public struct ResourceValue
{
    public string id;
    public int value;
}

// Resource changes applied when making a choice.
[Serializable]
public struct ResourceChange
{
    public ResourceValue[] values;
}
