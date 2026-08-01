using System;

// Comparison operator for resource condition checks.
public enum NarrativeComparison
{
    LessThan,
    LessThanOrEqual,
    Equal,
    GreaterThanOrEqual,
    GreaterThan
}


// Single resource requirement check.
[Serializable]
public struct NarrativeCondition
{
    public string resourceId;
    public NarrativeComparison comparison;
    public int value;
}
