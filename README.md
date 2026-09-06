# Weave

A bilingual (English / Arabic) narrative card game built in Unity 6: rule a kingdom one
audience at a time, swipe to decide, and watch the kingdom's resources react — with
characters voiced in real time by an LLM (Groq), either through a shared proxy or the
player's own API key.

This README doubles as the developer onboarding: setup, architecture, and trip hazards.

## Requirements

- **Unity 6000.0.79f1** (exact version).
- Nothing else — packages (incl. RTLTMPro via OpenUPM) resolve automatically on first open.

## Quick start

1. Open the project in Unity Hub and let it compile.
2. Open `Assets/Game/Scenes/Game.unity` — the only scene — and press Play. The repo ships
   a playable test story (the Wicked King) in `Assets/Game/Data/Story`. If the Console
   reports a missing reference at startup, assign the field it names.
3. First launch shows the **AI key panel** before the start screen; pick a line there
   (see *AI keys* below) or skip it and play with canned dialogue.
4. Author more content in **Weave → Card Graph**: + New DB → catalog → resources →
   speakers → cards. Assign the database and LLM settings on the Game Manager, the
   catalog on `ResourceState`.

## AI keys (BYOK) and the shared proxy

- The first-launch panel lets the player enter or paste their **own Groq API key**
  (get one at <https://console.groq.com/keys>). It is validated against Groq's models
  endpoint — no tokens spent — and saved in `PlayerPrefs` on the device only.
- With a stored key, chat requests go **directly to api.groq.com**; the key never
  transits the shared proxy.
- Without a key, requests go through the shared Cloudflare Worker (`proxy/` — dev-owned
  key, see `proxy/README.md`).
- If a stored key is rejected mid-session (HTTP 401), it is disabled for the session and
  the same request transparently re-sends through the shared proxy, so gameplay never blocks.
- The panel reopens from the AI button on the start screen or the pause menu. The panel
  in the scene is hand-authored — there is no editor tool that builds it.

## Controls

| Input | Action |
| --- | --- |
| Drag / swipe the card | Choose left / right |
| `A` / `←` — `D` / `→` | Choose left / right |
| `Esc` | Pause (AI button inside reopens the key panel) |
| Language toggle in menus (`F10` in Editor) | English ↔ Arabic |

## Where things live

| Area | Location |
| --- | --- |
| Runtime scripts | `Assets/Game/Scripts` — `GameManager` orchestrates, `NarrativeRunner` advances cards |
| Content types | `Assets/Game/Scripts/Definitions` — all ScriptableObjects |
| Authoring tool | **Weave → Card Graph** + custom inspectors |
| Content assets | `Assets/Game/Data` — story, cards, speakers, resources, LLM settings and prompts |
| Localization | `Assets/Game/Scripts/Localization` — `LanguageManager` (F10 toggle), `LocalizedLabel`, `RtlTextHelper` (RTL shaping), `FontSettings` (per-language fonts) |
| BYOK key flow | `Assets/Game/Scripts/Llm/LlmKeyStore.cs` + `Assets/Game/Scripts/UI/ByokPanelView.cs` |
| LLM proxy | `proxy/` — Cloudflare Worker; used when no player key is active |
| Card visuals | `Assets/Game/Art` (card art, portraits, fonts) + `Assets/Game/Content/CardTemplates` — looks assigned per card |
| UI art | `Assets/Gentleland` — third-party Steampunk pack (don't edit) |

## Architecture in one page

| Component | Responsibility |
| --- | --- |
| `GameManager` | Orchestration: input, pause, card presentation, petition flow, warnings, endings, restart, AI panel wiring |
| `NarrativeRunner` | Choice resolution, resource changes, day counter, story-graph validation at startup, ending and collapse detection |
| `ResourceState` | Current resource values; raises `Changed` after each apply |
| `LlmReactionClient` | HTTP to Groq (direct with the player key, or via proxy), response parsing and sanitization, key validation probe, session fallback |
| `ByokPanelView` | First-launch API key screen: enter/paste key, clipboard bridge, saved-key indicator, delete key |
| `CardView` + HUD views | Card rendering for all states (normal / reaction / petition / ending), petition input, choice animation |

- `PlayerChoiceInput` polls keyboard, mouse, and touch directly; dragging beyond the swipe
  threshold commits a choice. Keyboard input is suspended while the petition field is focused.
- ScriptableObjects created at runtime (warning cards, collapse placeholders, generated
  commoners) use `ScriptableObject.CreateInstance` and are destroyed by `GameManager`.

## Key facts

- Single scene, Inspector wiring only — no prefabs, Addressables, or DI. Missing required
  references fail fast at startup with a Console error naming the field.
- Card types: **branching** (left/right links), **LLM reaction** and **petition**
  (continue link), **ending** (no links). Branching cards need both links or startup aborts.
- Petition turns return JSON `{ phase, reaction, resourceChanges, historyTag }`; only an
  explicit `proposal` phase moves resources.
- Every LLM surface (reactions, warnings, petitions, epilogues) falls back to localized
  canned text when the service is down.
- All static prompt text is Inspector-editable on `LlmPromptTemplates`.
- Fonts resolve per language at runtime from the `FontSettings` asset referenced by the
  scene's `LanguageManager`; mixed-script text falls back across the English/Arabic fonts.

## Third-party

Gentleland Steampunk UI · [RTLTMPro](https://github.com/Nosuchstudios/RTLTMPro) ·
Amiri, Cormorant Garamond, Felipa, and Noto Sans Arabic fonts (SIL OFL).

## License

Apache License 2.0 — see [LICENSE](LICENSE).
