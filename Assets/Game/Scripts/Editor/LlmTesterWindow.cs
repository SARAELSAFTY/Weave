using System;
using System.Collections.Generic;
using System.Linq;
using Game.Scripts.Definitions;
using Game.Scripts.Localization;
using Game.Scripts.Runtime.Llm;
using Game.Scripts.Runtime.Narrative;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor
{
    /// <summary>
    /// Manual Groq request inspector (Weave -> LLM Tester). Builds the exact system prompt GameManager would
    /// send for a chosen Speaker/Card/Resource from a real NarrativeDatabase - including per-card seed
    /// overrides - so you can read the composed prompt without Play Mode, and optionally fire it for real.
    /// Sending still requires Play Mode (LlmReactionClient needs a running MonoBehaviour for its coroutines).
    /// </summary>
    public class LlmTesterWindow : EditorWindow
    {
        private enum TestMode { Reaction, ResourceWarning, PetitionOpening, PetitionTurn, Epilogue }

        private NarrativeDatabase database;
        private LlmSettings settings;
        private string proxyUrl = "https://your-proxy.workers.dev";
        private TestMode mode = TestMode.Reaction;

        private int resourceIndex;        // for ResourceWarning / Epilogue's collapsed resource
        private SpeakerData manualSpeaker;
        private string manualSeed = string.Empty;

        private int sampleDay = 5;
        private string sampleSnapshot = string.Empty;
        private string sampleFullHistory = "Rationed the granary; Reassured the court; Enforced the curfew.";
        private string petitionPlayerInput = "Send reinforcements to the eastern wall.";

        private LlmReactionClient runnerClient;
        private PetitionSession activePetitionSession;
        private CardData cachedSelectedCard;

        private string rawResponse = string.Empty;
        private string parsedResult = string.Empty;
        private string statusLine = string.Empty;
        private bool isBusy;
        private Vector2 promptScroll;
        private Vector2 resultScroll;

        [MenuItem("Weave/LLM Tester")]
        public static void Open()
        {
            LlmTesterWindow window = GetWindow<LlmTesterWindow>("LLM Tester");
            window.minSize = new Vector2(520, 640);
            window.Show();
        }

        private void OnGUI()
        {
            DrawHeader();
            EditorGUILayout.Space(6);
            DrawSourceSettings();
            EditorGUILayout.Space(6);

            if (database == null)
            {
                EditorGUILayout.HelpBox("Assign a Narrative Database above to pick real speakers, cards, and resources.", MessageType.Info);
                return;
            }

            DrawModeAndTarget();
            EditorGUILayout.Space(6);
            DrawSampleState();
            EditorGUILayout.Space(6);
            DrawComposedPromptPreview();
            EditorGUILayout.Space(6);
            DrawSendControls();
            EditorGUILayout.Space(6);
            DrawResults();
        }

        private void DrawHeader()
        {
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14, alignment = TextAnchor.MiddleLeft };
            EditorGUILayout.LabelField("Groq API Manual Tester", titleStyle);
            EditorGUILayout.LabelField("Composes the exact prompt GameManager would send, then fires it with the same client.", EditorStyles.miniLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Prompt preview works in Edit Mode. Enter Play Mode to actually send a request (coroutines require a running MonoBehaviour).", MessageType.Info);
            }
        }

        private void DrawSourceSettings()
        {
            EditorGUILayout.LabelField("Source", EditorStyles.boldLabel);
            NarrativeDatabase newDatabase = (NarrativeDatabase)EditorGUILayout.ObjectField("Narrative Database", database, typeof(NarrativeDatabase), false);
            if (newDatabase != database)
            {
                database = newDatabase;
                cachedSelectedCard = null;
                resourceIndex = 0;
                ResetPetitionSession();
                RegenerateSampleSnapshot();
            }

            settings = (LlmSettings)EditorGUILayout.ObjectField("LLM Settings", settings, typeof(LlmSettings), false);
            proxyUrl = EditorGUILayout.TextField("Proxy URL", proxyUrl);

            if (database != null && database.promptTemplates == null)
            {
                EditorGUILayout.HelpBox("This database has no LlmPromptTemplates assigned - system instructions will be empty, same as in-game.", MessageType.Warning);
            }
        }

        private void DrawModeAndTarget()
        {
            EditorGUILayout.LabelField("Target", EditorStyles.boldLabel);

            TestMode newMode = (TestMode)EditorGUILayout.EnumPopup("Mode", mode);
            if (newMode != mode)
            {
                mode = newMode;
                if (mode == TestMode.PetitionTurn)
                {
                    ResetPetitionSession();
                }
            }

            switch (mode)
            {
                case TestMode.Reaction:
                    DrawCardPicker(c => c != null && c.isLlmReactionCard, "Reaction Card");
                    break;
                case TestMode.PetitionOpening:
                case TestMode.PetitionTurn:
                    DrawCardPicker(c => c != null && c.isPetitionCard, "Petition Card");
                    break;
                case TestMode.ResourceWarning:
                    DrawResourcePicker("Resource");
                    break;
                case TestMode.Epilogue:
                    DrawResourcePicker("Collapsed Resource");
                    break;
            }
        }

        private void DrawCardPicker(Func<CardData, bool> filter, string label)
        {
            List<CardData> matching = (database.cards ?? new List<CardData>()).Where(filter).ToList();
            string[] options = new[] { "(Custom - pick speaker manually)" }
                .Concat(matching.Select(c => c.DisplayName))
                .ToArray();

            int currentSelection = cachedSelectedCard != null && matching.Contains(cachedSelectedCard)
                ? matching.IndexOf(cachedSelectedCard) + 1
                : 0;
            int newSelection = EditorGUILayout.Popup(label, currentSelection, options);

            if (newSelection == 0)
            {
                cachedSelectedCard = null;
                manualSpeaker = (SpeakerData)EditorGUILayout.ObjectField("Speaker", manualSpeaker, typeof(SpeakerData), false);
                manualSeed = EditorGUILayout.TextField("Seed", manualSeed);
            }
            else
            {
                cachedSelectedCard = matching[newSelection - 1];
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.ObjectField("Speaker (from card)", cachedSelectedCard.speaker, typeof(SpeakerData), false);
                }

                bool commonerVoiced = cachedSelectedCard.isPetitionCard &&
                    !(cachedSelectedCard.petitionerSource == PetitionerSource.DefinedSpeaker && cachedSelectedCard.speaker != null);
                if (commonerVoiced)
                {
                    EditorGUILayout.LabelField("Voiced by a generated commoner - persona comes from LlmPromptTemplates.defaultCommonerPersona.", EditorStyles.miniLabel);
                }
            }
        }

        private CardData GetSelectedCard(List<CardData> matching) =>
            cachedSelectedCard != null && matching.Contains(cachedSelectedCard) ? cachedSelectedCard : null;

        private void DrawResourcePicker(string label)
        {
            List<ResourceData> resources = (database.resourceCatalog != null ? database.resourceCatalog.resources : null) ?? new List<ResourceData>();
            resources = resources.Where(r => r != null).ToList();

            if (resources.Count == 0)
            {
                EditorGUILayout.HelpBox("This database's Resource Catalog has no resources.", MessageType.Warning);
                return;
            }

            resourceIndex = Mathf.Clamp(resourceIndex, 0, resources.Count - 1);
            string[] names = resources.Select(r => r.DisplayName).ToArray();
            resourceIndex = EditorGUILayout.Popup(label, resourceIndex, names);

            ResourceData resource = resources[resourceIndex];
            if (mode == TestMode.ResourceWarning)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.ObjectField("Warning Speaker (from resource)", resource.warningSpeaker, typeof(SpeakerData), false);
                }
                if (resource.warningSpeaker == null)
                {
                    EditorGUILayout.HelpBox("This resource has no Warning Speaker assigned - in-game, its warning would silently skip straight to the next card.", MessageType.Warning);
                }
            }
        }

        private void DrawSampleState()
        {
            EditorGUILayout.LabelField("Sample Game State", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("No live run exists in Edit Mode, so this snapshot is a stand-in - edit it freely to test different situations.", MessageType.None);

            EditorGUI.BeginChangeCheck();
            sampleDay = EditorGUILayout.IntField("Day", sampleDay);
            if (EditorGUI.EndChangeCheck())
            {
                RegenerateSampleSnapshot();
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Kingdom Snapshot");
            if (GUILayout.Button("Regenerate from Catalog", GUILayout.Width(170)))
            {
                RegenerateSampleSnapshot();
            }
            EditorGUILayout.EndHorizontal();
            sampleSnapshot = EditorGUILayout.TextArea(sampleSnapshot, GUILayout.MinHeight(60));

            if (mode == TestMode.PetitionTurn)
            {
                petitionPlayerInput = EditorGUILayout.TextField("Player Input (this turn)", petitionPlayerInput);
            }

            if (mode == TestMode.Epilogue)
            {
                EditorGUILayout.LabelField("Full Choice History (sample)");
                sampleFullHistory = EditorGUILayout.TextArea(sampleFullHistory, GUILayout.MinHeight(40));
            }
        }

        private void RegenerateSampleSnapshot()
        {
            List<ResourceData> resources = database?.resourceCatalog != null ? database.resourceCatalog.resources : null;
            string resourceSummary = "None";
            if (resources != null && resources.Count > 0)
            {
                resourceSummary = string.Join(", ", resources.Where(r => r != null).Select(r => $"{r.DisplayName}: {r.defaultStartingValue}"));
            }

            sampleSnapshot =
                $"Current Day: {sampleDay}\n" +
                $"Kingdom Resources -> {resourceSummary}\n" +
                "Recent Narrative History: None\n" +
                "Completed Petition Conversations: None";
        }

        private void DrawComposedPromptPreview()
        {
            EditorGUILayout.LabelField("Composed Prompt (exact - read before sending)", EditorStyles.boldLabel);

            string prompt;
            try
            {
                prompt = ComposeCurrentPrompt(out string modeWarning);
                if (!string.IsNullOrEmpty(modeWarning))
                {
                    EditorGUILayout.HelpBox(modeWarning, MessageType.Warning);
                }
            }
            catch (Exception exception)
            {
                prompt = $"(Could not compose prompt: {exception.Message})";
            }

            promptScroll = EditorGUILayout.BeginScrollView(promptScroll, GUILayout.Height(160));
            EditorGUILayout.SelectableLabel(prompt, EditorStyles.textArea, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private LlmPromptTemplates Templates => database != null ? database.promptTemplates : null;

        private GameLanguage TesterLanguage => LanguageManager.Instance != null
            ? LanguageManager.Instance.CurrentLanguage
            : GameLanguage.English;

        private IReadOnlyList<ResourceData> CatalogResources =>
            database != null && database.resourceCatalog != null ? database.resourceCatalog.resources : null;

        private SpeakerData ResolveSpeaker(List<CardData> matching)
        {
            if (mode == TestMode.ResourceWarning)
            {
                return GetSelectedResource()?.warningSpeaker;
            }

            if (mode == TestMode.Epilogue)
            {
                return null;
            }

            CardData card = GetSelectedCard(matching);
            if (card == null)
            {
                return manualSpeaker;
            }

            // Mirrors GameManager.ResolvePetitioner: petitions without a defined speaker
            // are voiced by a generated commoner, not by an empty persona.
            bool hasDefinedSpeaker = card.petitionerSource == PetitionerSource.DefinedSpeaker && card.speaker != null;
            if (card.isPetitionCard && !hasDefinedSpeaker)
            {
                return GetCommonerPreviewSpeaker();
            }

            return card.speaker;
        }

        private SpeakerData commonerPreviewSpeaker;

        private SpeakerData GetCommonerPreviewSpeaker()
        {
            if (commonerPreviewSpeaker == null)
            {
                commonerPreviewSpeaker = ScriptableObject.CreateInstance<SpeakerData>();
                commonerPreviewSpeaker.hideFlags = HideFlags.HideAndDontSave;
                commonerPreviewSpeaker.displayName = "A Common Subject (preview)";
            }

            commonerPreviewSpeaker.llmPersonaPrompt = Templates != null ? Templates.defaultCommonerPersona : string.Empty;
            return commonerPreviewSpeaker;
        }

        private ResourceData GetSelectedResource()
        {
            List<ResourceData> resources = database?.resourceCatalog != null ? database.resourceCatalog.resources : null;
            if (resources == null || resources.Count == 0) return null;
            return resources[Mathf.Clamp(resourceIndex, 0, resources.Count - 1)];
        }

        // Builds the same prompt GameManager would for the current selection. Read-only - never mutates
        // activePetitionSession, so it's safe to call every OnGUI repaint.
        private string ComposeCurrentPrompt(out string warning)
        {
            warning = null;
            LlmPromptTemplates templates = Templates;
            List<CardData> matching = mode == TestMode.Reaction
                ? (database.cards ?? new List<CardData>()).Where(c => c != null && c.isLlmReactionCard).ToList()
                : (database.cards ?? new List<CardData>()).Where(c => c != null && c.isPetitionCard).ToList();
            CardData card = GetSelectedCard(matching);
            SpeakerData speaker = ResolveSpeaker(matching);

            if (speaker != null && string.IsNullOrWhiteSpace(speaker.llmPersonaPrompt))
            {
                warning = $"Speaker '{speaker.DisplayName}' has no persona prompt authored - this will be voiced with no persona.";
            }

            switch (mode)
            {
                case TestMode.Reaction:
                {
                    string seed = card != null ? card.EffectiveReactionSeed(templates) : manualSeed;
                    return SpeakerPromptBuilder.BuildPersonaPrompt(speaker, sampleSnapshot, seed, templates, TesterLanguage, CatalogResources);
                }

                case TestMode.PetitionOpening:
                {
                    string seed = card != null ? card.EffectivePetitionSeed(templates) : manualSeed;
                    return SpeakerPromptBuilder.BuildPersonaPrompt(speaker, sampleSnapshot, seed, templates, TesterLanguage, CatalogResources);
                }

                case TestMode.ResourceWarning:
                {
                    ResourceData resource = GetSelectedResource();
                    string seed = PromptTemplateUtility.Fill(
                        templates != null ? templates.defaultWarningSeedPrompt : string.Empty,
                        "resourceName", resource != null ? resource.GetDisplayName(TesterLanguage) : "(resource)");
                    return SpeakerPromptBuilder.BuildPersonaPrompt(speaker, sampleSnapshot, seed, templates, TesterLanguage, CatalogResources);
                }

                case TestMode.PetitionTurn:
                {
                    EnsurePetitionSession();
                    string seed = card != null ? card.EffectivePetitionSeed(templates) : manualSeed;
                    IReadOnlyList<ResourceData> validResources = CatalogResources;
                    int clamp = settings != null ? settings.petitionResourceClampMagnitude : 20;
                    return SpeakerPromptBuilder.BuildPetitionTurnPrompt(
                        speaker, sampleSnapshot, seed, validResources, clamp, templates, TesterLanguage)
                        + "\n\n[Spam dots remaining: " + activePetitionSession.DotsRemaining + " of " + activePetitionSession.DotBudget + " - see Results below for transcript]";
                }

                case TestMode.Epilogue:
                {
                    ResourceData resource = GetSelectedResource();
                    return SpeakerPromptBuilder.BuildEpiloguePrompt(
                        sampleDay, resource != null ? resource.GetDisplayName(TesterLanguage) : "(resource)",
                        BuildFallbackResourceSummary(), sampleFullHistory, templates, TesterLanguage);
                }
            }

            return string.Empty;
        }

        private string BuildFallbackResourceSummary()
        {
            List<ResourceData> resources = database?.resourceCatalog != null ? database.resourceCatalog.resources : null;
            if (resources == null || resources.Count == 0) return "None";
            return string.Join(", ", resources.Where(r => r != null).Select(r => $"{r.DisplayName}: {r.defaultStartingValue}"));
        }

        private void DrawSendControls()
        {
            bool canSend = Application.isPlaying && !isBusy && settings != null && !string.IsNullOrEmpty(proxyUrl);

            using (new EditorGUI.DisabledScope(!canSend))
            {
                if (mode == TestMode.PetitionTurn)
                {
                    EnsurePetitionSession();
                    EditorGUILayout.LabelField($"Petition session: {activePetitionSession.DotsRemaining} of {activePetitionSession.DotBudget} spam dots remaining" +
                        (activePetitionSession.DotsExhausted ? " - dots exhausted" : "") +
                        (activePetitionSession.AwaitingConfirmation ? " - awaiting confirmation" : ""));

                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button(isBusy ? "Sending..." : "Send Turn", GUILayout.Height(30)))
                    {
                        SendPetitionTurn();
                    }
                    if (GUILayout.Button("Reset Session", GUILayout.Width(120), GUILayout.Height(30)))
                    {
                        ResetPetitionSession();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    if (GUILayout.Button(isBusy ? "Sending..." : "Send", GUILayout.Height(32)))
                    {
                        SendCurrentPrompt();
                    }
                }
            }

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to send.", MessageType.Warning);
            }
            else if (settings == null || string.IsNullOrEmpty(proxyUrl))
            {
                EditorGUILayout.HelpBox("Assign LLM Settings and a Proxy URL to send.", MessageType.Warning);
            }
        }

        private void DrawResults()
        {
            if (string.IsNullOrEmpty(statusLine))
            {
                return;
            }

            EditorGUILayout.LabelField("Results", EditorStyles.boldLabel);

            if (!string.IsNullOrEmpty(statusLine))
            {
                MessageType msgType = statusLine.StartsWith("[OK]") ? MessageType.Info
                    : statusLine.StartsWith("[Warning]") ? MessageType.Warning
                    : MessageType.Error;
                EditorGUILayout.HelpBox(statusLine, msgType);
            }

            if (!string.IsNullOrEmpty(parsedResult))
            {
                EditorGUILayout.LabelField("Parsed Output:");
                EditorGUILayout.HelpBox(parsedResult, MessageType.None);
            }

            if (mode == TestMode.PetitionTurn && activePetitionSession != null)
            {
                IReadOnlyList<string> transcriptLines = activePetitionSession.GetTranscript();
                string transcript = transcriptLines != null && transcriptLines.Count > 0
                    ? string.Join("\n", transcriptLines)
                    : string.Empty;
                if (!string.IsNullOrEmpty(transcript))
                {
                    EditorGUILayout.LabelField("Session Transcript:", EditorStyles.miniBoldLabel);
                    EditorGUILayout.HelpBox(transcript, MessageType.None);
                }
            }

            resultScroll = EditorGUILayout.BeginScrollView(resultScroll);
            if (!string.IsNullOrEmpty(rawResponse))
            {
                EditorGUILayout.LabelField("Raw Response:", EditorStyles.miniBoldLabel);
                EditorGUILayout.TextArea(rawResponse, EditorStyles.helpBox, GUILayout.ExpandHeight(false));
            }
            EditorGUILayout.EndScrollView();
        }

        private void EnsurePetitionSession()
        {
            if (activePetitionSession == null)
            {
                activePetitionSession = new PetitionSession(settings != null ? settings.petitionSpamDotBudget : 3);
            }
        }

        private void ResetPetitionSession()
        {
            activePetitionSession = null;
            rawResponse = string.Empty;
            parsedResult = string.Empty;
            statusLine = string.Empty;
        }

        private void EnsureRunnerClient()
        {
            if (runnerClient == null)
            {
                GameObject go = new GameObject("[LlmTesterRunner]") { hideFlags = HideFlags.HideAndDontSave };
                runnerClient = go.AddComponent<LlmReactionClient>();
            }
            runnerClient.Configure(settings, proxyUrl);
        }

        private void SendCurrentPrompt()
        {
            if (!Application.isPlaying) return;

            string prompt = ComposeCurrentPrompt(out _);
            rawResponse = string.Empty;
            parsedResult = string.Empty;
            statusLine = "Sending...";
            isBusy = true;
            Repaint();

            EnsureRunnerClient();
            int? maxTokens = mode == TestMode.Epilogue && settings != null ? settings.epilogueMaxTokens : (int?)null;
            LlmPromptTemplates templates = Templates;

            runnerClient.RequestReaction(prompt, templates != null ? templates.singleTurnUserMessage : string.Empty, TesterLanguage,
                line =>
                {
                    parsedResult = string.IsNullOrWhiteSpace(line) ? "(empty content)" : line;
                    statusLine = "[OK]";
                    isBusy = false;
                    Repaint();
                },
                error =>
                {
                    statusLine = $"[Error] {error}";
                    isBusy = false;
                    Repaint();
                },
                maxTokens);
        }

        private void SendPetitionTurn()
        {
            if (!Application.isPlaying) return;

            EnsurePetitionSession();
            LlmPromptTemplates templates = Templates;
            List<CardData> matching = (database.cards ?? new List<CardData>()).Where(c => c != null && c.isPetitionCard).ToList();
            CardData card = GetSelectedCard(matching);
            SpeakerData speaker = ResolveSpeaker(matching);
            string seed = card != null ? card.EffectivePetitionSeed(templates) : manualSeed;
            IReadOnlyList<ResourceData> validResources = database.resourceCatalog != null ? database.resourceCatalog.resources : null;
            int clamp = settings != null ? settings.petitionResourceClampMagnitude : 20;

            List<GroqApiMessage> messages = activePetitionSession.BuildMessagesForSubmission(
                petitionPlayerInput, speaker, sampleSnapshot, seed, validResources, clamp, templates, TesterLanguage);

            rawResponse = string.Empty;
            parsedResult = string.Empty;
            statusLine = "Sending...";
            isBusy = true;
            Repaint();

            EnsureRunnerClient();
            runnerClient.RequestPetitionTurn(messages, TesterLanguage,
                (result, raw) =>
                {
                    rawResponse = raw;
                    if (result == null || string.IsNullOrWhiteSpace(result.reaction))
                    {
                        statusLine = "[Error] Empty or unparsable resolution";
                        isBusy = false;
                        Repaint();
                        return;
                    }

                    activePetitionSession.RecordReply(result, raw);
                    parsedResult = $"phase={result.phase}, reaction=\"{result.reaction}\", historyTag=\"{result.historyTag}\", isSpam={result.isSpam}" +
                        (result.resourceChanges != null && result.resourceChanges.Length > 0
                            ? "\nresourceChanges: " + string.Join(", ", result.resourceChanges.Select(r => $"{r.resource}:{r.delta}"))
                            : string.Empty);

                    statusLine = activePetitionSession.DotsExhausted
                        ? "[Warning] Spam-dot budget exhausted - in-game this would request a closing line and convert the card."
                        : "[OK]";
                    isBusy = false;
                    Repaint();
                },
                error =>
                {
                    statusLine = $"[Error] {error}";
                    isBusy = false;
                    Repaint();
                });
        }
    }
}
