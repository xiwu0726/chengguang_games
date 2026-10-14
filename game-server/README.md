# Game Server with Error Handling

A Node.js/Express game server with comprehensive error handling for 429400 and 500 errors, retry logic, logging, and a playable **Word Chain (词链接龙)** API.

## Features

- **Express Server**: RESTful API with game status endpoints
- **Word Chain Game**: multiplayer word-chain (接龙) game under `/api/v1`, with challenges/judging, hints and scoring — see [game-design/word-chain-game-design.md](../game-design/word-chain-game-design.md)
- **Rate Limiting**: fixed-window limiter for `/api/v1` with two always-on tiers (10 req/min **and** 100 req/hour per IP by default) plus an **opt-in** burst tier for suspicious request patterns / bot detection (`WORD_CHAIN_RATE_LIMIT_BURST_MAX`, default `0` = off). Every `429` carries `Retry-After`, `X-RateLimit-Scope` and the design doc's numeric `code: 429400`
- **Cursor Pagination**: `GET /api/v1/games` pages newest-first through an opaque `?cursor=`, so a
  client can walk a large roster without duplicates or gaps; `history` supports `?since=` polling
- **CORS (opt-in)**: off by default; `WORD_CHAIN_CORS_ORIGIN` opens a whitelist. Preflight `OPTIONS`
  is answered before the rate limiter and never consumes quota
- **Bot players (`isBot`)**: a seat can be marked as a bot, so a single player can play alone — the server
  plays the bot's turns itself, through the very same validation path as a human
- **Player-name sanitization**: control characters are stripped, whitespace is collapsed, and HTML /
  attribute metacharacters or script keywords are rejected with `400` (never silently stripped)
- **Error Handling**: Special handlers for 429400 and 500 errors that continue execution
- **Retry Logic**: Automatic retry with exponential backoff for transient errors
- **Logging**: Winston-based logging with file and console output
- **Health Checks**: Built-in health check endpoint

## Installation

```bash
cd game-server
npm install
```

## Running the Server

```bash
npm start
```

The server will start on port 3000 (or the `PORT` environment variable).

## Testing

```bash
npm test          # 86 用例, 集成 + 单元 (node:test, 零额外依赖)
npm run test:watch
npm run test:perf # 独立的性能压测脚本 (不在 npm test 里)
```

The suite (`tests/api.test.js`) covers:
- Word chain: create/list/get, play word, turn enforcement, dictionary/turn validation, hint
- Roster: join before the first word, leave, forfeit (including the draw case), full game history
- Pagination & incremental reads: cursor paging over the whole list (no duplicate entries, stable
  `total`), invalid cursors rejected with `400`, `history?since/?limit` resumed via `nextSince`
- Document contracts: `403.requiredRole`, `resolve.gameStatus`, the single-player `{ playerName }`
  lobby, and CORS (whitelist only, `Retry-After` exposed to JS, preflight not eating quota)
- Challenges: raise, resolve, who may judge, mismatched client verdicts, draw detection
- Rate limiting: the `429` + `Retry-After` contract for **both** always-on tiers (minute and hour), the
  numeric `code: 429400` on the response body, the opt-in burst tier (off by default; with it on, the 3rd
  request in the window is a `429` with `scope: "burst"`), the governing-tier rule when all three are
  exhausted, and that a forged `X-Forwarded-For` cannot bypass any of them (driven by an injected clock —
  no real minute-long sleeps)
- Bots & names: `isBot` on create/join (`true` only), a bot-only game settling inside the create request,
  a human-vs-bot game played to the end, the `bot_stuck` forfeit (a stuck bot must not win even when it
  leads on points), the server resolving a challenge against a bot, and the player-name rules (control
  characters, metacharacters, empty/whitespace, the 40-character bound)
- Regression guards: `fetchWithRetry` backoff, `X-Request-ID` echo, `node server.js` really boots and serves

> Every functional case runs against a deliberately inflated quota, and an `after()` hook asserts that
> **no** request outside the explicit rate-limit cases (the ones wrapped in `allow429`) was ever answered
> with `RateLimitExceeded` — so a misconfigured tier, or accidentally enabling the burst tier, fails the
> suite instead of showing up as a flaky 429.

> `performance/performance-test.js` matches Node's built-in test glob (`*-test.js`), so run the
> suite through `npm test` rather than a bare `node --test`.

## Endpoints

### GET /health
Health check. Returns `503` with `"status": "degraded"` once the failure rate crosses the degraded threshold.

### GET /metrics
Error counters, circuit breaker states, degraded-mode stats, plus `wordChain` and `rateLimit` blocks
(the latter including `blockedByMinute` / `blockedByHour` / `blockedByBurst`).

### GET /simulate-error
Simulates upstream failures (cycles `429` / `500` / network). Every response carries `attempt`.

### GET /api/game-status
API endpoint with retry logic for error handling.

**Response (Error):**
```json
{
  "status": "error",
  "message": "Failed to retrieve game status",
  "errorType": "RATE_LIMIT",
  "continueExecution": true
}
```

## Word Chain API (`/api/v1`)

Every route below is mounted under `/api/v1` and passes through the rate limiter first.
Errors are JSON: `{ "error", "message", "details?", ...domainContext }`.

| Method | Path | Purpose |
| --- | --- | --- |
| `POST` | `/api/v1/games` | Create a game — `{ "players": [...] }` (2–4) **or** a one-seat lobby `{ "playerName", "isBot?" }`, `isBot` per player. `201` + `Location` |
| `GET` | `/api/v1/games` | List games — `?limit=` (default 50), `?status=active\|completed\|forfeited`, `?cursor=` (see below) |
| `GET` | `/api/v1/games/:gameId` | Full game state + `history` + `challenges` |
| `GET` | `/api/v1/games/:gameId/history` | Word chain with authors, challenges, and the append-only event log — `?since=` / `?limit=` for polling |
| `POST` | `/api/v1/games/:gameId/join` | Add a player — `{ "playerName", "isBot?" }` (alias `name`), `201` + `Location` |
| `DELETE` | `/api/v1/games/:gameId/players/:playerId` | Leave the game — `{ "success": true, "ended": ... }` |
| `POST` | `/api/v1/games/:gameId/forfeit` | Forfeit — `{ "playerId", "reason?" }`, ends the game immediately |
| `GET` | `/api/v1/games/:gameId/hint` | Candidates for the required letter — `?limit=` (default 3) |
| `POST` | `/api/v1/games/:gameId/words` | Play a word — `{ "playerId", "word" }` |
| `POST` | `/api/v1/games/:gameId/challenges` | Challenge a word — `{ "playerId", "challengedWordIndex" }`, `201` |
| `POST` | `/api/v1/games/:gameId/challenges/:challengeId/resolve` | Judge — `{ "playerId", "isValid" }` |

**Listing games (cursor pagination)**

`GET /api/v1/games` returns the newest game first. `limit`-only listings silently shift as games are
created, which makes a client miss or double-show rows; pass the opaque `nextCursor` back instead:

```json
{
  "total": 12, "remaining": 12, "limit": 2, "count": 2, "hasMore": true, "nextCursor": "MjAyNi0xMC0xMlQwNzo0Mjo...",
  "games": [{"gameId": "game-...", "status": "active", "playerCount": 2, "chainLength": 3, "maxChainLength": 10, "createdAt": "...", "updatedAt": "..."}]
}
```

- `total` is the full count **after** the `status` filter and stays constant while you page through;
  `remaining` is how many are left from this cursor (strictly decreasing, so it is a cheap progress bar).
- `nextCursor` is `null` exactly when `hasMore` is `false`. The cursor is an opaque token (base64url of
  `createdAt` + game sequence) — do not parse or construct one; a malformed value is a `400`
  `ValidationError` with `details[0].field === "cursor"` rather than a silent restart from page one.
- `?cursor=` (empty) is treated as absent, so a client can always append `&cursor=${nextCursor}`.

**Join / leave / forfeit**

`POST /api/v1/games` accepts either a full roster (`{ "players": [{"name":"A"},{"name":"B"}] }`,
2–4 players) or the design doc's one-seat form `{ "playerName": "Alice" }`, which opens a game holding
a single player (`status: "active"`, turn on seat 0) that others then fill with `join`. The first word
never needs a chaining letter, so a one-seat game is playable immediately; the 2–4 player array
contract itself is unchanged (`{"players": [{"name":"Solo"}]}` is still a `400`). Mark that one seat as a
bot (`{"playerName": "Robo", "isBot": true}`) and the server plays it for you — see **Bots (`isBot`)** below.

The design doc allows 2–4 players but gave no way to reach the table after creation, no way to
walk away, and listed "opponent forfeits" as a win condition with no way to do it. Those three
gaps are closed by `join` / `leave` / `forfeit`:

- `join` only works **before the first word** (`403` afterwards) and only up to `MAX_PLAYERS = 4`
  (`409` when full, `409` on a duplicate name — compared case- and whitespace-insensitively).
  Joining does not move the turn pointer.
- `leave` and `forfeit` are refused with `400` while a challenge is pending, because a pending
  challenge freezes the table.
- `leave` ends the game only when fewer than `MIN_PLAYERS = 2` remain; the survivor wins by
  forfeit (`endedBy: "player_left"`).
- `forfeit` always ends the game (`endedBy: "player_forfeited"`): the winner is the highest-scoring
  remaining player, and if the rest are tied the game is a draw (`isDraw: true`, no `winner`).
  Leaving players are kept in `departures` with their score at the time, so the word chain can
  still attribute every word to the person who played it.

```bash
curl -X POST localhost:3000/api/v1/games/$GID/join \
  -H 'content-type: application/json' -d '{"playerName":"Carol"}'
```
```json
{
  "gameId": "game-...", "status": "active",
  "player": {"id":"player-...","name":"Carol","score":0},
  "players": [{"id":"player-...","name":"Alice","score":0}, {"id":"player-...","name":"Bob","score":0}, {"id":"player-...","name":"Carol","score":0}],
  "playerCount": 3, "currentTurnIndex": 0, "currentPlayerId": "player-...", "wordChain": [], "createdAt": "..."
}
```

**Bots (`isBot`)** — any seat may be marked as a bot: `{ "players": [{"name":"Ada"},{"name":"Robo","isBot":true}] }`,
`{ "playerName": "Robo", "isBot": true }` (one-seat lobby), or `POST /:gameId/join` with `isBot: true`.
Only the boolean `true` counts — `"true"`, `1` and a missing field all mean a human, so pre-existing
request bodies keep their old behaviour. Every player object (`create` / `get` / `join` / `playWord`, and
the `winner` / `departures` / `scores` blocks) is serialized as exactly
`{ "id", "name", "score", "isBot" }`.

A bot needs nobody to drive it: the server plays its turn itself, picking a word from the dictionary and
submitting it through the *same* `playWord` path as a human, so dictionary membership, chaining and
duplicate-word rules cannot drift between the two. It acts whenever the turn reaches it, including inside
the request that created the game (seat 0 is a bot → the `201` body already has a 1-word chain) and inside
a human's `playWord` response. A one-seat `{ "playerName": "Robo", "isBot": true }` lobby therefore plays
itself to `completed` (`chainLength == maxChainLength`) within that single request — which is how one
person plays alone. Bot moves are attributed like anyone else's: each `word_played` event carries
`"bot": true|false`, pollable through `/history?since=`.

If a bot cannot produce a word the game is not left hanging and no `500` is raised: the bot is removed
from the table first, recorded in `departures` as `{ "playerId", "name", "score", "isBot": true,
"reason": "bot_stuck", "at" }`, and the game ends with `endedBy: "bot_stuck"`, `status: "forfeited"`. The
winner is the highest-scoring **remaining** player — a stuck bot can never win the game it just forfeited,
even when it leads on points (`game_forfeited.winnerId` always equals the response's `winner.id`).

A bot can also be challenged: since it cannot answer, the server resolves the challenge in the same
request, so it never stays `pending` and a one-human-plus-bot game never freezes. The response and the
`challenge_resolved` event carry `"resolvedByBot": true`, and the word is re-checked server-side as usual.
A human defender cannot borrow that channel: `isBot: true` in the resolve body is ignored, and only the
defender may judge (`403` otherwise).

**Player names** — `playerName` / `players[i].name` are sanitized at one shared entry point used by both
`create` and `join`:

1. control characters `U+0000`–`U+001F` / `U+007F` are removed (newlines, tabs, ANSI escapes);
2. runs of whitespace collapse to one space and the ends are trimmed;
3. a name containing HTML / attribute metacharacters (`<` `>` `&` `"` `'` `` ` `` `=`) or the script
   keywords `javascript:` / `vbscript:` / `data:` is rejected with `400`; `details[0].field` is
   `players[i].name` on create and `playerName` on join, and `details[0].message` names the offending code
   point (e.g. `(offending character U+003C)`) without echoing the raw name;
4. an empty (or non-string) name, or one longer than 40 characters, is a `400`.

Rejection is total, not a silent strip: `A<script>B` is a `400` instead of quietly becoming `AscriptB`, so
a sanitized name can never collide with a real one. Duplicate detection compares the sanitized names,
case- and whitespace-insensitively. The server guarantees what it stores and logs, not what a client
renders — output contexts still have to escape.

**Game history** — `GET /api/v1/games/:gameId/history` returns the chain with each word's author
(`playerId` / `playerName` / `submittedAt`), every challenge, and `events`: an append-only log
(`game_created`, `word_played`, `challenge_raised`, `challenge_resolved`, `game_completed`,
`player_joined`, `player_left`, `player_forfeited`, `game_forfeited`) with a monotonic `seq`. Words
revoked by a successful challenge disappear from `wordChain` but stay in the event log
(`revokedWords`), so a replay can still reconstruct what happened.

**Polling the event log** — `?since=<seq>` returns only events newer than `seq`, `?limit=<n>` caps the
batch, and the response carries `eventsTotal`, `latestSeq`, `nextSince` and `hasMoreEvents` so a poller
always knows where it is:

```bash
curl 'localhost:3000/api/v1/games/'$GID'/history?since=4&limit=50'
```

Feed the returned `nextSince` straight back in as the next `since`: it is the `seq` of the last event
actually delivered, so a batch truncated by `limit` (with `hasMoreEvents: true`) resumes without
skipping anything. Omitting both parameters returns the whole log, exactly as before. A `since` that is
not a non-negative integer is a `400` with `details[0].field === "since"` — a typo must not silently
hand you the full log again and re-trigger whatever the client already did with those events.

**Play a word**
```bash
curl -X POST localhost:3000/api/v1/games/$GID/words \
  -H 'content-type: application/json' \
  -d '{"playerId":"'"$PID"'","word":"apple"}'
```
```json
{
  "gameId": "game-...", "status": "active",
  "players": [{"id":"player-...","name":"A","score":5}, {"id":"player-...","name":"B","score":0}],
  "currentTurnIndex": 1, "currentPlayerId": "player-...",
  "wordChain": ["apple"], "chainLength": 1, "maxChainLength": 10,
  "requiredLetter": "e", "lastWord": "apple",
  "hasPendingChallenge": false, "pendingChallengeId": null, "isDraw": false,
  "word": "apple", "scoreAwarded": 5, "chain": ["apple"]
}
```

**Resolve a challenge** — the server re-checks the dictionary itself, so a client cannot
force a verdict: `challengeResult` is `challenge_failed` (word was valid, turn passes to the
challenger) or `challenge_succeeded` (word removed, chain truncated, challenger wins), and
`mismatch: true` marks a client verdict that contradicted the server. When the defender is a bot, the
server resolves the challenge itself in the same request (`resolvedByBot: true` on the response and the
`challenge_resolved` event) instead of leaving it pending — see **Bots (`isBot`)** above.

### Status codes

| Code | When |
| --- | --- |
| `400` | Validation: empty/duplicate players, empty or too-long player name, a name with HTML/attribute metacharacters or script keywords, word not in dictionary, wrong first letter, reused word, out of range index, resolving twice, acting while a challenge is pending, acting on a finished game |
| `403` | Playing out of turn, challenging your own word, judging a challenge you did not defend, or joining after the first word. The body names the missing capability in `requiredRole` (`player` / `challenger` / `defender`). A bot defender is judged by the server automatically, so it never blocks a game |
| `404` | Unknown `gameId` / `playerId` / `challengeId`, or an unregistered path |
| `409` | Game already has 4 players, or the joined name duplicates an existing player |
| `429` | `/api/v1` quota exceeded — minute, hour, or burst tier (see below); the body carries `code: 429400` |

## Rate Limiting

`/api/v1` is protected by a fixed-window limiter implemented in `src/rate-limit.js` with **two always-on
tiers** (default **10 requests / 60s** and **100 requests / hour**, per IP) plus an **opt-in burst tier**
for suspicious request patterns / bot detection (`burstMax`, default `0` = disabled). Exceeding any tier
returns:

```json
{ "error": "RateLimitExceeded", "message": "Too many requests. Please slow down.", "retryAfter": 3540, "scope": "hour", "code": 429400 }
```

`code` is the design doc's `#### 429400 - Rate Limiting` business code and is a **JSON number** (`429400`,
not the string `"429400"`); other codes on this server (e.g. `/api/game-status`'s `errorType`) are strings,
so type-check this one as a number. `error` / `message` / `retryAfter` / `scope` are unchanged.

Every `/api/v1` response carries HTTP `429`, a `Retry-After` header, and `X-RateLimit-Limit` /
`X-RateLimit-Remaining` / `X-RateLimit-Reset` (minute tier) plus `X-RateLimit-Hour-Limit` /
`X-RateLimit-Hour-Remaining` / `X-RateLimit-Hour-Reset` (hour tier). `X-RateLimit-Scope` names the tier
that actually blocked the request; while the burst tier is enabled it also emits
`X-RateLimit-Burst-Limit` / `-Remaining` / `-Reset`.

**Burst tier (suspicious / bot traffic).** It is **off by default** (`WORD_CHAIN_RATE_LIMIT_BURST_MAX=0`):
ordinary legitimate traffic also arrives in millisecond bursts, so having it on by default would
rate-limit real games. Enable it with `WORD_CHAIN_RATE_LIMIT_BURST_MAX=<n>` — that many requests are
allowed per `WORD_CHAIN_RATE_LIMIT_BURST_WINDOW_MS` (default `5000`) fixed window on the same per-IP key
(never a sliding window). Like the other tiers it is checked before the request is counted, so with
`BURST_MAX=2` the **3rd** request in the window is the first `429` (`scope: "burst"`), and a forged
`X-Forwarded-For` does not bypass it.

When several tiers are exhausted at once, `Retry-After` and `scope` report the **latest** window — the one
that governs. Reporting the minute window instead would make a well-behaved client wait 60s, retry, and
hit a second `429`, which is exactly the "unexpected 429" the contract has to prevent. Set a tier's max to
`0` to disable it (`WORD_CHAIN_RATE_LIMIT_HOURLY_MAX=0`, `WORD_CHAIN_RATE_LIMIT_BURST_MAX=0`); its headers
then disappear and no bucket is created for it.

`/metrics` exposes `rateLimit.blockedByMinute` / `blockedByHour` / `blockedByBurst` (plus `burstMax`,
`burstWindowMs` and `burstTrackedKeys`), so you can confirm which tier fired instead of reading logs;
`/health` reports the configured `burstMax` / `burstWindowMs` under `configuration.wordChainRateLimit`.

`X-Forwarded-For` is **ignored by default** so a
client cannot spoof its way around the quota — enable trust only behind a real gateway with
`TRUST_PROXY_HEADER=true`.

## CORS

Cross-origin access is **off by default**: no `Access-Control-*` header is emitted, so the server
behaves exactly as it did before `src/cors.js` existed (a same-origin deployment needs none).
Open a whitelist when the frontend is served from another origin:

```bash
WORD_CHAIN_CORS_ORIGIN=https://play.example,https://staging.example npm start
```

- `WORD_CHAIN_CORS_ORIGIN` — comma-separated origin whitelist, or `*` for any origin. `none` or an
  empty value means disabled. Matching is case-insensitive, and a request with no `Origin` header
  (curl, server-to-server, a same-origin page) never gets CORS headers at all.
- `WORD_CHAIN_CORS_CREDENTIALS=true` allows cookies; it is ignored when the whitelist is `*`, since
  browsers reject `Access-Control-Allow-Origin: *` combined with credentials.
- `WORD_CHAIN_CORS_MAX_AGE_SECONDS` (default `600`) is how long a browser may cache the preflight.
- An allowed response carries `Vary: Origin` (so a shared cache cannot hand one site's header to
  another) plus `Access-Control-Expose-Headers`, which is what lets browser JS read `Retry-After`,
  `X-RateLimit-Scope` and the `X-RateLimit-*` headers — including `X-RateLimit-Burst-Limit` /
  `-Remaining` / `-Reset` — on both successful and `429` responses, and back off on its own instead of
  colliding with a second `429`.
- Preflight (`OPTIONS` + `Access-Control-Request-Method`) is answered `204` **before** the rate limiter
  runs, so it consumes no quota. Counting preflights would exhaust the minute tier on its own: a browser
  sends one before every cross-origin write, so a client would be limited to a handful of moves per
  minute while its own request log looked nearly empty.

`/health` reports the live configuration under `configuration.wordChainCors`
(`enabled` / `origins` / `allowCredentials` / `maxAgeSeconds`).

## Error Handling

The server implements error handlers that:
1. Log errors to `logs/error.log`
2. Return appropriate HTTP status codes (`429`, `500`)
3. Continue execution without stopping the server
4. Provide `continueExecution: true` flag in responses

## Retry Logic

The `fetchWithRetry` function:
- Retries up to 3 times for 429400 and 500 errors
- Uses exponential backoff (1s, 2s, 4s)
- Logs each attempt
- Continues execution on success or after max retries

## Configuration

| Variable | Default | Meaning |
| --- | --- | --- |
| `PORT` | `3000` | Listen port |
| `LOG_LEVEL` | `info` | Winston level |
| `WORD_CHAIN_RATE_LIMIT_WINDOW_MS` | `60000` | Minute-tier window |
| `WORD_CHAIN_RATE_LIMIT_MAX` | `10` | Allowed requests per minute window (`0` disables the tier) |
| `WORD_CHAIN_RATE_LIMIT_HOUR_WINDOW_MS` | `3600000` | Hour-tier window |
| `WORD_CHAIN_RATE_LIMIT_HOURLY_MAX` | `100` | Allowed requests per hour window (`0` disables the tier) |
| `WORD_CHAIN_RATE_LIMIT_BURST_WINDOW_MS` | `5000` | Burst-tier / bot-detection window |
| `WORD_CHAIN_RATE_LIMIT_BURST_MAX` | `0` | Allowed requests per burst window; `0` (default) **disables** the tier |
| `WORD_CHAIN_MAX_CHAIN_LENGTH` | `10` | Words per chain before the game settles |
| `WORD_CHAIN_CORS_ORIGIN` | *(empty)* | Comma-separated CORS whitelist, or `*`; empty/`none` keeps CORS off |
| `WORD_CHAIN_CORS_CREDENTIALS` | `false` | Allow cookies cross-origin (ignored when the whitelist is `*`) |
| `WORD_CHAIN_CORS_MAX_AGE_SECONDS` | `600` | How long a browser may cache the CORS preflight |
| `TRUST_PROXY_HEADER` | `false` | Trust `X-Forwarded-For` for client IP |
| `ENABLE_PERFORMANCE_LOG` | `false` | Extra per-request log line |
| `ENABLE_AUDIT_LOG` | `false` | Audit log warning for `4xx`/`5xx` |

## Logs

Error logs are written to `logs/error.log` and combined logs to `logs/combined.log`.

## License

MIT