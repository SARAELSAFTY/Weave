namespace Game.Scripts.Localization
{
    public static class FallbackStrings
    {
        public static string PetitionRateLimited(GameLanguage language) =>
            language == GameLanguage.Arabic
                ? "الخط مشغول للحظة — حاول مرة أخرى."
                : "The line is busy for a moment — try again.";

        public static string PetitionSendFailed(GameLanguage language) =>
            language == GameLanguage.Arabic
                ? "حدث خطأ أثناء الإرسال — حاول مرة أخرى."
                : "Something went wrong sending that — try again.";

        public static string PetitionClosingLine(GameLanguage language) =>
            language == GameLanguage.Arabic
                ? "ينسحب مقدم العريضة في صمت، دون أن يزيد كلمة."
                : "The petitioner withdraws in silence, offering no further words.";

        public static string PetitionOpeningUnavailable(GameLanguage language) =>
            language == GameLanguage.Arabic
                ? "يقف مقدم العريضة أمامك، ينتظر حكمك."
                : "The petitioner stands before you, awaiting your judgment.";

        public static string ReactionUnavailable(GameLanguage language) =>
            language == GameLanguage.Arabic
                ? "يصمت المتحدث لحظة، ثم يومئ لك بالمتابعة."
                : "The speaker falls silent for a moment, then nods for you to continue.";

        public static string WarningUnavailable(GameLanguage language) =>
            language == GameLanguage.Arabic
                ? "يسير همس قلق في أرجاء البلاط."
                : "A worried murmur moves through the court.";

        public static string GeneratingFinalRecord(GameLanguage language) =>
            language == GameLanguage.Arabic
                ? "جارٍ إعداد السجل الختامي..."
                : "Generating the final record...";

        public static string RunEnded(GameLanguage language) =>
            language == GameLanguage.Arabic
                ? "انتهت الجولة."
                : "The run has ended.";

        public static string Dismiss(GameLanguage language) =>
            language == GameLanguage.Arabic ? "صرف" : "Dismiss";

        public static string Continue(GameLanguage language) =>
            language == GameLanguage.Arabic ? "متابعة" : "Continue";

        public static string KingdomStatusUnknown(GameLanguage language) =>
            language == GameLanguage.Arabic
                ? "حالة المملكة: غير معروفة"
                : "Kingdom Status: Unknown";
    }
}
