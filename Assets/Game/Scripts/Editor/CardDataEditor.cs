using Game.Scripts.Definitions;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor
{
    [CustomEditor(typeof(CardData))]
    public class CardDataEditor : UnityEditor.Editor
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

            // ── Identity ─────────────────────────────────────────────────────────
            EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);

            SerializedProperty assetNameProp = serializedObject.FindProperty("assetName");
            EditorGUILayout.PropertyField(assetNameProp, new GUIContent(
                "Asset Name (ID)",
                "Author-facing identifier. Convention: <Scene>_<Speaker>_<Slug>, e.g. Market_Advisor_WarnsBlight. " +
                "Drives the graph node title and auto-renames the asset file on save. NOT shown to players."));

            SerializedProperty displayNameProp = serializedObject.FindProperty("displayName");
            EditorGUILayout.PropertyField(displayNameProp, new GUIContent(
                "Display Name (UI)",
                "Optional player-facing label. Most cards leave this empty. " +
                "Falls back to Asset Name when blank."));

            EditorGUILayout.Space(8);

            // ── Speaker & Content ─────────────────────────────────────────────────
            EditorGUILayout.LabelField("Speaker & Content", EditorStyles.boldLabel);

            SerializedProperty speakerProp = serializedObject.FindProperty("speaker");
            EditorGUILayout.PropertyField(speakerProp, new GUIContent("Speaker", "Character speaking this card. Required for narrative run execution."));

            if (speakerProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Every card must have a speaker assigned — the run will fail to start without one.", MessageType.Warning);
            }

            EditorGUI.BeginDisabledGroup(card.isLlmReactionCard);
            card.description = EditorGUILayout.TextArea(card.description, GUILayout.MinHeight(50));
            EditorGUI.EndDisabledGroup();

            card.dayAdvance = Mathf.Max(0, EditorGUILayout.IntField("Day Advance", card.dayAdvance));

            EditorGUILayout.Space(8);

            // ── LLM Reaction Card ─────────────────────────────────────────────────
            card.isLlmReactionCard = EditorGUILayout.Toggle(
                new GUIContent("Is LLM Reaction Card", "Generates description text dynamically at runtime via LLM."),
                card.isLlmReactionCard);

            if (card.isLlmReactionCard)
            {
                card.description = string.Empty;
                EditorGUILayout.HelpBox(
                    "Description text is generated dynamically at runtime from the seed prompt below.",
                    MessageType.Info);

                card.llmPromptSeed = EditorGUILayout.TextArea(card.llmPromptSeed, GUILayout.MinHeight(50));

                if (card.speaker != null && string.IsNullOrWhiteSpace(card.speaker.llmPersonaPrompt))
                {
                    EditorGUILayout.HelpBox($"Speaker '{card.speaker.DisplayName}' has no persona prompt authored.", MessageType.Warning);
                }

                EditorGUILayout.Space(6);
                SerializedProperty continueNextCardProp = serializedObject.FindProperty("continueNextCard");
                EditorGUILayout.PropertyField(continueNextCardProp, new GUIContent("Continue Next Card"));
            }
            else if (card.IsEnding)
            {
                EditorGUILayout.HelpBox("This is an Ending card (no outgoing next card links).", MessageType.Info);
            }
            else
            {
                // ── Left Choice ───────────────────────────────────────────────────
                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("Left Choice", EditorStyles.boldLabel);
                card.leftChoiceText = EditorGUILayout.TextField("Left Choice Text", card.leftChoiceText);

                SerializedProperty leftChangeProp = serializedObject.FindProperty("leftResourceChange");
                if (leftChangeProp != null)
                {
                    EditorGUILayout.PropertyField(leftChangeProp, true);
                }

                SerializedProperty leftNextCardProp = serializedObject.FindProperty("leftNextCard");
                EditorGUILayout.PropertyField(leftNextCardProp, new GUIContent("Left Next Card"));

                // ── Right Choice ──────────────────────────────────────────────────
                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("Right Choice", EditorStyles.boldLabel);
                card.rightChoiceText = EditorGUILayout.TextField("Right Choice Text", card.rightChoiceText);

                SerializedProperty rightChangeProp = serializedObject.FindProperty("rightResourceChange");
                if (rightChangeProp != null)
                {
                    EditorGUILayout.PropertyField(rightChangeProp, true);
                }

                SerializedProperty rightNextCardProp = serializedObject.FindProperty("rightNextCard");
                EditorGUILayout.PropertyField(rightNextCardProp, new GUIContent("Right Next Card"));
            }

            if (GUI.changed)
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(card);
                CardGraphWindow.RefreshOpenWindows();
            }
        }
    }
}