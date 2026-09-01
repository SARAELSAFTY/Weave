# Weave

A bilingual (English / Arabic) narrative card game built in Unity 6: rule a kingdom one
audience at a time, swipe to decide, and watch the kingdom's resources react — with
characters voiced in real time by an LLM (Groq) behind a Cloudflare Worker proxy.

Everything project-specific — setup, scene wiring, systems, trip hazards — lives in
**[ONBOARDING.md](ONBOARDING.md)**.

## Requirements

- **Unity 6000.0.79f1** (exact version).
- Nothing else — packages (incl. RTLTMPro via OpenUPM) resolve automatically on first open.

## Quick start

1. Open the project in Unity Hub.
2. Open `Assets/Game/Scenes/Game.unity` — the only scene.
3. Author content in **Weave → Card Graph**: new DB → catalog → resources → speakers → cards.
4. Assign the database and LLM settings on the Game Manager, the catalog on
   `ResourceState`, press Play.

The repo ships **no story** — the scene's content slots are intentionally empty. The LLM
proxy is already wired into the scene.

## Controls

| Input | Action |
| --- | --- |
| Drag / swipe the card | Choose left / right |
| `A` / `←` — `D` / `→` | Choose left / right |
| `Esc` | Pause |
| Language toggle in menus (`F10` in Editor) | English ↔ Arabic |

## Where things live

| Area | Location |
| --- | --- |
| Runtime scripts | `Assets/Game/Scripts` — `GameManager` orchestrates, `NarrativeRunner` advances cards |
| Content types | `Assets/Game/Scripts/Definitions` — all ScriptableObjects |
| Authoring tool | **Weave → Card Graph** + custom inspectors |
| Content assets | `Assets/Game/Data` — cards, speakers, resources |
| LLM proxy | `proxy/` — Cloudflare Worker; the Groq key stays server-side |
| UI art | `Assets/Gentleland` — third-party Steampunk pack (don't edit) |

## Key facts

- Single scene, Inspector wiring only — no prefabs, Addressables, or DI. Missing required
  references fail fast at startup with a Console error naming the field.
- Card types: **branching** (left/right links), **LLM reaction** and **petition**
  (continue link), **ending** (no links). Branching cards need both links or startup aborts.
- Petition turns return JSON `{ phase, reaction, resourceChanges, historyTag }`; only an
  explicit `proposal` phase moves resources.
- Every LLM surface (reactions, warnings, petitions, epilogues) falls back to localized
  canned text when the proxy is down.
- All static prompt text is Inspector-editable on `LlmPromptTemplates`.

## Third-party

Gentleland Steampunk UI · [RTLTMPro](https://github.com/Nosuchstudios/RTLTMPro) ·
Amiri and Cormorant Garamond fonts (SIL OFL).

## License

Apache License 2.0 — see [LICENSE](LICENSE).
