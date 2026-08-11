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

            SerializedProperty descriptionProp = serializedObject.FindProperty("description");
            SerializedProperty dayAdvanceProp = serializedObject.FindProperty("dayAdvance");
            SerializedProperty leftChoiceTextProp = serializedObject.FindProperty("leftChoiceText");
            SerializedProperty rightChoiceTextProp = serializedObject.FindProperty("rightChoiceText");
            SerializedProperty llmPromptSeedProp = serializedObject.FindProperty("llmPromptSeed");
            SerializedProperty petitionSeedPromptProp = serializedObject.FindProperty("petitionSeedPrompt");

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

            SerializedProperty speakerProp = serializedObject.FindProperty("speaker");
            EditorGUILayout.PropertyField(speakerProp, new GUIContent("Speaker", "Character speaking this card. Required for narrative run execution."));

            if (speakerProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Every card must have a speaker assigned — the run will fail to start without one.", MessageType.Warning);
            }

            if (!card.isLlmReactionCard && !card.isPetitionCard)
            {
                EditorGUILayout.LabelField("Card Description (Story Text)", EditorStyles.boldLabel);
                descriptionProp.stringValue = EditorGUILayout.TextArea(descriptionProp.stringValue, GUILayout.MinHeight(50));
            }

            dayAdvanceProp.intValue = Mathf.Max(0, EditorGUILayout.IntField("Day Advance", dayAdvanceProp.intValue));

            EditorGUILayout.Space(8);

            // Empty seed fields fall back to LlmPromptTemplates defaults at runtime — do not write
            // hardcoded seeds when toggling these flags.

            bool newIsLlm = EditorGUILayout.Toggle(
                new GUIContent("Is LLM Reaction Card", "Generates description text dynamically at runtime via LLM."),
                card.isLlmReactionCard);

            if (newIsLlm != card.isLlmReactionCard)
            {
                card.isLlmReactionCard = newIsLlm;
                if (newIsLlm)
                {
                    card.isPetitionCard = false;
                }
            }

            bool newIsPetition = EditorGUILayout.Toggle(
                new GUIContent("Is Petition Card", "Prompts player for free-form command input resolved dynamically by LLM."),
                card.isPetitionCard);

            if (newIsPetition != card.isPetitionCard)
            {
                card.isPetitionCard = newIsPetition;
                if (newIsPetition)
                {
                    card.isLlmReactionCard = false;
                    descriptionProp.stringValue = string.Empty;
                }
            }

            if (card.isLlmReactionCard)
            {
                descriptionProp.stringValue = string.Empty;
                EditorGUILayout.HelpBox(
                    "Description text is generated dynamically at runtime from the seed prompt below.",
                    MessageType.Info);

                EditorGUILayout.LabelField("LLM Prompt Seed", EditorStyles.boldLabel);
                if (string.IsNullOrWhiteSpace(llmPromptSeedProp.stringValue))
                {
                    EditorGUILayout.HelpBox(
                        "Empty — will use LlmPromptTemplates.defaultReactionSeedPrompt at runtime.",
                        MessageType.None);
                }
                llmPromptSeedProp.stringValue = EditorGUILayout.TextArea(llmPromptSeedProp.stringValue, GUILayout.MinHeight(50));

                if (card.speaker != null && string.IsNullOrWhiteSpace(card.speaker.llmPersonaPrompt))
                {
                    EditorGUILayout.HelpBox($"Speaker '{card.speaker.DisplayName}' has no persona prompt authored.", MessageType.Warning);
                }

                EditorGUILayout.Space(6);
                SerializedProperty continueNextCardProp = serializedObject.FindProperty("continueNextCard");
                EditorGUILayout.PropertyField(continueNextCardProp, new GUIContent("Continue Next Card"));
            }
            else if (card.isPetitionCard)
            {
                EditorGUILayout.HelpBox(
                    "Petition cards generate their opening line via AI at runtime. If the request fails or no LLM client is configured, the run silently advances to the next card.",
                    MessageType.Info);

                EditorGUILayout.LabelField("Petition Seed Prompt", EditorStyles.boldLabel);
                if (string.IsNullOrWhiteSpace(petitionSeedPromptProp.stringValue))
                {
                    EditorGUILayout.HelpBox(
                        "Empty — will use LlmPromptTemplates.defaultPetitionSeedPrompt at runtime.",
                        MessageType.None);
                }
                petitionSeedPromptProp.stringValue = EditorGUILayout.TextArea(petitionSeedPromptProp.stringValue, GUILayout.MinHeight(50));

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

                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("Left Choice", EditorStyles.boldLabel);
                leftChoiceTextProp.stringValue = EditorGUILayout.TextField("Left Choice Text", leftChoiceTextProp.stringValue);

                SerializedProperty leftChangeProp = serializedObject.FindProperty("leftResourceChange");
                if (leftChangeProp != null)
                {
                    EditorGUILayout.PropertyField(leftChangeProp, true);
                }

                SerializedProperty leftNextCardProp = serializedObject.FindProperty("leftNextCard");
                EditorGUILayout.PropertyField(leftNextCardProp, new GUIContent("Left Next Card"));


                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("Right Choice", EditorStyles.boldLabel);
                rightChoiceTextProp.stringValue = EditorGUILayout.TextField("Right Choice Text", rightChoiceTextProp.stringValue);

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