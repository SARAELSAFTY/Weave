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

            ResourceData resource = (ResourceData)target;

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Resource Identity", EditorStyles.boldLabel);

            SerializedProperty assetNameProp = serializedObject.FindProperty("assetName");
            SerializedProperty displayNameLocalizedProp = serializedObject.FindProperty("displayNameLocalized");
            SerializedProperty iconProp = serializedObject.FindProperty("icon");
            SerializedProperty startingValProp = serializedObject.FindProperty("defaultStartingValue");

            EditorGUILayout.PropertyField(assetNameProp, new GUIContent(
                "Asset Name (ID)"));

            LocalizedTextGui.Draw(displayNameLocalizedProp, "Display Name");

            if (displayNameLocalizedProp.FindPropertyRelative("english").stringValue.Trim().Length == 0 &&
                displayNameLocalizedProp.FindPropertyRelative("arabic").stringValue.Trim().Length == 0)
            {
            }

            EditorGUILayout.PropertyField(iconProp, new GUIContent("Icon"));
            EditorGUILayout.PropertyField(startingValProp, new GUIContent("Starting Value"));

            SerializedProperty collapseThresholdProp = serializedObject.FindProperty("collapseThreshold");
            EditorGUILayout.PropertyField(collapseThresholdProp, new GUIContent("Collapse Threshold"));

            EditorGUILayout.Space(10);
            SerializedProperty warningThresholdProp = serializedObject.FindProperty("warningThresholdPercent");
            EditorGUILayout.PropertyField(warningThresholdProp, new GUIContent("Warning Threshold (%)"));

            EditorGUILayout.Space(10);
            SerializedProperty warningSpeakerProp = serializedObject.FindProperty("warningSpeaker");
            SerializedProperty cooldownProp = serializedObject.FindProperty("warningCooldownCards");

            EditorGUILayout.PropertyField(warningSpeakerProp, new GUIContent("Warning Speaker"));
            EditorGUILayout.PropertyField(cooldownProp, new GUIContent("Warning Cooldown (Cards)"));

            if (GUI.changed)
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
                CardGraphWindow.RefreshOpenWindows();
            }
        }
    }
}
