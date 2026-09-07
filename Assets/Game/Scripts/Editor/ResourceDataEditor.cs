using Game.Scripts.Definitions;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor
{
    /// <summary>Custom inspector for ResourceData: identity, values and low-value warning settings.</summary>
    [CustomEditor(typeof(ResourceData))]
    public class ResourceDataEditor : UnityEditor.Editor
    {
        /// <summary>Draws the resource fields and refreshes open graph windows when anything changes.</summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Resource Identity", EditorStyles.boldLabel);

            SerializedProperty assetNameProp = serializedObject.FindProperty("assetName");
            SerializedProperty displayNameLocalizedProp = serializedObject.FindProperty("displayNameLocalized");
            SerializedProperty iconProp = serializedObject.FindProperty("icon");
            SerializedProperty startingValProp = serializedObject.FindProperty("defaultStartingValue");

            EditorGUILayout.PropertyField(assetNameProp, new GUIContent(
                "Asset Name (ID)"));

            LocalizedTextGui.Draw(displayNameLocalizedProp, "Display Name");

            EditorGUILayout.PropertyField(iconProp, new GUIContent("Icon"));
            EditorGUILayout.PropertyField(startingValProp, new GUIContent("Starting Value"));

            SerializedProperty collapseThresholdProp = serializedObject.FindProperty("collapseThreshold");
            EditorGUILayout.PropertyField(collapseThresholdProp, new GUIContent("Collapse Threshold"));

            EditorGUILayout.Space(10);
            SerializedProperty warningThresholdProp = serializedObject.FindProperty("warningThresholdPercent");
            EditorGUILayout.PropertyField(warningThresholdProp, new GUIContent("Warning Threshold (%)"));

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Speaker & Warning", EditorStyles.boldLabel);

            SerializedProperty speakerProp = serializedObject.FindProperty("speaker");
            SerializedProperty cooldownProp = serializedObject.FindProperty("warningCooldownCards");

            EditorGUILayout.PropertyField(speakerProp, new GUIContent("Speaker",
                "Speaker providing both the portrait for cards and the LLM persona prompt for collapse."));
            EditorGUILayout.PropertyField(cooldownProp, new GUIContent("Warning Cooldown (Cards)"));

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Collapse Ending Fallback", EditorStyles.boldLabel);

            SerializedProperty fallbackEnProp = serializedObject.FindProperty("collapseEndingFallbackEnglish");
            SerializedProperty fallbackArProp = serializedObject.FindProperty("collapseEndingFallbackArabic");

            EditorGUILayout.PropertyField(fallbackEnProp, new GUIContent("Fallback Text (English)",
                "Shown when LLM collapse generation fails or is offline."));
            EditorGUILayout.PropertyField(fallbackArProp, new GUIContent("Fallback Text (Arabic)",
                "Shown when LLM collapse generation fails or is offline."));

            if (GUI.changed)
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
                CardGraphWindow.RefreshOpenWindows();
            }
        }
    }
}
