# Weave

A data-driven narrative engine and visual authoring toolset for Unity. Stories are built as ScriptableObject content in a node graph, with kingdom resources, AI character reactions, multi-turn petitions, and collapse epilogues powered by the [Groq](https://groq.com) API.

## Features

- **Card Graph editor** (`Weave > Card Graph`) — author cards, speakers, and resources in a GraphView canvas; create assets from the toolbar; set the starting card per database
- **Safe asset IDs** — changing `assetName` renames the `.asset` file; `DisplayName` is the optional player-facing label
- **Resources** — catalog-driven stats, per-choice deltas, threshold warnings with cooldowns, and per-resource collapse endings
- **LLM reaction cards** — in-character lines from persona, seed, and live game state
- **Petition cards** — multi-turn free-form audiences with structured resolutions and player confirmation
- **Collapse epilogues** — generated from run length, collapse cause, resources, and choice history
- **Game loop** — start screen, pause menu, day/resource HUD, swipe or keyboard choices
- **Editor tools** — JSON export, LLM tester, full test-story generator

## Getting Started

### Prerequisites

- Unity 6
- Input System
- TextMesh Pro
- [Groq API key](https://console.groq.com) (optional; LLM cards auto-advance without it)

### Setup

1. Open `Assets/Game/Scenes/Game.unity`.
2. Open **Weave > Card Graph** and select or create a `NarrativeDatabase`.
3. Assign the database and UI references on `GameManager`.
4. For LLM features: create or assign `LlmSettings` and `LlmPromptTemplates`, then set a Groq API key TextAsset on `LlmReactionClient`.
5. Enter Play Mode. Choose with swipe, touch, or **A/D** / arrow keys.

Optional: **Weave > Generate Full Test Story** creates a sample database under `Assets/_TestContent`.

## Authoring

| Task | How |
|------|-----|
| Create content | Card Graph toolbar: **+ New DB / Card / Speaker / Catalog / Resource** |
| Link cards | Connect Left, Right, or Continue ports |
| Starting card | Toolbar **Starting Card** dropdown |
| Reaction card | Enable **Is LLM Reaction Card**; wire continue; seed optional |
| Petition card | Enable **Is Petition Card**; wire continue; seed optional |
| Warnings | On `ResourceData`: threshold, speaker, seed, cooldown |
| Collapse ending | On `ResourceCatalog.collapseEndings`: resource → ending card |
| Export JSON | **Tools > Weave > Export Cards to JSON** |

### Naming

| Type | Convention | Example |
|------|------------|---------|
| Cards | `Scene_Speaker_Slug` | `Market_Advisor_WarnsBlight` |
| Speakers | `Spk_<Name>` | `Spk_Advisor` |
| Resources | `Res_<Name>` | `Res_Trust` |

Empty reaction or petition seeds use defaults from `LlmPromptTemplates`.

## Architecture

```
Assets/Game/Scripts/
├── GameManager.cs          # Run lifecycle and card / LLM orchestration
├── Definitions/            # Card, speaker, resource, database assets
├── Runtime/
│   ├── Llm/                # Groq client, prompts, petition session
│   └── Narrative/          # Runner, resources, warnings, history
├── UI/                     # Card, HUD, start, pause views
├── Input/                  # Choice input
└── Editor/                 # Card Graph, exporters, test tools
```

## License

Apache License 2.0. See [LICENSE](LICENSE).
