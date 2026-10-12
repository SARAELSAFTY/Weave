# Weave Groq Proxy

Cloudflare Worker that forwards the game's chat-completions requests to the Groq API.
The Groq API key lives only here as a Worker secret — it never ships with the game build.

This worker is the **shared-key path**: it serves players who have not saved their own
key. When the player stores a Groq key through the in-game AI panel (BYOK), the game
calls `api.groq.com` directly and bypasses this worker; the player's key lives only in
their device's `PlayerPrefs` and never transits this worker.

## Setup

```sh
npm install
npx wrangler secret put GROQ_API_KEY   # key from https://console.groq.com/keys
npx wrangler deploy
```

Note: on this account `wrangler deploy` resets bindings, so re-run the
`wrangler secret put GROQ_API_KEY` step after every deploy.

## What the worker enforces

- Only `POST` / `OPTIONS`; body capped at 20 KB.
- Per-IP rate limit of 30 requests per 60 seconds (`RELAY_LIMITER` Workers Rate Limiting binding, keyed by `cf-connecting-ip`). This limit is enforced per Cloudflare location and is approximate; exceeding it returns HTTP `429` before reading the request body or calling Groq.
- Upstream HTTP status codes (including `429`) from Groq are forwarded to the client so `LlmReactionClient` can trigger its authored fallback path.

Known trade-off: the worker URL ships inside the game build and is unauthenticated,
so anyone who extracts it can spend this key's free quota. Players can avoid relying
on it entirely by saving their own key in-game. If the shared key gets abused, rotate
it (`wrangler secret put GROQ_API_KEY`); the game falls back to canned dialogue lines
in the meantime.
