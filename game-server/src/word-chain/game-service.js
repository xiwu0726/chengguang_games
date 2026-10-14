'use strict';

/**
 * 词链接龙核心领域服务 (内存态, 无外部依赖)。
 *
 * 契约来源: game-design/word-chain-game-design.md
 *   POST /api/v1/games                                     创建游戏 (2-4 名玩家)
 *   POST /api/v1/games/:gameId/join                         加入游戏 (设计文档 §2)
 *   POST /api/v1/games/:gameId/words                        提交单词
 *   POST /api/v1/games/:gameId/challenges                   质疑单词
 *   POST /api/v1/games/:gameId/challenges/:challengeId/resolve  裁决质疑
 *   DELETE /api/v1/games/:gameId/players/:playerId          离开游戏 (设计文档 §4)
 *   POST /api/v1/games/:gameId/forfeit                      主动认输 (胜利条件"对手弃赛")
 *   GET  /api/v1/games/:gameId                              查询状态
 *   GET  /api/v1/games/:gameId/history                      对局历史 (设计文档 §8)
 *   (扩展) GET /api/v1/games                                游戏列表
 *   (扩展) GET /api/v1/games/:gameId/hint                   提示
 *
 * 规则要点(文档未写明的部分在此显式固定, 便于测试与评审):
 *   - 接龙: 新词首字母必须等于链上最后一词的尾字母; 第一个词不限制首字母。
 *   - 去重: 同一个词不可在一条链里重复出现(服务端强校验)。
 *   - 得分: 每个字母 1 分(提交时累加到提交者)。
 *   - 回合: 提交成功后轮转到下一位玩家。
 *   - 结束: 链长达到 maxChainLength(默认 10) 时 settle, 最高分获胜, 平分则 winner 缺省。
 *   - 质疑: 任意玩家可质疑他人提交的词(不必轮到自己); 存在待裁决质疑时禁止继续提交单词。
 *   - 裁决: 只有被质疑词的提交者可以裁决; 服务端用词库独立复核, 客户端传入的 isValid 仅作为
 *           "是否与服务器判定一致"的对照记录(mismatch), 不作为裁决依据 —— 客户端校验不可信。
 *   - 裁决失败: 被质疑的词及其之后的所有词从链上移除, 游戏状态置 forfeited, 质疑者获胜。
 *   - 裁决成功: 回合轮转到质疑者。
 *   - 加入: 只有首词落地之前(链为空)才能加入, 且人数上限仍是 MAX_PLAYERS; 开局后加入会破坏
 *           回合顺序与已提交的链, 因此拒绝(403)。
 *   - 离开: 存在待裁决质疑时禁止离开(与"质疑期间冻结对局"一致); 离开后不足 MIN_PLAYERS 人,
 *           按"对手弃赛"判负, 剩下的那名玩家获胜。
 *   - 认输: 提交者主动弃赛, 状态置 forfeited; 胜者取剩余玩家中分数最高者, 平分则无胜者。
 *   - 终局: 状态不再是 active 之后, 任何会改动链上/回合的动作一律拒绝。
 *   - 机器人 (设计文档 Data Structures 的 Player.isBot): 创建/加入时可标记 isBot, 轮到它时
 *           由服务端从词库挑词并走与真人完全相同的出词校验路径; 连续多个机器人会一直自动出词
 *           直到轮到真人或对局结束(循环次数以 maxChainLength 为硬上界)。
 *   - 机器人无词可出: 按"对手弃赛"收尾(endedBy='bot_stuck'), 机器人判负, 不抛 500。
 *   - 玩家名净化 (设计文档 Security Considerations #6): 剔除控制字符、折叠空白, 含 HTML
 *           元字符 < > & 与属性上下文元字符 ' " ` =, 以及 javascript:/vbscript:/data: 之类的
 *           名字直接 400 拒绝(不静默剥离, 避免剥完与真实名字撞名)。
 */

const {
  createDictionary,
  normalizeWord,
  MIN_WORD_LENGTH,
  MAX_WORD_LENGTH
} = require('./dictionary');

const DEFAULT_MAX_CHAIN_LENGTH = 10;
const MIN_PLAYERS = 2;
const MAX_PLAYERS = 4;
const MAX_PLAYER_NAME_LENGTH = 40;
const SCORE_PER_LETTER = 1;

const GAME_STATUS = {
  ACTIVE: 'active',
  COMPLETED: 'completed',
  FORFEITED: 'forfeited'
};

/**
 * 领域错误: 由路由层翻译成设计文档约定的 JSON 响应体。
 *
 * @property {number} status HTTP 状态码
 * @property {string} error  机器可读错误名 (ValidationError / NotFound / Forbidden ...)
 * @property {Array<{field: string, message: string}>} [details]
 */
class GameError extends Error {
  constructor(status, error, message, options) {
    super(message);
    this.name = 'GameError';
    this.status = status;
    this.error = error;
    const opts = options || {};
    if (Array.isArray(opts.details) && opts.details.length > 0) {
      this.details = opts.details;
    }
    if (opts.extra && typeof opts.extra === 'object') {
      Object.assign(this, opts.extra);
    }
  }
}

function isPlainObject(value) {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function toPositiveInt(value, fallback) {
  const parsed = Number(value);
  if (!Number.isFinite(parsed) || parsed <= 0) {
    return fallback;
  }
  return Math.floor(parsed);
}

function validationError(field, message) {
  return new GameError(400, 'ValidationError', 'Invalid request parameters', {
    details: [{ field, message }]
  });
}

/**
 * 玩家名净化 (设计文档 Security Considerations #6 "XSS prevention (sanitize user input)")。
 * 创建与加入共用这一处, 避免两条入口出现口径漂移。
 *
 * 顺序(有意如此):
 *   1. 剔除控制字符 U+0000-U+001F / U+007F —— 换行、制表、ANSI 转义等不该进回显与日志;
 *      先剔再 trim, 否则末尾的 \n 会连带"看不见的空白"一起被裁掉, 掩盖真实输入;
 *   2. 折叠内部连续空白并去掉首尾空白;
 *   3. 元字符检查 —— 含 < > & 或 javascript: 之类直接拒绝, 不做静默剥离:
 *      'A<script>B' 若被剥成 'AscriptB' 反而会与真实存在的 'AscriptB' 撞名;
 *      引号 / 反引号 / 等号同样拒绝: 名字一旦被裸拼进属性上下文 (name="...") 就是逃逸点,
 *      服务端只保证存储与流水安全, 输出端仍须转义。
 *   4. 空名 / 长度上限。
 */
const CONTROL_CHAR_PATTERN = /[\u0000-\u001F\u007F]/g;
const DISALLOWED_NAME_PATTERN = /[<>&"'`=]|javascript:|vbscript:|data:/i;

function sanitizePlayerName(rawName, field) {
  const source = typeof rawName === 'string' ? rawName : '';
  const withoutControl = source.replace(CONTROL_CHAR_PATTERN, '');
  const name = withoutControl.replace(/\s+/g, ' ').trim();
  if (name.length === 0) {
    throw validationError(field, 'Player name cannot be empty');
  }
  const offending = DISALLOWED_NAME_PATTERN.exec(name);
  if (offending) {
    // 只报第一个越界片段, 不把整串码位连成 (U+003C)(U+0026)(U+0022)... 那种读不懂的东西。
    // 元字符类匹配长度恒为 1; 关键字匹配是固定的 ASCII 词, 回显它是安全的(名字整体不回显)。
    const token = offending[0];
    const readable = token.length === 1
      ? `offending character U+${token.codePointAt(0).toString(16).toUpperCase().padStart(4, '0')}`
      : `offending sequence "${token}"`;
    const totalSpans = (name.match(new RegExp(DISALLOWED_NAME_PATTERN.source, 'gi')) || []).length;
    const suffix = totalSpans > 1 ? `, ${totalSpans} offending spans total` : '';
    throw validationError(
      field,
      `Player name must not contain HTML metacharacters or script content (${readable}${suffix})`
    );
  }
  if (name.length > MAX_PLAYER_NAME_LENGTH) {
    throw validationError(field, `Player name cannot exceed ${MAX_PLAYER_NAME_LENGTH} characters`);
  }
  return name;
}

/** 机器人标记: 只有布尔 true 才算机器人, 其余写法(含 'true' 字符串)一律按 false, 与缺省行为一致 */
function normalizeIsBot(value) {
  return value === true;
}

function firstLetter(word) {
  return word.length > 0 ? word[0] : '';
}

const LIST_CURSOR_PREFIX = 'v1';

/**
 * 对局列表的游标: 把排序锚点 (createdAt, sequence) 编码成不透明字符串。
 * 用 keyset(锚点)而不是 offset, 因为中途有人开局/结局都不会让某一页重复或漏项。
 */
function encodeListCursor(game) {
  const raw = `${LIST_CURSOR_PREFIX}:${game.createdAt}:${game.sequence}`;
  return Buffer.from(raw, 'utf8').toString('base64url');
}

/** @returns {{createdAt: string, sequence: number}} 解析失败 -> 400 */
function decodeListCursor(raw) {
  const invalid = validationError('cursor', 'cursor is not a valid pagination cursor');
  const text = String(raw);
  if (text.length === 0 || text.length > 200) {
    throw invalid;
  }
  let decoded;
  try {
    decoded = Buffer.from(text, 'base64url').toString('utf8');
  } catch {
    throw invalid;
  }
  const matched = /^([^:]+):(.+):(\d+)$/.exec(decoded);
  if (!matched || matched[1] !== LIST_CURSOR_PREFIX) {
    throw invalid;
  }
  const sequence = Number(matched[3]);
  if (!Number.isSafeInteger(sequence) || sequence <= 0) {
    throw invalid;
  }
  return { createdAt: matched[2], sequence };
}

/**
 * 事件增量拉取的锚点: 非负整数 seq。
 * 非法值直接 400 —— 客户端把游标写错却拿到全量, 比报错更难查。
 * @returns {number|null} null 表示不裁剪
 */
function parseEventSince(raw) {
  if (raw === undefined || raw === null || String(raw).trim() === '') {
    return null;
  }
  const value = Number(raw);
  if (!Number.isSafeInteger(value) || value < 0) {
    throw validationError('since', 'since must be a non-negative integer event sequence');
  }
  return value;
}

/** 列表排序: 新局在前 (createdAt desc, 同刻则 sequence desc) */
function compareGamesNewestFirst(a, b) {
  if (a.createdAt !== b.createdAt) {
    return a.createdAt < b.createdAt ? 1 : -1;
  }
  return b.sequence - a.sequence;
}

function lastLetter(word) {
  return word.length > 0 ? word[word.length - 1] : '';
}

/**
 * 创建词链接龙服务。
 *
 * @param {object} [options]
 * @param {object} [options.dictionary]     自定义词库(默认内置)
 * @param {number} [options.maxChainLength] 结束链长, 默认 10
 * @param {number} [options.hintLimit]      提示条数, 默认 3
 * @param {function} [options.idGenerator]  自定义 id 生成器 (prefix) => string
 * @param {function} [options.clock]        时钟 () => epochMs | ISO string, 便于测试
 */
function createGameService(options) {
  const opts = options || {};
  const dictionary = opts.dictionary || createDictionary();
  const maxChainLength = toPositiveInt(opts.maxChainLength, DEFAULT_MAX_CHAIN_LENGTH);
  const defaultHintLimit = toPositiveInt(opts.hintLimit, 3);
  const clock = typeof opts.clock === 'function' ? opts.clock : Date.now;

  let idSeq = 0;
  const idGenerator = typeof opts.idGenerator === 'function'
    ? opts.idGenerator
    : (prefix) => {
      idSeq += 1;
      return `${prefix}-${Date.now().toString(36)}-${idSeq.toString(36)}`;
    };

  /** @type {Map<string, object>} gameId -> game */
  const games = new Map();
  /** 创建序号: 列表分页的游标锚点, 比 createdAt 更确定(同一毫秒开局也不会串页) */
  let gameSeq = 0;
  /** @type {Map<string, object>} challengeId -> challenge */
  const challenges = new Map();

  function timestamp() {
    const value = clock();
    return typeof value === 'number' ? new Date(value).toISOString() : String(value);
  }

  function findGame(gameId) {
    const game = games.get(String(gameId));
    if (!game) {
      throw new GameError(404, 'NotFound', 'Game not found', {
        extra: { resource: 'game', gameId: String(gameId) }
      });
    }
    return game;
  }

  function findPlayer(game, playerId) {
    const id = typeof playerId === 'string' ? playerId.trim() : '';
    const player = id.length > 0 ? game.players.find((item) => item.id === id) : undefined;
    if (!player) {
      throw new GameError(404, 'NotFound', 'Player not found in this game', {
        extra: { resource: 'player', gameId: game.gameId, playerId: id }
      });
    }
    return player;
  }

  function requireActive(game) {
    if (game.status !== GAME_STATUS.ACTIVE) {
      throw new GameError(400, 'ValidationError', `Game is not active (status: ${game.status})`, {
        details: [{ field: 'gameId', message: `Game status is ${game.status}` }]
      });
    }
  }

  function pendingChallengeOf(game) {
    for (const challengeId of game.challengeIds) {
      const challenge = challenges.get(challengeId);
      if (challenge && challenge.challengeStatus === 'pending') {
        return challenge;
      }
    }
    return null;
  }

  function usedWords(game) {
    return game.wordChain.map((entry) => entry.word);
  }

  /** 当前需要衔接的首字母; 链为空时返回 null(任意字母) */
  function requiredLetter(game) {
    if (game.wordChain.length === 0) {
      return null;
    }
    return lastLetter(game.wordChain[game.wordChain.length - 1].word);
  }

  function currentPlayer(game) {
    return game.players[game.currentTurnIndex] || null;
  }

  /** 在座玩家中分数最高者; 平分返回 null(无胜者) */
  function topScorerId(game) {
    let best = null;
    let tied = false;
    for (const player of game.players) {
      if (best === null || player.score > best.score) {
        best = player;
        tied = false;
      } else if (player.score === best.score) {
        tied = true;
      }
    }
    return best && !tied ? best.id : null;
  }

  function settle(game) {
    game.status = GAME_STATUS.COMPLETED;
    game.completedAt = timestamp();
    game.updatedAt = game.completedAt;
    game.winnerId = topScorerId(game);
    game.endedBy = 'chain_completed';
    recordEvent(game, 'game_completed', {
      winnerId: game.winnerId,
      chainLength: game.wordChain.length,
      scores: game.players.map((p) => ({ id: p.id, name: p.name, score: p.score, isBot: p.isBot === true }))
    });
  }

  function serializePlayer(player) {
    // 设计文档 Data Structures 的 Player.isBot?: boolean —— 缺省 false, 客户端可直接判定
    return {
      id: player.id,
      name: player.name,
      score: player.score,
      isBot: player.isBot === true
    };
  }

  /** 对局事件流水: 供 GET /history 还原"谁在什么时候做了什么" */
  function recordEvent(game, type, data) {
    const event = { seq: game.events.length + 1, type, at: timestamp() };
    if (data && typeof data === 'object') {
      Object.assign(event, data);
    }
    game.events.push(event);
    return event;
  }

  function serializeGame(game) {
    const winner = game.winnerId ? game.players.find((p) => p.id === game.winnerId) : null;
    const pending = pendingChallengeOf(game);
    return {
      gameId: game.gameId,
      status: game.status,
      players: game.players.map(serializePlayer),
      currentTurnIndex: game.currentTurnIndex,
      currentPlayerId: currentPlayer(game) ? currentPlayer(game).id : null,
      wordChain: usedWords(game),
      chainLength: game.wordChain.length,
      maxChainLength: game.maxChainLength,
      requiredLetter: requiredLetter(game),
      lastWord: game.wordChain.length > 0 ? game.wordChain[game.wordChain.length - 1].word : null,
      hasPendingChallenge: Boolean(pending),
      pendingChallengeId: pending ? pending.challengeId : null,
      // 终局且没有胜者 = 平局(链满平分, 或认输后剩余玩家平分)
      isDraw: game.status !== GAME_STATUS.ACTIVE && !winner,
      winner: winner ? serializePlayer(winner) : undefined,
      departures: game.departures.slice(),
      endedBy: game.endedBy || undefined,
      createdAt: game.createdAt,
      updatedAt: game.updatedAt
    };
  }

  function serializeChallenge(challenge) {
    const out = {
      challengeId: challenge.challengeId,
      gameId: challenge.gameId,
      challengerId: challenge.challengerId,
      challengedWordIndex: challenge.challengedWordIndex,
      challengedWord: challenge.challengedWord,
      challengedPlayerId: challenge.challengedPlayerId,
      challengeStatus: challenge.challengeStatus,
      challengeTimestamp: challenge.challengeTimestamp,
      serverValidated: challenge.serverValidated === true
    };
    if (challenge.result) {
      out.challengeResult = challenge.result;
      out.resolvedBy = challenge.resolvedBy;
      out.resolvedAt = challenge.resolvedAt;
      out.mismatch = challenge.mismatch === true;
    }
    return out;
  }

  // ---------------------------------------------------------------- 用例

  function createGame(payload) {
    const body = isPlainObject(payload) ? payload : {};
    const rawPlayers = body.players;

    // 设计文档 §1 的开局形状是 { playerName } —— 单人开桌, 之后用 §2 join 补员。
    // 只有这种显式形状允许 1 人; players 数组仍要求 2..4, 免得悄悄放宽既有契约。
    const lobby = !Array.isArray(rawPlayers) && (body.playerName !== undefined || body.name !== undefined);
    const roster = lobby
      ? [{
        raw: { name: body.playerName !== undefined ? body.playerName : body.name, id: body.playerId },
        field: body.playerName !== undefined ? 'playerName' : 'name',
        isBot: body.isBot
      }]
      : Array.isArray(rawPlayers)
        ? rawPlayers.map((rawPlayer, index) => ({
          raw: rawPlayer,
          field: `players[${index}].name`,
          isBot: undefined
        }))
        : null;

    if (!roster || roster.length < (lobby ? 1 : MIN_PLAYERS) || roster.length > MAX_PLAYERS) {
      throw validationError(
        'players',
        `players must contain between ${MIN_PLAYERS} and ${MAX_PLAYERS} players`
      );
    }

    const players = [];
    const seenIds = new Set();
    const seenNames = new Set();

    roster.forEach((entry, index) => {
      const source = isPlainObject(entry.raw) ? entry.raw : { name: entry.raw };
      const isBotRaw = entry.isBot !== undefined ? entry.isBot : source.isBot;
      const name = sanitizePlayerName(source.name, entry.field);
      const nameKey = name.toLowerCase();
      if (seenNames.has(nameKey)) {
        throw validationError(entry.field, `Duplicate player name: ${name}`);
      }
      seenNames.add(nameKey);

      let id = typeof source.id === 'string' ? source.id.trim() : '';
      if (id.length === 0) {
        id = idGenerator('player');
      }
      if (seenIds.has(id)) {
        throw validationError(`players[${index}].id`, `Duplicate player id: ${id}`);
      }
      seenIds.add(id);

      players.push({ id, name, score: 0, isBot: normalizeIsBot(isBotRaw) });
    });

    const createdAt = timestamp();
    gameSeq += 1;
    const game = {
      gameId: idGenerator('game'),
      sequence: gameSeq,
      status: GAME_STATUS.ACTIVE,
      players,
      currentTurnIndex: 0,
      wordChain: [],
      challengeIds: [],
      events: [],
      departures: [],
      endedBy: null,
      maxChainLength,
      winnerId: null,
      createdAt,
      updatedAt: createdAt
    };

    games.set(game.gameId, game);
    recordEvent(game, 'game_created', {
      players: players.map((p) => ({ id: p.id, name: p.name, isBot: p.isBot }))
    });

    // 开局就轮到机器人(players[0].isBot)时, 它应当立刻出首词, 否则这张桌子没有任何人能推进;
    // 首词不带衔接约束, 一定选得出词。
    runBotTurns(game);

    return serializeGame(game);
  }

  function getGame(gameId) {
    const game = findGame(gameId);
    const view = serializeGame(game);
    view.history = game.wordChain.map((entry, index) => ({
      index,
      word: entry.word,
      playerId: entry.playerId,
      submittedAt: entry.submittedAt
    }));
    view.challenges = game.challengeIds
      .map((id) => challenges.get(id))
      .filter(Boolean)
      .map(serializeChallenge);
    return view;
  }

  function listGames(query) {
    const query_ = isPlainObject(query) ? query : {};
    const limit = query_.limit === undefined ? 50 : toPositiveInt(query_.limit, 50);
    const statusFilter = typeof query_.status === 'string' && query_.status.trim().length > 0
      ? query_.status.trim()
      : null;

    let filtered = Array.from(games.values());
    if (statusFilter) {
      filtered = filtered.filter((game) => game.status === statusFilter);
    }
    filtered.sort(compareGamesNewestFirst);

    // total 是"状态过滤后"的全量, 翻遍所有页都不变; 游标只裁剪后面的窗口。
    const cursor = query_.cursor === undefined || query_.cursor === null
      || String(query_.cursor).trim().length === 0
      ? null
      : decodeListCursor(String(query_.cursor).trim());
    const windowed = cursor
      ? filtered.filter((game) => game.createdAt < cursor.createdAt
        || (game.createdAt === cursor.createdAt && game.sequence < cursor.sequence))
      : filtered;

    const sliced = windowed.slice(0, limit);
    const hasMore = windowed.length > sliced.length;
    return {
      total: filtered.length,
      remaining: windowed.length,
      limit,
      count: sliced.length,
      hasMore,
      nextCursor: hasMore && sliced.length > 0
        ? encodeListCursor(sliced[sliced.length - 1])
        : null,
      games: sliced.map((game) => ({
        gameId: game.gameId,
        status: game.status,
        playerCount: game.players.length,
        chainLength: game.wordChain.length,
        maxChainLength: game.maxChainLength,
        createdAt: game.createdAt,
        updatedAt: game.updatedAt
      }))
    };
  }

  /**
   * 提交单词。
   *
   * @param {string} gameId
   * @param {object} payload  { playerId, word }
   * @param {object} [submitOptions]
   * @param {boolean} [submitOptions.isBot] 机器人自动出词: 事件流水里带上 bot 标记便于复盘;
   *        其余校验路径与真人出词完全一致(dictionary.has / 首字母衔接 / 重复词)。
   */
  function playWord(gameId, payload, submitOptions) {
    const opts_ = submitOptions || {};
    const fromBot = opts_.isBot === true;
    const game = findGame(gameId);
    const body = isPlainObject(payload) ? payload : {};

    const player = findPlayer(game, body.playerId);
    requireActive(game);

    assertNoPendingChallenge(game, 'play');

    const turnPlayer = currentPlayer(game);
    if (!turnPlayer || turnPlayer.id !== player.id) {
      throw new GameError(403, 'Forbidden', 'It is not your turn', {
        extra: {
          // 设计文档 403 形状里的字段: 调用方需要具备的角色
          requiredRole: 'player',
          currentTurnIndex: game.currentTurnIndex,
          currentPlayerId: turnPlayer ? turnPlayer.id : null,
          playerId: player.id
        }
      });
    }

    const rawWord = body.word;
    if (typeof rawWord !== 'string' || rawWord.trim().length === 0) {
      throw validationError('word', 'Word cannot be empty');
    }
    const word = normalizeWord(rawWord);
    if (!/^[a-z]+$/.test(word)) {
      throw validationError('word', 'Word must contain letters only');
    }
    if (word.length < MIN_WORD_LENGTH || word.length > MAX_WORD_LENGTH) {
      throw validationError(
        'word',
        `Word length must be between ${MIN_WORD_LENGTH} and ${MAX_WORD_LENGTH} characters`
      );
    }
    if (!dictionary.has(word)) {
      throw validationError('word', 'Word is not in the dictionary');
    }

    const required = requiredLetter(game);
    if (required !== null && firstLetter(word) !== required) {
      throw new GameError(400, 'ValidationError', 'Word does not match the chain', {
        details: [
          {
            field: 'word',
            message: `Word must start with "${required}" to follow "${game.wordChain[game.wordChain.length - 1].word}"`
          }
        ],
        extra: {
          previousWord: game.wordChain[game.wordChain.length - 1].word,
          requiredLetter: required,
          actualLetter: firstLetter(word)
        }
      });
    }

    if (usedWords(game).includes(word)) {
      throw validationError('word', 'Word has already been used in this chain');
    }

    const record = {
      word,
      playerId: player.id,
      submittedAt: timestamp()
    };
    game.wordChain.push(record);
    player.score += word.length * SCORE_PER_LETTER;
    game.updatedAt = record.submittedAt;
    recordEvent(game, 'word_played', {
      index: game.wordChain.length - 1,
      word,
      playerId: player.id,
      playerName: player.name,
      scoreAwarded: word.length * SCORE_PER_LETTER,
      bot: fromBot
    });

    if (game.wordChain.length >= game.maxChainLength) {
      settle(game);
    } else {
      game.currentTurnIndex = (game.currentTurnIndex + 1) % game.players.length;
    }

    // 机器人自动续打 (设计文档 §2 Player Roles): 轮到机器人时用词库出词。
    // 这里放在"确实推进了链条"之后, 因此 403/400 之类的失败路径不会触发连锁出词。
    runBotTurns(game);

    const view = serializeGame(game);
    view.word = word;
    view.scoreAwarded = word.length * SCORE_PER_LETTER;
    view.chain = usedWords(game);
    return view;
  }

  /**
   * 从词库里挑一个"以 requiredLetter 开头且本局未用过"的词。
   * 空字符串表示任意首字母(链为空时), 与 GET /hint 的口径一致。
   * @returns {string|null} null = 机器人无词可出
   */
  function chooseBotWord(game) {
    const required = requiredLetter(game);
    const result = dictionary.hint(required === null ? '' : required, {
      limit: 1,
      usedWords: usedWords(game)
    });
    return result.hints.length > 0 ? result.hints[0] : null;
  }

  /**
   * 机器人回合: 只要当前轮到机器人就自动出词, 直到轮到真人或对局结束。
   *
   * 每次迭代必定往链上追加一个词, 而链条长度上限是 maxChainLength, 所以循环次数
   * 有硬上界 —— 即使词库被换成"每步都能接"的极端配置也不会无限循环或爆栈。
   * 出词走的是与真人完全相同的 playWord 校验路径(不复制平行实现)。
   */
  function runBotTurns(game) {
    for (let guard = 0; guard < game.maxChainLength; guard += 1) {
      if (game.status !== GAME_STATUS.ACTIVE) {
        return;
      }
      if (pendingChallengeOf(game)) {
        return;
      }
      const turnPlayer = currentPlayer(game);
      if (!turnPlayer || turnPlayer.isBot !== true) {
        return;
      }

      const word = chooseBotWord(game);
      if (word === null) {
        // 词库被本局用光: 按设计文档的胜负条件收尾(机器人判负), 而不是抛 500。
        // 必须先把它移出在座并记入 departures, 再 endByDeparture —— 与 leave/forfeit 的
        // 收尾模式一致: topScorerId 只在"其余玩家"里选最高分, 否则卡死方会因为分数最高
        // 而赢下自己认输的这一局(例如机器人首发即卡死: 它 5 分, 真人 2 分)。
        const stuckIndex = game.players.findIndex((p) => p.id === turnPlayer.id);
        const stuck = removePlayerFromGame(game, stuckIndex);
        game.departures.push({
          playerId: stuck.id,
          name: stuck.name,
          score: stuck.score,
          isBot: stuck.isBot === true,
          reason: 'bot_stuck',
          at: timestamp()
        });
        endByDeparture(game, 'bot_stuck', stuck);
        return;
      }

      playWord(game.gameId, { playerId: turnPlayer.id, word }, { isBot: true });
    }
  }

  function challengeWord(gameId, payload) {
    const game = findGame(gameId);
    const body = isPlainObject(payload) ? payload : {};

    // 设计文档 §6 的请求体字段名是 playerId, 实现早期用的是 challengerId;
    // 两者都接受(challengerId 优先), 避免客户端按文档写就 404。
    const challengerRaw = body.challengerId !== undefined ? body.challengerId : body.playerId;
    const challenger = findPlayer(game, challengerRaw);
    requireActive(game);

    if (pendingChallengeOf(game)) {
      throw new GameError(400, 'ValidationError', 'A challenge is already pending for this game', {
        details: [{ field: 'challengeId', message: 'Resolve the pending challenge first' }]
      });
    }

    const rawIndex = body.challengedWordIndex;
    const index = Number(rawIndex);
    if (!Number.isInteger(index) || index < 0 || index >= game.wordChain.length) {
      throw validationError(
        'challengedWordIndex',
        `challengedWordIndex must be an integer between 0 and ${Math.max(0, game.wordChain.length - 1)}`
      );
    }

    const target = game.wordChain[index];
    if (target.playerId === challenger.id) {
      throw new GameError(403, 'Forbidden', 'You cannot challenge your own word', {
        extra: {
          requiredRole: 'challenger',
          challengedWordIndex: index,
          challengedPlayerId: target.playerId
        }
      });
    }

    const challengeId = idGenerator('challenge');
    const challenge = {
      challengeId,
      gameId: game.gameId,
      challengerId: challenger.id,
      challengedWordIndex: index,
      challengedWord: target.word,
      challengedPlayerId: target.playerId,
      challengeStatus: 'pending',
      challengeTimestamp: timestamp(),
      serverValidated: false
    };

    challenges.set(challengeId, challenge);
    game.challengeIds.push(challengeId);
    game.updatedAt = challenge.challengeTimestamp;
    recordEvent(game, 'challenge_raised', {
      challengeId,
      challengerId: challenger.id,
      challengerName: challenger.name,
      challengedWordIndex: index,
      challengedWord: target.word,
      challengedPlayerId: target.playerId
    });

    // 机器人无法自证 (它没有客户端): 轮到机器人当 Defender 时由服务端代裁, 否则真人质疑机器人
    // 之后对局会永久冻结在 pending(playWord/leaveGame/forfeitGame 都要求无待裁决质疑)。
    // 裁决本身仍然复用 resolveChallenge, 判定依据是服务端独立复核 isWordValidAt, 不看机器人"自述"。
    const challengedPlayer = game.players.find((p) => p.id === challenge.challengedPlayerId);
    if (challengedPlayer && challengedPlayer.isBot === true) {
      return resolveChallenge(game.gameId, challengeId, { playerId: challengedPlayer.id }, { isBot: true });
    }

    return {
      challengeId,
      challengeStatus: challenge.challengeStatus,
      challengedWordIndex: index,
      challengedWord: target.word,
      challengedPlayerId: target.playerId,
      challengerId: challenger.id,
      playerId: challenger.id,
      challengeTimestamp: challenge.challengeTimestamp
    };
  }

  /**
   * 服务端独立复核链上第 index 个词(客户端校验不可信)。
   * @returns {boolean}
   */
  function isWordValidAt(game, index) {
    const entry = game.wordChain[index];
    if (!entry) {
      return false;
    }
    if (!dictionary.has(entry.word)) {
      return false;
    }
    if (index === 0) {
      return true;
    }
    const previous = game.wordChain[index - 1];
    return lastLetter(previous.word) === firstLetter(entry.word);
  }

  /**
   * 裁决质疑 (设计文档 §7)。
   *
   * @param {string} gameId
   * @param {string} challengeId
   * @param {object} payload { playerId, isValid? }
   * @param {object} [submitOptions]
   * @param {boolean} [submitOptions.isBot] 服务端代替机器人裁决: 只有这条路允许以 robot 身份
   *        裁决(设计文档 §2 的 Defender 角色), 且客户端传的 isValid 一律忽略, 只认服务端复核。
   */
  function resolveChallenge(gameId, challengeId, payload, submitOptions) {
    const opts_ = submitOptions || {};
    const fromBot = opts_.isBot === true;
    const game = findGame(gameId);
    const body = isPlainObject(payload) ? payload : {};

    const challenge = challenges.get(String(challengeId));
    if (!challenge || challenge.gameId !== game.gameId) {
      throw new GameError(404, 'NotFound', 'Challenge not found', {
        extra: { resource: 'challenge', gameId: game.gameId, challengeId: String(challengeId) }
      });
    }
    if (challenge.challengeStatus !== 'pending') {
      throw new GameError(400, 'ValidationError', 'Challenge has already been resolved', {
        details: [{ field: 'challengeId', message: `Challenge status is ${challenge.challengeStatus}` }]
      });
    }
    // 终局对局不允许再改动链上/回合(防御性: 正常流程下 pending 质疑只可能出现在 active 对局)
    requireActive(game);

    const resolver = findPlayer(game, body.playerId);
    if (resolver.id !== challenge.challengedPlayerId) {
      throw new GameError(403, 'Forbidden', 'Only the challenged player can resolve this challenge', {
        extra: {
          // 设计文档 §2 的角色: 被质疑的一方是 Defender, 只有他能裁决这次质疑
          requiredRole: 'defender',
          challengedPlayerId: challenge.challengedPlayerId,
          playerId: resolver.id
        }
      });
    }
    // 服务端代裁仅限机器人本人; 真人不能借机器人通道替自己裁决
    if (!fromBot && resolver.isBot === true) {
      throw new GameError(403, 'Forbidden', 'A bot defender is resolved by the server automatically', {
        extra: {
          requiredRole: 'defender',
          challengedPlayerId: challenge.challengedPlayerId,
          playerId: resolver.id,
          automaticResolution: true
        }
      });
    }

    const index = challenge.challengedWordIndex;
    let revoked = []; // 质疑成功时被撤回的词, 要留在事件流水里
    const serverIsValid = isWordValidAt(game, index);
    // 服务端代裁不看客户端口径, 因此 mismatch 必然为 false
    const clientClaim = !fromBot && typeof body.isValid === 'boolean' ? body.isValid : undefined;
    const mismatch = clientClaim !== undefined && clientClaim !== serverIsValid;

    challenge.serverValidated = true;
    challenge.resolvedBy = resolver.id;
    challenge.resolvedAt = timestamp();
    challenge.mismatch = mismatch;

    let result;
    if (serverIsValid) {
      result = 'challenge_failed';
      challenge.challengeStatus = 'rejected';
      // 质疑失败: 回合轮转到质疑者
      const challengerIndex = game.players.findIndex((p) => p.id === challenge.challengerId);
      if (challengerIndex >= 0) {
        game.currentTurnIndex = challengerIndex;
      }
    } else {
      result = 'challenge_succeeded';
      challenge.challengeStatus = 'accepted';
      // 质疑成功: 被质疑的词及其之后全部撤回, 提交者判负
      revoked = game.wordChain.slice(index);
      game.wordChain = game.wordChain.slice(0, index);
      const challenger = game.players.find((p) => p.id === challenge.challengerId);
      game.status = GAME_STATUS.FORFEITED;
      game.winnerId = challenger ? challenger.id : null;
      game.completedAt = challenge.resolvedAt;
      game.endedBy = 'challenge_succeeded';
    }

    challenge.result = result;
    game.updatedAt = challenge.resolvedAt;
    recordEvent(game, 'challenge_resolved', {
      challengeId: challenge.challengeId,
      result,
      serverValidated: serverIsValid,
      mismatch,
      resolvedBy: resolver.id,
      // 服务端代机器人裁决时标记出来, 复盘时能区分"真人裁决"与"代裁"
      resolvedByBot: fromBot,
      winnerId: game.winnerId
    });

    if (result === 'challenge_succeeded') {
      recordEvent(game, 'game_forfeited', {
        reason: 'challenge_succeeded',
        winnerId: game.winnerId,
        chainLength: game.wordChain.length,
        // 与 settle / endByDeparture 的聚合口径一致: 出现玩家的结构一律带 isBot
        scores: game.players.map((p) => ({
          id: p.id,
          name: p.name,
          score: p.score,
          isBot: p.isBot === true
        })),
        revokedWords: revoked.map((entry, offset) => ({
          index: game.wordChain.length + offset,
          word: entry.word,
          playerId: entry.playerId
        }))
      });
    }

    const view = serializeGame(game);
    view.challengeResult = result;
    // 设计文档 §7 的响应字段名是 gameStatus, 与 status 同值, 便于按文档写的客户端直接读
    view.gameStatus = game.status;
    view.serverValidated = serverIsValid;
    view.mismatch = mismatch;
    if (fromBot) {
      view.resolvedByBot = true;
    }
    view.challenge = serializeChallenge(challenge);
    return view;
  }

  // -------------------------------------------------- 加入 / 离开 / 认输 / 对局历史

  /** 质疑待裁决期间对局冻结: 不允许提交单词 / 离开 / 认输 */
  function assertNoPendingChallenge(game, action) {
    const pending = pendingChallengeOf(game);
    if (pending) {
      throw new GameError(400, 'ValidationError', 'A pending challenge must be resolved first', {
        details: [{ field: 'challengeId', message: `Challenge ${pending.challengeId} is still pending` }],
        extra: { challengeId: pending.challengeId, action }
      });
    }
    return pending;
  }

  /** 链上词的作者可能已经离桌, 因此按 players -> departures 顺序回溯名字 */
  function playerNameOf(game, playerId) {
    const inGame = game.players.find((p) => p.id === playerId);
    if (inGame) {
      return inGame.name;
    }
    const departed = game.departures.find((d) => d.playerId === playerId);
    return departed ? departed.name : null;
  }

  /**
   * 对局因人员变动而结束(设计文档胜利条件 "Opponent forfeits")。
   *   reason = 'player_forfeited' —— 有人主动认输: 整局立即结束, 不因在座人数够而豁免;
   *   reason = 'player_left'      —— 有人离席后不足 MIN_PLAYERS 人, 无法继续。
   * 胜者取在座玩家中分数最高者, 平分则无胜者(平局)。
   */
  function endByDeparture(game, reason, departingPlayer) {
    const at = timestamp();
    game.status = GAME_STATUS.FORFEITED;
    game.completedAt = at;
    game.updatedAt = at;
    game.endedBy = reason;
    game.winnerId = topScorerId(game);

    recordEvent(game, 'game_forfeited', {
      reason,
      // departing 的 isBot 直接取玩家对象, 因此"因无词可出而认输的机器人"也能在流水里认得出来
      departingPlayerId: departingPlayer.id,
      departingPlayerName: departingPlayer.name,
      departingPlayerIsBot: departingPlayer.isBot === true,
      winnerId: game.winnerId,
      scores: game.players.map((p) => ({ id: p.id, name: p.name, score: p.score, isBot: p.isBot === true }))
    });
  }

  /** 从在座玩家里移除一人, 并把回合指针收敛到合法范围 */
  function removePlayerFromGame(game, index) {
    const [removed] = game.players.splice(index, 1);
    if (game.players.length > 0) {
      if (index < game.currentTurnIndex) {
        game.currentTurnIndex -= 1;
      }
      game.currentTurnIndex = ((game.currentTurnIndex % game.players.length) + game.players.length)
        % game.players.length;
    } else {
      game.currentTurnIndex = 0;
    }
    return removed;
  }

  /**
   * 加入对局 (设计文档 §2): POST /games/:gameId/join { playerName }
   * 只允许在首词落地之前加入 —— 链上已有词时补员会打乱回合顺序与已结算的分数。
   */
  function joinGame(gameId, payload) {
    const game = findGame(gameId);
    const body = isPlainObject(payload) ? payload : {};

    requireActive(game);

    if (game.wordChain.length > 0) {
      throw new GameError(403, 'Forbidden', 'Cannot join after the first word has been played', {
        details: [{
          field: 'gameId',
          message: `Chain already contains ${game.wordChain.length} word(s); joining is only allowed before the first word`
        }],
        extra: { gameId: game.gameId, chainLength: game.wordChain.length }
      });
    }

    if (game.players.length >= MAX_PLAYERS) {
      throw new GameError(409, 'Conflict', `Game is full (${MAX_PLAYERS} players)`, {
        extra: { gameId: game.gameId, playerCount: game.players.length, maxPlayers: MAX_PLAYERS }
      });
    }

    // 设计文档字段名是 playerName, 同时兼容 name
    const rawName = body.playerName !== undefined ? body.playerName : body.name;
    const name = sanitizePlayerName(rawName, 'playerName');
    const nameKey = name.toLowerCase();
    if (game.players.some((p) => p.name.toLowerCase() === nameKey)) {
      throw new GameError(409, 'Conflict', `Duplicate player name: ${name}`, {
        details: [{ field: 'playerName', message: `Player name already taken: ${name}` }]
      });
    }

    let id = typeof body.playerId === 'string' ? body.playerId.trim() : '';
    if (id.length === 0) {
      id = idGenerator('player');
    }
    if (game.players.some((p) => p.id === id)) {
      throw new GameError(409, 'Conflict', `Duplicate player id: ${id}`, {
        details: [{ field: 'playerId', message: `Player id already taken: ${id}` }]
      });
    }

    // 设计文档 Data Structures 的 Player.isBot?: boolean —— 加入时也可标记为机器人
    const isBot = normalizeIsBot(body.isBot);
    const player = { id, name, score: 0, isBot };
    game.players.push(player);
    game.updatedAt = timestamp();
    recordEvent(game, 'player_joined', { playerId: id, name, isBot });

    // 单人开桌后补进来的正好是机器人: 此刻就该由它接手出首词, 无需再人工触发一次请求。
    // 只有当回合本来就落在机器人头上时才动手(join 不改变 currentTurnIndex)。
    runBotTurns(game);

    return {
      gameId: game.gameId,
      status: game.status,
      player: serializePlayer(player),
      players: game.players.map(serializePlayer),
      playerCount: game.players.length,
      currentTurnIndex: game.currentTurnIndex,
      currentPlayerId: currentPlayer(game) ? currentPlayer(game).id : null,
      wordChain: usedWords(game),
      createdAt: game.createdAt
    };
  }

  /**
   * 离开对局 (设计文档 §4): DELETE /games/:gameId/players/:playerId -> { success: true }
   * 离开后不足 MIN_PLAYERS 人则按"对手弃赛"结束对局。
   */
  function leaveGame(gameId, playerId) {
    const game = findGame(gameId);
    const player = findPlayer(game, playerId);
    requireActive(game);
    assertNoPendingChallenge(game, 'leave');

    const index = game.players.findIndex((p) => p.id === player.id);
    const removed = removePlayerFromGame(game, index);
    game.departures.push({
      playerId: removed.id,
      name: removed.name,
      score: removed.score,
      isBot: removed.isBot === true,
      reason: 'left',
      at: timestamp()
    });
    game.updatedAt = timestamp();
    recordEvent(game, 'player_left', {
      playerId: removed.id,
      name: removed.name,
      score: removed.score,
      isBot: removed.isBot === true,
      remainingPlayers: game.players.length
    });

    let ended = false;
    if (game.players.length < MIN_PLAYERS) {
      endByDeparture(game, 'player_left', removed);
      ended = true;
    }

    const view = serializeGame(game);
    view.success = true;
    view.leftPlayerId = removed.id;
    view.ended = ended;
    return view;
  }

  /**
   * 主动认输: POST /games/:gameId/forfeit { playerId, reason? }
   * 设计文档把"对手弃赛"列为胜利条件, 却没有主动弃赛的入口 —— 这里补上。
   * 认输即认输整局: 对局立刻置 forfeited, 胜者取其余在座玩家中分数最高者(平分则无胜者),
   * 而不是"人够就继续" —— 否则同一张桌子上会出现"有人已经认输但还在算分"的歧义状态。
   */
  function forfeitGame(gameId, payload) {
    const game = findGame(gameId);
    const body = isPlainObject(payload) ? payload : {};

    const player = findPlayer(game, body.playerId);
    requireActive(game);
    assertNoPendingChallenge(game, 'forfeit');

    const index = game.players.findIndex((p) => p.id === player.id);
    const removed = removePlayerFromGame(game, index);
    const at = timestamp();
    game.departures.push({
      playerId: removed.id,
      name: removed.name,
      score: removed.score,
      isBot: removed.isBot === true,
      reason: 'forfeited',
      at
    });
    recordEvent(game, 'player_forfeited', {
      playerId: removed.id,
      name: removed.name,
      score: removed.score,
      reason: typeof body.reason === 'string' ? body.reason.trim() : '',
      remainingPlayers: game.players.length
    });
    endByDeparture(game, 'player_forfeited', removed);

    const view = serializeGame(game);
    view.success = true;
    view.forfeitedBy = removed.id;
    view.ended = true;
    return view;
  }

  /**
   * 对局历史 (设计文档 §8): GET /games/:gameId/history -> { wordChain, challenges }
   * 额外给出 events 事件流水: 被质疑撤回的词虽然不在 wordChain 里了, 但仍能在 events 中还原。
   */
  function getHistory(gameId, query) {
    const game = findGame(gameId);
    const query_ = isPlainObject(query) ? query : {};

    // 事件流水支持增量拉取: ?since=<seq> 只要比 seq 更新的事件, ?limit=<n> 限制条数。
    // 不带这两个参数时行为与之前完全一致(返回全量 events)。
    const since = parseEventSince(query_.since);
    const eventLimit = query_.limit === undefined ? null : toPositiveInt(query_.limit, null);

    const allEvents = game.events;
    const latestSeq = allEvents.length > 0 ? allEvents[allEvents.length - 1].seq : 0;
    let events = since === null ? allEvents.slice() : allEvents.filter((item) => item.seq > since);
    let hasMoreEvents = false;
    if (eventLimit !== null && events.length > eventLimit) {
      events = events.slice(0, eventLimit);
      hasMoreEvents = true;
    }
    const nextSince = events.length > 0
      ? events[events.length - 1].seq
      : (since === null ? latestSeq : since);

    return {
      gameId: game.gameId,
      status: game.status,
      wordChain: game.wordChain.map((entry, index) => ({
        index,
        word: entry.word,
        playerId: entry.playerId,
        playerName: playerNameOf(game, entry.playerId),
        submittedAt: entry.submittedAt
      })),
      chainLength: game.wordChain.length,
      challenges: game.challengeIds
        .map((id) => challenges.get(id))
        .filter(Boolean)
        .map(serializeChallenge),
      events,
      eventsTotal: allEvents.length,
      latestSeq,
      nextSince,
      hasMoreEvents,
      departures: game.departures.slice(),
      currentTurnIndex: game.currentTurnIndex,
      currentPlayerId: currentPlayer(game) ? currentPlayer(game).id : null,
      winner: game.winnerId
        ? (() => {
          const winner = game.players.find((p) => p.id === game.winnerId);
          return winner ? serializePlayer(winner) : undefined;
        })()
        : undefined,
      endedBy: game.endedBy || undefined,
      createdAt: game.createdAt,
      updatedAt: game.updatedAt,
      completedAt: game.completedAt || null
    };
  }

  function getHint(gameId, query) {
    const game = findGame(gameId);
    const query_ = isPlainObject(query) ? query : {};
    requireActive(game);

    if (pendingChallengeOf(game)) {
      throw new GameError(400, 'ValidationError', 'A pending challenge must be resolved first', {
        details: [{ field: 'challengeId', message: 'Resolve the pending challenge first' }]
      });
    }

    const limit = query_.limit === undefined ? defaultHintLimit : toPositiveInt(query_.limit, defaultHintLimit);
    const letter = requiredLetter(game);
    const result = dictionary.hint(letter === null ? '' : letter, {
      limit,
      usedWords: usedWords(game)
    });

    return {
      gameId: game.gameId,
      requiredLetter: letter,
      nextPlayerId: currentPlayer(game) ? currentPlayer(game).id : null,
      hints: result.hints,
      totalAvailable: result.totalAvailable,
      dictionarySize: dictionary.size
    };
  }

  return {
    createGame,
    joinGame,
    leaveGame,
    forfeitGame,
    getGame,
    getHistory,
    listGames,
    playWord,
    challengeWord,
    resolveChallenge,
    getHint,
    // 供运维/测试使用
    stats() {
      return {
        games: games.size,
        activeGames: Array.from(games.values()).filter((g) => g.status === GAME_STATUS.ACTIVE).length,
        challenges: challenges.size,
        dictionarySize: dictionary.size,
        maxChainLength
      };
    },
    reset() {
      games.clear();
      challenges.clear();
    }
  };
}

module.exports = {
  createGameService,
  GameError,
  GAME_STATUS,
  DEFAULT_MAX_CHAIN_LENGTH,
  MIN_PLAYERS,
  MAX_PLAYERS,
  MAX_PLAYER_NAME_LENGTH,
  SCORE_PER_LETTER
};