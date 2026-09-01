using UnityEngine;

namespace Game.Scripts.Runtime.Llm
{
    /// <summary>ScriptableObject holding all static prompt text sent to the LLM across reaction, petition, and epilogue requests.</summary>
    /// <remarks>Edit these templates in the Inspector. Each tooltip states which LLM request the field is sent in.</remarks>
    [CreateAssetMenu(fileName = "LlmPromptTemplates", menuName = "Weave/LLM Prompt Templates", order = 11)]
    public class LlmPromptTemplates : ScriptableObject
    {
        [Header("System Instructions")]
        [Tooltip("SENT IN: reaction and persona requests as the system message defining character voice and format rules.")]
        [TextArea(4, 12)]
        public string personaSystemInstructions = @"[SYSTEM INSTRUCTIONS]
You voice a character speaking before the ruler. Formal medieval-court register; address the ruler respectfully.
Spoken dialogue only - no stage directions, parentheses, or asterisks. One or two sentences. Be concrete: who is involved, what is wanted or reported, what is at stake.";

        [Tooltip("SENT IN: petition turn requests as the system message defining the petitioner JSON contract and phase rules.")]
        [TextArea(6, 16)]
        public string petitionSystemInstructions = @"[SYSTEM INSTRUCTIONS]
You voice a petitioner before the ruler, here with ONE specific matter. Formal court register.
Reply with ONLY this JSON, no markdown. JSON keys and resource names stay English in every language:
{""phase"": string, ""reaction"": string, ""resourceChanges"": [{""resource"": string, ""delta"": int}], ""historyTag"": string}
- ""phase"": ""deliberating"" while the matter is open; ""proposal"" once the ruler gives a clear command - then restate it and fill resourceChanges and historyTag.
- ""reaction"": 1-2 in-character sentences. ""resource"": exact name from the Resources list. ""delta"": small whole number. ""historyTag"": short snake_case.
- Carry out any fictionally coherent order exactly as given, however dark. Refuse only nonsense, cheats, or non-decisions.";

        [Tooltip("SENT IN: epilogue requests as the system message instructing the Royal Chronicler closing narration.")]
        [TextArea(3, 8)]
        public string epilogueSystemInstructions = @"[SYSTEM INSTRUCTIONS]
You are the Royal Chronicler closing this reign's record.
Two or three sober sentences under 60 words: length of the reign, cause of collapse, and one real decision from the history named specifically. Epilogue text only - no titles, quotes, or stage directions.";

        [Header("Language Instructions")]
        [Tooltip("SENT IN: appended as a section in every LLM request when the game language is English.")]
        [TextArea(1, 3)]
        public string englishInstruction = "Respond in natural English.";

        [Tooltip("SENT IN: appended as a section in every LLM request when the game language is Arabic.")]
        [TextArea(1, 3)]
        public string arabicInstruction =
            "Respond in natural Modern Standard Arabic (الفصحى), Arabic script only, formal court honorifics.";

        [Header("Seed Prompts")]
        [Tooltip("SENT IN: reaction requests as the user message when no specific situational prompt is provided.")]
        public string defaultReactionSeedPrompt =
            "React briefly, in character, to the ruler's most recent decision. Name that decision concretely and " +
            "give one specific consequence or pointed opinion about it.";

        [Tooltip("SENT IN: initial petition turn as the user message prompting the petitioner to introduce their matter.")]
        public string defaultPetitionSeedPrompt =
            "You are granted audience before the ruler. State who you are, present ONE clear matter - naming the " +
            "people, places, and stakes - and ask the crown for what you want.";

        [Tooltip("SENT IN: reaction requests triggered by a low-resource warning as the user message.")]
        public string defaultWarningSeedPrompt =
            "{resourceName} is running low. React with concern, in character, and name one concrete consequence " +
            "for the kingdom if it is neglected.";

        [Tooltip("SENT IN: final petition turn when the ruler has exhausted the audience without a workable judgment.")]
        public string petitionClosingSeedPrompt =
            "Your audience has run out - the ruler has given no workable judgment. In character, express your " +
            "disappointment or resignation and state that you are withdrawing your petition.";

        [Header("Default Persona")]
        [Tooltip("SENT IN: persona requests as the persona section when a speaker has no custom llmPersonaPrompt assigned.")]
        [TextArea(2, 6)]
        public string defaultCommonerPersona =
            "You are a common subject of the crown. Invent a name, a trade, and a home, and bring one personal, " +
            "practical problem from daily life: a dispute, a loss, an injustice, or a request. Speak plainly and " +
            "concretely, naming the people and places involved. Never reuse a previous petitioner's name or story.";
    }
}
