using System.Collections.Generic;

// Evaluates story conditions against resource levels.
public static class NarrativeConditionEvaluator
{
    public static bool AreConditionsMet(IEnumerable<NarrativeCondition> conditions, ResourceState resources)
    {
        if (conditions == null)
        {
            return true;
        }

        if (resources == null)
        {
            return false;
        }

        foreach (NarrativeCondition condition in conditions)
        {
            if (!Compare(resources.Get(condition.resourceId), condition.comparison, condition.value))
            {
                return false;
            }
        }

        return true;
    }

    public static bool Compare(int actual, NarrativeComparison comparison, int expected)
    {
        return comparison switch
        {
            NarrativeComparison.LessThan => actual < expected,
            NarrativeComparison.LessThanOrEqual => actual <= expected,
            NarrativeComparison.Equal => actual == expected,
            NarrativeComparison.GreaterThanOrEqual => actual >= expected,
            NarrativeComparison.GreaterThan => actual > expected,
            _ => false
        };
    }
}
