# Weave

Weave is a Unity narrative-game framework built around authored card graphs. Narrative content, speakers, resources, prompts, and runtime settings are stored as ScriptableObject assets and edited through a dedicated graph tool.

The included runtime supports branching choices, resource-driven consequences, generated character reactions, multi-turn petition scenes, and LLM-generated collapse epilogues.

## Requirements

- Unity 6.0.0.79f1 or a compatible Unity 6 release
- TextMesh Pro and the Unity Input System
- A Groq API key for LLM-backed features

The project remains usable without an API key: authored cards and resource logic continue to work, while LLM-dependent cards follow their configured fallback behavior.

## Open the Project

1. Open the repository in Unity Hub.
2. Load [Game.unity](Assets/Game/Scenes/Game.unity).
3. Select the `GameManager` in the scene and assign the narrative database, UI references, resource state, LLM client, and LLM settings as needed.
4. Configure the `LlmReactionClient` with a `LlmSettings` asset and a `TextAsset` containing the Groq API key.
5. Enter Play Mode.

For a working sample configuration, use **Weave > Generate Full Test Story**. It creates assets under `Assets/_TestContent`.

## Content Model

`NarrativeDatabase` is the root asset for a run. It holds the entry card, card and speaker collections, a resource catalog, graph-layout metadata, and the shared prompt templates.

| Asset | Purpose |
| --- | --- |
| `CardData` | Authored narrative card, choice text, resource changes, routing, and optional LLM behavior. |
| `SpeakerData` | Character portrait, display identity, and the LLM persona prompt. |
| `ResourceData` | Starting value, warning configuration, and an editable collapse threshold. |
| `ResourceCatalog` | Resource collection and an authored fallback ending card for each collapse condition. |
| `LlmPromptTemplates` | Shared system instructions and default seed prompts. |
| `LlmSettings` | Groq model, generation limits, history depth, petition limits, and request timeout. |

Cards, speakers, and resources use an author-facing asset ID and a player-facing display name. Asset IDs can be used by editor tooling and default to the Unity asset name when omitted.

## Authoring Workflow

Open **Weave > Card Graph**, then select or create a `NarrativeDatabase`.

1. Create cards, speakers, a resource catalog, and resources from the graph toolbar.
2. Set the database's starting card.
3. Connect standard cards through their left and right ports, and apply resource changes to either choice.
4. Assign a speaker to every card. The runtime validates this before a run starts.
5. Configure resource warnings and collapse thresholds in each `ResourceData` asset.
6. Add a matching `ResourceCollapseEnding` entry for each resource that can end a run.

### Card Types

| Type | Configuration | Runtime behavior |
| --- | --- | --- |
| Standard card | Left/right choices and outgoing links | Applies the selected resource change, then follows the selected link. |
| LLM reaction card | Enable `Is LLM Reaction Card`; use `Continue Next Card` | Requests one in-character line using the card seed or the shared default. |
| Petition card | Enable `Is Petition Card`; use `Continue Next Card` | Opens a multi-turn audience. The model returns a structured proposal that the player confirms before resource changes are applied. |
| Terminal card | Leave all outgoing links empty | Ends the run with the authored card content. |

### Resources, Warnings, and Collapse

Each resource begins at `defaultStartingValue` and may be modified by card choices or petition resolutions.

- A warning can trigger at or below `warningThresholdPercent` of the starting value. It uses the assigned warning speaker and observes the configured cooldown.
- A resource collapses at or below `collapseThreshold`, which defaults to zero.
- On collapse, Weave requests a contextual epilogue using the run length, final resource state, and recorded player decisions.
- `ResourceCollapseEnding.endingCard` is the fallback shown when that request is unavailable, times out, or returns no usable text.

## LLM Configuration

All requests use `LlmReactionClient` and the settings assigned to it. The client sends requests to Groq's OpenAI-compatible chat-completions endpoint.

`LlmPromptTemplates` is the single source of shared prompt text. Per-card reaction and petition seed overrides take precedence over its defaults. The prompt system has three paths:

- Persona prompts for reaction cards, warning alerts, and petition openings.
- Petition-turn prompts that require a JSON response containing a phase, spoken reaction, resource changes, and a history tag.
- Epilogue prompts for resource-collapse endings.

Use **Weave > LLM Tester** to inspect the composed prompt for a selected database, card, speaker, or resource before sending a live request. This is useful for validating templates, resource names, and prompt context without starting a run.

## Runtime Behavior

`GameManager` coordinates input, UI, the narrative runner, resource warnings, player history, petitions, and LLM requests. `NarrativeRunner` owns card progression and detects terminal and collapse outcomes. `PlayerHistoryTracker` builds the resource summaries and decision history supplied to the LLM.

The default scene includes start and pause flows, a day display, resource display, and card view. Choice input supports the configured swipe/touch flow as well as keyboard input.

## Project Layout

```text
Assets/Game/
|-- Scenes/                 Main Unity scenes
|-- Data/                   Shared runtime configuration assets
|-- Scripts/
|   |-- Definitions/        ScriptableObject content definitions
|   |-- Runtime/
|   |   |-- Llm/            Prompt composition, Groq client, petitions, settings
|   |   `-- Narrative/      Progression, resources, warnings, and history
|   |-- UI/                 Card, HUD, start, and pause views
|   |-- Input/              Player-choice input
|   `-- Editor/             Graph editor, LLM tester, exporters, and test tools
`-- _TestContent/           Generated sample content
```

## Utilities

- **Weave > Card Graph**: create and connect narrative content.
- **Weave > LLM Tester**: compose and optionally send test LLM requests.
- **Weave > Generate Full Test Story**: generate a sample database and supporting assets.
- **Tools > Weave > Export Cards to JSON**: export a narrative database to `Assets/StreamingAssets/cards.json`.

## License

Licensed under the Apache License, Version 2.0. See [LICENSE](LICENSE).
