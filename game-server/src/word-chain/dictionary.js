'use strict';

/**
 * 词链接龙词库 (429 契约之外的本地判定依据)。
 *
 * 契约来源: game-design/word-chain-game-design.md
 *   - "服务端必须验证: 单词是否在词库中" (客户端校验不可信)
 *   - POST /api/v1/games/:gameId/words -> 400 word not in dictionary
 *
 * 选词规则(保证接龙可继续):
 *   - 全部小写、只含 a-z;
 *   - 词库中不存在以 b/i/j/p/q/u/v/x 结尾的词, 因此不会出现"必死"字母;
 *   - 每个字母 a-z 都至少有一个可接的词, 提示(hint)功能不会返回空。
 */

const WORDS = [
  // a
  'apple', 'ant', 'arrow', 'anchor', 'amber',
  // b
  'banana', 'boat', 'bridge', 'balloon', 'breeze',
  // c
  'cat', 'candle', 'cherry', 'cloud', 'crystal',
  // d
  'dog', 'dragon', 'dolphin', 'diamond', 'desert',
  // e
  'elephant', 'eagle', 'engine', 'ember', 'echo',
  // f
  'frog', 'forest', 'feather', 'flame', 'fossil',
  // g
  'goat', 'garden', 'glacier', 'guitar', 'galaxy',
  // h
  'horse', 'honey', 'harbor', 'horizon', 'hammer',
  // i
  'iguana', 'island', 'insect', 'ivory', 'iceberg',
  // j
  'jellyfish', 'jungle', 'jewel', 'journey', 'jacket',
  // k
  'kite', 'kettle', 'kingdom', 'kayak', 'kernel',
  // l
  'lion', 'lantern', 'lagoon', 'legend', 'lighthouse',
  // m
  'monkey', 'mountain', 'mirror', 'meadow', 'magnet',
  // n
  'nest', 'needle', 'nebula', 'nectar', 'noodle',
  // o
  'owl', 'ocean', 'orchid', 'orbit', 'oasis',
  // p
  'panda', 'planet', 'puzzle', 'pepper', 'pyramid',
  // q
  'queen', 'quill', 'quokka', 'quartz', 'quest',
  // r
  'rabbit', 'river', 'rocket', 'ribbon', 'rainbow',
  // s
  'snake', 'sunset', 'silver', 'spiral', 'station',
  // t
  'tiger', 'tree', 'tunnel', 'thunder', 'telescope',
  // u
  'unicorn', 'umbrella', 'universe', 'urchin', 'ukulele',
  // v
  'violin', 'valley', 'velvet', 'volcano', 'voyage',
  // w
  'wolf', 'window', 'whisper', 'walnut', 'waterfall',
  // x
  'xylophone', 'xenon', 'xerus', 'xylem', 'xenolith',
  // y
  'yak', 'yarrow', 'yarn', 'yacht', 'yellow',
  // z
  'zebra', 'zephyr', 'zigzag', 'zinc', 'zone'
];

const MIN_WORD_LENGTH = 2;
const MAX_WORD_LENGTH = 24;

function normalizeWord(raw) {
  if (typeof raw !== 'string') {
    return '';
  }
  return raw.trim().toLowerCase();
}

/**
 * 创建词库查询器。
 *
 * @param {object} [options]
 * @param {string[]} [options.words] 自定义词表(测试可注入小词表)
 * @param {string[]} [options.extraWords] 在默认词表上追加的词
 * @returns {{has: function(string): boolean, hint: function(string, object): object, size: number, words: string[]}}
 */
function createDictionary(options) {
  const opts = options || {};
  const base = Array.isArray(opts.words) ? opts.words : WORDS;
  const extra = Array.isArray(opts.extraWords) ? opts.extraWords : [];

  const normalized = [];
  const wordSet = new Set();
  for (const raw of base.concat(extra)) {
    const word = normalizeWord(raw);
    if (word.length === 0 || wordSet.has(word)) {
      continue;
    }
    wordSet.add(word);
    normalized.push(word);
  }
  normalized.sort();

  /** @type {Map<string, string[]>} 首字母 -> 候选词 */
  const byFirstLetter = new Map();
  for (const word of normalized) {
    const letter = word[0];
    if (!byFirstLetter.has(letter)) {
      byFirstLetter.set(letter, []);
    }
    byFirstLetter.get(letter).push(word);
  }

  return {
    size: normalized.length,
    words: normalized.slice(),

    /** 是否为合法单词(大小写与首尾空格不敏感) */
    has(rawWord) {
      return wordSet.has(normalizeWord(rawWord));
    },

    /**
     * 给出以某字母开头、且未在 usedWords 中的候选项。
     *
     * @param {string} rawLetter 目标首字母, 空字符串表示"任意字母"
     * @param {object} [hintOptions]
     * @param {number} [hintOptions.limit=3]
     * @param {Iterable<string>} [hintOptions.usedWords]
     * @returns {{nextLetter: string|null, hints: string[], totalAvailable: number}}
     */
    hint(rawLetter, hintOptions) {
      const hintOpts = hintOptions || {};
      const limit = Number.isFinite(Number(hintOpts.limit)) && Number(hintOpts.limit) > 0
        ? Math.floor(Number(hintOpts.limit))
        : 3;
      const used = new Set();
      if (hintOpts.usedWords) {
        for (const word of hintOpts.usedWords) {
          used.add(normalizeWord(word));
        }
      }

      const letter = typeof rawLetter === 'string' ? rawLetter.trim().toLowerCase() : '';
      const pool = letter.length > 0 ? (byFirstLetter.get(letter) || []) : normalized;
      const available = pool.filter((word) => !used.has(word));

      return {
        nextLetter: letter.length > 0 ? letter : null,
        hints: available.slice(0, limit),
        totalAvailable: available.length
      };
    }
  };
}

module.exports = {
  createDictionary,
  normalizeWord,
  WORDS,
  MIN_WORD_LENGTH,
  MAX_WORD_LENGTH
};