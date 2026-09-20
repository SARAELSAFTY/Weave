# Weave

Bilingual (English / Arabic) narrative card-strategy client built with **Unity `6000.0.79f1`** and **URP**.
The player issues rulings by swiping decision cards; selected cards can open real-time generative
dialogue (reactions, multi-turn petitions, free-form chat) backed by a Groq-compatible
chat-completions endpoint. All generated text is flavor-only: every LLM output has an authored
fallback, so the client is fully playable offline with no key and no network.

Web build (touch-screen supported): [itch.io](https://debo0.itch.io/weave).

---

## Tech stack

| Layer | Technology |
| --- | --- |
| Engine | Unity `6000.0.79f1`, Universal Render Pipeline (`com.unity.render-pipelines.universal` 17.0.4) |
| Input / UI | Unity Input System 1.19.0, uGUI, TextMeshPro |
| Arabic text | RTLTMPro 4.0.0 (OpenUPM) + per-language `FontSettings`, multi-atlas SDF fonts |
| Assemblies | `Game.Runtime` (ships), `Game.Editor` (Editor-only graph window + inspectors) |
| Data | Unity ScriptableObjects, checked in under `Assets/Game/Data/` |
| LLM backend | Groq OpenAI-compatible `chat/completions`; direct (BYOK) or via the Cloudflare Worker in `proxy/` |
| Proxy | Cloudflare Worker (`proxy/src/index.js`), deployed with Wrangler |
| Binary assets | Git LFS (art, audio, fonts) |

---

## Repository layout

```
Weave/
├── Assets/Game/
│   ├── Art/                        # Card art, portraits, fonts (LFS)
│   ├── Data/                       # Generated definition assets — authoritative content store
│   │   ├── NarrativeDatabase.asset # Graph: card/speaker refs, start card, editor layouts
│   │   ├── ResourceCatalog.asset   # Crown / Gold / Army wiring
│   │   ├── LlmSettings.asset       # Model, endpoint knobs, token/timeout budgets
│   │   ├── LlmPromptTemplates.asset# System/user prompt seeds per mechanic
│   │   ├── FontSettings.asset      # Per-language font assignments
│   │   ├── Cards/                  # 47 CardData assets (one per node)
│   │   ├── Speakers/               # 7 SpeakerData assets (persona prompts, portraits)
│   │   ├── Resources/              # 3 ResourceData assets (thresholds, collapse fallbacks)
│   │   └── CardVisual/             # CardVisualTemplate presets
│   ├── Scenes/Game.unity           # Single scene; composition root lives here
│   └── Scripts/                    # Game.Runtime + Game.Editor (see below)
│       ├── Definitions/            # CardData, SpeakerData, ResourceData, NarrativeDatabase,
│       │                           #   ResourceCatalog, CardVisualTemplate, ResourceChange
│       ├── Narrative/              # NarrativeRunner, ResourceState, ResourceWarningMonitor,
│       │                           #   PlayerHistoryTracker
│       ├── Llm/                    # LlmReactionClient, flow controllers, sessions,
│       │                           #   prompt builders, fallback text, sanitizer
│       ├── Localization/           # LanguageManager, RtlTextHelper, LocalizedText/Label,
│       │                           #   FontSettings, FallbackStrings
│       ├── UI/                     # CardView, HUD, start/pause views, BYOK panel
│       ├── Input/                  # Swipe + keyboard bindings (Input System)
│       ├── Music/                  # MusicController
│       ├── Editor/                 # Card Graph window, node types, custom inspectors
│       └── GameManager.cs          # Composition root
├── Assets/WebGLTemplates/WeaveResponsive/  # Container-filling WebGL template (do not delete)
├── proxy/                          # Cloudflare Worker: shared Groq relay (see proxy/README.md)
└── ProjectSettings/                # Unity project settings (Editor version pinned)
```

---

## Runtime architecture

`GameManager` (`Assets/Game/Scripts/GameManager.cs`) is the composition root. It wires scene
references (fail-fast via `InspectorValidation`), owns the run lifecycle (start → cards → warnings →
endings → restart), and routes input, pause, audience flows, and the AI settings panel.

| Component | Responsibility |
| --- | --- |
| `NarrativeRunner` | Graph traversal, choice resolution, resource deltas, day counter, startup graph validation, ending/collapse detection |
| `ResourceState` / `ResourceWarningMonitor` | Clamped 0–100 resource values; 30% warning advisors; 0 = collapse |
| `AudienceFlowController` | Shared audience lifecycle: defined speaker or generated commoner (null speaker + default persona + model-supplied name string), kingdom snapshot, teardown |
| `AudienceSession` | Turn history so each request carries full conversation context |
| `LlmReactionClient` | Sole HTTP gateway (`UnityWebRequest` coroutines): direct Groq (BYOK) or shared proxy, key validation, health probe |
| `PromptComposer` / `SpeakerPromptBuilder` / `LlmPromptTemplates` | System/user prompt assembly from persona seeds + kingdom state |
| `CollapseEndingBuilder` | State-driven epilogue for resource collapse (generated, with authored fallback) |
| `LlmFallbackText` / `LlmTextSanitizer` | Authored fallback strings; output cleanup (reasoning tags, stage directions, code fences, non-Arabic glyphs in Arabic mode) |
| `LanguageManager` / `RtlTextHelper` | Language state; correct bidirectional shaping for Arabic |

Card node kinds (all driven by `CardData` flags): **choice** (L/R branches), **verdict**
(three-way L/M/R), **reaction** / **petition** / **chat** (single `continueNextCard` exit into the
LLM flow), **ending** (terminal, no exits), **evaluator** (state-driven router over
resource values + story flags to one of five terminal endings).

Controls: drag/swipe or `A`/`D` (arrows) for left/right, swipe-down or `S`/`↓` for the middle
verdict option, `Esc` for pause. The language toggle in menus switches English ↔ Arabic
(`F10` in the Editor).

---

## LLM integration

One gateway, two pathways, both configured from the in-game AI panel:

| Pathway | Request path | Key handling |
| --- | --- | --- |
| **Shared (default)** | Client → Cloudflare Worker → `api.groq.com/openai/v1/chat/completions` | Dev-owned key held as a Worker secret; never ships in the build. Rate-limited, 20 KB body cap. |
| **BYOK** | Client → `api.groq.com` directly | Player-pasted [Groq key](https://console.groq.com/keys) (free tier suffices), validated against the `/models` endpoint without spending tokens, stored only in device `PlayerPrefs`. |

Behavioral contracts:

- Health probe with short timeout (~10 s) and ~60 s freshness window drives the panel status line; failures log the exact cause to the console.
- Generation is non-load-bearing: timeouts, HTTP 429/5xx, missing/invalid keys, and unparseable replies all fall back to authored text for the same turn.
- HTTP 401 on a stored key disables it for the session and retries the request through the shared pathway.
- Petitions take typed player replies; each turn parses into a `PetitionResolution` (proposal + spam flag), and turns-exhausted is evaluated before the proposal branch so the audience closes correctly on the final turn.
- Spam detection is governed by `DetectSpamMode` on the `LlmSettings` asset (`Auto` enables it only when the player supplies their own key); spam turns never consume the turn budget.
- Model selection (default `qwen/qwen3.8-27b`) and token/turn budgets live on the `LlmSettings` asset; the shared endpoint URL is the **Proxy URL** field on the scene's `LlmReactionClient`.
- To self-host the shared path, deploy `proxy/` and point **Proxy URL** at your deployment. See [`proxy/README.md`](proxy/README.md) for the `wrangler secret put GROQ_API_KEY` / `wrangler deploy` flow and the unauthenticated-URL quota trade-off.

---

## Setup and builds

### Requirements

- Unity `6000.0.79f1` via Unity Hub.
- [Git LFS](https://git-lfs.com) — required before clone for art/audio/fonts.
- Node + Wrangler — only for `proxy/`.

### First launch

1. Clone (with LFS), open the project in Unity Hub, let it compile and resolve packages (RTLTMPro installs automatically).
2. Open `Assets/Game/Scenes/Game.unity`, press Play.
3. The AI settings panel appears on first run: continue offline, use the shared connection, or paste your own Groq key. Reopen anytime from the start-screen AI button or the pause menu.

### WebGL build

Target WebGL in **File → Build Settings** and build. The `Assets/WebGLTemplates/WeaveResponsive`
template (container-filling layout) and anti-aliasing are preconfigured.

---

## Developer conventions

| Topic | Rule |
| --- | --- |
| Arabic text | Route all writes through `RtlTextHelper` / `LocalizedLabel`; never assign `TMP_Text.text` directly. |
| Font assets | Ignore `*.SDF.asset` churn — Unity regenerates them while the editor is open. |
| Missing refs | Required scene references fail fast in the console at startup; fix there, not downstream. |
| Fresh state | **Edit → Clear All PlayerPrefs** wipes a stored BYOK key. |
| Assemblies | Runtime ships `Game.Runtime`; `Game.Editor` is Editor-only and never builds into players. |
| Vendored UI | `Assets/Gentleland` is third-party (Asset Store EULA) — do not edit. |
| Content edits | Use the **Weave → Card Graph** window and definition inspectors; content assets are final and checked in. |

---

## Credits and licenses

- **Project code and content**: [Apache License 2.0](LICENSE).
- **Steampunk UI Pack** (Gentleland, Unity Asset Store EULA) — vendored under `Assets/Gentleland`.
- **RTLTMPro** (Nosuch Studios, Apache-2.0) — bidirectional Arabic shaping.
- **Fonts** — *Amiri*, *Noto Sans Arabic*, *Cormorant Garamond*, *Felipa* (SIL Open Font License); see `OFL.txt` files alongside the font assets.
