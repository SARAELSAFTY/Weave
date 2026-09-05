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
        /// Draws the card inspector with mode-dependent field visibility: speaker is hidden for petition cards using generated commoners;
        /// description/choices are hidden for LLM and petition cards; LLM and petition toggles are mutually exclusive.
        /// </summary>
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
                "Asset Name (ID)"));

            EditorGUILayout.Space(8);

            SerializedProperty speakerProp = serializedObject.FindProperty("speaker");
            SerializedProperty petitionerSourceProp = serializedObject.FindProperty("petitionerSource");
            bool usesGeneratedCommoner = isPetitionProp.boolValue && petitionerSourceProp != null &&
                                         petitionerSourceProp.enumValueIndex == (int)PetitionerSource.GeneratedCommoner;

            // Speaker selector is only relevant for non-petition cards; petition cards use petitionerSource instead.
            if (!isPetitionProp.boolValue)
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

            // Description text areas are only relevant for standard narrative cards; LLM/petition cards generate text at runtime.
            if (!isLlmProp.boolValue && !isPetitionProp.boolValue)
            {
                LocalizedTextGui.DrawTextAreas(descriptionLocalizedProp, "Card Description (Story Text)");
            }

            dayAdvanceProp.intValue = Mathf.Max(0, EditorGUILayout.IntField("Day Advance", dayAdvanceProp.intValue));

            EditorGUILayout.Space(8);

            EditorGUI.BeginChangeCheck();
            bool newIsLlm = EditorGUILayout.Toggle(
                new GUIContent("Is LLM Reaction Card"),
                isLlmProp.boolValue);
            if (EditorGUI.EndChangeCheck())
            {
                isLlmProp.boolValue = newIsLlm;
                if (newIsLlm)
                {
                    // LLM and petition modes are mutually exclusive; clear hand-authored description since LLM generates it.
                    isPetitionProp.boolValue = false;
                    LocalizedTextGui.Clear(descriptionLocalizedProp);
                }
            }

            EditorGUI.BeginChangeCheck();
            bool newIsPetition = EditorGUILayout.Toggle(
                new GUIContent("Is Petition Card"),
                isPetitionProp.boolValue);
            if (EditorGUI.EndChangeCheck())
            {
                isPetitionProp.boolValue = newIsPetition;
                if (newIsPetition)
                {
                    // Petition and LLM modes are mutually exclusive; clear hand-authored description since petition text is generated.
                    isLlmProp.boolValue = false;
                    LocalizedTextGui.Clear(descriptionLocalizedProp);
                }
            }

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
                EditorGUILayout.PropertyField(petitionerSourceProp, new GUIContent(
                    "Petitioner Source"));

                if (!usesGeneratedCommoner)
                {
                    EditorGUILayout.PropertyField(speakerProp, new GUIContent("Speaker"));
                }

                SerializedProperty petitionSeedOverrideProp = serializedObject.FindProperty("petitionSeedOverride");
                EditorGUILayout.PropertyField(petitionSeedOverrideProp, new GUIContent(
                    "Petition Seed Override"));

                EditorGUILayout.Space(6);
                SerializedProperty continueNextCardProp = serializedObject.FindProperty("continueNextCard");
                EditorGUILayout.PropertyField(continueNextCardProp, new GUIContent("Continue Next Card"));
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
