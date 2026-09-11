using Game.Scripts.Definitions;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor
{
    /// <summary>
    /// Custom inspector for <see cref="CardData"/> that conditionally shows fields based on card mode (standard, LLM reaction, or petition) and syncs changes back to open graph windows.
    /// </summary>
    [CustomEditor(typeof(CardData))]
    public class CardDataEditor : UnityEditor.Editor
    {
        /// <summary>
        /// Draws the card inspector with mode-dependent field visibility: speaker is hidden for petition/chat cards using generated commoners;
        /// description/choices are hidden for LLM, petition, and chat cards; LLM, petition, and chat toggles are mutually exclusive.
        /// </summary>
        public override void OnInspectorGUI()
        {
            CardData card = (CardData)target;
            if (card == null)
            {
                return;
            }

            serializedObject.Update();

            SerializedProperty descriptionLocalizedProp = serializedObject.FindProperty("descriptionLocalized");
            SerializedProperty dayAdvanceProp = serializedObject.FindProperty("dayAdvance");
            SerializedProperty leftChoiceLocalizedProp = serializedObject.FindProperty("leftChoiceLocalized");
            SerializedProperty rightChoiceLocalizedProp = serializedObject.FindProperty("rightChoiceLocalized");
            SerializedProperty isLlmProp = serializedObject.FindProperty("isLlmReactionCard");
            SerializedProperty isPetitionProp = serializedObject.FindProperty("isPetitionCard");
            SerializedProperty isChatProp = serializedObject.FindProperty("isChatCard");

            SerializedProperty assetNameProp = serializedObject.FindProperty("assetName");
            EditorGUILayout.PropertyField(assetNameProp, new GUIContent(
                "Asset Name (ID)"));

            EditorGUILayout.Space(8);

            SerializedProperty speakerProp = serializedObject.FindProperty("speaker");
            SerializedProperty petitionerSourceProp = serializedObject.FindProperty("petitionerSource");
            bool usesGeneratedPersona = (isPetitionProp.boolValue || isChatProp.boolValue) && petitionerSourceProp != null &&
                                        petitionerSourceProp.enumValueIndex == (int)PetitionerSource.GeneratedCommoner;

            // Speaker selector is only relevant for non-audience cards; petition/chat cards use petitionerSource instead.
            if (!isPetitionProp.boolValue && !isChatProp.boolValue)
            {
                EditorGUILayout.PropertyField(speakerProp, new GUIContent("Speaker"));
            }

            EditorGUILayout.Space(6);
            SerializedProperty artModeProp = serializedObject.FindProperty("artMode");
            EditorGUILayout.PropertyField(artModeProp, new GUIContent(
                "Art Mode"));
            if (artModeProp.enumValueIndex == (int)CardArtMode.EventImage)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cardImage"), new GUIContent(
                    "Event Image"));
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("visualTemplate"), new GUIContent(
                "Visual Template"));

            // Description text areas are only relevant for standard narrative cards; LLM/petition/chat cards generate text at runtime.
            if (!isLlmProp.boolValue && !isPetitionProp.boolValue && !isChatProp.boolValue)
            {
                LocalizedTextGui.DrawTextAreas(descriptionLocalizedProp, "Card Description (Story Text)");
            }

            dayAdvanceProp.intValue = Mathf.Max(0, EditorGUILayout.IntField("Day Advance", dayAdvanceProp.intValue));

            EditorGUILayout.Space(8);

            DrawExclusiveModeToggle(isLlmProp, "Is LLM Reaction Card", descriptionLocalizedProp, isPetitionProp, isChatProp);
            DrawExclusiveModeToggle(isPetitionProp, "Is Petition Card", descriptionLocalizedProp, isLlmProp, isChatProp);
            DrawExclusiveModeToggle(isChatProp, "Is Chat Card", descriptionLocalizedProp, isLlmProp, isPetitionProp);

            if (isLlmProp.boolValue)
            {
                SerializedProperty reactionSeedOverrideProp = serializedObject.FindProperty("reactionSeedOverride");
                EditorGUILayout.PropertyField(reactionSeedOverrideProp, new GUIContent(
                    "Reaction Seed Override"));

                EditorGUILayout.Space(6);
                SerializedProperty continueNextCardProp = serializedObject.FindProperty("continueNextCard");
                EditorGUILayout.PropertyField(continueNextCardProp, new GUIContent("Continue Next Card"));
            }
            else if (isPetitionProp.boolValue)
            {
                DrawAudienceFields(petitionerSourceProp, speakerProp, usesGeneratedPersona,
                    serializedObject.FindProperty("petitionSeedOverride"), "Petition Seed Override");
            }
            else if (isChatProp.boolValue)
            {
                DrawAudienceFields(petitionerSourceProp, speakerProp, usesGeneratedPersona,
                    serializedObject.FindProperty("chatSeedOverride"), "Chat Seed Override");
            }
            else if (!card.IsEnding)
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

                EditorGUILayout.Space(8);
                SerializedProperty threeWayProp = serializedObject.FindProperty("isThreeWayVerdict");
                EditorGUILayout.PropertyField(threeWayProp, new GUIContent("Three-Way Verdict"));
                if (threeWayProp.boolValue)
                {
                    LocalizedTextGui.Draw(serializedObject.FindProperty("middleChoiceLocalized"), "Middle Choice");
                    SerializedProperty middleChangeProp = serializedObject.FindProperty("middleResourceChange");
                    if (middleChangeProp != null)
                    {
                        EditorGUILayout.PropertyField(middleChangeProp, true);
                    }

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("middleNextCard"), new GUIContent("Middle Next Card"));
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty("isEndingEvaluator"), new GUIContent("Ending Evaluator"));
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Story Flags & Gates", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("leftSetFlags"), new GUIContent("Left Set Flags"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("rightSetFlags"), new GUIContent("Right Set Flags"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("middleSetFlags"), new GUIContent("Middle Set Flags"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("continueSetFlags"), new GUIContent("Continue Set Flags"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("leftRequiresFlags"), new GUIContent("Left Requires Flags"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("rightRequiresFlags"), new GUIContent("Right Requires Flags"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("middleRequiresFlags"), new GUIContent("Middle Requires Flags"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("alternateDescriptionIfFlags"), new GUIContent("Alternate If Flags"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("alternateDescriptionIfMissing"), new GUIContent("Alternate If Missing"));
            LocalizedTextGui.DrawTextAreas(serializedObject.FindProperty("alternateDescriptionLocalized"), "Alternate Description");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("entryRequiresFlags"), new GUIContent("Entry Requires Flags"));
            SerializedProperty gateProp = serializedObject.FindProperty("hasResourceGate");
            EditorGUILayout.PropertyField(gateProp, new GUIContent("Has Resource Gate"));
            if (gateProp.boolValue)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("gateResource"), new GUIContent("Gate Resource"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("gateMinInclusive"), new GUIContent("Gate Min"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("gateMaxInclusive"), new GUIContent("Gate Max"));
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("skipToCard"), new GUIContent("Skip To Card"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("skipToAltCard"), new GUIContent("Skip To Alt Card"));

            if (GUI.changed)
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(card);
                CardGraphWindow.RefreshOpenWindows();
            }
        }

        // Draws one card-kind toggle; LLM, petition, and chat modes are mutually exclusive, so enabling one
        // clears the others and the hand-authored description those modes generate at runtime.
        private void DrawExclusiveModeToggle(SerializedProperty toggleProp, string label, SerializedProperty descriptionLocalizedProp, params SerializedProperty[] otherModeProps)
        {
            EditorGUI.BeginChangeCheck();
            bool newValue = EditorGUILayout.Toggle(new GUIContent(label), toggleProp.boolValue);
            if (EditorGUI.EndChangeCheck())
            {
                toggleProp.boolValue = newValue;
                if (newValue)
                {
                    foreach (SerializedProperty other in otherModeProps)
                    {
                        other.boolValue = false;
                    }

                    LocalizedTextGui.Clear(descriptionLocalizedProp);
                }
            }
        }

        // Draws the shared audience configuration (petition/chat): persona source, optional speaker, seed override, and continue exit.
        private void DrawAudienceFields(SerializedProperty petitionerSourceProp, SerializedProperty speakerProp,
            bool usesGeneratedPersona, SerializedProperty seedOverrideProp, string seedOverrideLabel)
        {
            EditorGUILayout.PropertyField(petitionerSourceProp, new GUIContent(
                "Petitioner Source"));

            if (!usesGeneratedPersona)
            {
                EditorGUILayout.PropertyField(speakerProp, new GUIContent("Speaker"));
            }

            EditorGUILayout.PropertyField(seedOverrideProp, new GUIContent(seedOverrideLabel));

            EditorGUILayout.Space(6);
            SerializedProperty continueNextCardProp = serializedObject.FindProperty("continueNextCard");
            EditorGUILayout.PropertyField(continueNextCardProp, new GUIContent("Continue Next Card"));
        }
    }
}
