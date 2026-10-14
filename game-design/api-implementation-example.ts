/**
 * Word Chain Game API Implementation
 * Demonstrates error handling for 429400 and 500 errors
 */

import express, { Request, Response, NextFunction } from 'express';
import { v4 as uuidv4 } from 'uuid';

// Types
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

// Sample dictionary (in production, use a proper dictionary API)
const DICTIONARY = new Set([
  'apple', 'elephant', 'tiger', 'rabbit', 'goat', 'nest', 'tree', 'elephant',
  'banana', 'ant', 'tiger', 'goat', 'nest', 'tree', 'elephant', 'apple', 'elephant',
  'cat', 'tiger', 'rabbit', 'goat', 'nest', 'tree', 'elephant', 'apple', 'elephant',
  'dog', 'goat', 'nest', 'tree', 'elephant', 'apple', 'elephant', 'banana', 'ant',
  'elephant', 'tiger', 'rabbit', 'goat', 'nest', 'tree', 'elephant', 'apple', 'elephant',
  'frog', 'goat', 'nest', 'tree', 'elephant', 'apple', 'elephant', 'banana', 'ant',
  'gorilla', 'ant', 'tiger', 'goat', 'nest', 'tree', 'elephant', 'apple', 'elephant',
  'horse', 'elephant', 'apple', 'elephant', 'banana', 'ant', 'tiger', 'goat', 'nest',
  'iguana', 'ant', 'tiger', 'goat', 'nest', 'tree', 'elephant', 'apple', 'elephant',
  'jellyfish', 'fish', 'snake', 'eagle', 'goat', 'nest', 'tree', 'elephant', 'apple',
]);

// In-memory storage (in production, use a database)
const games = new Map<string, GameState>();
const challenges = new Map<string, Challenge>();

// Rate limiting
const requestCounts = new Map<string, { count: number; resetTime: number }>();
const RATE_LIMIT_WINDOW = 60 * 1000; // 1 minute
const RATE_LIMIT_MAX = 10;

// Express app setup
const app = express();
app.use(express.json());

// Error handling middleware
app.use((err: Error, req: Request, res: Response, next: NextFunction) => {
  console.error('Unhandled error:', err);
  res.status(500).json({
    error: 'InternalError',
    message: 'An unexpected error occurred. Please try again later.',
  });
});

// 429400 - Rate Limiting Middleware
function checkRateLimit(req: Request, res: Response, next: NextFunction) {
  const ip = req.ip || req.connection.remoteAddress;
  const now = Date.now();

  const countData = requestCounts.get(ip);
  if (countData && countData.resetTime > now) {
    const remaining = Math.max(0, RATE_LIMIT_MAX - countData.count);
    const retryAfter = Math.ceil((countData.resetTime - now) / 1000);

    res.setHeader('X-RateLimit-Limit', RATE_LIMIT_MAX);
    res.setHeader('X-RateLimit-Remaining', remaining);
    res.setHeader('X-RateLimit-Reset', countData.resetTime);
    res.setHeader('Retry-After', retryAfter);

    return res.status(429).json({
      error: 'RateLimitExceeded',
      message: 'Too many requests. Please slow down.',
      retryAfter,
    });
  }

  requestCounts.set(ip, { count: 1, resetTime: now + RATE_LIMIT_WINDOW });
  next();
}

// Utility: Validate word
function validateWord(word: string): boolean {
  if (!word || typeof word !== 'string') return false;
  const trimmed = word.trim().toLowerCase();
  return trimmed.length > 0 && DICTIONARY.has(trimmed);
}

// Utility: Check if word starts with correct letter
function isValidChainWord(lastWord: string, newWord: string): boolean {
  if (!lastWord || !newWord) return false;
  const lastChar = lastWord.trim().toLowerCase().slice(-1);
  const firstChar = newWord.trim().toLowerCase().charAt(0);
  return lastChar === firstChar;
}

// Utility: Validate player turn
function validatePlayerTurn(gameId: string, playerId: string): boolean {
  const game = games.get(gameId);
  if (!game) return false;
  return game.players[game.currentTurnIndex]?.playerId === playerId;
}

// Utility: Validate player in game
function validatePlayerInGame(gameId: string, playerId: string): boolean {
  const game = games.get(gameId);
  if (!game) return false;
  return game.players.some(p => p.playerId === playerId);
}

// Utility: Generate error response
function errorResponse(errorCode: string, message: string, details?: any) {
  return {
    error: errorCode,
    message,
    details,
  };
}

// ==================== GAME MANAGEMENT ENDPOINTS ====================

// Create Game
app.post('/api/v1/games', checkRateLimit, (req: Request, res: Response) => {
  try {
    const { playerName } = req.body;
    if (!playerName || typeof playerName !== 'string') {
      return res.status(400).json(errorResponse(
        'ValidationError',
        'Player name is required and must be a string',
        { field: 'playerName' }
      ));
    }

    const gameId = uuidv4();
    const player: Player = {
      playerId: uuidv4(),
      name: playerName.trim(),
    };

    const gameState: GameState = {
      gameId,
      players: [player],
      currentTurnIndex: 0,
      wordChain: [],
      status: 'active',
      createdAt: new Date(),
      updatedAt: new Date(),
    };

    games.set(gameId, gameState);

    res.json({
      gameId,
      players: gameState.players,
      status: gameState.status,
    });
  } catch (err) {
    console.error('Error creating game:', err);
    throw err; // Will be caught by error middleware
  }
});

// Join Game
app.post('/api/v1/games/:gameId/join', checkRateLimit, (req: Request, res: Response) => {
  try {
    const { gameId } = req.params;
    const { playerName } = req.body;

    if (!validatePlayerInGame(gameId, '')) {
      return res.status(404).json(errorResponse(
        'NotFound',
        'Game not found',
        { resource: 'game', gameId }
      ));
    }

    if (!playerName || typeof playerName !== 'string') {
      return res.status(400).json(errorResponse(
        'ValidationError',
        'Player name is required and must be a string',
        { field: 'playerName' }
      ));
    }

    const game = games.get(gameId);
    if (game.status !== 'active') {
      return res.status(400).json(errorResponse(
        'ValidationError',
        'Game is no longer active',
        { status: game.status }
      ));
    }

    const newPlayer: Player = {
      playerId: uuidv4(),
      name: playerName.trim(),
    };

    game.players.push(newPlayer);
    game.updatedAt = new Date();

    res.json({
      gameId: game.gameId,
      players: game.players,
      currentTurnIndex: game.currentTurnIndex,
      wordChain: game.wordChain,
    });
  } catch (err) {
    console.error('Error joining game:', err);
    throw err;
  }
});

// Get Game State
app.get('/api/v1/games/:gameId', checkRateLimit, (req: Request, res: Response) => {
  try {
    const { gameId } = req.params;

    if (!games.has(gameId)) {
      return res.status(404).json(errorResponse(
        'NotFound',
        'Game not found',
        { resource: 'game', gameId }
      ));
    }

    const game = games.get(gameId);
    res.json(game);
  } catch (err) {
    console.error('Error getting game state:', err);
    throw err;
  }
});

// Leave Game
app.delete('/api/v1/games/:gameId/players/:playerId', checkRateLimit, (req: Request, res: Response) => {
  try {
    const { gameId, playerId } = req.params;

    if (!games.has(gameId)) {
      return res.status(404).json(errorResponse(
        'NotFound',
        'Game not found',
        { resource: 'game', gameId }
      ));
    }

    const game = games.get(gameId);
    if (game.players.length <= 1) {
      return res.status(400).json(errorResponse(
        'ValidationError',
        'Cannot leave the last player in the game',
        { playersRemaining: game.players.length }
      ));
    }

    game.players = game.players.filter(p => p.playerId !== playerId);
    game.updatedAt = new Date();

    res.json({ success: true });
  } catch (err) {
    console.error('Error leaving game:', err);
    throw err;
  }
});

// ==================== GAMEPLAY ENDPOINTS ====================

// Play Word
app.post('/api/v1/games/:gameId/play', checkRateLimit, (req: Request, res: Response) => {
  try {
    const { gameId } = req.params;
    const { playerId, word } = req.body;

    if (!validatePlayerInGame(gameId, playerId)) {
      return res.status(404).json(errorResponse(
        'NotFound',
        'Game or player not found',
        { resource: 'game', playerId }
      ));
    }

    if (!validatePlayerTurn(gameId, playerId)) {
      return res.status(403).json(errorResponse(
        'Forbidden',
        'It is not your turn to play',
        { currentTurnIndex: games.get(gameId)!.currentTurnIndex }
      ));
    }

    const game = games.get(gameId);
    if (game.status !== 'active') {
      return res.status(400).json(errorResponse(
        'ValidationError',
        'Game is no longer active',
        { status: game.status }
      ));
    }

    if (!word || typeof word !== 'string') {
      return res.status(400).json(errorResponse(
        'ValidationError',
        'Word is required and must be a string',
        { field: 'word' }
      ));
    }

    const trimmedWord = word.trim().toLowerCase();

    // Validate word is in dictionary
    if (!validateWord(trimmedWord)) {
      return res.status(400).json(errorResponse(
        'ValidationError',
        'Word is not in the dictionary',
        { word: trimmedWord }
      ));
    }

    // Validate word starts with correct letter
    if (game.wordChain.length > 0) {
      const lastWord = game.wordChain[game.wordChain.length - 1];
      if (!isValidChainWord(lastWord, trimmedWord)) {
        return res.status(400).json(errorResponse(
          'ValidationError',
          'Word must start with the last letter of the previous word',
          {
            previousWord: lastWord,
            lastLetter: lastWord.slice(-1),
            newWord: trimmedWord,
            firstLetter: trimmedWord.charAt(0),
          }
        ));
      }
    }

    // Add word to chain
    game.wordChain.push(trimmedWord);
    game.updatedAt = new Date();

    // Check if game should end (10 turns)
    if (game.wordChain.length >= 10) {
      game.status = 'completed';
      game.winner = undefined; // Draw
    } else {
      // Advance turn
      game.currentTurnIndex = (game.currentTurnIndex + 1) % game.players.length;
    }

    res.json({
      gameId: game.gameId,
      wordChain: game.wordChain,
      currentTurnIndex: game.currentTurnIndex,
      status: game.status,
    });
  } catch (err) {
    console.error('Error playing word:', err);
    throw err;
  }
});

// Challenge Word
app.post('/api/v1/games/:gameId/challenge', checkRateLimit, (req: Request, res: Response) => {
  try {
    const { gameId } = req.params;
    const { playerId, challengedWordIndex } = req.body;

    if (!validatePlayerInGame(gameId, playerId)) {
      return res.status(404).json(errorResponse(
        'NotFound',
        'Game or player not found',
        { resource: 'game', playerId }
      ));
    }

    const game = games.get(gameId);
    if (game.status !== 'active') {
      return res.status(400).json(errorResponse(
        'ValidationError',
        'Game is no longer active',
        { status: game.status }
      ));
    }

    if (challengedWordIndex < 0 || challengedWordIndex >= game.wordChain.length) {
      return res.status(400).json(errorResponse(
        'ValidationError',
        'Invalid word index to challenge',
        { challengedWordIndex, maxIndex: game.wordChain.length - 1 }
      ));
    }

    const challenger = game.players.find(p => p.playerId === playerId);
    if (!challenger) {
      return res.status(404).json(errorResponse(
        'NotFound',
        'Player not found',
        { playerId }
      ));
    }

    // Only the next player can challenge
    const nextPlayerIndex = (game.currentTurnIndex + 1) % game.players.length;
    const nextPlayer = game.players[nextPlayerIndex];

    if (challenger.playerId !== nextPlayer.playerId) {
      return res.status(403).json(errorResponse(
        'Forbidden',
        'Only the next player can challenge',
        { currentTurnIndex: game.currentTurnIndex, nextPlayer: nextPlayer.playerId }
      ));
    }

    const challengedWord = game.wordChain[challengedWordIndex];
    const challengeId = uuidv4();

    const challenge: Challenge = {
      challengeId,
      gameId,
      challengerId: playerId,
      challengedWordIndex,
      challengedWord,
      challengeStatus: 'pending',
      challengeTimestamp: new Date(),
    };

    challenges.set(challengeId, challenge);

    res.json({
      challengeId,
      challengeStatus: 'pending',
    });
  } catch (err) {
    console.error('Error creating challenge:', err);
    throw err;
  }
});

// Resolve Challenge
app.post('/api/v1/games/:gameId/challenge/:challengeId/resolve', checkRateLimit, (req: Request, res: Response) => {
  try {
    const { gameId, challengeId } = req.params;
    const { playerId, isValid, reason } = req.body;

    if (!validatePlayerInGame(gameId, playerId)) {
      return res.status(404).json(errorResponse(
        'NotFound',
        'Game or player not found',
        { resource: 'game', playerId }
      ));
    }

    const challenge = challenges.get(challengeId);
    if (!challenge) {
      return res.status(404).json(errorResponse(
        'NotFound',
        'Challenge not found',
        { challengeId }
      ));
    }

    if (challenge.gameId !== gameId) {
      return res.status(400).json(errorResponse(
        'ValidationError',
        'Challenge does not belong to this game',
        { challengeId, gameId }
      ));
    }

    if (challenge.challengeStatus !== 'pending') {
      return res.status(400).json(errorResponse(
        'ValidationError',
        'Challenge is no longer pending',
        { challengeStatus: challenge.challengeStatus }
      ));
    }

    // Only the challenged player can resolve
    const game = games.get(gameId);
    if (!game) {
      return res.status(404).json(errorResponse(
        'NotFound',
        'Game not found',
        { gameId }
      ));
    }

    const challengedPlayer = game.players[challenge.challengedWordIndex];
    if (challengedPlayer.playerId !== playerId) {
      return res.status(403).json(errorResponse(
        'Forbidden',
        'Only the player who played the challenged word can resolve the challenge',
        { challengedPlayer: challengedPlayer.playerId }
      ));
    }

    // Resolve the challenge
    challenge.challengeStatus = 'resolved';
    challenge.challengeResult = isValid ? 'valid' : 'invalid';
    challenge.challengeTimestamp = new Date();

    if (!isValid) {
      // Word was invalid - game forfeited
      game.status = 'forfeited';
      game.winner = challenge.challengerId;

      // Remove all words from the challenged word onwards
      game.wordChain = game.wordChain.slice(0, challenge.challengedWordIndex);
    } else {
      // Word was valid - continue game
      // Advance turn to the challenger
      game.currentTurnIndex = game.players.findIndex(p => p.playerId === challenge.challengerId);
    }

    game.updatedAt = new Date();

    res.json({
      challengeResult: challenge.challengeResult,
      gameStatus: game.status,
    });
  } catch (err) {
    console.error('Error resolving challenge:', err);
    throw err;
  }
});

// Get Game History
app.get('/api/v1/games/:gameId/history', checkRateLimit, (req: Request, res: Response) => {
  try {
    const { gameId } = req.params;

    if (!games.has(gameId)) {
      return res.status(404).json(errorResponse(
        'NotFound',
        'Game not found',
        { resource: 'game', gameId }
      ));
    }

    const game = games.get(gameId);
    res.json({
      wordChain: game.wordChain,
      challenges: Array.from(challenges.values()).filter(c => c.gameId === gameId),
    });
  } catch (err) {
    console.error('Error getting game history:', err);
    throw err;
  }
});

// Start server
const PORT = process.env.PORT || 3000;
app.listen(PORT, () => {
  console.log(`Word Chain Game API running on port ${PORT}`);
  console.log(`API version: v1`);
  console.log(`Dictionary size: ${DICTIONARY.size}`);
});

export default app;
