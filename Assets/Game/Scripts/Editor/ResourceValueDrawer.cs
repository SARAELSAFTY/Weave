using Game.Scripts.Definitions;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor
{
    /// <summary>Draws a ResourceValue (resource reference + delta) on a single inspector row.</summary>
    [CustomPropertyDrawer(typeof(ResourceValue))]
    public class ResourceValueDrawer : PropertyDrawer
    {
        /// <summary>Draws the resource reference at 65% width and the value beside it at 30%.</summary>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty resourceProp = property.FindPropertyRelative("resource");
            SerializedProperty valueProp = property.FindPropertyRelative("value");

            float resourceWidth = position.width * 0.65f;
            float valueWidth = position.width * 0.30f;
            float spacing = position.width * 0.05f;

            Rect resourceRect = new Rect(position.x, position.y, resourceWidth, position.height);
            Rect valueRect = new Rect(position.x + resourceWidth + spacing, position.y, valueWidth, position.height);

            EditorGUI.PropertyField(resourceRect, resourceProp, GUIContent.none);
            EditorGUI.PropertyField(valueRect, valueProp, GUIContent.none);

            EditorGUI.EndProperty();
        }
    }
}
