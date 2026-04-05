# 08 — API Reference

## Base URL

```
Development:  http://localhost:5170/api
Production:   https://<deployed-domain>/api
```

## Conventions

| Convention | Detail |
|-----------|--------|
| Format | JSON (`application/json`) |
| Authentication | None (open API — future: JWT) |
| Date format | ISO 8601 (`2026-01-11T12:00:00Z`) |
| ID format | `{PREFIX}-{unix_ms}-{6_hex}` |
| Error format | `{ "error": { "code": "...", "message": "..." } }` |

---

## Game Endpoints

### POST `/api/game/start`

Start a new game session.

**Request Body** (optional — omit or send `{}` for guest mode):

```json
{
  "userId": "USR-1736588400000-A1B2C3",
  "difficulty": 25
}
```

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `userId` | string | No | null | User ID for tracked games. Null = guest. |
| `difficulty` | int | No | 10 | Difficulty level (0-100) |

**Response** `200 OK`:

```json
{
  "sessionId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "words": [
    { "index": 0, "displayText": "Actions",  "isHidden": false, "isRevealed": true  },
    { "index": 1, "displayText": null,        "isHidden": true,  "isRevealed": false },
    { "index": 2, "displayText": "louder",    "isHidden": false, "isRevealed": true  },
    { "index": 3, "displayText": "than",      "isHidden": false, "isRevealed": true  },
    { "index": 4, "displayText": null,        "isHidden": true,  "isRevealed": false }
  ],
  "score": 0,
  "isComplete": false
}
```

---

### GET `/api/game/{sessionId}`

Get current game state.

**Response** `200 OK`: Same schema as Start Game response.

**Response** `404`:

```json
{ "message": "Game session not found" }
```

---

### POST `/api/game/{sessionId}/guess`

Submit a guess for a hidden word.

**Request Body**:

```json
{
  "wordIndex": 1,
  "guess": "speak"
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `wordIndex` | int | Yes | Index of the hidden word to guess |
| `guess` | string | Yes | The guessed word (case-insensitive comparison) |

**Response** `200 OK`:

```json
{
  "isCorrect": true,
  "isPhraseComplete": false,
  "currentScore": 100,
  "revealedWord": "speak"
}
```

| Field | Type | Description |
|-------|------|-------------|
| `isCorrect` | bool | Whether the guess matched |
| `isPhraseComplete` | bool | Whether all words are now revealed |
| `currentScore` | int | Running score total |
| `revealedWord` | string? | The actual word (only when correct) |

**Response** `404`: Session not found.

---

### POST `/api/game/{sessionId}/give-up`

Give up and reveal all hidden words. Score resets to 0.

**Response** `200 OK`: Full game state with all words revealed.

**Response** `404`: Session not found.

---

### GET `/api/game/{sessionId}/record`

Get the game record for a completed or in-progress game.

**Response** `200 OK`:

```json
{
  "gameId": "GAME-1736588400000-D4E5F6",
  "playedAt": "2026-01-11T13:00:00Z",
  "completedAt": "2026-01-11T13:05:32Z",
  "score": 300,
  "phraseId": 12345,
  "phraseText": "Actions speak louder than words",
  "difficulty": 25,
  "result": "Solved",
  "events": [
    {
      "$type": "clue",
      "eventType": "clue",
      "wordIndex": 1,
      "searchTerm": "articulate",
      "url": "https://www.merriam-webster.com/dictionary/articulate",
      "timestamp": "2026-01-11T13:00:05Z"
    },
    {
      "$type": "guess",
      "eventType": "guess",
      "wordIndex": 1,
      "guessText": "speak",
      "isCorrect": true,
      "pointsAwarded": 100,
      "timestamp": "2026-01-11T13:00:25Z"
    }
  ],
  "isCompleted": true
}
```

**Response** `404`: Session not found or guest session (no record available).

---

## Clue Endpoints

### GET `/api/clue/{sessionId}/{wordIndex}`

Get a clue URL for a hidden word. Returns a synonym-based web search result.

**Query Parameters**:

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `excludeUrl` | string[] | No | URLs to exclude from results (repeatable) |

**Example**: `GET /api/clue/{sid}/1?excludeUrl=https://dead-link.com&excludeUrl=https://blocked.com`

**Response** `200 OK`:

```json
{
  "url": "https://www.merriam-webster.com/dictionary/articulate",
  "searchTerm": "articulate"
}
```

| Field | Type | Description |
|-------|------|-------------|
| `url` | string | The clue URL to show the player |
| `searchTerm` | string | The synonym used for the search |

**Response** `404`: Session or word not found.

**Response** `400`: Word is not hidden.

---

## User Endpoints

### POST `/api/user`

Create a new user.

**Request Body**:

```json
{
  "name": "PlayerOne",
  "email": "player@example.com"
}
```

**Response** `201 Created`:

```json
{
  "uniqueId": "USR-1736588400000-A1B2C3",
  "name": "PlayerOne",
  "email": "player@example.com",
  "lifetimePoints": 0,
  "preferredDifficulty": 10,
  "gamesPlayed": 0,
  "createdAt": "2026-01-11T12:00:00Z"
}
```

**Response** `400`: Validation error.

**Response** `409`:

```json
{ "error": { "code": "NAME_TAKEN", "message": "Username already exists" } }
```

---

### GET `/api/user/{uniqueId}`

Get user by ID.

**Response** `200 OK`: User object.

**Response** `404`:

```json
{ "error": { "code": "USER_NOT_FOUND", "message": "User with given ID not found" } }
```

---

### GET `/api/user/by-email/{email}`

Get user by email address.

**Response** `200 OK`: User object.

**Response** `404`: Not found.

---

### PUT `/api/user/{uniqueId}`

Update user name and email.

**Request Body**:

```json
{
  "name": "NewName",
  "email": "newemail@example.com"
}
```

**Response** `200 OK`: Updated user object.

**Response** `400 / 404 / 409`: Validation, not found, or conflict.

---

### DELETE `/api/user/{uniqueId}`

Delete a user and all their data.

**Response** `204 No Content`.

**Response** `404`: Not found.

---

### PATCH `/api/user/{uniqueId}/difficulty`

Update preferred difficulty level.

**Request Body**:

```json
{ "difficulty": 50 }
```

**Response** `200 OK`:

```json
{
  "uniqueId": "USR-1736588400000-A1B2C3",
  "preferredDifficulty": 50
}
```

**Response** `400`: Difficulty out of range (0-100).

---

### POST `/api/user/{uniqueId}/points`

Add points to lifetime total.

**Request Body**:

```json
{ "points": 100 }
```

**Response** `200 OK`:

```json
{
  "uniqueId": "USR-1736588400000-A1B2C3",
  "lifetimePoints": 1500,
  "pointsAdded": 100
}
```

**Response** `400`: Negative points.

---

### GET `/api/user/check-name/{name}`

Check if a username is available.

**Response** `200 OK`:

```json
{ "available": true }
```

---

### GET `/api/user/check-email/{email}`

Check if an email is available.

**Response** `200 OK`:

```json
{ "available": true }
```

---

### GET `/api/user/{uniqueId}/games`

Get a user's game history.

**Response** `200 OK`: Array of `GameRecord` objects ordered by `playedAt` descending.

---

## LLM Endpoints

### POST `/api/llm/test`

Test LLM connectivity (development/debug tool).

**Request Body**:

```json
{
  "prompt": "Say hello",
  "systemPrompt": "You are a helpful assistant"
}
```

**Response** `200 OK`:

```json
{
  "success": true,
  "content": "Hello! How can I help you?",
  "model": "gpt-4o-mini",
  "promptTokens": 15,
  "completionTokens": 8,
  "totalTokens": 23
}
```

---

## Error Codes

| Code | HTTP Status | Description |
|------|-------------|-------------|
| `VALIDATION_ERROR` | 400 | Input validation failed |
| `NAME_TAKEN` | 409 | Username already exists |
| `EMAIL_TAKEN` | 409 | Email already registered |
| `USER_NOT_FOUND` | 404 | User with given ID not found |
| `INVALID_EMAIL` | 400 | Email format is invalid |
| `INVALID_DIFFICULTY` | 400 | Difficulty out of range (0-100) |
| `INVALID_POINTS` | 400 | Points must be non-negative |

---

## HTTP Status Code Summary

| Status | Usage |
|--------|-------|
| `200 OK` | Successful read, update, or action |
| `201 Created` | Successful entity creation |
| `204 No Content` | Successful deletion |
| `400 Bad Request` | Validation failure |
| `404 Not Found` | Entity or session not found |
| `409 Conflict` | Duplicate name or email |
| `500 Internal Server Error` | Unexpected server error |

---

*Next: [09 — UI & Interaction Design](09-ui-and-interaction-design.md)*
