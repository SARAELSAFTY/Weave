using Game.Scripts.Localization;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor
{
    /// <summary>Compact inspector for FontSettings: global default fonts plus optional per-category overrides behind foldouts.</summary>
    [CustomEditor(typeof(FontSettings))]
    public class FontSettingsEditor : UnityEditor.Editor
    {
        /// <summary>Serialized field name and display label for each font category row.</summary>
        private static readonly (string field, string label)[] Categories =
        {
            ("titleFont", "Title"),
            ("speakerNameFont", "Speaker Name"),
            ("menuUIFont", "Menu UI"),
            ("dialogueBodyFont", "Dialogue Body"),
            ("choiceFont", "Choice")
        };

        private readonly bool[] expanded = new bool[Categories.Length];

        /// <summary>Draws global defaults, then per-category override rows whose fonts only appear when expanded and overridden.</summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Global Defaults", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("englishFont"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("arabicFont"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("alignmentMode"));

            EditorGUILayout.Space(8);

            SerializedProperty enableOverrides = serializedObject.FindProperty("enableCategoryOverrides");
            EditorGUILayout.PropertyField(enableOverrides, new GUIContent(
                "Category Overrides"));

            if (enableOverrides.boolValue)
            {
                EditorGUI.indentLevel++;
                for (int i = 0; i < Categories.Length; i++)
                {
                    SerializedProperty pair = serializedObject.FindProperty(Categories[i].field);
                    if (pair == null)
                    {
                        continue;
                    }

                    SerializedProperty overrideDefault = pair.FindPropertyRelative("overrideDefault");
                    SerializedProperty englishFont = pair.FindPropertyRelative("englishFont");
                    SerializedProperty arabicFont = pair.FindPropertyRelative("arabicFont");

                    // Each category gets one row: foldout on the left, Override toggle pinned to the right.
                    Rect row = EditorGUILayout.GetControlRect();
                    float toggleWidth = 70f;
                    Rect foldoutRect = new Rect(row.x, row.y, row.width - toggleWidth, row.height);
                    Rect toggleRect = new Rect(row.xMax - toggleWidth, row.y, toggleWidth, row.height);

                    expanded[i] = EditorGUI.Foldout(foldoutRect, expanded[i], Categories[i].label);
                    overrideDefault.boolValue = EditorGUI.ToggleLeft(toggleRect, "Override", overrideDefault.boolValue);

                    if (expanded[i] && overrideDefault.boolValue)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(englishFont, new GUIContent("English Font"));
                        EditorGUILayout.PropertyField(arabicFont, new GUIContent("Arabic Font"));
                        EditorGUI.indentLevel--;
                    }
                }
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
