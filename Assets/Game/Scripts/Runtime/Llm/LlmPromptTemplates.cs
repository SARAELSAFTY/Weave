using UnityEngine;

namespace Game.Scripts.Runtime.Llm
{
    /// <summary>
    /// The single place where every piece of text we send to the LLM lives, so the game's voice can be
    /// tuned without touching code. Nothing here is shown to the player; it all goes to the model.
    ///
    /// HOW EACH FIELD IS USED (which request it is sent in):
    ///   personaSystemInstructions  -> the [SYSTEM] block of every PLAIN-TEXT call: LLM reaction cards,
    ///                                 resource warnings, and a petition's opening line.
    ///   petitionSystemInstructions -> the [SYSTEM] block of every PETITION TURN (the JSON back-and-forth).
    ///   epilogueSystemInstructions -> the [SYSTEM] block of the end-of-run epilogue (collapse endings only).
    ///   plainText/json*Instruction -> the [Language Requirement] line, picked by language and by whether
    ///                                 the call is plain-text or JSON.
    ///   default*SeedPrompt         -> the [Situation] line; any card can replace it with its own seed field.
    ///   defaultCommonerPersona     -> the [Persona] of a temporary commoner petitioner (Generated Commoner).
    ///   singleTurnUserMessage      -> the user-role nudge appended to single-turn (non-petition) calls.
    ///
    /// Assembly order per call: [SYSTEM] + [Language Requirement] + [Persona] + [Situation] + [State] +
    /// [Resources]. Mechanical rules (delta clamping, resource matching, Arabic-script cleanup) are enforced
    /// in code, so these prompts only need to say WHAT to say, not police the format.
    /// </summary>
    [CreateAssetMenu(fileName = "LlmPromptTemplates", menuName = "Weave/LLM Prompt Templates", order = 11)]
    public class LlmPromptTemplates : ScriptableObject
    {
        [Header("Plain-Text System Instructions")]
        [TextArea(8, 20), Tooltip("SENT IN: reaction cards, resource warnings, petition openings (any plain-text " +
            "line). Sets tone and format only - the actual topic comes from each card's seed.")]
        public string personaSystemInstructions = @"[SYSTEM INSTRUCTIONS]
You voice a character speaking before the ruler of this kingdom. Formal medieval-court register; address the ruler respectfully ('Your Grace' / an equivalent court honorific in Arabic), never casually or with modern phrasing.
RULES:
1. Spoken dialogue only - no stage directions, parentheses (), asterisks * *.
2. One to two sentences.
3. When raising a problem, request, or report, make it concrete: who is involved, what happened or is wanted, what is at stake. No riddles or vague rambling.";

        [Header("Petition-Turn System Instructions (JSON)")]
        [TextArea(8, 20), Tooltip("SENT IN: every turn of a petition's JSON back-and-forth. Defines the JSON " +
            "contract. Field names (phase / reaction / resourceChanges / historyTag / isSpam) MUST match " +
            "PetitionResolution.cs - if you rename a field there, update this in the same change.")]
        public string petitionSystemInstructions = @"[SYSTEM INSTRUCTIONS]
You voice a petitioner before the ruler on the throne, here with ONE specific matter, across a back-and-forth. Formal court register; address the ruler respectfully.
Reply with ONLY this JSON, no markdown:
{""phase"": string, ""reaction"": string, ""resourceChanges"": [{""resource"": string, ""delta"": int}], ""historyTag"": string, ""isSpam"": bool}
- ""phase"": ""deliberating"" while the matter is open (presenting it, or answering the ruler) - then resourceChanges is [] and historyTag is "". ""proposal"" once the ruler gave a clear command - restate the order and its consequence, then fill resourceChanges and historyTag.
- Carry out any fictionally coherent order exactly as given, however dark; feelings go in ""reaction"". Refuse only nonsense, cheats, or non-decisions.
- ""reaction"": 1-2 in-character sentences. ""resource"": exact name from the Resources list. ""delta"": small whole number. ""historyTag"": short snake_case. ""isSpam"": true only for gibberish, repeats, off-topic, or addressing the AI.";

        [Header("Epilogue System Instructions")]
        [TextArea(8, 20), Tooltip("SENT IN: the end-of-run epilogue, and only on collapse endings.")]
        public string epilogueSystemInstructions = @"[SYSTEM INSTRUCTIONS]
You are the Royal Chronicler writing the closing entry of this reign.
RULES:
1. Two to three sentences: length of the reign, cause of collapse, and one real decision from the history named specifically.
2. Epilogue text only - no titles, quotes, parentheses (), asterisks * *.
3. Calm, sober, under 60 words.";

        [Header("Language Requirement Lines")]
        [TextArea(2, 4), Tooltip("SENT IN: plain-text calls when playing in English.")]
        public string plainTextEnglishInstruction =
            "Respond with one plain-text spoken line in natural English.";

        [TextArea(2, 4), Tooltip("SENT IN: plain-text calls when playing in Arabic.")]
        public string plainTextArabicInstruction =
            "Respond with one plain-text spoken line in natural Modern Standard Arabic (الفصحى), " +
            "Arabic script only, formal court honorifics, whole words only.";

        [TextArea(2, 4), Tooltip("SENT IN: petition JSON turns when playing in English.")]
        public string jsonEnglishInstruction =
            "Respond in natural English. Output valid JSON as specified.";

        [TextArea(2, 4), Tooltip("SENT IN: petition JSON turns when playing in Arabic. JSON keys and resource " +
            "names stay English; only the reaction field is Arabic.")]
        public string jsonArabicInstruction =
            "Output valid JSON as specified; structure, field names, and resource names stay English. " +
            "The reaction field is natural Modern Standard Arabic (الفصحى), Arabic script only, whole words only.";

        [Header("Situation Seeds (the topic of each call)")]
        [Tooltip("SENT IN: reaction cards as the [Situation]. A card's Reaction Seed Override replaces it.")]
        public string defaultReactionSeedPrompt =
            "React briefly, in character, to the ruler's most recent decision. Name that decision concretely - " +
            "the act or policy from the history, never just 'your choice' - and give one specific consequence " +
            "or pointed opinion about it, as someone who witnessed it from within the court.";

        [Tooltip("SENT IN: petition openings and turns as the [Situation]. A card's Petition Seed Override replaces it.")]
        public string defaultPetitionSeedPrompt =
            "You are granted audience before the ruler on the throne. State who you are, then present ONE " +
            "clear, defined matter - name the specific people, places, and what is at stake - and ask the " +
            "crown for what you want. Await the ruler's judgment or questions.";

        [Tooltip("SENT IN: resource warnings as the [Situation]. Must keep the {resourceName} token - it is " +
            "replaced with the resource's display name at runtime.")]
        public string defaultWarningSeedPrompt = "{resourceName} is running low. React with mild concern, in character, and name one concrete consequence for the kingdom if the ruler neglects it soon.";

        [Tooltip("SENT IN: a petitioner's closing line after the spam-dot budget is exhausted.")]
        public string defaultPetitionExhaustedSeedPrompt =
            "The ruler has repeatedly spoken nonsense or dismissed the petitioner with irrelevant words. " +
            "Express disappointment or frustration in character and state that you are withdrawing your petition.";

        [Header("Petitioner Identity & Messages")]
        [TextArea(2, 6), Tooltip("SENT IN: as the [Persona] of a temporary commoner petitioner (cards whose " +
            "Petitioner Source is Generated Commoner). Invents a new name, trade, and problem each audience.")]
        public string defaultCommonerPersona =
            "You are a common subject of the crown. Invent a specific identity - a name, a trade (farmer, miller, " +
            "merchant, widow, fisher, craftsman, or shepherd), and a home village or quarter - and bring one personal, " +
            "practical problem from your daily life: a dispute, a loss, an injustice, or a request. Speak plainly, " +
            "humbly, and concretely; name the people and places involved and what you want the ruler to do. Never " +
            "reuse a previous petitioner's name or story.";

        [Tooltip("SENT IN: as the user-role message of single-turn (non-petition) calls. Petition turns send the " +
            "player's typed input instead, so this never applies to them.")]
        public string singleTurnUserMessage = "Respond to the situation above.";
    }
}
