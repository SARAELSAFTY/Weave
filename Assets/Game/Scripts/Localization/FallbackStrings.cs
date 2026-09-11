namespace Game.Scripts.Localization
{
    /// <summary>Hardcoded bilingual last-resort strings used when dynamic or LLM-generated content is unavailable.</summary>
    /// <remarks>Narrative fallback lines have editable copies on LlmPromptTemplates; these constants are
    /// only reached when that asset (or a field on it) is missing. UI status labels (dismiss, continue,
    /// run ended) have no template copies and come from here directly.</remarks>
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

        /// <summary>Hint prepended to the down-swipe / middle option on three-way cards.</summary>
        public static string SwipeDownHint(GameLanguage language) =>
            language == GameLanguage.Arabic ? "اسحب للأسفل / S:" : "Swipe down / S:";

        /// <summary>Returns the localized label for the petition proposal confirm button.</summary>
        /// <param name="language">Target language.</param>
        public static string Confirm(GameLanguage language) =>
            language == GameLanguage.Arabic ? "تأكيد" : "Confirm";

        /// <summary>Returns the localized label for the chat audience end button.</summary>
        /// <param name="language">Target language.</param>
        public static string EndAudience(GameLanguage language) =>
            language == GameLanguage.Arabic ? "إنهاء المقابلة" : "End Audience";

        /// <summary>Returns the placeholder kingdom-status string when no game-state snapshot is available.</summary>
        /// <param name="language">Target language.</param>
        public static string KingdomStatusUnknown(GameLanguage language) =>
            language == GameLanguage.Arabic
                ? "حالة المملكة: غير معروفة"
                : "Kingdom Status: Unknown";

        /// <summary>Returns the line used in kingdom-history summaries when no ruler decisions were recorded.</summary>
        /// <param name="language">Target language.</param>
        public static string FullHistoryUnavailable(GameLanguage language) =>
            language == GameLanguage.Arabic
                ? "لم تُسجَّل أي قرارات."
                : "No decisions were recorded.";

        /// <summary>Returns the title of the AI settings panel.</summary>
        /// <param name="language">Target language.</param>
        public static string ByokPanelTitle(GameLanguage language) =>
            language == GameLanguage.Arabic ? "إعدادات الذكاء الاصطناعي" : "AI Settings";

        /// <summary>Returns the concise panel explanation. Only some characters are AI-driven; most story text is static.</summary>
        /// <param name="language">Target language.</param>
        public static string ByokPanelInfo(GameLanguage language) =>
            language == GameLanguage.Arabic
                ? "بعض الشخصيات تتحدث بذكاء اصطناعي حي، وردودها تُكتب في اللحظة.\n\nافتراضياً يتشارك الجميع اتصالاً مجانياً واحداً قد يزدحم. مفتاح Groq المجاني الخاص بك يمنحك اتصالاً خاصاً. يبقى المفتاح على جهازك ولا يُرسل إلا إلى Groq."
                : "Some characters speak with a live AI: their replies are written on the spot.\n\nBy default everyone shares one free connection, which can get busy. Your own free Groq key gives you a private one. The key stays on this device and is sent only to Groq.";

        /// <summary>Returns the localized placeholder for the key input field.</summary>
        /// <param name="language">Target language.</param>
        public static string ByokPlaceholder(GameLanguage language) =>
            language == GameLanguage.Arabic ? "الصق مفتاحك هنا (يبدأ بـ gsk_)" : "Paste your key here (it starts with gsk_)";

        /// <summary>Returns the label for the button opening the Groq key page.</summary>
        /// <param name="language">Target language.</param>
        public static string ByokOpenGroq(GameLanguage language) =>
            language == GameLanguage.Arabic ? "احصل على مفتاح" : "Get a key";

        /// <summary>Returns the label for the paste-from-clipboard button.</summary>
        /// <param name="language">Target language.</param>
        public static string ByokPaste(GameLanguage language) =>
            language == GameLanguage.Arabic ? "لصق" : "Paste";

        /// <summary>Returns the status shown when a gsk_ key was found in the clipboard.</summary>
        /// <param name="language">Target language.</param>
        public static string ByokClipboardFound(GameLanguage language) =>
            language == GameLanguage.Arabic ? "وجدنا المفتاح في الحافظة. اضغط تحقق." : "Found your key in the clipboard. Press Check.";

        /// <summary>Returns the status shown when the clipboard has no key to paste.</summary>
        /// <param name="language">Target language.</param>
        public static string ByokClipboardEmpty(GameLanguage language) =>
            language == GameLanguage.Arabic ? "لا يوجد مفتاح للصق. انسخ المفتاح من صفحة Groq أولاً." : "Nothing to paste. Copy your key from the Groq page first.";

        /// <summary>Returns the label of the shared-connection option button.</summary>
        /// <param name="language">Target language.</param>
        public static string ByokSharedOption(GameLanguage language) =>
            language == GameLanguage.Arabic ? "اتصال مشترك" : "Shared connection";

        /// <summary>Returns the label of the own-key option button.</summary>
        /// <param name="language">Target language.</param>
        public static string ByokOwnOption(GameLanguage language) =>
            language == GameLanguage.Arabic ? "مفتاحي الخاص" : "My own key";

        /// <summary>Returns the label for the button validating the pasted key.</summary>
        /// <param name="language">Target language.</param>
        public static string ByokCheckButton(GameLanguage language) =>
            language == GameLanguage.Arabic ? "تحقق من المفتاح" : "Check key";

        /// <summary>Returns the status shown while a candidate key is being validated.</summary>
        /// <param name="language">Target language.</param>
        public static string ByokChecking(GameLanguage language) =>
            language == GameLanguage.Arabic ? "جارٍ التحقق..." : "Checking...";

        /// <summary>Returns the status confirming a stored, working key.</summary>
        /// <param name="language">Target language.</param>
        public static string ByokStatusSaved(GameLanguage language) =>
            language == GameLanguage.Arabic ? "تم حفظ المفتاح على هذا الجهاز." : "Key saved on this device.";

        /// <summary>Returns the friendly status for a key Groq rejected during validation.</summary>
        /// <param name="language">Target language.</param>
        public static string ByokRejectedKey(GameLanguage language) =>
            language == GameLanguage.Arabic
                ? "لم يقبل Groq هذا المفتاح. تأكد من نسخه كاملاً، يبدأ بـ gsk_"
                : "Groq did not accept this key. Make sure you copied the whole key, it starts with gsk_";

        /// <summary>Returns the status when the validation probe could not reach Groq.</summary>
        /// <param name="language">Target language.</param>
        public static string ByokUnreachable(GameLanguage language) =>
            language == GameLanguage.Arabic ? "تعذر الوصول إلى Groq. تحقق من اتصالك." : "Could not reach Groq. Check your connection.";

        /// <summary>Returns the nudge shown when continuing with the own-key option but no valid key is stored.</summary>
        /// <param name="language">Target language.</param>
        public static string ByokNag(GameLanguage language) =>
            language == GameLanguage.Arabic ? "تحقق من مفتاحك أولاً، أو اختر الاتصال المشترك." : "Check your key first, or choose the shared connection.";

        /// <summary>Returns the shared-line status while the service probe is in flight.</summary>
        /// <param name="language">Target language.</param>
        public static string ByokSharedChecking(GameLanguage language) =>
            language == GameLanguage.Arabic ? "جارٍ التحقق من الاتصال المشترك..." : "Checking the shared connection...";

        /// <summary>Returns the shared-line status once a probe completed a request end to end.</summary>
        /// <param name="language">Target language.</param>
        public static string ByokSharedOnline(GameLanguage language) =>
            language == GameLanguage.Arabic ? "الاتصال المشترك يعمل." : "Shared connection is working.";

        /// <summary>Returns the shared-line status when the probe failed. Deliberately generic: the reason
        /// (dead key, exhausted quota, outage) is only actionable for us and goes to the console.</summary>
        /// <param name="language">Target language.</param>
        public static string ByokSharedOffline(GameLanguage language) =>
            language == GameLanguage.Arabic ? "الاتصال المشترك غير متاح حالياً." : "Shared connection is unavailable right now.";
    }
}
