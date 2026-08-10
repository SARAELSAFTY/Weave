# Weave

A data-driven narrative engine and visual node authoring toolset for Unity. Stories are authored visually in a graph editor with dynamic kingdom resources, procedural warning reactions, and live character dialogue generated via the [Groq](https://groq.com) LLM API.

---

## Overview

Weave strictly separates narrative content from code. Stories, characters, and kingdom resources are ScriptableObject assets authored and linked inside a visual **Card Graph**. 

Renaming or reorganizing content is completely safe — asset IDs and disk filenames (`.asset`) are kept in sync automatically. At runtime, player choices drive resource state changes, trigger AI character reactions, monitor resource collapse conditions, and generate personalized loss epilogues summarizing each run.

---

## Key Features

### Visual Card Graph Editor (`Weave > Card Graph`)
- **Node-Based Authoring**: Built with Unity UIElements and GraphView. Connect card nodes, speaker nodes, and resource nodes visually.
- **In-Graph Toolbar & Inspector**: Create databases (`+ New DB`), cards (`+ New Card`), speakers (`+ New Speaker`), resource catalogs (`+ New Catalog`), and resources (`+ New Resource`) directly from the graph toolbar or context menu.
- **Live Node Property Editing**: Edit card text, day advances, speaker assignments, choice labels, resource deltas, and LLM prompts directly inside graph nodes.
- **Starting Card Selector**: Set and switch the entry point card for any narrative database via toolbar dropdown.

### Automatic Asset Synchronization
- **Safe File Renaming**: Modifying the `assetName` field on any `CardData`, `SpeakerData`, or `ResourceData` automatically renames the underlying `.asset` file on disk via `AssetDatabase`.
- **Reference Integrity**: Renames update internal identifiers seamlessly without breaking node graph connections or database references.
- **Live Graph Refresh**: Open graph windows auto-refresh when assets are renamed or created.

### Kingdom Resources & Warning System
- **Extensible Resource System**: Non-hardcoded resource stats managed via `ResourceCatalog` and `ResourceState`.
- **Per-Choice Deltas**: Each choice branch applies customizable positive or negative resource changes (`ResourceChange`) on swipe.
- **Procedural Resource Warnings (`ResourceWarningMonitor`)**: Monitors resource levels against customizable warning thresholds (`warningThresholdPercent`, e.g. 30%). When crossed, a designated warning speaker interrupts narrative flow with a character reaction card.
- **Warning Cooldowns**: Configurable card cooldowns (`warningCooldownCards`) prevent repeated warning spam.
- **Resource Collapse Endings**: When a resource drops to 0 or below, it triggers an immediate kingdom collapse ending card (`collapseEndingCard`).

### Live AI Dialogue & Epilogues (Groq LLM Integration)
- **LLM Reaction Cards**: Cards marked with `isLlmReactionCard` dynamically query the Groq LLM API (`LlmReactionClient`) at runtime to generate character dialogue based on `llmPromptSeed`, speaker `llmPersonaPrompt`, and live game state snapshots.
- **Personalized Loss Epilogues**: On collapse game overs, `EpiloguePromptBuilder` compiles full player choice history, final resource states, collapse cause, and days survived into a Groq prompt to write a unique epilogue detailing the kingdom's downfall.
- **Configurable LLM Parameters**: Tune model selection, temperature, timeout, and max tokens using the `LlmSettings` asset and `LlmPromptTemplates`.
- **Graceful Offline Fallback**: If an API key or network connection is missing, reaction cards auto-advance safely without crashing the game.

### One-Click JSON Export Utility
- **Export to JSON**: Access **Tools > Weave > Export Cards to JSON** to serialize all cards, speakers, and resources in the active `NarrativeDatabase` into `Assets/StreamingAssets/cards.json` for external tooling, web previews, or alternative runtimes.

### Complete Game Loop & UI Controls
- **Start Screen (`StartScreenView`)**: Clean title screen with Play and Quit options.
- **Pause Menu (`PauseMenuView`)**: Overlay menu toggled via the `Escape` key with Resume and Quit controls.
- **HUD Displays**: In-game `DayDisplay` for tracking survival progression and `ResourceDisplay` showing dynamic resource bars and icons.
- **Run History Tracker (`PlayerHistoryTracker`)**: Records player decisions, choice strings, and resource snapshots throughout a run for AI context and epilogue generation.
- **Flexible Controls**: Make choices using mouse drag, touch swipe, or keyboard shortcuts (**A/D** or **Left/Right Arrow**).

---

## Getting Started

### Prerequisites

- Unity 6 (or Unity 2022.3 LTS+)
- Input System package (`com.unity.inputsystem`)
- TextMeshPro package
- A [Groq API Key](https://console.groq.com) *(optional for offline play)*

### Setup & First Run

1. **Open Scene**: Open `Assets/Game/Scenes/Game.unity`.
2. **Open Card Graph**: Select **Weave > Card Graph** from the top menu bar.
3. **Load or Create Database**: Select an existing `NarrativeDatabase` asset or click **+ New DB** to generate a new database in `Assets/Game/Data`.
4. **Configure LLM Settings**:
   - Create an `LlmSettings` asset via **Assets > Create > Weave > LLM Settings**.
   - Configure model ID (e.g. `llama-3.3-70b-versatile`), temperature, timeout, and epilogue max tokens.
5. **Configure API Key**:
   - Create a text asset containing your Groq API key.
   - On the `LlmReactionClient` component in the scene, assign your `LlmSettings` asset and your API key TextAsset. *(If left empty, LLM cards will auto-advance).*
6. **Verify GameManager References**: Ensure the `GameManager` object in `Game.unity` has `CardView`, `ResourceState`, `NarrativeDatabase`, `DayDisplay`, `StartScreenView`, `PauseMenuView`, and `LlmReactionClient` assigned.
7. **Press Play**: Swipe left/right with the mouse, use touch drag, or press **A/D** / **Left/Right Arrow** keys to make choices.

---

## Authoring Content Guide

All story authoring takes place inside **Weave > Card Graph**.

### Naming Conventions

To keep assets organized, Weave enforces clean asset naming conventions:
- **Cards**: `Scene_Speaker_Slug` (e.g. `Market_Advisor_WarnsBlight`)
- **Speakers**: `Spk_Name` (e.g. `Spk_Advisor`, `Spk_King`)
- **Resources**: `Res_Name` (e.g. `Res_Treasury`, `Res_Food`)

> **Note**: `AssetName` is the authoring/file identifier. `DisplayName` is the optional player-facing label shown in the UI. If `DisplayName` is empty, the UI falls back to `AssetName`.

### Authoring Workflows

| Task | Steps |
|---|---|
| **Create a Database** | Toolbar **+ New DB** → Choose location in `Assets/Game/Data` |
| **Create a Card** | Toolbar **+ New Card** or right-click canvas → **Create Card** |
| **Connect Cards** | Drag from a card's Left/Right/Continue output port to the next card's input port |
| **Set Starting Card** | Select starting card in toolbar **Starting Card** dropdown |
| **Create a Speaker** | Toolbar **+ New Speaker** or right-click canvas → **Create Speaker** |
| **Create a Resource** | Toolbar **+ New Resource** or right-click canvas → **Create Resource** *(requires a Resource Catalog)* |
| **Create a Catalog** | Toolbar **+ New Catalog** |
| **Set Resource Changes** | On a Card Node, add items to **Left Resource Changes** / **Right Resource Changes** |
| **Author AI Reaction Card** | Enable **Is Llm Reaction Card** on the card node; specify **Llm Prompt Seed** and connect **Continue Next Card** |
| **Configure AI Speaker Persona** | Select a Speaker Node; enter **Llm Persona Prompt** (identity, tone, behavior rules) |
| **Configure Resource Warnings** | On a Resource asset/node, set `warningThresholdPercent`, assign `warningSpeaker`, and enter `warningSeedPrompt` |
| **Configure Collapse Ending** | On a Resource asset/node, assign a `collapseEndingCard` to trigger when the stat reaches 0 |
| **Export to JSON** | Select **Tools > Weave > Export Cards to JSON** from the top menu |

---

## Project Architecture

```
Assets/Game/Scripts/
├── Definitions/             # ScriptableObject data schemas
│   ├── CardData.cs          # Narrative card schema, branches, day advance, LLM flags
│   ├── SpeakerData.cs       # Character speaker schema, portraits, persona prompts
│   ├── ResourceData.cs      # Resource stat schema, warning thresholds, collapse cards
│   ├── ResourceChange.cs    # Delta struct for per-choice resource modifications
│   └── NarrativeDatabase.cs # Central container for cards, speakers, and resource catalogs
├── Runtime/
│   ├── Llm/                 # Groq API client & prompt builders
│   │   ├── LlmReactionClient.cs    # UnityWebRequest async Groq API integration
│   │   ├── LlmPersonaPromptBuilder.cs # Constructs character reaction system prompts
│   │   ├── EpiloguePromptBuilder.cs   # Constructs run summary prompts on loss
│   │   ├── LlmSettings.cs         # LLM configuration ScriptableObject
│   │   └── LlmPromptTemplates.cs    # System instructions and fallback seeds
│   └── Narrative/           # Core game state & run execution logic
│       ├── NarrativeRunner.cs       # Traverses graph nodes and executes choices
│       ├── ResourceState.cs         # Dynamic dictionary of current resource values
│       ├── ResourceWarningMonitor.cs# Tracks resource warning thresholds & cooldowns
│       └── PlayerHistoryTracker.cs  # Logs choice history & state snapshots for LLM
├── UI/                      # Presentation layer
│   ├── CardView.cs          # Card visual display & swipe exit animations
│   ├── ResourceDisplay.cs   # Dynamic HUD resource stat icons and values
│   ├── DayDisplay.cs        # Survival day counter UI
│   ├── StartScreenView.cs   # Title screen controller
│   └── PauseMenuView.cs     # Pause overlay controller
├── Input/
│   └── PlayerChoiceInput.cs # Touch, mouse drag, and keyboard choice handling
└── Editor/                  # Unity Editor toolset
    ├── CardGraphWindow.cs   # Window shell, toolbar controls, database selector
    ├── CardGraphView.cs     # UIElements node graph canvas & port connections
    ├── CardNode.cs          # Visual node representation of CardData
    ├── SpeakerNode.cs       # Visual node representation of SpeakerData
    ├── ResourceNode.cs      # Visual node representation of ResourceData
    └── CardJsonExporter.cs  # One-click JSON exporter utility
```

---

## License

This project is licensed under the MIT License - see the [LICENSE](file:///d:/UnityProject/Weave/LICENSE) file for details.