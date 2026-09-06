# Weave

A bilingual narrative card game built in Unity 6. You rule a kingdom one audience at a time:
swipe to decide, listen to a petition, and watch the realm's food, treasury and faith answer
for it. Some characters are voiced in the moment by an LLM, so no two runs quote you quite
the same way — and every generated line has a written fallback, so the game is playable with
no network and no API key at all.

The whole game is one scene, one story database, and no prefabs: everything you see is
Inspector wiring you can read end to end.

- **Two languages, properly:** English and Arabic, right-to-left shaped and re-typeset live.
- **A real story ships in the repo:** the Wicked King, 53 cards, several endings.
- **Your AI line, your call:** use a shared connection or bring your own Groq key (BYOK).
- **Apache-2.0** for code and story content; see [Third-party](#third-party).

## Playing

Reach a verdict on each card by dragging or swiping the card, or with the keyboard.

| Input | Action |
| --- | --- |
| Drag / swipe the card | Choose left / right |
| `A` / `←` — `D` / `→` | Choose left / right |
| `Esc` | Pause (the AI button inside reopens the AI settings panel) |
| Language toggle in menus (`F10` in the Editor) | English ↔ Arabic |

Cards come in four shapes:

- **Branching** — a straight choice with two authored outcomes.
- **Reaction** — you decide, then the character answers you in generated dialogue.
- **Petition** — an audience member argues their case across several turns; you can type a
  reply, and their verdict moves resources.
- **Ending** — the run stops; an epilogue is generated from how you ruled.

Resources drift as you play. Let one collapse and the kingdom falls regardless of the card you
were on, which is its own ending family.

## Run it from source

### Requirements

- **Unity 6000.0.79f1**, exactly (see `ProjectSettings/ProjectVersion.txt`).
- Nothing else. Every package, including the Arabic text stack
  [RTLTMPro](https://github.com/Nosuchstudios/RTLTMPro) pulled from OpenUPM, is declared in
  `Packages/manifest.json` and resolves on first open.

### First launch

1. Open the project in Unity Hub and let it compile.
2. Open `Assets/Game/Scenes/Game.unity` — the only scene — and press Play.
3. The **AI settings** panel appears before the start screen. Choose a connection or press
   Continue to play on canned dialogue; you can change your mind later from the AI button on
   the start screen or in the pause menu. It won't ask again once you've chosen.
4. If the Console reports a missing reference at startup, assign the field it names — required
   references fail fast rather than degrading quietly.

### Building for the web

`File → Build Settings → WebGL`. The project ships a `WeaveResponsive` template under
`Assets/WebGLTemplates` whose canvas fills its container, so itch.io's fullscreen button scales
the game edge-to-edge; 4× MSAA is enabled in the active URP asset to stop edge shimmer while
cards are dragged.

## Choosing an AI connection

Characters that speak live need a chat-completions endpoint. Two ways to get one, both reachable
from the in-game panel:

- **Shared connection.** Requests go through a Cloudflare Worker that holds a dev-owned key, so
  you pay nothing and configure nothing. It is rate-limited and shared with every other player.
- **Your own key (BYOK).** Paste a [Groq key](https://console.groq.com/keys) — the free tier is
  enough. The panel validates it against Groq's models endpoint, which costs no tokens, before
  storing it. After that your chat requests go **directly to api.groq.com**; the key lives only in
  `PlayerPrefs` on your device and never transits the shared worker. If a stored key is rejected
  mid-session (HTTP 401), it is disabled for that session and the same request re-sends through
  the shared connection, so play never blocks.

The panel's status line also reports whether the shared connection is currently working. Opening
it sends one throwaway request — around forty tokens, at most once a minute — that stands up for
the whole chain: the worker, its secret, Groq, and the configured model. Anything but a success
reads as "unavailable" to the player and logs the precise cause (rejected key, exhausted quota,
missing secret, unreachable worker) to the console.

Whichever line you pick, generation is never load-bearing: reactions, warnings, petition turns and
epilogues all fall back to authored text when the service is down.

### Running your own shared endpoint

The worker lives in [`proxy/`](proxy/README.md) — deployment, its 20 KB payload cap, and the
key-rotation caveat are documented there. Point the **Proxy URL** field on the scene's
`LlmReactionClient` component at your deployment to use it instead of the bundled one.

## Where things live

| Area | Location |
| --- | --- |
| Runtime scripts | `Assets/Game/Scripts` — `GameManager` orchestrates, `NarrativeRunner` advances cards |
| Content types | `Assets/Game/Scripts/Definitions` — all ScriptableObjects |
| Authoring tool | **Weave → Card Graph**, plus custom inspectors |
| Content assets | `Assets/Game/Data` — `Story` (database, LLM settings, prompts, fonts), `Cards`, `Speakers`, `Resources` |
| Localization | `Assets/Game/Scripts/Localization` — `LanguageManager`, `LocalizedLabel`, `RtlTextHelper` (RTL shaping), `FontSettings` (per-language fonts) |
| AI layer | `Assets/Game/Scripts/Llm` — request client, key storage, prompt templates, fallback text |
| AI settings UI | `Assets/Game/Scripts/UI/ByokPanelView.cs` |
| Shared proxy | `proxy/` — Cloudflare Worker; used when no player key is active |
| Card visuals | `Assets/Game/Art` (card art, portraits, fonts) + `Assets/Game/Content/CardTemplates` — looks assigned per card |
| Web build template | `Assets/WebGLTemplates/WeaveResponsive` |
| UI art | `Assets/Gentleland` — third-party Steampunk pack (don't edit) |

## Architecture in one page

| Component | Responsibility |
| --- | --- |
| `GameManager` | Orchestration: input, pause, card presentation, petition flow, warnings, endings, restart, AI panel wiring |
| `NarrativeRunner` | Choice resolution, resource changes, day counter, story-graph validation at startup, ending and collapse detection |
| `ResourceState` | Current resource values; raises `Changed` after each apply |
| `LlmReactionClient` | HTTP to Groq (direct with the player key, or via the proxy), response parsing and sanitization, key validation probe, shared-service health probe, session fallback |
| `LlmKeyStore` | Player key in `PlayerPrefs`, first-launch gate, session fallback flag, key sanitization |
| `ByokPanelView` | AI settings panel: enter/paste and validate a key, saved-key state, shared-connection status |
| `CardView` + HUD views | Card rendering for all states (normal / reaction / petition / ending), petition input, choice animation |

- `PlayerChoiceInput` polls keyboard, mouse, and touch directly; dragging beyond the swipe
  threshold commits a choice. Keyboard input is suspended while the petition field is focused.
- ScriptableObjects created at runtime (warning cards, collapse placeholders, generated
  commoners) use `ScriptableObject.CreateInstance` and are destroyed by `GameManager`.
- Petition turns return JSON `{ phase, reaction, resourceChanges, historyTag }`; only an explicit
  `proposal` phase moves resources. Strict `json_schema` is tried first, with one retry without
  `response_format` if the request shape is ever rejected.

## Trip hazards

- Inspector wiring only — no prefabs, Addressables, or DI. Missing required references fail fast
  at startup with a Console error naming the field.
- Branching cards need both outcome links or startup aborts; broken links are checked when the
  story graph is validated, not when you save an asset.
- The AI settings panel is hand-authored in the scene and saved *active*; `GameManager.Start()`
  hides it for returning players. Don't rely on the scene state alone to gate it.
- Fonts resolve per language at runtime from the `FontSettings` asset referenced by the scene's
  `LanguageManager`; mixed-script text falls back across the English and Arabic fonts.
- All static prompt text is Inspector-editable on `LlmPromptTemplates` — change prompts there,
  not in code.
- Unity re-caches TMP font SDF assets while you play. If `*.SDF.asset` files show as modified in
  `git status`, that is churn; revert them unless glyph tables genuinely changed.

## Third-party

Code and story content in this repository are Apache-2.0. Bundled third-party material carries its
own terms and is **not** re-licensed by that grant:

- **Gentleland Steampunk UI** (`Assets/Gentleland`) — Unity Asset Store content, used under its
  EULA. Its own third-party fonts and CC0 environment maps are listed in
  `Assets/Gentleland/SteampunkUI/ThirdPartyLicenses`. Purchase and import it yourself before
  redistributing a build that depends on it.
- **[RTLTMPro](https://github.com/Nosuchstudios/RTLTMPro)** by Nosuch Studios — Arabic text
  shaping for TextMeshPro, resolved from OpenUPM.
- **Amiri, Cormorant Garamond, Felipa, Noto Sans Arabic** — SIL Open Font License.

Please check each license before publishing a fork or a build of your own.

## License

Apache License 2.0 — see [LICENSE](LICENSE). Third-party assets are covered by their own terms, as
noted above.
