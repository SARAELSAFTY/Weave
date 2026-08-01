using System;
using System.Collections.Generic;
using UnityEngine;

// Tracks active kingdom resource amounts and notifies UI when values change.
public class ResourceState : MonoBehaviour
{
    [SerializeField, Tooltip("Starting resource amounts when game begins.")] private ResourceValue[] startingValues;

    private readonly Dictionary<string, int> values = new Dictionary<string, int>();

    public event Action Changed;

    private void Awake()
    {
        values.Clear();
        if (startingValues == null || startingValues.Length == 0)
        {
            Debug.LogError($"[ResourceState] Missing or empty required Inspector field '{nameof(startingValues)}' on '{gameObject.name}'.", this);
            enabled = false;
            return;
        }

        foreach (ResourceValue resource in startingValues)
        {
            if (!string.IsNullOrEmpty(resource.id))
            {
                values[resource.id] = resource.value;
            }
        }
    }

    // Modifies tracked resources based on choice outcome and triggers update listeners.
    public void Apply(ResourceChange change)
    {
        if (change.values != null)
        {
            foreach (ResourceValue changeValue in change.values)
            {
                if (!string.IsNullOrEmpty(changeValue.id))
                {
                    values.TryGetValue(changeValue.id, out int currentValue);
                    values[changeValue.id] = currentValue + changeValue.value;
                }
            }
        }

        Changed?.Invoke();
    }

    // Returns the quantity for the specified resource ID, returning zero if unassigned.
    public int Get(string resourceId)
    {
        return !string.IsNullOrEmpty(resourceId) && values.TryGetValue(resourceId, out int value) ? value : 0;
    }
}
