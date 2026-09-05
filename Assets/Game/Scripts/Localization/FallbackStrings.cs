namespace Game.Scripts.Localization
{
    /// <summary>Hardcoded bilingual fallback strings used when dynamic or LLM-generated content is unavailable.</summary>
    public static class FallbackStrings
    {
        /// <summary>Returns the rate-limit message shown when petition submissions are throttled.</summary>
        /// <param name="language">Target language.</param>
        public static string PetitionRateLimited(GameLanguage language) =>
            language == GameLanguage.Arabic
                ? "الخط مشغول للحظة — حاول مرة أخرى."
                : "The line is busy for a moment — try again.";

        /// <summary>Returns the generic send-failure message for petition submissions.</summary>
        /// <param name="language">Target language.</param>
        public static string PetitionSendFailed(GameLanguage language) =>
            language == GameLanguage.Arabic
                ? "حدث خطأ أثناء الإرسال — حاول مرة أخرى."
                : "Something went wrong sending that — try again.";

        /// <summary>Returns the closing narration line when a petition session exhausts its turn limit without a proposal.</summary>
        /// <param name="language">Target language.</param>
        public static string PetitionClosingLine(GameLanguage language) =>
            language == GameLanguage.Arabic
                ? "ينسحب مقدم العريضة في صمت، دون أن يزيد كلمة."
                : "The petitioner withdraws in silence, offering no further words.";

        /// <summary>Returns the placeholder opening line when petition situation generation fails.</summary>
        /// <param name="language">Target language.</param>
        public static string PetitionOpeningUnavailable(GameLanguage language) =>
            language == GameLanguage.Arabic
                ? "يقف مقدم العريضة أمامك، ينتظر حكمك."
                : "The petitioner stands before you, awaiting your judgment.";

        /// <summary>Returns the placeholder reaction text when an LLM speaker reaction request fails.</summary>
        /// <param name="language">Target language.</param>
        public static string ReactionUnavailable(GameLanguage language) =>
            language == GameLanguage.Arabic
                ? "يصمت المتحدث لحظة، ثم يومئ لك بالمتابعة."
                : "The speaker falls silent for a moment, then nods for you to continue.";

        /// <summary>Returns the placeholder text when a resource warning LLM reaction cannot be generated.</summary>
        /// <param name="language">Target language.</param>
        public static string WarningUnavailable(GameLanguage language) =>
            language == GameLanguage.Arabic
                ? "يسير همس قلق في أرجاء البلاط."
                : "A worried murmur moves through the court.";

        /// <summary>Returns the interim loading text displayed while an epilogue is being generated.</summary>
        /// <param name="language">Target language.</param>
        public static string GeneratingFinalRecord(GameLanguage language) =>
            language == GameLanguage.Arabic
                ? "جارٍ إعداد السجل الختامي..."
                : "Generating the final record...";

        /// <summary>Returns the run-ended status message.</summary>
        /// <param name="language">Target language.</param>
        public static string RunEnded(GameLanguage language) =>
            language == GameLanguage.Arabic
                ? "انتهت الجولة."
                : "The run has ended.";

        /// <summary>Returns the localized label for the Dismiss action button.</summary>
        /// <param name="language">Target language.</param>
        public static string Dismiss(GameLanguage language) =>
            language == GameLanguage.Arabic ? "صرف" : "Dismiss";

        /// <summary>Returns the localized label for the Continue action button.</summary>
        /// <param name="language">Target language.</param>
        public static string Continue(GameLanguage language) =>
            language == GameLanguage.Arabic ? "متابعة" : "Continue";

        /// <summary>Returns the placeholder kingdom-status string when no game-state snapshot is available.</summary>
        /// <param name="language">Target language.</param>
        public static string KingdomStatusUnknown(GameLanguage language) =>
            language == GameLanguage.Arabic
                ? "حالة المملكة: غير معروفة"
                : "Kingdom Status: Unknown";
    }
}
