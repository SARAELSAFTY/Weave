using UnityEngine;

namespace Game.Scripts
{
    /// <summary>Shared validation for required MonoBehaviour Inspector references.</summary>
    public static class InspectorValidation
    {
        /// <summary>Logs a standard missing-reference error if the field is null. Returns true when the field is invalid (missing).</summary>
        public static bool RequireField(object field, string fieldName, string componentTag, Object context)
        {
            if (field == null || field is Object unityObject && unityObject == null)
            {
                Debug.LogError($"[{componentTag}] Missing required Inspector reference '{fieldName}' on '{context.name}'.", context);
                return true;
            }

            return false;
        }
    }
}
