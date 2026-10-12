# Weave — Architecture & Technical Reference

This document details the repository layout, runtime architecture, LLM integration pathways, and localization pipeline for **Weave**.

---

## Repository Layout

```text
Weave/
├── Assets/Game/
│   ├── Art/                         # Card art, portraits, UI icons, fonts (Git LFS)
│   ├── Data/                        # Authoritative ScriptableObject content store
│   │   ├── NarrativeDatabase.asset  # Story graph: card/speaker refs, start card, editor node layouts
│   │   ├── ResourceCatalog.asset    # Crown / Gold / Army wiring
│   │   ├── LlmSettings.asset        # Model, temperature, token/turn limits, timeouts, spam mode
│   │   ├── LlmPromptTemplates.asset # System/user prompt seeds and localized fallbacks per mechanic
│   │   ├── FontSettings.asset       # Per-language font assignments
│   │   ├── Cards/                   # 50 CardData assets (Card_01 through Card_52)
│   │   ├── Speakers/                # 7 SpeakerData assets (persona prompts, localized names, portraits)
│   │   ├── Resources/               # 3 ResourceData assets (thresholds, warning advisors, collapse text)
│   │   └── CardVisual/              # CardVisualTemplate presets
│   ├── Music/                       # Background music (Git LFS)
│   ├── Resources/Fonts/             # Default fallback SDF font assets
│   ├── Scenes/Game.unity            # Single scene; composition root lives here
│   └── Scripts/                     # Game.Runtime + Game.Editor assemblies
│       ├── Definitions/             # CardData, SpeakerData, ResourceData, NarrativeDatabase,
│       │                            #   ResourceCatalog, CardVisualTemplate, ResourceChange
│       ├── Narrative/               # NarrativeRunner, ResourceState, ResourceWarningMonitor,
│       │                            #   PlayerHistoryTracker
│       ├── Llm/                     # LlmReactionClient, AudienceFlowController, AudienceSession,
│       │                            #   CollapseEndingBuilder, prompt builders, fallback text, sanitizer
│       ├── Localization/            # LanguageManager, RtlTextHelper, LocalizedText/Label,
│       │                            #   FontSettings, FallbackStrings
│       ├── UI/                      # CardView, ChoiceCardAnimator, HUD, start/pause views, ByokPanelView
│       ├── Input/                   # PlayerChoiceInput (swipe + keyboard bindings)
│       ├── Music/                   # MusicController
│       ├── Editor/                  # Card Graph window, node types, custom inspectors
│       └── GameManager.cs           # Composition root
├── Assets/WebGLTemplates/WeaveResponsive/ # Container-filling WebGL template
├── proxy/                           # Cloudflare Worker: shared Groq relay (see proxy/README.md)
└── ProjectSettings/                 # Unity project settings (Editor version pinned to 6000.0.79f1)
```

---

## Runtime Architecture

`GameManager` (`Assets/Game/Scripts/GameManager.cs`) is the composition root. It validates required Inspector references at startup via `InspectorValidation`, owns the run lifecycle (`start → cards → warnings → endings → restart`), and coordinates input, pause, audience flows, and the AI settings panel.

| Component | Responsibility |
| --- | --- |
| `NarrativeRunner` | Graph traversal, left/right and continue exit resolution, resource deltas, day counter, startup graph validation, ending/collapse detection |
| `ResourceState` / `ResourceWarningMonitor` | Clamped 0–100 values for **Crown**, **Gold**, and **Army**; 30% warning advisors; 0 = collapse |
| `AudienceFlowController` | Shared audience lifecycle for both petitions and free-form chats: defined speaker or generated commoner (`defaultCommonerPersona` + model-supplied speaker name), kingdom state snapshot, proposal confirmation, and teardown |
| `AudienceSession` | Conversation turn history and transcript tracking so each request carries full context |
| `LlmReactionClient` | Sole HTTP gateway (`UnityWebRequest` coroutines): direct Groq (BYOK) or shared proxy, key validation, health probe, and request cancellation on restart |
| `PromptComposer` / `SpeakerPromptBuilder` / `LlmPromptTemplates` | System/user prompt assembly from persona seeds + recent kingdom history |
| `CollapseEndingBuilder` | State-driven epilogue for resource collapse (LLM-generated with authored fallback) |
| `LlmFallbackText` / `LlmTextSanitizer` | Authored fallback strings; output cleanup (strips `<think>` reasoning tags, stage directions, code fences, and stray non-Arabic glyphs in Arabic mode) |
| `LanguageManager` / `RtlTextHelper` | Active language singleton and event bus; bidirectional shaping for Arabic |

### Card Node Types

Driven by `CardData` flags and edited visually in `Weave → Card Graph`:

- **Choice** — Two-way Left / Right branches with independent resource deltas and next-card links.
- **Reaction / Petition / Chat** — Single `continueNextCard` exit routed through the LLM flow (`isLlmReactionCard`, `isPetitionCard`, `isChatCard`).
- **Ending** — Terminal card (`IsEnding` when no outgoing branch is assigned).

### Resources & Court Speakers

- **Resources (`Assets/Game/Data/Resources/`):**
  - **Crown** (`التاج`) — Royal authority and noble obedience; monitored by **The Chancellor**.
  - **Gold** (`الذهب`) — Royal treasury and trade stability; monitored by **The Merchant**.
  - **Army** (`الجيش`) — Military strength and border defense; monitored by **The General**.
- **Speakers (`Assets/Game/Data/Speakers/`):**
  - **The Chancellor** (`المستشار`), **The General** (`الجنرال`), **The Treasurer** (`أمين الخزينة`), **The Merchant** (`كبير التجار`), **The Jester** (`بهلوان البلاط`), **The Seer** (`العرّافة`), and **The Spirit Mother** (`روح الملكة الأم`), plus runtime-generated **Commoners** on petition cards where `petitionerSource` is set to `GeneratedCommoner`.

---

## LLM Integration

One gateway (`LlmReactionClient`), two pathways, both configurable from the in-game **AI Voice** panel on first launch or anytime from the main/pause menu:

| Pathway | Request Path | Key Handling |
| --- | --- | --- |
| **Shared (default)** | Client → Cloudflare Worker → `api.groq.com/openai/v1/chat/completions` | Dev-owned key held as a Worker secret; never ships in the build. Enforces `POST`/`OPTIONS`, a 20 KB request body cap, and a per-IP rate limit (30 requests/minute per Cloudflare location). |
| **BYOK (Bring Your Own Key)** | Client → `api.groq.com` directly | Player-pasted [Groq API key](https://console.groq.com/keys) (starts with `gsk_`; free tier suffices), validated against `/openai/v1/models` without spending tokens, stored locally in device `PlayerPrefs`. |

### Behavioral Contracts

- **Health Probe:** Probes the shared proxy with a 30 s timeout (`apiTimeoutSeconds` on `LlmSettings.asset`) and a ~60 s freshness window to drive the in-game status indicator; failures log exact diagnostics to the console.
- **Authored Fallbacks:** Timeouts, HTTP 429/5xx, missing/invalid keys, and unparseable replies fall back to authored English/Arabic strings (`LlmFallbackText` / `FallbackStrings`). On petition cards, a failed turn converts the card into a standard left/right choice card so the run can advance.
- **Automatic Session Fallback:** An HTTP 401 on a stored BYOK key disables it for the session and transparently retries the request through the shared pathway.
- **Petitions & Spam Detection:** Petition turns parse into a `PetitionResolution` JSON schema (`phase`, `reaction`, `speakerName`, `resourceChanges`, `historyTag`, `isSpam`) with resource deltas clamped to `[-30, +30]`. Spam detection is governed by `DetectSpamMode` on `LlmSettings.asset` and is enabled by default (`ForceOn`).
- **Self-Hosting the Proxy:** Model selection (default `qwen/qwen3.8-27b`) and token/turn budgets live on `Assets/Game/Data/LlmSettings.asset`; the shared endpoint URL is the **Proxy URL** field on the scene's `LlmReactionClient`. To deploy your own Cloudflare Worker relay, see [`../proxy/README.md`](../proxy/README.md) for the `npx wrangler secret put GROQ_API_KEY` and `npx wrangler deploy` instructions.

---

## Localization Pipeline (English & Arabic)

- All UI labels, card descriptions, choice banners, speaker names, and fallback lines are authored bilingually via `LocalizedText` and `FallbackStrings`.
- Arabic text is shaped right-to-left at runtime using **RTLTMPro** through `RtlTextHelper` and `LocalizedLabel` (never assign `TMP_Text.text` directly).
- `FontSettings.asset` maps each language and text category to dedicated SDF fonts (*Cormorant Garamond* / *Felipa* for English; *Amiri* / *Noto Sans Arabic* with dynamic multi-atlas fallback for Arabic).
- LLM prompts dynamically inject language instructions (`LlmPromptTemplates.asset`), and `LlmTextSanitizer` strips leaked Latin/CJK characters from Arabic model outputs.
