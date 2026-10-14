# Word Chain Game - Design Document

## Overview
A simple multiplayer Word Chain game where players take turns adding words to a chain. Each word must start with the last letter of the previous word.

## Game Rules

1. **Basic Gameplay**
   - A game consists of a chain of words
   - Players alternate turns
   - On each turn, a player adds one word that starts with the last letter of the previous word
   - Players can challenge invalid words (not in dictionary, doesn't start with correct letter)
   - A word chain is valid for 10 turns or until a player challenges an invalid word

2. **Player Roles**
   - **Creator**: Starts a new game and invites players
   - **Challenger**: Can challenge an opponent's word
   - **Defender**: Must prove their word is valid or forfeit

3. **Winning Conditions**
   - Opponent forfeits (cannot find a valid word)
   - Chain reaches 10 valid words (game ends in draw)
   - Opponent challenges and loses (invalid word)

## Data Structures

### Game State
```typescript
interface GameState {
  gameId: string;
  players: Player[];
  currentTurnIndex: number;
  wordChain: string[];
  status: 'active' | 'completed' | 'forfeited';
  winner?: Player;
  createdAt: Date;
  updatedAt: Date;
}

interface Player {
  playerId: string;
  name: string;
  isBot?: boolean;
  score?: number;
}
```

### Challenge State
```typescript
interface Challenge {
  challengeId: string;
  gameId: string;
  challengerId: string;
  challengedWordIndex: number;
  challengedWord: string;
  challengeStatus: 'pending' | 'accepted' | 'rejected' | 'resolved';
  challengeResult?: 'valid' | 'invalid';
  challengeTimestamp: Date;
}
```

### Dictionary (Simplified)
```typescript
const DICTIONARY: Set<string> = new Set([
  // Sample words - in production, use a proper dictionary API
  'apple', 'elephant', 'tiger', 'rabbit', 'tiger', 'goat', 'tiger', 'nest', 'tree', 'elephant',
  'banana', 'ant', 'tiger', 'goat', 'tiger', 'nest', 'tree', 'elephant', 'apple', 'elephant',
  // ... more words
]);
```

## RESTful API Endpoints

### Game Management

#### 1. Create Game
```
POST /api/games
Request Body: { "playerName": "string" }
Response: { "gameId": "string", "players": [...], "status": "active" }
```

#### 2. Join Game
```
POST /api/games/{gameId}/join
Request Body: { "playerName": "string" }
Response: { "gameId": "string", "players": [...], "currentTurnIndex": 0, "wordChain": [] }
```

#### 3. Get Game State
```
GET /api/games/{gameId}
Response: GameState
```

#### 4. Leave Game
```
DELETE /api/games/{gameId}/players/{playerId}
Response: { "success": true }
```

### Gameplay

#### 5. Play Word
```
POST /api/games/{gameId}/play
Request Body: { "playerId": "string", "word": "string" }
Response: { "gameId": "string", "wordChain": [...], "currentTurnIndex": number }
```

#### 6. Challenge Word
```
POST /api/games/{gameId}/challenge
Request Body: { "playerId": "string", "challengedWordIndex": number }
Response: { "challengeId": "string", "challengeStatus": "pending" }
```

#### 7. Resolve Challenge
```
POST /api/games/{gameId}/challenge/{challengeId}/resolve
Request Body: { "playerId": "string", "isValid": boolean, "reason?: string" }
Response: { "challengeResult": "valid" | "invalid", "gameStatus": "active" | "completed" }
```

#### 8. Get Game History
```
GET /api/games/{gameId}/history
Response: { "wordChain": [...], "challenges": [...] }
```

## Error Handling

### Error Codes and Messages

#### 429400 - Rate Limiting
```
{
  "error": "RateLimitExceeded",
  "message": "Too many requests. Please slow down.",
  "retryAfter": 60
}
```

**When to occur:**
- More than 10 requests per minute from the same IP
- More than 100 requests per hour from the same IP
- Bot detection (suspicious pattern of requests)

**Handling:**
- Return HTTP 429 with Retry-After header
- Client should respect Retry-After and retry after the specified delay
- Implement exponential backoff for retries

#### 500 - Internal Server Error
```
{
  "error": "InternalError",
  "message": "An unexpected error occurred. Please try again later.",
  "details?: string"
}
```

**When to occur:**
- Database connection failures
- Unhandled exceptions in game logic
- Invalid word dictionary lookup
- Concurrent modification conflicts
- File system errors

**Handling:**
- Log full error details for debugging
- Return generic error message to client
- Implement retry mechanisms for transient errors
- Monitor error rates for alerting

### Validation Errors (4xx)

#### 400 - Bad Request
```
{
  "error": "ValidationError",
  "message": "Invalid request parameters",
  "details": [
    { "field": "word", "message": "Word cannot be empty" }
  ]
}
```

**When to occur:**
- Empty or null request parameters
- Invalid word format
- Player not in game
- Game not found

#### 404 - Not Found
```
{
  "error": "NotFound",
  "message": "Game not found",
  "resource": "game"
}
```

**When to occur:**
- Game ID doesn't exist
- Player not found in game

#### 403 - Forbidden
```
{
  "error": "Forbidden",
  "message": "You are not allowed to perform this action",
  "requiredRole": "player"
}
```

**When to occur:**
- Player trying to play when it's not their turn
- Player trying to challenge when not opponent
- Unauthorized access

## API Versioning
- Current version: v1
- URL prefix: /api/v1
- Future: /api/v2 with backward compatibility

## Security Considerations
1. Rate limiting on all endpoints
2. Input validation on all user-provided data
3. Authentication for game management endpoints
4. CORS configuration for frontend integration
5. SQL injection prevention (parameterized queries)
6. XSS prevention (sanitize user input)

## Performance Considerations
1. Cache frequently accessed game states
2. Use Redis for session management
3. Implement connection pooling for database
4. Optimize word dictionary lookup (Trie data structure)
5. Use WebSockets for real-time game updates (future enhancement)
