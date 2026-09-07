using Game.Scripts.Localization;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor
{
    /// <summary>Custom inspector for LocalizedLabel: target text, font category, translations and edit-mode preview buttons.</summary>
    [CustomEditor(typeof(LocalizedLabel))]
    [CanEditMultipleObjects]
    public class LocalizedLabelEditor : UnityEditor.Editor
    {
        private SerializedProperty textComponentProp;
        private SerializedProperty fontCategoryProp;
        private SerializedProperty englishProp;
        private SerializedProperty arabicProp;

        private void OnEnable()
        {
            textComponentProp = serializedObject.FindProperty("textComponent");
            fontCategoryProp = serializedObject.FindProperty("fontCategory");
            englishProp = serializedObject.FindProperty("english");
            arabicProp = serializedObject.FindProperty("arabic");
        }

        /// <summary>Draws the label fields and preview buttons that apply either translation in the editor.</summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(textComponentProp, new GUIContent("Target Text"));
            EditorGUILayout.PropertyField(fontCategoryProp, new GUIContent("Font Category"));

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Translations", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(englishProp, new GUIContent("English (EN)"));
            EditorGUILayout.PropertyField(arabicProp, new GUIContent("Arabic (AR)"));

            serializedObject.ApplyModifiedProperties();

            LocalizedLabel label = (LocalizedLabel)target;

            EditorGUILayout.Space(8);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Preview English"))
            {
                // Undo and SetDirty must cover the object SetText actually modifies: the target text when assigned, otherwise the label itself.
                Object previewTarget = label.TargetText != null ? (Object)label.TargetText : label;
                Undo.RecordObject(previewTarget, "Preview English");
                RtlTextHelper.SetText(label.TargetText, label.English, GameLanguage.English, label.FontCategory);
                EditorUtility.SetDirty(previewTarget);
            }

            if (GUILayout.Button("Preview Arabic"))
            {
                Object previewTarget = label.TargetText != null ? (Object)label.TargetText : label;
                Undo.RecordObject(previewTarget, "Preview Arabic");
                RtlTextHelper.SetText(label.TargetText, label.Arabic, GameLanguage.Arabic, label.FontCategory);
                EditorUtility.SetDirty(previewTarget);
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
