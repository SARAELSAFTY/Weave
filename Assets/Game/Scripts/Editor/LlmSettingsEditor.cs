using Game.Scripts.Runtime.Llm;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor
{
    [CustomEditor(typeof(LlmSettings))]
    public class LlmSettingsEditor : UnityEditor.Editor
    {
        private readonly struct ModelOption
        {
            public readonly string label;
            public readonly string id;

            public ModelOption(string label, string id)
            {
                this.label = label;
                this.id = id;
            }
        }

        private static readonly ModelOption[] Models =
        {
            new ModelOption("GPT-OSS 20B", "openai/gpt-oss-20b"),
            new ModelOption("Qwen 3.6 27B", "qwen/qwen3.6-27b")
        };

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty modelProperty = serializedObject.FindProperty("groqModel");
            SerializedProperty reasoningEffortProperty = serializedObject.FindProperty("reasoningEffort");
            EditorGUILayout.LabelField("API Settings", EditorStyles.boldLabel);

            int selectedIndex = FindModelIndex(modelProperty.stringValue);
            if (selectedIndex >= 0)
            {
                int newIndex = EditorGUILayout.Popup("Model", selectedIndex, GetModelLabels());
                if (newIndex != selectedIndex)
                {
                    modelProperty.stringValue = Models[newIndex].id;
                    ApplyRecommendedReasoningEffort(newIndex, reasoningEffortProperty);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("The current model is not in the quick-select list. Choose a listed model to replace it.", MessageType.Warning);
                EditorGUI.BeginChangeCheck();
                int newIndex = EditorGUILayout.Popup("Model", 0, GetModelLabels());
                if (EditorGUI.EndChangeCheck())
                {
                    modelProperty.stringValue = Models[newIndex].id;
                    ApplyRecommendedReasoningEffort(newIndex, reasoningEffortProperty);
                }
            }

            EditorGUILayout.Space(4);
            DrawPropertiesExcluding(serializedObject, "m_Script", "groqModel");

            serializedObject.ApplyModifiedProperties();
        }

        private static void ApplyRecommendedReasoningEffort(int modelIndex, SerializedProperty reasoningEffortProperty)
        {
            if (reasoningEffortProperty != null)
            {
                reasoningEffortProperty.stringValue = modelIndex == 0 ? "low" : "none";
            }
        }

        private static int FindModelIndex(string modelId)
        {
            for (int i = 0; i < Models.Length; i++)
            {
                if (Models[i].id == modelId)
                {
                    return i;
                }
            }

            return -1;
        }

        private static string[] GetModelLabels()
        {
            string[] labels = new string[Models.Length];
            for (int i = 0; i < Models.Length; i++)
            {
                labels[i] = Models[i].label;
            }

            return labels;
        }
    }
}
