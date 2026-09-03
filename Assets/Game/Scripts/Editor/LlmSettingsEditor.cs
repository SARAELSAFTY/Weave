using Game.Scripts.Runtime.Llm;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor
{
    /// <summary>Custom inspector for LlmSettings: a friendly model picker plus all remaining settings, hiding turn-limit fields that don't match the selected mode.</summary>
    [CustomEditor(typeof(LlmSettings))]
    public class LlmSettingsEditor : UnityEditor.Editor
    {
        /// <summary>Display label paired with a Groq model identifier.</summary>
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

        /// <summary>Models offered in the popup; ids are the Groq model identifiers stored on LlmSettings.</summary>
        private static readonly ModelOption[] Models =
        {
            new ModelOption("GPT-OSS 20B", "openai/gpt-oss-20b"),
            new ModelOption("Qwen 3.8 27B", "qwen/qwen3.8-27b")
        };

        /// <summary>Draws the model popup (applying the recommended reasoning effort on change) and all settings except fields irrelevant to the current turn-limit mode.</summary>
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
                // Unknown/custom model id: show the popup from the first entry but keep the custom id until the user picks a listed model.
                EditorGUI.BeginChangeCheck();
                int newIndex = EditorGUILayout.Popup("Model", 0, GetModelLabels());
                if (EditorGUI.EndChangeCheck())
                {
                    modelProperty.stringValue = Models[newIndex].id;
                    ApplyRecommendedReasoningEffort(newIndex, reasoningEffortProperty);
                }
            }

            EditorGUILayout.Space(4);

            SerializedProperty modeProperty = serializedObject.FindProperty("petitionTurnLimitMode");
            // Hide the turn-limit fields that don't apply to the selected mode (Fixed uses one value, RandomRange uses min/max).
            System.Collections.Generic.List<string> excluded = new System.Collections.Generic.List<string> { "m_Script", "groqModel" };
            if (modeProperty != null && (PetitionTurnLimitMode)modeProperty.enumValueIndex == PetitionTurnLimitMode.Fixed)
            {
                excluded.Add("petitionMinTurnLimit");
                excluded.Add("petitionMaxTurnLimit");
            }
            else
            {
                excluded.Add("petitionTurnLimit");
            }

            DrawPropertiesExcluding(serializedObject, excluded.ToArray());

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
