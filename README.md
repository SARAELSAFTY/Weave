# Weave

Weave is a bilingual (English / Arabic) narrative card game built in Unity 6. You rule a kingdom one audience at a time: swipe to decide, watch the kingdom's resources react, and face courtiers who are voiced — in real time — by an LLM.

Content is authored as ScriptableObject assets and edited through a dedicated graph tool. The runtime supports branching choices, resource consequences, LLM-generated character reactions, free-form multi-turn petitions, resource warnings, collapse endings, and generated epilogues.

## Features

- **Swipe-based narrative cards** — drag, touch, or keyboard choices with code-driven card and choice-card animation
- **Kingdom resources** — every choice shifts resources; low resources trigger in-character LLM warnings; collapse ends the run
- **LLM character reactions** — speakers react to your decisions in persona, voiced through a Groq model behind a secure proxy
- **Free-form petitions** — type commands to a petitioner across multiple turns; negotiate until a proposal is made, then confirm or press on (with a spam-tolerance meter)
- **Collapse epilogues** — when a resource collapses, a chronicler summarizes your reign from your actual decision history, with authored fallback cards
- **Full English/Arabic localization** — RTL text shaping, per-language fonts, localized authored content and UI, and Arabic-sanitized LLM output
- **Authoring tooling** — visual Card Graph editor, custom inspectors, and an LLM prompt tester

## Requirements

| Dependency | Version / Notes |
| --- | --- |
| Unity | **6000.0.79f1** (Unity 6) |
| Render pipeline | Universal Render Pipeline |
| Input | Unity Input System (device polling: mouse, touch, keyboard) |
| Text | TextMesh Pro + [RTLTMPro](https://openupm.com/packages/com.nosuchstudio.rtltmpro/) 4.0.0 (OpenUPM, already configured in `Packages/manifest.json`) |
| LLM backend | A deployed copy of the Cloudflare Worker in [`/proxy`](proxy) |
| UI assets | Gentleland Steampunk UI pack (bundled in `Assets/Gentleland`) |

The game remains playable without a configured proxy: authored cards and all resource logic work normally, and every LLM-dependent surface falls back to authored or canned text.

## Getting Started

1. Open the repository folder in **Unity Hub** (Unity 6000.0.79f1).
2. Open [`Assets/Game/Scenes/Game.unity`](Assets/Game/Scenes/Game.unity).
3. Assign your `NarrativeDatabase` to the Game Manager, then enter Play Mode. The runner validates the graph on startup and refuses to start on broken wiring, so authoring mistakes surface immediately.

Story content is authored as ScriptableObjects (see [Content model](#content-model)) and edited through the **Weave → Card Graph** window. The repository ships with card art and visual templates but no bundled story; create your own database under `Assets/Game/Data`.

### Controls

| Input | Action |
| --- | --- |
| Drag / swipe the card (mouse or touch) | Choose left or right |
| `A` / `←` — `D` / `→` | Choose left / right |
| `Esc` | Pause / resume |
| Language toggle button (main menu & in-game) | Switch English ↔ Arabic |
| `F10` (Editor only) | Dev hotkey to flip the language |

## How It Works

### Runtime architecture

```
PlayerChoiceInput ──► GameManager ──► NarrativeRunner (card progression, day, endings)
                          │           ResourceState (values) ◄── card & petition changes
                          │           ResourceWarningMonitor (thresholds, cooldowns)
                          │           PlayerHistoryTracker (choices, transcripts, snapshots)
                          ▼
                        CardView / HUD views (localized, RTL-aware)
                          │
                          ▼
                    LlmReactionClient ──HTTP──► Cloudflare Worker proxy ──► Groq
```

- `GameManager` orchestrates the run: input gating, pause, card presentation, petition flow, warnings, endings, and restart (scene reload).
- `NarrativeRunner` owns card progression and day count, applies resource changes, and detects terminal and collapse outcomes.
- `PetitionSession` holds per-audience conversation state: message history, spam-dot budget, and any proposal awaiting confirmation. Resource deltas from the model are applied by `PetitionResolutionApplier`, which fails closed — only an explicit `proposal` phase can move resources.
- `PlayerHistoryTracker` records decisions and petition transcripts and builds the game-state snapshots injected into prompts.
- `LanguageManager` is an auto-bootstrapped, scene-persistent singleton that owns the current language; every localized view refreshes on change.

### Content model

`NarrativeDatabase` is the root asset of a run: starting card, card/speaker collections, resource catalog, prompt templates, and graph layout metadata.

| Asset | Purpose |
| --- | --- |
| `CardData` | Story text, localized choices, resource changes, routing, card art (portrait / illustration modes, per-card image, visual template), and optional LLM behavior |
| `CardVisualTemplate` | Reusable background + border pair that cards can pick instead of individual sprites |
| `SpeakerData` | Portrait, localized display name, and the LLM persona prompt (optional per card — speaker-less cards render as event cards) |
| `ResourceData` | Starting value, warning threshold/speaker/cooldown, collapse threshold, HUD icon, localized name |
| `ResourceCatalog` | Resource list + authored fallback ending card per collapse condition |
| `LlmPromptTemplates` | All shared system instructions, language requirements, seed prompts, and the single-turn user message (single source of truth, fully Inspector-editable) |
| `LlmSettings` | Model, temperature, token limits, history depths, petition budgets, timeouts |

All content assets share `NamedGameAsset` identity: an author-facing **Asset Name (ID)** and a player-facing **Display Name**, plus a localized display name. Editing the Asset Name auto-renames the `.asset` file.

### Card types

| Type | Configuration | Runtime behavior |
| --- | --- | --- |
| Branching card | Left/right choices + outgoing links | Applies the chosen resource change, follows the chosen link |
| LLM reaction card | `Is LLM Reaction Card` + `Continue Next Card` | Requests one in-character line from the speaker (card seed override or shared default), then continues |
| Petition card | `Is Petition Card` + `Continue Next Card` | Opens a multi-turn audience: opening announcement, free-form turns, spam-dot budget, proposal confirmation; continues when resolved or exhausted |
| Ending card | No outgoing links | Ends the run and shows the restart screen |

### Resources, warnings, and collapse

- Choices and confirmed petition proposals modify resource values.
- **Warning**: when a resource drops to `warningThresholdPercent` of its starting value (and its cooldown has elapsed), its warning speaker delivers an LLM reaction.
- **Collapse**: when a resource reaches `collapseThreshold`, the run ends. Weave generates a personalized epilogue from the reign length, final kingdom state, and full decision history; if generation fails, the catalog's authored fallback ending card is shown.

## LLM Integration

All requests flow through `LlmReactionClient` → the Cloudflare Worker proxy in [`/proxy`](proxy) → Groq.

- **Secret isolation** — the client (desktop or WebGL) never holds the Groq API key; the proxy attaches `GROQ_API_KEY` server-side.
- **Abuse protection** — the proxy enforces `POST`-only, body-size limits, and CORS.
- **Prompt pipeline** — every static prompt fragment (system instructions, language requirements, the single-turn user message) lives Inspector-editable in `LlmPromptTemplates`; `SpeakerPromptBuilder` composes each system prompt from those blocks: system instructions → language requirement → persona → situation (seed) → kingdom state snapshot → resources (valid names, delta range, Arabic terminology). Per-card seed overrides take priority over the defaults. Prompts only say what to *say* — mechanical enforcement (delta clamping, resource-name matching, fail-closed phases, Arabic-script cleanup) lives in code.
- **JSON contract** — petition turns run in Groq JSON mode and must return `{ phase, reaction, resourceChanges, historyTag, isSpam }` matching `PetitionResolution`. The field names in `LlmPromptTemplates.petitionSystemInstructions` and the C# class must stay in sync.
- **Response hygiene** — responses are stripped of `<think>` blocks, leaked JSON, and stage directions; Arabic responses pass `LlmTextSanitizer` so only Arabic script survives.
- **Graceful degradation** — every LLM surface (reactions, warnings, petition openings/turns, epilogues) has a localized fallback for missing clients, network errors, rate limits (HTTP 429 → retry cooldown), and empty responses.

Use **Weave → LLM Tester** to compose the exact prompt GameManager would send for any card, speaker, resource, or epilogue — and optionally fire it live in Play Mode.

## Localization

- **Languages**: English and Arabic, switchable at runtime; authored content uses the `LocalizedText` struct (`english` / `arabic`), UI text uses the `LocalizedLabel` component.
- **RTL**: Arabic is shaped and rendered right-to-left via RTLTMPro; `RtlTextHelper` centralizes font, alignment, and shaping rules.
- **Fonts**: `FontSettings` (auto-loaded from `Assets/Game/Resources`) holds per-language default fonts plus optional category overrides (Title, Speaker Name, Menu UI, Dialogue Body, Choice). Amiri (Arabic) and Cormorant Garamond (English) are bundled under `Assets/Game/Resources/Fonts`.
- **LLM output**: prompts instruct the model to answer in the active language; Arabic output is sanitized to Arabic script.
- **Fallbacks**: `FallbackStrings` carries canned bilingual lines for every degraded LLM path.

## Authoring Workflow

Open **Weave → Card Graph**, then select or create a `NarrativeDatabase`:

1. Create cards, speakers, a resource catalog, and resources from the toolbar or the right-click menu.
2. Set the database's **Starting Card**.
3. Connect cards through their ports: left/right for branching cards, `Continue` for LLM/petition cards. Resource→Collapse-Ending edges visualize fallback endings.
4. Assign a speaker where a card should speak — cards without one render as speaker-less event cards. Branching cards still need both outgoing links, or the runner refuses to start.
5. Author resource changes per choice, warning settings per resource, and a collapse ending entry per collapsible resource.
6. Optionally set per-card **Reaction/Petition Seed Overrides**; empty fields fall back to the `LlmPromptTemplates` defaults.
7. Test prompts in **Weave → LLM Tester**, then Play Mode.

## Editor Tools

| Tool | Purpose |
| --- | --- |
| **Weave → Card Graph** | Visual graph editor for cards, speakers, and resources (create, connect, delete, layout persistence) |
| **Weave → LLM Tester** | Compose and send real prompts against your proxy without a full run |
| Custom inspectors | Card/Speaker/Resource editors with authoring guidance; `LlmSettings` model picker; `LocalizedLabel` preview buttons |

## Project Layout

```text
/
├── Assets/
│   ├── Game/
│   │   ├── Art/Cards/              Card art sets (composites, borders, patterns)
│   │   ├── Art/Characters/         Speaker portrait art
│   │   ├── Content/CardTemplates/  Reusable CardVisualTemplate assets
│   │   ├── Data/                   Authoring destination folders (Cards, Speakers, Resources)
│   │   ├── Resources/              FontSettings.asset + bundled fonts (auto-loaded)
│   │   ├── Scenes/Game.unity       Main scene
│   │   └── Scripts/
│   │       ├── Definitions/        ScriptableObject content types
│   │       ├── Editor/             Card Graph editor, inspectors, and prompt tester
│   │       ├── Input/              Player choice input (drag, touch, keyboard)
│   │       ├── Localization/       Language manager, RTL helper, fonts, fallback strings
│   │       ├── Runtime/Llm/        Proxy client, prompt builders, petitions, settings
│   │       ├── Runtime/Narrative/  Runner, resources, warnings, history
│   │       └── UI/                 Card view, HUD, menus, choice-card animation
│   └── Gentleland/                 Steampunk UI asset pack (third-party)
├── proxy/                          Cloudflare Worker LLM proxy (Wrangler)
└── README.md
```

## Building for the Web

The project's WebGL player settings are configured for itch.io hosting. Build via **File → Build Settings → WebGL**; deploy the output folder to itch.io as a zip. The game talks to your proxy URL (set on the `LlmReactionClient` component in the scene), so no API secret ships with the build.

## Third-Party Credits

- [Gentleland Steampunk UI](https://assetstore.unity.com/packages/2d/gui/icons/steampunk-ui-230858) — UI art and components
- [RTLTMPro](https://github.com/Nosuchstudios/RTLTMPro) — Persian/Arabic RTL text shaping for TextMesh Pro
- [Amiri](https://www.amirifont.org/) and [Cormorant Garamond](https://github.com/CatharsisFonts/Cormorant) fonts — SIL Open Font License
- Groq — LLM inference behind the proxy

## License

Licensed under the Apache License, Version 2.0. See [LICENSE](LICENSE).
