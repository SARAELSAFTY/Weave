using Game.Scripts.Definitions;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor
{
    [CustomEditor(typeof(SpeakerData))]
    public class SpeakerDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SpeakerData speaker = (SpeakerData)target;

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Speaker Identity", EditorStyles.boldLabel);

            SerializedProperty assetNameProp = serializedObject.FindProperty("assetName");
            SerializedProperty displayNameProp = serializedObject.FindProperty("displayName");
            SerializedProperty portraitProp = serializedObject.FindProperty("portrait");
            SerializedProperty llmPersonaPromptProp = serializedObject.FindProperty("llmPersonaPrompt");

            EditorGUILayout.PropertyField(assetNameProp, new GUIContent(
                "Asset Name (ID)",
                "Author-facing identifier. Convention: Spk_<PascalName>. " +
                "Drives the asset filename — changing this renames the .asset file. " +
                "Leave empty to keep the current filename ('" + speaker.name + "')."));

            EditorGUILayout.PropertyField(displayNameProp, new GUIContent(
                "Display Name (UI)",
                "Player-facing name shown in the in-game UI. " +
                "Leave empty to fall back to the Asset Name."));

            if (string.IsNullOrWhiteSpace(displayNameProp.stringValue))
            {
                EditorGUILayout.HelpBox(
                    "Display Name is empty — in-game UI will show the Asset Name (\"" + speaker.AssetName + "\") instead.",
                    MessageType.None);
            }

            EditorGUILayout.PropertyField(portraitProp, new GUIContent("Portrait", "Character portrait image."));

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("LLM Persona", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "The persona prompt defines this character's tone, identity, and behavior when voiced by AI during reactions.",
                MessageType.Info);
            EditorGUILayout.PropertyField(llmPersonaPromptProp, GUIContent.none);

            if (GUI.changed)
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
                CardGraphWindow.RefreshOpenWindows();
            }
        }
    }
}