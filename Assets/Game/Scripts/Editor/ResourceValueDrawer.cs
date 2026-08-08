using System.Collections.Generic;
using Game.Scripts.Definitions;
using Game.Scripts.Runtime.Narrative;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor
{
    [CustomPropertyDrawer(typeof(ResourceValue))]
    public class ResourceValueDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty idProp = property.FindPropertyRelative("id");
            SerializedProperty valueProp = property.FindPropertyRelative("value");

            float idWidth = position.width * 0.6f;
            float valueWidth = position.width * 0.35f;
            float spacing = position.width * 0.05f;

            Rect idRect = new Rect(position.x, position.y, idWidth, position.height);
            Rect valueRect = new Rect(position.x + idWidth + spacing, position.y, valueWidth, position.height);

            CardData owningCard = property.serializedObject.targetObject as CardData;
            NarrativeDatabase database = CardGraphEditor.FindOwningDatabase(owningCard);
            if (database != null && database.resourceCatalog != null)
            {
                List<string> choices = CardGraphEditor.GetResourceCatalogChoices(database);
                List<string> displayChoices = new List<string>(choices) { "+ New Resource..." };

                int currentIndex = Mathf.Max(0, choices.IndexOf(idProp.stringValue));
                int newIndex = EditorGUI.Popup(idRect, currentIndex, displayChoices.ToArray());

                if (newIndex == displayChoices.Count - 1)
                {
                    idProp.stringValue = CreateNewResource(database.resourceCatalog);
                }
                else
                {
                    idProp.stringValue = newIndex > 0 ? choices[newIndex] : string.Empty;
                }
            }
            else
            {
                idProp.stringValue = EditorGUI.TextField(idRect, idProp.stringValue);
            }

            valueProp.intValue = EditorGUI.IntField(valueRect, valueProp.intValue);

            EditorGUI.EndProperty();
        }

        private static string CreateNewResource(ResourceCatalog catalog)
        {
            string id = CardGraphEditor.CreateResource(catalog);
            AssetDatabase.SaveAssets();
            return id;
        }

    }
}