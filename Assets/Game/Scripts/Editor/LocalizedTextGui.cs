using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor
{
    public static class LocalizedTextGui
    {
        public static void Draw(SerializedProperty localizedProp, string label)
        {
            if (localizedProp == null)
            {
                return;
            }

            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            SerializedProperty englishProp = localizedProp.FindPropertyRelative("english");
            SerializedProperty arabicProp = localizedProp.FindPropertyRelative("arabic");
            if (englishProp != null)
            {
                EditorGUILayout.PropertyField(englishProp, new GUIContent("English"));
            }

            if (arabicProp != null)
            {
                EditorGUILayout.PropertyField(arabicProp, new GUIContent("Arabic"));
            }
        }

        public static void DrawTextAreas(SerializedProperty localizedProp, string label, float minHeight = 50f)
        {
            if (localizedProp == null)
            {
                return;
            }

            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            SerializedProperty englishProp = localizedProp.FindPropertyRelative("english");
            SerializedProperty arabicProp = localizedProp.FindPropertyRelative("arabic");
            if (englishProp != null)
            {
                EditorGUILayout.LabelField("English");
                englishProp.stringValue = EditorGUILayout.TextArea(englishProp.stringValue, GUILayout.MinHeight(minHeight));
            }

            if (arabicProp != null)
            {
                EditorGUILayout.LabelField("Arabic");
                arabicProp.stringValue = EditorGUILayout.TextArea(arabicProp.stringValue, GUILayout.MinHeight(minHeight));
            }
        }

        public static void Clear(SerializedProperty localizedProp)
        {
            if (localizedProp == null)
            {
                return;
            }

            SerializedProperty englishProp = localizedProp.FindPropertyRelative("english");
            SerializedProperty arabicProp = localizedProp.FindPropertyRelative("arabic");
            if (englishProp != null)
            {
                englishProp.stringValue = string.Empty;
            }

            if (arabicProp != null)
            {
                arabicProp.stringValue = string.Empty;
            }
        }
    }
}
