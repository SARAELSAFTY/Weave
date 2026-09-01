# Weave — Developer Onboarding

Weave is a bilingual (English / Arabic) narrative card game built on Unity 6
(6000.0.79f1, URP 2D). The project uses a single scene
(`Assets/Game/Scenes/Game.unity`), ScriptableObject content authored through a custom
graph editor, and Groq-hosted LLM features behind a Cloudflare Worker proxy (`proxy/`,
API key server-side). This guide covers the project-specific setup and architecture, for
developers already familiar with Unity and C#.

The repository ships no story content. The content slots in the scene are unassigned and
must be filled as described in section 1.

## 1. Setup

### 1.1 Create the story content — Weave → Card Graph

1. **+ New DB** — creates the story database (`Assets/Game/Data/NarrativeDatabase_001.asset`).
2. **+ New Catalog** — creates the resource catalog and assigns it to the database.
3. **+ New Resource** (one per kingdom resource) — icon, starting value, collapse
   threshold, warning settings (threshold %, warning speaker, cooldown in cards).
4. **+ New Speaker** (one per character) — portrait, localized name, LLM persona prompt.
   A character without a persona cannot produce LLM reactions or warnings.
5. **+ New Card** — the first card created becomes the Starting Card. Author text and
   choices in the Inspector, then connect cards by dragging between ports.
6. Confirm the **Starting Card** dropdown points at the intended opening card.

### 1.2 Assign the scene references

Scene-internal references (views, input, canvas) are already wired. If a required asset
reference is missing, the component logs `Missing required Inspector reference …` at
startup and disables itself.

| Field | Value |
| --- | --- |
| `GameManager.narrativeDatabase` | The story database |
| `ResourceState.catalog` | The resource catalog |
| `GameManager.llmSettings` and `LlmReactionClient.settings` | An `LlmSettings` asset (*Create → Weave → LLM Settings*). Optional: without it, LLM features fall back to pre-written text. |
| `NarrativeDatabase.promptTemplates` | An `LlmPromptTemplates` asset (*Create → Weave → LLM Prompt Templates*) |
| `ResourceDisplay.labels` | One entry per resource: HUD label + icon |
| `LanguageManager.fontSettings` | Optional: loads automatically from `Resources` when unassigned |

`LlmReactionClient.proxyUrl` is already set to the shared development Worker.

### 1.3 Run

The game starts at the start screen; Play begins the run at the starting card.

| Startup error | Resolution |
| --- | --- |
| `Missing required Inspector reference …` | Assign the named field (section 1.2) |
| `NarrativeRunner` broken branch | A branching card is missing one link (section 3) |

If the proxy is unavailable, every LLM feature falls back to pre-written localized lines
(`FallbackStrings`). F10 (Editor only) switches the language at runtime; Esc pauses.

## 2. Architecture

| Component | Kind | Responsibility |
| --- | --- | --- |
| `GameManager` | MonoBehaviour | Orchestration: input, pause, card presentation, petition flow, warnings, endings, restart (scene reload) |
| `NarrativeRunner` | plain C# | Choice resolution, resource changes, day counter, story-graph validation at startup, ending and collapse detection |
| `ResourceState` | MonoBehaviour | Current resource values; raises `Changed` after each apply |
| `ResourceWarningMonitor` | plain C# | Warning checks against `warningThresholdPercent` with per-resource cooldowns; requires a warning speaker with a persona |
| `PlayerHistoryTracker` | plain C# | Records choices and petition transcripts; builds the kingdom summary included in every prompt |
| `PetitionSession` | plain C# | Per-petition state: turn budget, full message history (resent with each turn), pending proposal |
| `LlmReactionClient` | MonoBehaviour | HTTP communication with the proxy, response parsing and sanitization; error types `NotConfigured`, `RateLimited`, `NetworkError`, `EmptyResponse` |
| `CardView` + HUD views | UI | Card rendering for all states (normal / reaction / petition / ending), petition input, choice animation |

Input:

- `PlayerChoiceInput` polls keyboard, mouse, and touch directly; there are no gameplay
  action maps.
- Dragging beyond `swipeThreshold` commits a choice; `A`/`←` and `D`/`→` are the keyboard
  equivalents. Keyboard input is suspended while the petition text field is focused.

ScriptableObjects created at runtime (warning cards, collapse placeholders, generated
commoners) are created with `ScriptableObject.CreateInstance` and destroyed by
`GameManager`. Follow the same pattern for new runtime assets.

## 3. Content model

All content assets derive identity from **`NamedGameAsset`**:

- `assetName` — the stable author-facing ID. Renaming it renames the `.asset` file.
- `displayNameLocalized` — the player-facing name per language; falls back to `assetName`.

| Asset | Purpose |
| --- | --- |
| `NarrativeDatabase` | Story root: starting card, cards, speakers, `resourceCatalog`, `promptTemplates` |
| `CardData` | Speaker, localized text, day advance, left/right choices (text + resource change + next card), LLM options, continue card, art |
| `SpeakerData` | Portrait, localized name, LLM persona prompt |
| `ResourceData` | Icon, starting value, collapse threshold, warning threshold % / speaker / cooldown |
| `ResourceCatalog` | Resource list and one collapse-ending card per resource |
| `CardVisualTemplate` | Reusable background + border pair (six provided in `Content/CardTemplates/`) |
| `LlmSettings` | Model and all LLM tunables |
| `LlmPromptTemplates` | All static prompt text |
| `FontSettings` | Per-language fonts and category overrides; must reside under a `Resources/` folder (section 6) |

Each card is exactly one type. Switching a card between LLM reaction and petition clears
its hand-authored text by design.

| Type | Configuration | Behavior |
| --- | --- | --- |
| Branching | Left/right choices with links | Applies the chosen resource change and follows the chosen link |
| LLM reaction | `isLlmReactionCard` + `continueNextCard` | The speaker delivers one generated line, then the story continues |
| Petition | `isPetitionCard` + `continueNextCard` | Multi-turn audience with the card's speaker or a generated commoner; ends on a confirmed proposal or exhausted turns |
| Ending | No outgoing links | Ends the run; restart screen |

Startup validation — `NarrativeRunner.StartRun` refuses to start unless:

- Every branching card links both sides or neither (an ending). A single missing link
  aborts startup and names the card.
- The database has a starting card.

Resource mechanics:

- Choices and confirmed petition proposals change resource values.
- A value in the warning zone triggers a generated reaction from the warning speaker,
  subject to its cooldown.
- A value at `collapseThreshold` ends the run: the LLM generates a personalized epilogue
  from the reign record; on failure, the catalog's authored fallback ending is shown.

## 4. Card Graph editor

- **Create:** toolbar buttons (`+ New DB / Card / Speaker / Catalog / Resource`) or the
  graph background context menu.
- **Connect:** card nodes have an input port and either left/right outputs (branching) or
  a single Continue port (LLM / petition). Dragging from an output into another card's
  input assigns `leftNextCard` / `rightNextCard` / `continueNextCard`.
- **Collapse endings:** each resource node exposes a red **Collapse Ending** port;
  connecting it to a card sets that resource's collapse ending in the catalog.
- **Starting card:** toolbar dropdown or the node context menu (*Set as Starting Card*).

## 5. LLM pipeline

### 5.1 Prompt composition

`SpeakerPromptBuilder` composes each request from the `LlmPromptTemplates` sections:

| Request | Sections, in send order |
| --- | --- |
| Reaction / warning | Instructions → `[Language Requirement]` → `[Persona]` → `[Situation]` (seed) → `[State]` (kingdom summary) → `[Terminology]` (Arabic only) |
| Petition | Same, ending with `[Resources]` — valid resource names, allowed ±`petitionResourceClampMagnitude` range, Arabic terminology |
| Epilogue | Chronicler instructions → language → reign record (length, collapse cause, final state, full history) |

- Single-turn requests append the fixed user message
  `"Respond to the situation above."` — the only prompt string outside the templates
  asset.
- Per-card seed overrides take precedence over template defaults. The only placeholder is
  `{resourceName}` in the warning seed.

### 5.2 Petition JSON contract

Petition turns run with `response_format: json_object` and must return:

```json
{ "phase": "deliberating|proposal",
  "reaction": "…",
  "resourceChanges": [ { "resource": "<ResourceData assetName>", "delta": 5 } ],
  "historyTag": "short_snake_case" }
```

`LlmPromptTemplates.petitionSystemInstructions` and the `PetitionResolution` class define
this contract jointly — changing one without the other silently breaks parsing.

Application is fail-closed (`PetitionResolutionApplier`):

- Resource changes apply only on an explicit `proposal` phase.
- Resource names must match a catalog entry (case-insensitive) or the change is dropped.
- Deltas are clamped to ±`petitionResourceClampMagnitude`.

### 5.3 Sanitization and tunables

- Responses are stripped of reasoning blocks, leaked JSON, and stage directions. Arabic
  output additionally passes `LlmTextSanitizer` (Arabic script only, repairs broken
  letter joins).
- All tunables are in `LlmSettings`: model, temperature, token caps, prompt history
  depths, petition turn-limit mode (Fixed / RandomRange), clamp magnitude, retry
  cooldown, HTTP timeout.
- HTTP 429 surfaces a localized rate-limit message; the submit button re-enables after a
  cooldown.

## 6. Localization

- **`LanguageManager`** — singleton bootstrapped automatically at startup
  (`DontDestroyOnLoad`). Owns the current language (defaults to English) and raises
  `LanguageChanged`; localized views subscribe in `OnEnable`. The F10 editor toggle lives
  here.
- **Story content** uses the `LocalizedText` struct (`english` / `arabic`). Fallback is
  one-way: missing Arabic falls back to English.
- **UI text** uses `LocalizedLabel`, rendered through `RtlTextHelper`; Arabic shaping is
  handled by RTLTMPro. The petition input field is replaced with `RTLTextMeshPro` at
  runtime by design.
- **`FontSettings`** — per-language fonts plus category overrides (Title, Speaker Name,
  Menu UI, Dialogue Body, Choice). It resolves via `Resources.Load`, so the active asset
  must reside under a `Resources/` folder (currently
  `Assets/Game/Resources/Fonts/FontSettings.asset`).
- **LLM output** — the templates' language instructions specify the response language;
  Arabic output is then sanitized as above.
- **`FallbackStrings`** — pre-written bilingual lines for every LLM fallback path.
