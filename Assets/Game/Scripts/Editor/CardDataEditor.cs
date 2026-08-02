using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CardData))]
public class CardDataEditor : Editor
{
    private CardData card;

    private void OnEnable()
    {
        card = (CardData)target;
    }

    public override void OnInspectorGUI()
    {
        if (card == null) return;

        serializedObject.Update();

        // Header / Identity
        EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);

        // Card ID is read-only here — rename the asset file in the Project window to change it.
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.TextField(new GUIContent("Card Id", "Rename the asset file in the Project window to change this"), card.cardId);
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space(6);

        // Content
        EditorGUILayout.LabelField("Card Content", EditorStyles.boldLabel);
        card.description = EditorGUILayout.TextArea(card.description, GUILayout.MinHeight(50));
        card.speakerId = EditorGUILayout.TextField("Speaker Id", card.speakerId);
        card.dayAdvance = Mathf.Max(0, EditorGUILayout.IntField("Day Advance", card.dayAdvance));

        EditorGUILayout.Space(6);

        bool ending = card.isEnding;

        if (ending)
        {
            // An ending card has no swipe - CardView.ShowEnding blanks both choice labels at
            // runtime, so Left/Right Choice text and resource changes here would never actually
            // be shown or applied. Hiding them avoids authoring dead data.
            EditorGUILayout.HelpBox(
                "This is an Ending card (no Left/Right links). Left and Right Choice fields are " +
                "hidden because an ending card has no swipe - they're never shown or applied. " +
                "Wire a next card in the Card Graph window to turn this back into a choice card.",
                MessageType.Info);
        }
        else
        {
            // Left Choice
            EditorGUILayout.LabelField("Left Choice", EditorStyles.boldLabel);
            card.leftChoiceText = EditorGUILayout.TextField("Left Choice Text", card.leftChoiceText);

            SerializedProperty leftChangeProp = serializedObject.FindProperty("leftResourceChange");
            if (leftChangeProp != null)
            {
                EditorGUILayout.PropertyField(leftChangeProp, true);
            }

            // Left Next Card ID is READ-ONLY (wired in Card Graph)
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField(new GUIContent("Left Next Card Id", "Set by wiring edges in the Card Graph window"), card.leftNextCardId);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(6);

            // Right Choice
            EditorGUILayout.LabelField("Right Choice", EditorStyles.boldLabel);
            card.rightChoiceText = EditorGUILayout.TextField("Right Choice Text", card.rightChoiceText);

            SerializedProperty rightChangeProp = serializedObject.FindProperty("rightResourceChange");
            if (rightChangeProp != null)
            {
                EditorGUILayout.PropertyField(rightChangeProp, true);
            }

            // Right Next Card ID is READ-ONLY (wired in Card Graph)
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField(new GUIContent("Right Next Card Id", "Set by wiring edges in the Card Graph window"), card.rightNextCardId);
            EditorGUI.EndDisabledGroup();
        }

        EditorGUILayout.Space(6);

        // Ending (Automatic status display)
        EditorGUILayout.LabelField("Ending Status", EditorStyles.boldLabel);
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.Toggle(new GUIContent("Is Ending Card", "Automatically true when both Left and Right Next Card IDs are empty"), ending);
        EditorGUI.EndDisabledGroup();

        if (GUI.changed)
        {
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(card);
            CardGraphWindow.RefreshOpenWindows();
        }
    }
}