using Game.Scripts.Definitions;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor
{
    /// <summary>Custom inspector for SpeakerData: identity fields followed by the LLM persona prompt.</summary>
    [CustomEditor(typeof(SpeakerData))]
    public class SpeakerDataEditor : UnityEditor.Editor
    {
        /// <summary>Draws the speaker fields and refreshes open graph windows when anything changes.</summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SpeakerData speaker = (SpeakerData)target;

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Speaker Identity", EditorStyles.boldLabel);

            SerializedProperty assetNameProp = serializedObject.FindProperty("assetName");
            SerializedProperty displayNameLocalizedProp = serializedObject.FindProperty("displayNameLocalized");
            SerializedProperty portraitProp = serializedObject.FindProperty("portrait");
            SerializedProperty llmPersonaPromptProp = serializedObject.FindProperty("llmPersonaPrompt");

            EditorGUILayout.PropertyField(assetNameProp, new GUIContent(
                "Asset Name (ID)"));

            LocalizedTextGui.Draw(displayNameLocalizedProp, "Display Name");

            if (displayNameLocalizedProp.FindPropertyRelative("english").stringValue.Trim().Length == 0 &&
                displayNameLocalizedProp.FindPropertyRelative("arabic").stringValue.Trim().Length == 0)
            {
            }

            EditorGUILayout.PropertyField(portraitProp, new GUIContent("Portrait"));

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("LLM Persona", EditorStyles.boldLabel);
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
