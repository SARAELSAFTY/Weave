using Game.Scripts.Definitions;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor
{
    [CustomEditor(typeof(CardData))]
    public class CardDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            CardData card = (CardData)target;
            if (card == null) return;

            serializedObject.Update();

            SerializedProperty descriptionLocalizedProp = serializedObject.FindProperty("descriptionLocalized");
            SerializedProperty dayAdvanceProp = serializedObject.FindProperty("dayAdvance");
            SerializedProperty leftChoiceLocalizedProp = serializedObject.FindProperty("leftChoiceLocalized");
            SerializedProperty rightChoiceLocalizedProp = serializedObject.FindProperty("rightChoiceLocalized");
            SerializedProperty isLlmProp = serializedObject.FindProperty("isLlmReactionCard");
            SerializedProperty isPetitionProp = serializedObject.FindProperty("isPetitionCard");

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
            SerializedProperty petitionerSourceProp = serializedObject.FindProperty("petitionerSource");
            bool usesGeneratedCommoner = isPetitionProp.boolValue && petitionerSourceProp != null &&
                                         petitionerSourceProp.enumValueIndex == (int)PetitionerSource.GeneratedCommoner;

            // Show Speaker at the top for non-petition cards only.
            if (!isPetitionProp.boolValue)
            {
                EditorGUILayout.PropertyField(speakerProp, new GUIContent("Speaker", "Character speaking this card. Required for narrative run execution."));
                if (speakerProp.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("Every card must have a speaker assigned; the run will fail to start without one.", MessageType.Warning);
                }
            }

            if (!isLlmProp.boolValue && !isPetitionProp.boolValue)
            {
                LocalizedTextGui.DrawTextAreas(descriptionLocalizedProp, "Card Description (Story Text)");
            }

            dayAdvanceProp.intValue = Mathf.Max(0, EditorGUILayout.IntField("Day Advance", dayAdvanceProp.intValue));

            EditorGUILayout.Space(8);

            EditorGUI.BeginChangeCheck();
            bool newIsLlm = EditorGUILayout.Toggle(
                new GUIContent("Is LLM Reaction Card", "Generates description text dynamically at runtime via LLM."),
                isLlmProp.boolValue);
            if (EditorGUI.EndChangeCheck())
            {
                isLlmProp.boolValue = newIsLlm;
                if (newIsLlm)
                {
                    isPetitionProp.boolValue = false;
                    LocalizedTextGui.Clear(descriptionLocalizedProp);
                }
            }

            EditorGUI.BeginChangeCheck();
            bool newIsPetition = EditorGUILayout.Toggle(
                new GUIContent("Is Petition Card", "Prompts player for free-form command input resolved dynamically by LLM."),
                isPetitionProp.boolValue);
            if (EditorGUI.EndChangeCheck())
            {
                isPetitionProp.boolValue = newIsPetition;
                if (newIsPetition)
                {
                    isLlmProp.boolValue = false;
                    LocalizedTextGui.Clear(descriptionLocalizedProp);
                }
            }

            if (isLlmProp.boolValue)
            {
                LocalizedTextGui.Clear(descriptionLocalizedProp);

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

                SpeakerData speaker = speakerProp.objectReferenceValue as SpeakerData;
                if (speaker != null && string.IsNullOrWhiteSpace(speaker.llmPersonaPrompt))
                {
                    EditorGUILayout.HelpBox($"Speaker '{speaker.DisplayName}' has no persona prompt authored.", MessageType.Warning);
                }

                EditorGUILayout.Space(6);
                SerializedProperty continueNextCardProp = serializedObject.FindProperty("continueNextCard");
                EditorGUILayout.PropertyField(continueNextCardProp, new GUIContent("Continue Next Card"));
            }
            else if (isPetitionProp.boolValue)
            {
                EditorGUILayout.PropertyField(petitionerSourceProp, new GUIContent(
                    "Petitioner Source",
                    "Generated Commoner invents a temporary common subject (new name, trade, problem) each audience " +
                    "and discards it afterwards. Defined Speaker uses the speaker assigned below and lets the AI " +
                    "craft a problem suited to that character."));

                if (usesGeneratedCommoner)
                {
                    EditorGUILayout.HelpBox("A temporary common subject will be generated for each audience; no speaker asset is needed.", MessageType.Info);
                }
                else
                {
                    // Defined Speaker — show the Speaker field here, in context.
                    EditorGUILayout.PropertyField(speakerProp, new GUIContent("Speaker", "The noble or character bringing this petition."));
                    SpeakerData definedSpeaker = speakerProp.objectReferenceValue as SpeakerData;
                    if (definedSpeaker == null)
                    {
                        EditorGUILayout.HelpBox("Assign a speaker asset for Defined Speaker mode.", MessageType.Warning);
                    }
                    else if (string.IsNullOrWhiteSpace(definedSpeaker.llmPersonaPrompt))
                    {
                        EditorGUILayout.HelpBox($"Speaker '{definedSpeaker.DisplayName}' has no persona prompt authored.", MessageType.Warning);
                    }
                }

                SerializedProperty petitionSeedOverrideProp = serializedObject.FindProperty("petitionSeedOverride");
                EditorGUILayout.PropertyField(petitionSeedOverrideProp, new GUIContent(
                    "Petition Seed Override",
                    "Leave empty to use the global default from LlmPromptTemplates.defaultPetitionSeedPrompt."));

                bool usingOverride = !string.IsNullOrWhiteSpace(petitionSeedOverrideProp.stringValue);
                EditorGUILayout.HelpBox(
                    (usingOverride
                        ? "This card uses its own Petition Seed Override above"
                        : "This card uses the global petition seed from LlmPromptTemplates.defaultPetitionSeedPrompt")
                    + " for both the opening announcement and every turn. If a turn request fails, the player can retry.",
                    MessageType.Info);

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
                LocalizedTextGui.Draw(leftChoiceLocalizedProp, "Left Choice");

                SerializedProperty leftChangeProp = serializedObject.FindProperty("leftResourceChange");
                if (leftChangeProp != null)
                {
                    EditorGUILayout.PropertyField(leftChangeProp, true);
                }

                SerializedProperty leftNextCardProp = serializedObject.FindProperty("leftNextCard");
                EditorGUILayout.PropertyField(leftNextCardProp, new GUIContent("Left Next Card"));

                EditorGUILayout.Space(6);
                LocalizedTextGui.Draw(rightChoiceLocalizedProp, "Right Choice");

                SerializedProperty rightChangeProp = serializedObject.FindProperty("rightResourceChange");
                if (rightChangeProp != null)
                {
                    EditorGUILayout.PropertyField(rightChangeProp, true);
                }

                SerializedProperty rightNextCardProp = serializedObject.FindProperty("rightNextCard");
                EditorGUILayout.PropertyField(rightNextCardProp, new GUIContent("Right Next Card"));

                if (card.HasBrokenBranch)
                {
                    EditorGUILayout.HelpBox(
                        "One branch has no next card. The run fails the moment the player swipes that way - " +
                        "link a card or leave both sides empty to make this an ending.",
                        MessageType.Error);
                }
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
