using Game.Scripts.Definitions;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor
{
    [CustomEditor(typeof(ResourceData))]
    public class ResourceDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            ResourceData resource = (ResourceData)target;

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Resource Identity", EditorStyles.boldLabel);

            SerializedProperty assetNameProp = serializedObject.FindProperty("assetName");
            SerializedProperty displayNameProp = serializedObject.FindProperty("displayName");
            SerializedProperty displayNameLocalizedProp = serializedObject.FindProperty("displayNameLocalized");
            SerializedProperty iconProp = serializedObject.FindProperty("icon");
            SerializedProperty startingValProp = serializedObject.FindProperty("defaultStartingValue");

            EditorGUILayout.PropertyField(assetNameProp, new GUIContent(
                "Asset Name (ID)",
                "Author-facing identifier. Convention: Res_<PascalName>, e.g. Res_Trust. " +
                "Drives the asset filename - changing this renames the .asset file. " +
                "Leave empty to keep the current filename ('" + resource.name + "')."));

            EditorGUILayout.PropertyField(displayNameProp, new GUIContent(
                "Display Name (UI)",
                "Player-facing label shown in the HUD and resource bars. " +
                "Leave empty to fall back to the Asset Name."));

            LocalizedTextGui.Draw(displayNameLocalizedProp, "Display Name Localized");

            if (string.IsNullOrWhiteSpace(displayNameProp.stringValue))
            {
                EditorGUILayout.HelpBox(
                    "Display Name is empty - HUD will show the Asset Name (\"" + resource.AssetName + "\") instead unless a localized name is set.",
                    MessageType.None);
            }

            EditorGUILayout.PropertyField(iconProp, new GUIContent("Icon", "Resource icon image."));
            EditorGUILayout.PropertyField(startingValProp, new GUIContent("Starting Value", "Default value when a run begins."));

            SerializedProperty collapseThresholdProp = serializedObject.FindProperty("collapseThreshold");
            EditorGUILayout.PropertyField(collapseThresholdProp, new GUIContent("Collapse Threshold", "Resource triggers its collapse ending at or below this value (default 0)."));

            EditorGUILayout.Space(10);
            SerializedProperty warningThresholdProp = serializedObject.FindProperty("warningThresholdPercent");
            EditorGUILayout.PropertyField(warningThresholdProp, new GUIContent("Warning Threshold (%)",
                "Resource triggers a Warning reaction at or below this percentage of starting value."));

            EditorGUILayout.Space(10);
            SerializedProperty warningSpeakerProp = serializedObject.FindProperty("warningSpeaker");
            SerializedProperty cooldownProp = serializedObject.FindProperty("warningCooldownCards");

            EditorGUILayout.PropertyField(warningSpeakerProp, new GUIContent("Warning Speaker",
                "Speaker who reacts when this resource crosses the Warning threshold."));
            EditorGUILayout.PropertyField(cooldownProp, new GUIContent("Warning Cooldown (Cards)",
                "Minimum cards that must pass before this resource can warn again."));

            if (GUI.changed)
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
                CardGraphWindow.RefreshOpenWindows();
            }
        }
    }
}
