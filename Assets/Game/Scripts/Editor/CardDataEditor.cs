using System.Collections.Generic;
using Game.Scripts.Definitions;
using Game.Scripts.Runtime.Narrative;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor
{
    [CustomEditor(typeof(CardData))]
    public class CardDataEditor : UnityEditor.Editor
    {
        private CardData card;
        private NarrativeDatabase database;

        private void OnEnable()
        {
            card = (CardData)target;
            database = CardGraphEditor.FindOwningDatabase(card);
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

            EditorGUI.BeginDisabledGroup(card.isLlmReactionCard);
            card.description = EditorGUILayout.TextArea(card.description, GUILayout.MinHeight(50));
            EditorGUI.EndDisabledGroup();

            if (database == null)
            {
                card.speakerId = EditorGUILayout.TextField("Speaker Id", card.speakerId);
            }
            else
            {
                List<string> speakerIds = CardGraphEditor.GetSpeakerChoices(database);

                int selectedIndex = 0;
                if (!string.IsNullOrEmpty(card.speakerId))
                {
                    int matchIndex = speakerIds.IndexOf(card.speakerId);
                    if (matchIndex >= 0)
                    {
                        selectedIndex = matchIndex;
                    }
                }

                int newIndex = EditorGUILayout.Popup("Speaker Id", selectedIndex, speakerIds.ToArray());
                if (newIndex != selectedIndex)
                {
                    card.speakerId = newIndex == 0 ? string.Empty : speakerIds[newIndex];
                }
            }

            if (string.IsNullOrEmpty(card.speakerId))
            {
                EditorGUILayout.HelpBox("Every card must have a speaker assigned - the run will fail to start without one.", MessageType.Warning);
            }

            card.dayAdvance = Mathf.Max(0, EditorGUILayout.IntField("Day Advance", card.dayAdvance));

            EditorGUILayout.Space(6);

            card.isLlmReactionCard = EditorGUILayout.Toggle(
                new GUIContent("Is LLM Reaction Card", "Generates card description at runtime via LLM. Swiping either direction advances to the same next card with no resource changes."),
                card.isLlmReactionCard);

            EditorGUILayout.Space(6);

            bool ending = card.IsEnding;

            if (card.isLlmReactionCard)
            {
                card.description = string.Empty;
                EditorGUILayout.HelpBox(
                    "This card's description is LLM-generated at runtime from the prompt seed below. " +
                    "Authoring text in 'Card Content' is disabled for LLM cards.",
                    MessageType.Info);

                card.llmPromptSeed = EditorGUILayout.TextArea(card.llmPromptSeed, GUILayout.MinHeight(50));

                CouncilMemberData assignedSpeaker = FindSpeakerById(database, card.speakerId);
                if (assignedSpeaker != null && !CardGraph.IsLlmSpeaker(database, assignedSpeaker))
                {
                    EditorGUILayout.HelpBox($"Speaker '{card.speakerId}' is not marked as an AI Speaker, so this reaction will have no persona.", MessageType.Warning);
                }
                
                // Also disabled resource changes for LLM cards
                card.leftResourceChange = new ResourceChange();
                card.rightResourceChange = new ResourceChange();

                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField(
                    new GUIContent("Continue Next Card Id", "Set by wiring the Continue port in the Card Graph window"),
                    card.continueNextCardId);
                EditorGUI.EndDisabledGroup();
            }
            else if (ending)
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
            EditorGUILayout.Toggle(new GUIContent("Is Ending Card", card.isLlmReactionCard
                ? "Automatically true when Continue Next Card Id is empty"
                : "Automatically true when both Left and Right Next Card IDs are empty"), ending);
            EditorGUI.EndDisabledGroup();

            if (GUI.changed)
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(card);
                CardGraphWindow.RefreshOpenWindows();
            }
        }


        private static CouncilMemberData FindSpeakerById(NarrativeDatabase database, string speakerId)
        {
            if (database == null || database.speakers == null || string.IsNullOrEmpty(speakerId))
            {
                return null;
            }

            return database.speakers.Find(s => s != null && s.memberId == speakerId);
        }
    }
}