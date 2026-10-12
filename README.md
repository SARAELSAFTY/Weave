# Weave

A bilingual (English / Arabic) narrative card-strategy game built in **Unity `6000.0.79f1`** with real-time LLM character audiences, petitions, and dynamic advisor reactions.

**Play in browser (desktop & touch-screen):** **[abdullahmohieldin.itch.io/weave](https://abdullahmohieldin.itch.io/weave)**

<!-- SCREENSHOTS: add after docs/images exists -->

## Overview

In **Weave**, you rule a troubled kingdom as the newly crowned monarch, swiping decision cards left or right to balance three pillars of power — **Crown** (`التاج`), **Gold** (`الذهب`), and **Army** (`الجيش`). Alongside authored branching choices, selected cards open live generative encounters with seven court figures (**The Chancellor**, **The General**, **The Treasurer**, **The Merchant**, **The Jester**, **The Seer**, and **The Spirit Mother**) as well as commoner petitioners. Every LLM touchpoint includes authored fallback text in both English and Arabic, so runs can progress when the LLM service is unreachable or no API key is configured.

## Features

- **50-Card Branching Story Graph** — Authored narrative with 7 court speakers, multiple endings, resource collapse states, and best-run day tracking.
- **Live LLM Encounters** — Dynamic advisor reactions, 30% resource warnings, multi-turn JSON-structured petitions (with spam detection enabled by default), free-form character chats, and state-driven collapse epilogues.
- **Dual LLM Pathways** — Zero-setup play via a shared Cloudflare Worker relay (`proxy/`, 20 KB body cap, per-IP rate limit (30 requests/minute per Cloudflare location)) or direct **Bring Your Own Key (BYOK)** calls to Groq (`qwen/qwen3.8-27b`), with a 30 s health probe and authored fallbacks.
- **Full English & Arabic Localization** — Instant in-game language switching with right-to-left (RTL) shaping via `RTLTMPro` and dedicated per-language SDF font stacks.
- **Custom Node-Graph Editor** — Visual `UIElements`/`GraphView` authoring tool (`Weave → Card Graph`) with live validation badges (`START`, `LLM`, `PETITION`, `CHAT`, `ENDING`, `DEAD END`).
- **Responsive Desktop & Touch Controls** — Smooth drag physics across mouse, touch, and keyboard, paired with a container-filling WebGL template (`WeaveResponsive`).

## How It Works & Controls

Each turn presents a card from the royal court. Preview resource impacts by dragging the card slightly left or right, release past the threshold to commit, or type decrees directly during live petition and chat audiences. If **Crown**, **Gold**, or **Army** reaches `0`, your reign ends in a collapse epilogue.

| Action | Input |
| --- | --- |
| **Choose Left / Right** | Drag/swipe card left or right, or press `A` / `D` (`←` / `→` arrow keys) |
| **Submit Petition / Chat Reply** | Type in the audience input field and press **Send** (or `Enter`) |
| **Confirm Proposal / End Chat** | Click **Confirm** / **End Audience** on the card |
| **Pause / Language / AI Settings** | Press `Esc` for Pause; toggle **EN ↔ AR** (`F10` in Editor) or open **AI** settings from Start/Pause menus |

## Tech Stack

| Layer | Technology |
| --- | --- |
| **Engine & Rendering** | Unity `6000.0.79f1`, Universal Render Pipeline (URP 17.0.4) |
| **Input & UI** | Unity Input System 1.19.0, uGUI, TextMeshPro |
| **Arabic Localization** | [RTLTMPro](https://github.com/pnarimani/RTLTMPro) 4.0.0 + custom `FontSettings` & multi-atlas SDF fonts |
| **Architecture & Data** | ScriptableObject data store (`Assets/Game/Data/`), `Game.Runtime` & `Game.Editor` assemblies |
| **LLM Backend** | Groq OpenAI-compatible `chat/completions` (`qwen/qwen3.8-27b`) via Cloudflare Worker (`proxy/`) or direct BYOK |
| **Binary Assets** | Git LFS (art, audio, fonts) |

## Getting Started

### Requirements

- **Unity `6000.0.79f1`** (via Unity Hub)
- **[Git LFS](https://git-lfs.com)** (installed prior to cloning)
- **Node.js + Wrangler** *(optional, only for deploying `proxy/`)*

### Setup & Run

1. Clone with Git LFS:
   ```sh
   git lfs install
   git clone https://github.com/SARAELSAFTY/Weave.git
   ```
2. Open the project in **Unity `6000.0.79f1`** (`RTLTMPro` resolves automatically via OpenUPM).
3. Open `Assets/Game/Scenes/Game.unity` and press **Play**.
4. **API Key Options:** In the **AI Voice** panel on first launch (or from the menu), either use the **Shared Service** (via the Cloudflare Worker URL on `LlmReactionClient`) or select **Own Key** and paste a free [Groq API key](https://console.groq.com/keys) (`gsk_...`, stored in local `PlayerPrefs`). To self-host the relay, see [`proxy/README.md`](proxy/README.md).
5. **WebGL Build:** Open **File → Build Profiles**, select **WebGL**, and build (`Assets/WebGLTemplates/WeaveResponsive` is preconfigured).

## Project Structure

```text
Weave/
├── Assets/Game/                 # Core game assets, ScriptableObject data, Game.unity, and C# scripts
├── Assets/Gentleland/           # SteampunkUI asset pack (UI chrome sprites)
├── Assets/WebGLTemplates/       # Custom responsive WebGL embed template (WeaveResponsive)
├── Packages/                    # Unity package manifest and lockfile (URP, Input System, RTLTMPro)
├── ProjectSettings/             # Unity project settings (pinned to 6000.0.79f1)
├── docs/                        # Architecture and technical documentation
└── proxy/                       # Cloudflare Worker relay for shared Groq requests
```

## Architecture

For the full directory breakdown, runtime component responsibilities, card node types, LLM behavioral contracts, and Arabic localization pipeline, see **[`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md)**.

## Licenses & Third-Party Notices

- **Project Code & Original Content:** Licensed under the [Apache License 2.0](LICENSE).
- **Music:** *"Breton"* by **Pufino** (sourced via [freetouse.com](https://freetouse.com/music) — free to use with creator attribution).
- **Arabic Text Shaping:** [RTLTMPro](https://github.com/pnarimani/RTLTMPro) by pnarimani (MIT License).
- UI chrome: SteampunkUI by Gentleland (free on the Unity Asset Store; included under Assets/Gentleland, see the Asset Store license)
- **Fonts:** *Amiri*, *Noto Sans Arabic*, *Cormorant Garamond*, and *Felipa* (SIL Open Font License 1.1 — see `OFL.txt` files under `Assets/Game/Art/Fonts/`).
- **Icons:** HUD and utility icons from Pngtree and Flaticon.

