using Game.Scripts.Definitions;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor
{
    [CustomEditor(typeof(CouncilMemberData))]
    public class CouncilMemberDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty memberIdProp = serializedObject.FindProperty("memberId");
            SerializedProperty displayNameProp = serializedObject.FindProperty("displayName");
            SerializedProperty titleProp = serializedObject.FindProperty("title");
            SerializedProperty portraitProp = serializedObject.FindProperty("portrait");
            SerializedProperty llmPersonaPromptProp = serializedObject.FindProperty("llmPersonaPrompt");

            EditorGUILayout.PropertyField(memberIdProp);
            EditorGUILayout.PropertyField(displayNameProp);
            EditorGUILayout.PropertyField(titleProp);
            EditorGUILayout.PropertyField(portraitProp);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("LLM Configuration", EditorStyles.boldLabel);
            
            SerializedProperty isLlmSpeakerProp = serializedObject.FindProperty("isLlmSpeaker");
            EditorGUILayout.PropertyField(isLlmSpeakerProp);
            
            if (isLlmSpeakerProp.boolValue)
            {
                EditorGUILayout.PropertyField(llmPersonaPromptProp);
            }

            if (GUI.changed)
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
                CardGraphWindow.RefreshOpenWindows();
            }
        }
    }
}