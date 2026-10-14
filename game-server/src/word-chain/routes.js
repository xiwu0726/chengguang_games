'use strict';

/**
 * 词链接龙 REST 路由 (设计文档 429400 约定的接口层)。
 *
 * 挂载前缀: /api/v1  ->  server.js
 *   POST   /games
 *   GET    /games
 *   GET    /games/:gameId
 *   GET    /games/:gameId/history
 *   GET    /games/:gameId/hint
 *   POST   /games/:gameId/join
 *   DELETE /games/:gameId/players/:playerId
 *   POST   /games/:gameId/forfeit
 *   POST   /games/:gameId/words
 *   POST   /games/:gameId/challenges
 *   POST   /games/:gameId/challenges/:challengeId/resolve
 *
 * 错误响应体统一为 { error, message, details? } 外加领域上下文字段
 * (resource / gameId / previousWord / requiredLetter / currentTurnIndex ...)。
 */

const express = require('express');
const { GameError } = require('./game-service');

/** 把 GameError 翻译成 HTTP 响应; 非领域错误交给全局错误处理器 */
function handle(fn, logger) {
  return function wrapped(req, res, next) {
    try {
      return fn(req, res);
    } catch (error) {
      if (error instanceof GameError) {
        const body = { error: error.error, message: error.message };
        for (const key of Object.keys(error)) {
          if (key === 'status' || key === 'error') {
            continue;
          }
          body[key] = error[key];
        }
        if (logger && error.status >= 500) {
          logger.error('Word chain domain error', {
            requestId: req.requestId,
            path: req.path,
            message: error.message
          });
        }
        return res.status(error.status).json(body);
      }
      return next(error);
    }
  };
}

/**
 * 创建词链接龙路由。
 *
 * @param {object} options
 * @param {object} options.service                   createGameService() 的返回值
 * @param {function} [options.rateLimiter]           express 中间件(固定窗口限流)
 * @param {object} [options.logger]                  winston logger
 * @param {string} [options.apiPrefix='/api/v1']     仅用于生成 Location 头
 */
function createWordChainRouter(options) {
  const opts = options || {};
  const service = opts.service;
  const logger = opts.logger;
  const apiPrefix = typeof opts.apiPrefix === 'string' && opts.apiPrefix.length > 0
    ? opts.apiPrefix.replace(/\/+$/, '')
    : '/api/v1';

  if (!service || typeof service.createGame !== 'function') {
    throw new Error('createWordChainRouter requires a game service instance');
  }

  const router = express.Router();

  // 限流: 设计文档 429400 —— 同一 IP 每分钟 10 次 / 每小时 100 次, 超出返回 429 + Retry-After
  if (opts.rateLimiter) {
    router.use(opts.rateLimiter);
  }

  router.post('/games', handle((req, res) => {
    const game = service.createGame(req.body);
    res.setHeader('Location', `${apiPrefix}/games/${game.gameId}`);
    res.status(201).json(game);
  }, logger));

  router.get('/games', handle((req, res) => {
    res.json(service.listGames(req.query));
  }, logger));

  router.get('/games/:gameId', handle((req, res) => {
    res.json(service.getGame(req.params.gameId));
  }, logger));

  // 设计文档 §8: 对局历史 (词链 + 质疑 + 事件流水); ?since=<seq>&limit=<n> 可增量拉取事件
  router.get('/games/:gameId/history', handle((req, res) => {
    res.json(service.getHistory(req.params.gameId, req.query));
  }, logger));

  router.get('/games/:gameId/hint', handle((req, res) => {
    res.json(service.getHint(req.params.gameId, req.query));
  }, logger));

  // 设计文档 §2: 加入对局 { playerName }
  router.post('/games/:gameId/join', handle((req, res) => {
    const result = service.joinGame(req.params.gameId, req.body);
    res.setHeader('Location', `${apiPrefix}/games/${result.gameId}/players/${result.player.id}`);
    res.status(201).json(result);
  }, logger));

  // 设计文档 §4: 离开对局 -> { success: true }
  router.delete('/games/:gameId/players/:playerId', handle((req, res) => {
    res.json(service.leaveGame(req.params.gameId, req.params.playerId));
  }, logger));

  // 胜利条件"对手弃赛"的主动入口
  router.post('/games/:gameId/forfeit', handle((req, res) => {
    res.json(service.forfeitGame(req.params.gameId, req.body));
  }, logger));

  router.post('/games/:gameId/words', handle((req, res) => {
    res.json(service.playWord(req.params.gameId, req.body));
  }, logger));

  router.post('/games/:gameId/challenges', handle((req, res) => {
    res.status(201).json(service.challengeWord(req.params.gameId, req.body));
  }, logger));

  router.post('/games/:gameId/challenges/:challengeId/resolve', handle((req, res) => {
    res.json(service.resolveChallenge(req.params.gameId, req.params.challengeId, req.body));
  }, logger));

  // 该前缀下的其它路径 -> 404 (与全局 404 处理器格式一致)
  router.use((req, res) => {
    res.status(404).json({
      error: 'NotFound',
      message: `Cannot ${req.method} ${req.originalUrl}`,
      requestId: req.requestId
    });
  });

  return router;
}

module.exports = { createWordChainRouter, handle };