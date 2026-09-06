# Weave Groq Proxy

Cloudflare Worker that forwards the game's chat-completions requests to the Groq API.
The Groq API key lives only here as a Worker secret — it never ships with the game build.

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
- Groq rate-limit headers (`Retry-After`, `x-ratelimit-*`) are forwarded to the
  client so the game can react to HTTP 429.

Known trade-off: the worker URL ships inside the game build and is unauthenticated,
so anyone who extracts it can spend this key's free quota. If that happens, rotate
the key (`wrangler secret put GROQ_API_KEY`); the game falls back to canned dialogue
lines in the meantime.
