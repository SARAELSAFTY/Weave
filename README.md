# Weave

A data-driven narrative prototype for Unity. Stories are built entirely in a visual card graph; character reactions are generated live via the [Groq](https://groq.com) LLM API.

## Overview

Weave separates content from code. Cards, speakers, and kingdom resources are all ScriptableObject assets created and connected inside the Card Graph — no manual asset creation or code changes required to build or extend a story. Renaming or moving any asset is safe; IDs are kept in sync automatically.

## Features

- **Visual card graph** — author branching narratives by creating and connecting card nodes (**Weave > Card Graph**)
- **In-graph asset creation** — cards, speakers, resources, databases, and catalogs are all created from the graph toolbar or right-click menu
- **Speaker system** — assign characters to cards with portraits and display names
- **Kingdom resources** — extensible, non-hardcoded stats; each card choice applies resource changes on swipe
- **Live AI reactions** — LLM Reaction cards generate character dialogue at runtime via Groq, driven by speaker persona, card prompt seed, current game state, and player history
- **Auto-sync** — renaming or moving a card, speaker, or resource asset automatically updates its ID and refreshes the graph

## Getting Started

### Prerequisites

- Unity 6 (or the version used by this project)
- A [Groq API key](https://console.groq.com)

### Setup

1. Open **Weave > Card Graph**, use **+ New DB** to create a `NarrativeDatabase`, then build your story from there.
2. Create a `LlmSettings` asset via **Assets > Create > Weave > LLM Settings** and configure the model, temperature, and timeout.
3. Open `Assets/Game/Scenes/Game.unity` and confirm `GameManager` has `CardView`, `ResourceState`, `NarrativeDatabase`, and `DayDisplay` assigned.
4. On the `LlmReactionClient` component, assign your `LlmSettings` asset and a TextAsset containing your Groq API key to **Api Key Asset**. *(Optional — if missing, LLM reaction cards auto-advance.)*
5. Press **Play**. Make choices by dragging the card left/right with the mouse, swiping on touch, or pressing **A/D** or **Left/Right Arrow**.

## Authoring Content

Everything is done inside **Weave > Card Graph**.

| Task | How |
|---|---|
| Create a card | Toolbar **+ New Card** or right-click canvas → Create Card |
| Connect cards | Drag from a card's output port to the next card's input |
| Set the starting card | Toolbar **Starting Card** dropdown |
| Create a speaker | Toolbar **+ New Speaker** or right-click → Create Speaker |
| Create a resource | Toolbar **+ New Resource** or right-click → Create Resource (requires a Catalog) |
| Create a resource catalog | Toolbar **+ New Catalog** |
| Mark a card as an AI reaction | Enable **Is Llm Reaction Card** on the card; fill in **Llm Prompt Seed** |
| Configure a speaker for AI | Enable **Is Llm Speaker**; write a **Llm Persona Prompt** |
| Tune AI behavior | Adjust model, temperature, max tokens, and timeout in the `LlmSettings` asset |
| Create a new database | Toolbar **+ New DB** |