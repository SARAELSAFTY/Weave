using UnityEngine;

namespace Game.Scripts
{
    /// <summary>Provides runtime validation helpers for required Inspector-assigned references.</summary>
    public static class InspectorValidation
    {
        /// <summary>Logs an error and returns true if the field reference is null or destroyed.</summary>
        /// <param name="field">The reference to check (supports both managed and Unity object types).</param>
        /// <param name="fieldName">Name of the field, used in the log message.</param>
        /// <param name="componentTag">Owning component name, used as a log tag prefix.</param>
        /// <param name="context">Unity object to attach the log message to.</param>
        /// <returns>True when the reference is missing; false when valid.</returns>
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
