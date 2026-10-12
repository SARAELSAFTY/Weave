# Weave

A bilingual (English / Arabic) narrative card-strategy game built in **Unity `6000.0.79f1`** with real-time LLM character audiences, petitions, and dynamic advisor reactions.

**Play the WebGL build (desktop & touch-screen supported):** **[abdullahmohieldin.itch.io/weave](https://abdullahmohieldin.itch.io/weave)**

---

## Overview

In **Weave**, you rule a troubled kingdom by swiping decision cards left or right, balancing three core pillars of power — **Crown**, **Gold**, and **Army** — across each day of your reign. Beyond authored branching choices, selected cards open live generative audiences backed by a Groq-compatible `chat/completions` endpoint:

- **Advisor Reactions & Warnings** — Court figures react in character to your decisions and issue urgent warnings when a resource drops below 30%.
- **Multi-Turn Petitions** — Subjects and guild leaders bring grievances to the throne; you type free-form royal decrees that the petitioner negotiates into concrete resource proposals.
- **Free-Form Character Audiences** — Multi-turn open dialogue with key characters with no resource stakes, ending whenever you close the audience.
- **Dynamic Collapse Epilogues** — If any resource hits 0, the responsible advisor delivers a state-driven epilogue reflecting your reign's history.

Every LLM touchpoint includes authored fallback text in both English and Arabic, so runs can still progress when the LLM service is unreachable or no API key is configured.

---

## Features

- **50-Card Branching Narrative** — Authored story graph with 7 distinct court speakers, multiple endings, resource collapse states, and best-run survival tracking.
- **Dual-Pathway LLM Gateway** — Supports zero-setup play via a shared **Cloudflare Worker** relay (`proxy/`) or direct **Bring Your Own Key (BYOK)** requests to Groq (`qwen/qwen3.8-27b`), with automatic fallback on timeouts, HTTP 401/429/5xx, or offline play.
- **Structured JSON Petitions** — Multi-turn petition audiences parse model outputs into structured `PetitionResolution` payloads (in-character reaction, resource deltas clamped to safe bounds, history tags, and spam detection enabled by default).
- **Full English & Arabic Localization** — Instant in-game language switching with right-to-left (RTL) bidirectional shaping via `RTLTMPro` and dedicated per-language multi-atlas SDF font stacks.
- **Custom Unity Graph Editor** — Visual node-graph authoring window (`Weave → Card Graph`) built with UIElements/GraphView for connecting cards, speakers, and resources with live validation badges (`START`, `LLM`, `PETITION`, `CHAT`, `ENDING`, `DEAD END`).
- **Responsive Touch & Desktop Input** — Frame-rate-independent exponential drag smoothing across mouse, touch, and keyboard (`A`/`D` or `←`/`→`), packaged with a custom container-filling WebGL template (`WeaveResponsive`).

---

## Controls

| Action | Input |
| --- | --- |
| **Choose Left / Right** | Drag/swipe card left or right, or press `A` / `D` (`←` / `→` arrow keys) |
| **Submit Petition / Chat Reply** | Type in the audience input field and press **Send** (or `Enter`) |
| **Confirm Proposal / End Chat** | Click the **Confirm** / **End Audience** button on the card |
| **Pause Menu** | Press `Esc` (or click the pause button) |
| **Switch Language (EN ↔ AR)** | Click the language toggle in the main/pause menu (or press `F10` in the Unity Editor) |
| **AI Voice / API Key Settings** | Click the **AI** button on the start screen or pause menu |

---

## Tech Stack

| Layer | Technology |
| --- | --- |
| **Engine** | Unity `6000.0.79f1`, Universal Render Pipeline (`com.unity.render-pipelines.universal` 17.0.4) |
| **Input & UI** | Unity Input System 1.19.0, uGUI, TextMeshPro |
| **Arabic Localization** | RTLTMPro 4.0.0 (OpenUPM) + per-language `FontSettings` & multi-atlas SDF fonts |
| **Assemblies** | `Game.Runtime` (player build), `Game.Editor` (Editor-only Card Graph window & custom inspectors) |
| **Data Architecture** | Unity ScriptableObjects (`Assets/Game/Data/`) |
| **LLM Backend** | Groq OpenAI-compatible `chat/completions` (`qwen/qwen3.8-27b`); direct BYOK or Cloudflare Worker |
| **Serverless Proxy** | Cloudflare Worker (`proxy/src/index.js`), deployed with Wrangler |
| **Binary Assets** | Git LFS (art, audio, fonts) |

---

## Repository Layout

```
Weave/
├── Assets/Game/
│   ├── Art/                        # Card art, portraits, UI icons, fonts (Git LFS)
│   ├── Data/                       # Authoritative ScriptableObject content store
│   │   ├── NarrativeDatabase.asset # Story graph: card/speaker refs, start card, editor node layouts
│   │   ├── ResourceCatalog.asset   # Crown / Gold / Army wiring
│   │   ├── LlmSettings.asset       # Model, temperature, token/turn limits, timeouts, spam mode
│   │   ├── LlmPromptTemplates.asset# System/user prompt seeds and localized fallbacks per mechanic
│   │   ├── FontSettings.asset      # Per-language font assignments
│   │   ├── Cards/                  # 50 CardData assets (Card_01 through Card_52)
│   │   ├── Speakers/               # 7 SpeakerData assets (persona prompts, localized names, portraits)
│   │   ├── Resources/              # 3 ResourceData assets (thresholds, warning advisors, collapse text)
│   │   └── CardVisual/             # CardVisualTemplate presets
│   ├── Music/                      # Background music (Git LFS)
│   ├── Resources/Fonts/            # Default fallback SDF font assets
│   ├── Scenes/Game.unity           # Single scene; composition root lives here
│   └── Scripts/                    # Game.Runtime + Game.Editor assemblies
│       ├── Definitions/            # CardData, SpeakerData, ResourceData, NarrativeDatabase,
│       │                           #   ResourceCatalog, CardVisualTemplate, ResourceChange
│       ├── Narrative/              # NarrativeRunner, ResourceState, ResourceWarningMonitor,
│       │                           #   PlayerHistoryTracker
│       ├── Llm/                    # LlmReactionClient, AudienceFlowController, AudienceSession,
│       │                           #   CollapseEndingBuilder, prompt builders, fallback text, sanitizer
│       ├── Localization/           # LanguageManager, RtlTextHelper, LocalizedText/Label,
│       │                           #   FontSettings, FallbackStrings
│       ├── UI/                     # CardView, ChoiceCardAnimator, HUD, start/pause views, ByokPanelView
│       ├── Input/                  # PlayerChoiceInput (swipe + keyboard bindings)
│       ├── Music/                  # MusicController
│       ├── Editor/                 # Card Graph window, node types, custom inspectors
│       └── GameManager.cs          # Composition root
├── Assets/WebGLTemplates/WeaveResponsive/  # Container-filling WebGL template
├── proxy/                          # Cloudflare Worker: shared Groq relay (see proxy/README.md)
└── ProjectSettings/                # Unity project settings (Editor version pinned to 6000.0.79f1)
```

---

## Runtime Architecture

`GameManager` (`Assets/Game/Scripts/GameManager.cs`) is the composition root. It validates required Inspector references at startup via `InspectorValidation`, owns the run lifecycle (start → cards → warnings → endings → restart), and coordinates input, pause, audience flows, and the AI settings panel.

| Component | Responsibility |
| --- | --- |
| `NarrativeRunner` | Graph traversal, left/right and continue exit resolution, resource deltas, day counter, startup graph validation, ending/collapse detection |
| `ResourceState` / `ResourceWarningMonitor` | Clamped 0–100 resource values; 30% warning advisors; 0 = collapse |
| `AudienceFlowController` | Shared audience lifecycle for both petitions and free-form chats: defined speaker or generated commoner (using default commoner persona + model-supplied speaker name), kingdom state snapshot, proposal confirmation, and teardown |
| `AudienceSession` | Conversation turn history and transcript tracking so each request carries full context |
| `LlmReactionClient` | Sole HTTP gateway (`UnityWebRequest` coroutines): direct Groq (BYOK) or shared proxy, key validation, health probe, and request cancellation on restart |
| `PromptComposer` / `SpeakerPromptBuilder` / `LlmPromptTemplates` | System/user prompt assembly from persona seeds + recent kingdom history |
| `CollapseEndingBuilder` | State-driven epilogue for resource collapse (LLM-generated with authored fallback) |
| `LlmFallbackText` / `LlmTextSanitizer` | Authored fallback strings; output cleanup (strips `<think>` reasoning tags, stage directions, code fences, and stray non-Arabic glyphs in Arabic mode) |
| `LanguageManager` / `RtlTextHelper` | Active language singleton and event bus; bidirectional shaping for Arabic |

**Card node kinds** (driven by `CardData` flags):
- **Choice** — Two-way Left / Right branches with independent resource deltas and next-card links.
- **Reaction / Petition / Chat** — Single `continueNextCard` exit routed through the LLM flow (`isLlmReactionCard`, `isPetitionCard`, `isChatCard`).
- **Ending** — Terminal card (`IsEnding` when no outgoing branch is assigned).

---

## LLM Integration & API Key Setup

One gateway (`LlmReactionClient`), two pathways, both configurable from the in-game AI panel on first launch or anytime from the main/pause menu:

| Pathway | Request Path | Key Handling |
| --- | --- | --- |
| **Shared (default)** | Client → Cloudflare Worker → `api.groq.com/openai/v1/chat/completions` | Dev-owned key held as a Worker secret; never ships in the build. Enforces `POST`/`OPTIONS` and a 20 KB request body cap. |
| **BYOK (Bring Your Own Key)** | Client → `api.groq.com` directly | Player-pasted [Groq API key](https://console.groq.com/keys) (starts with `gsk_`; free tier suffices), validated against `/openai/v1/models` without spending tokens, stored locally in device `PlayerPrefs`. |

### Behavioral Contracts

- **Health Probe:** Probes the shared proxy with a 30 s timeout (`apiTimeoutSeconds` on `LlmSettings.asset`) and a ~60 s freshness window to drive the in-game status indicator; failures log exact diagnostics to the console.
- **Non-Load-Bearing Generation:** Timeouts, HTTP 429/5xx, missing/invalid keys, and unparseable replies all fall back cleanly to authored text for that turn.
- **Automatic Session Fallback:** An HTTP 401 on a stored BYOK key disables it for the session and transparently retries the request through the shared pathway.
- **Petitions & Spam Detection:** Petition turns parse into a `PetitionResolution` JSON schema (`phase`, `reaction`, `speakerName`, `resourceChanges`, `historyTag`, `isSpam`). Spam detection is governed by `DetectSpamMode` on `LlmSettings.asset` and is enabled by default (`ForceOn`).
- **Self-Hosting the Proxy:** Model selection (default `qwen/qwen3.8-27b`) and token/turn budgets live on `Assets/Game/Data/LlmSettings.asset`; the shared endpoint URL is the **Proxy URL** field on the scene's `LlmReactionClient`. To deploy your own Cloudflare Worker relay, see [`proxy/README.md`](proxy/README.md) for the `npx wrangler secret put GROQ_API_KEY` and `npx wrangler deploy` instructions.

---

## Localization (English & Arabic)

Weave is fully localized in **English** and **Arabic**:

- All UI labels, card descriptions, choice banners, speaker names, and fallback lines are authored bilingually via `LocalizedText` and `FallbackStrings`.
- Arabic text is shaped right-to-left at runtime using **RTLTMPro** through `RtlTextHelper` and `LocalizedLabel` (never assign `TMP_Text.text` directly).
- `FontSettings.asset` maps each language and text category to dedicated SDF fonts (*Cormorant Garamond* / *Felipa* for English; *Amiri* / *Noto Sans Arabic* with dynamic multi-atlas fallback for Arabic).
- LLM prompts dynamically inject language instructions (`LlmPromptTemplates.asset`), and `LlmTextSanitizer` strips leaked Latin/CJK characters from Arabic model outputs.

---

## Setup and Run Instructions

### Requirements

- **Unity `6000.0.79f1`** (via Unity Hub).
- **[Git LFS](https://git-lfs.com)** — required before cloning so art, audio, and font binaries are pulled properly.
- **Node.js + Wrangler** — optional, only needed if deploying changes to `proxy/`.

### Required Assets

The UI chrome sprites come from **[SteampunkUI](https://assetstore.unity.com/packages/2d/gui/steampunkui-244170)** by **Gentleland** (free on the Unity Asset Store, excluded from git under the standard Unity Asset Store EULA). After cloning, add **SteampunkUI** to your Unity account and import it via **Window → Package Manager → My Assets** into `Assets/Gentleland/` so all UI frames, buttons, and input fields in `Game.unity` resolve their sprites.

### Running in the Unity Editor

1. Clone the repository with Git LFS enabled:
   ```sh
   git lfs install
   git clone https://github.com/SARAELSAFTY/Weave.git
   ```
2. Open the cloned folder in **Unity Hub** with Editor `6000.0.79f1` and let Unity import packages (`RTLTMPro` resolves automatically via OpenUPM).
3. Import **SteampunkUI** by Gentleland from the Unity Asset Store into `Assets/Gentleland/`.
4. Open `Assets/Game/Scenes/Game.unity` and press **Play**.
5. On first run, the **AI Voice** panel appears: select the shared service or paste your own free [Groq API key](https://console.groq.com/keys) (`gsk_...`), then press **Continue**. (When offline, keep the shared option selected and press **Continue** to play with authored fallbacks. Use **Edit → Clear All PlayerPrefs** in the Editor to reset first-run state or clear a saved key.)

### Building for WebGL

Open **File → Build Profiles** (or **Build Settings**), select **WebGL**, and build. The custom `Assets/WebGLTemplates/WeaveResponsive` template (edge-to-edge container scaling for itch.io embeds) and 4x MSAA in URP are preconfigured.

---

## Licenses & Third-Party Notices

- **Project Code & Original Content:** Licensed under the [Apache License 2.0](LICENSE).
- **Music:** *"Breton"* by **Pufino** (sourced via [freetouse.com](https://freetouse.com/music) — free to use with creator attribution).
- **Arabic Text Shaping:** [RTLTMPro](https://github.com/pnarimani/RTLTMPro) by pnarimani (MIT License).
- **UI Chrome:** [SteampunkUI](https://assetstore.unity.com/packages/2d/gui/steampunkui-244170) by Gentleland (Standard Unity Asset Store EULA — not tracked in this repository; import into `Assets/Gentleland/` from the Asset Store).
- **Fonts:** *Amiri*, *Noto Sans Arabic*, *Cormorant Garamond*, and *Felipa* (SIL Open Font License 1.1 — see `OFL.txt` files alongside the font assets under `Assets/Game/Art/Fonts/`).
- **Icons:** HUD and utility icons from Pngtree and Flaticon.

