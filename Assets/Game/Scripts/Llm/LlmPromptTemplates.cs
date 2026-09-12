using Game.Scripts.Localization;
using UnityEngine;

namespace Game.Scripts.Llm
{
    /// <summary>ScriptableObject holding all static prompt and fallback text sent to or shown around the LLM
    /// across reaction, petition, and epilogue requests.</summary>
    /// <remarks>Edit these templates in the Inspector. Each tooltip states where the field is used.
    /// Every player-visible narrative fallback line lives here so tone is editable without touching code;
    /// <see cref="FallbackStrings"/> is the last-resort copy used only when this asset is missing.</remarks>
    [CreateAssetMenu(fileName = "LlmPromptTemplates", menuName = "Weave/LLM Prompt Templates", order = 11)]
    public class LlmPromptTemplates : ScriptableObject
    {
        [Header("System Instructions")]
        [Tooltip("SENT IN: reaction and persona requests as the system message defining character voice and format rules.")]
        [TextArea(4, 12)]
        public string personaSystemInstructions = @"[SYSTEM INSTRUCTIONS]
You voice a character speaking before the ruler. Formal medieval-court register; address the ruler respectfully.
Spoken dialogue only - no stage directions, parentheses, or asterisks. One or two sentences. Be concrete: who is involved, what is wanted or reported, what is at stake.";

        [Tooltip("SENT IN: petition turn requests as the system message defining the petitioner JSON contract and phase rules. Keep it in sync with PetitionResolution and GroqPetitionSchema (keys, empty-array deliberation convention).")]
        [TextArea(6, 16)]
        public string petitionSystemInstructions = @"[SYSTEM INSTRUCTIONS]
You voice a petitioner before the ruler, here with ONE specific matter. Formal court register.
Reply with ONLY this JSON, no markdown. JSON keys and resource names stay English in every language:
{""phase"": string, ""reaction"": string, ""speakerName"": string, ""resourceChanges"": [{""resource"": string, ""delta"": int}], ""historyTag"": string, ""isSpam"": bool}
- ""phase"": ""deliberating"" while the matter is open; ""proposal"" once the ruler gives a clear command - then restate it and fill resourceChanges and historyTag.
- ""reaction"": your spoken reply, 1-3 in-character sentences. Always concrete: name the people, places, and stakes. Answer the ruler's latest words directly; never repeat an earlier line, never stall, and never say you are waiting for an answer.
- ""speakerName"": on the first turn only, the petitioner's name with a short role (2-5 words, e.g. ""Mara, the miller's widow""); empty string on every later turn.
- ""resource"": exact name from the Resources list. ""delta"": small whole number. ""historyTag"": short snake_case.
- ""isSpam"": true only for gibberish, repeats, off-topic, or addressing the AI; false for genuine conversation or decisions.
- While deliberating, send resourceChanges as [] and historyTag as """".
- This is a continuing conversation: your earlier lines are in the history, so advance the matter instead of restating it.
- Carry out any fictionally coherent order exactly as given, however dark. Refuse only nonsense, cheats, or non-decisions.";

        [Tooltip("SENT IN: epilogue requests as the system message instructing the closing narration. The Persona section supplies the voice; this text frames it.")]
        [TextArea(3, 8)]
        public string epilogueSystemInstructions = @"[SYSTEM INSTRUCTIONS]
Close this reign's record as the Royal Chronicler, but the record is spoken aloud by the figure in the Persona section, standing over the end of the reign. The Chronicler is only the scribe; the Persona is the voice - stay fully in that persona's manner, title, and attitude.
Two or three sentences under 60 words: how long the reign lasted, how it ended, and one real decision from the history named specifically. Epilogue text only - no titles, quotes, or stage directions.";

        [Tooltip("SENT IN: chat card turn requests as the system message defining the free-chat contract. This conversation cannot affect resources or history.")]
        [TextArea(4, 12)]
        public string chatSystemInstructions = @"[SYSTEM INSTRUCTIONS]
You are the figure in the Persona section, alone with the ruler in a private audience. Formal medieval-court register; address the ruler respectfully.
Reply with spoken dialogue only, in character: one to three sentences, no stage directions, parentheses, asterisks, or JSON.
Answer the ruler's latest words directly and concretely; never repeat an earlier line and never wait for instructions. Your earlier lines are in the conversation history, so build on them. This audience is talk only - it cannot change kingdom resources, decisions, or history.";

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
            "You are granted audience before the ruler. In one spoken turn, give your name and trade, then state " +
            "your one matter plainly: what happened, to whom, and what exactly you ask of the crown.";

        [Tooltip("SENT IN: initial chat turn as the user message prompting the speaker to open a private audience.")]
        public string defaultChatSeedPrompt =
            "The private audience begins. Open in character: greet the ruler, say who you are, and give the reason " +
            "you asked to speak with them alone.";

        [Tooltip("SENT IN: reaction requests triggered by a low-resource warning as the user message.")]
        public string defaultWarningSeedPrompt =
            "{resourceName} is running low. React with concern, in character, and name one concrete consequence " +
            "for the kingdom if it is neglected.";

        [Tooltip("SENT IN: final petition turn when the ruler has exhausted the audience without a workable judgment.")]
        public string petitionClosingSeedPrompt =
            "Your audience has run out - the ruler has given no workable judgment. In character, express your " +
            "disappointment or resignation and state that you are withdrawing your petition.";

        [Tooltip("SENT IN: every single-turn reaction request as the user message after the system prompt.")]
        public string singleTurnUserMessage = "Respond to the situation above.";

        [Header("Default Persona")]
        [Tooltip("SENT IN: persona requests as the persona section when a speaker has no custom llmPersonaPrompt assigned.")]
        [TextArea(2, 6)]
        public string defaultCommonerPersona =
            "You are a common subject of the crown. Invent a name, a trade, and a home, and bring one personal, " +
            "practical problem from daily life: a dispute, a loss, an injustice, or a request. Speak plainly and " +
            "concretely, naming the people and places involved. Never reuse a previous petitioner's name or story.";

        [Header("Button Labels (set on the shared petition confirm button)")]
        [Tooltip("SHOWN: label on the petition confirm button while a proposal awaits the ruler's confirmation.")]
        public LocalizedText petitionConfirmButtonLabel = new LocalizedText
        {
            english = "Confirm",
            arabic = "تأكيد"
        };

        [Tooltip("SHOWN: label on the confirm button while a chat card is open, ending the audience.")]
        public LocalizedText chatEndButtonLabel = new LocalizedText
        {
            english = "End Audience",
            arabic = "إنهاء المقابلة"
        };

        [Header("Fallback Lines (shown when an LLM request fails)")]
        [Tooltip("SHOWN: description text when a speaker reaction request fails.")]
        public LocalizedText reactionFallback = new LocalizedText
        {
            english = "The speaker falls silent for a moment, then nods for you to continue.",
            arabic = "يصمت المتحدث لحظة، ثم يومئ لك بالمتابعة."
        };

        [Tooltip("SHOWN: description text when a low-resource warning reaction request fails.")]
        public LocalizedText warningFallback = new LocalizedText
        {
            english = "A worried murmur moves through the court.",
            arabic = "يسير همس قلق في أرجاء البلاط."
        };

        [Tooltip("SHOWN: opening line when the petitioner's first line request fails.")]
        public LocalizedText petitionOpeningFallback = new LocalizedText
        {
            english = "The petitioner stands before you, awaiting your judgment.",
            arabic = "يقف مقدم العريضة أمامك، ينتظر حكمك."
        };

        [Tooltip("SHOWN: closing line when the audience is exhausted and the closing-line request fails.")]
        public LocalizedText petitionClosingFallback = new LocalizedText
        {
            english = "The petitioner withdraws in silence, offering no further words.",
            arabic = "ينسحب مقدم العريضة في صمت، دون أن يزيد كلمة."
        };

        [Tooltip("SHOWN: petition error message when a submission was rate limited (HTTP 429).")]
        public LocalizedText petitionRateLimitedMessage = new LocalizedText
        {
            english = "The line is busy for a moment — try again.",
            arabic = "الخط مشغول للحظة — حاول مرة أخرى."
        };

        [Tooltip("SHOWN: petition error message when a submission fails for any other reason.")]
        public LocalizedText petitionSendFailedMessage = new LocalizedText
        {
            english = "Something went wrong sending that — try again.",
            arabic = "حدث خطأ أثناء الإرسال — حاول مرة أخرى."
        };
    }
}
