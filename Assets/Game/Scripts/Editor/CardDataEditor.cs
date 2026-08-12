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
                EditorGUILayout.HelpBox("Every card must have a speaker assigned; the run will fail to start without one.", MessageType.Warning);
            }

            if (!card.isLlmReactionCard && !card.isPetitionCard)
            {
                EditorGUILayout.LabelField("Card Description (Story Text)", EditorStyles.boldLabel);
                descriptionProp.stringValue = EditorGUILayout.TextArea(descriptionProp.stringValue, GUILayout.MinHeight(50));
            }

            dayAdvanceProp.intValue = Mathf.Max(0, EditorGUILayout.IntField("Day Advance", dayAdvanceProp.intValue));

            EditorGUILayout.Space(8);

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

                SerializedProperty reactionSeedOverrideProp = serializedObject.FindProperty("reactionSeedOverride");
                EditorGUILayout.PropertyField(reactionSeedOverrideProp, new GUIContent(
                    "Reaction Seed Override",
                    "Leave empty to use the global default from LlmPromptTemplates.defaultReactionSeedPrompt."));

                bool usingOverride = !string.IsNullOrWhiteSpace(reactionSeedOverrideProp.stringValue);
                EditorGUILayout.HelpBox(
                    usingOverride
                        ? "This card uses its own Reaction Seed Override above."
                        : "This card uses the global reaction seed from LlmPromptTemplates.defaultReactionSeedPrompt.",
                    MessageType.Info);

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
                SerializedProperty petitionSeedOverrideProp = serializedObject.FindProperty("petitionSeedOverride");
                EditorGUILayout.PropertyField(petitionSeedOverrideProp, new GUIContent(
                    "Petition Seed Override",
                    "Leave empty to use the global default from LlmPromptTemplates.defaultPetitionSeedPrompt."));

                bool usingOverride = !string.IsNullOrWhiteSpace(petitionSeedOverrideProp.stringValue);
                EditorGUILayout.HelpBox(
                    (usingOverride
                        ? "This card uses its own Petition Seed Override above"
                        : "This card uses the global petition seed from LlmPromptTemplates.defaultPetitionSeedPrompt")
                    + " for both the opening announcement and every turn. If the request fails or no LLM client is configured, the run silently advances to the next card.",
                    MessageType.Info);

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
