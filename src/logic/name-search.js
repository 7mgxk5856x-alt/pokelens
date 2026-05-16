const MAX_RESULTS = 10;

const HALFWIDTH_KANA = {
  'ｱ': 'ア', 'ｲ': 'イ', 'ｳ': 'ウ', 'ｴ': 'エ', 'ｵ': 'オ',
  'ｶ': 'カ', 'ｷ': 'キ', 'ｸ': 'ク', 'ｹ': 'ケ', 'ｺ': 'コ',
  'ｻ': 'サ', 'ｼ': 'シ', 'ｽ': 'ス', 'ｾ': 'セ', 'ｿ': 'ソ',
  'ﾀ': 'タ', 'ﾁ': 'チ', 'ﾂ': 'ツ', 'ﾃ': 'テ', 'ﾄ': 'ト',
  'ﾅ': 'ナ', 'ﾆ': 'ニ', 'ﾇ': 'ヌ', 'ﾈ': 'ネ', 'ﾉ': 'ノ',
  'ﾊ': 'ハ', 'ﾋ': 'ヒ', 'ﾌ': 'フ', 'ﾍ': 'ヘ', 'ﾎ': 'ホ',
  'ﾏ': 'マ', 'ﾐ': 'ミ', 'ﾑ': 'ム', 'ﾒ': 'メ', 'ﾓ': 'モ',
  'ﾔ': 'ヤ', 'ﾕ': 'ユ', 'ﾖ': 'ヨ',
  'ﾗ': 'ラ', 'ﾘ': 'リ', 'ﾙ': 'ル', 'ﾚ': 'レ', 'ﾛ': 'ロ',
  'ﾜ': 'ワ', 'ｦ': 'ヲ', 'ﾝ': 'ン',
  'ｧ': 'ァ', 'ｨ': 'ィ', 'ｩ': 'ゥ', 'ｪ': 'ェ', 'ｫ': 'ォ',
  'ｬ': 'ャ', 'ｭ': 'ュ', 'ｮ': 'ョ',
  'ｯ': 'ッ', 'ｰ': 'ー',
  '｡': '。', '｢': '「', '｣': '」', '､': '、', '･': '・',
};

const DAKUTEN_PAIR = {
  'カ': 'ガ', 'キ': 'ギ', 'ク': 'グ', 'ケ': 'ゲ', 'コ': 'ゴ',
  'サ': 'ザ', 'シ': 'ジ', 'ス': 'ズ', 'セ': 'ゼ', 'ソ': 'ゾ',
  'タ': 'ダ', 'チ': 'ヂ', 'ツ': 'ヅ', 'テ': 'デ', 'ト': 'ド',
  'ハ': 'バ', 'ヒ': 'ビ', 'フ': 'ブ', 'ヘ': 'ベ', 'ホ': 'ボ',
  'ウ': 'ヴ',
};

const HANDAKUTEN_PAIR = {
  'ハ': 'パ', 'ヒ': 'ピ', 'フ': 'プ', 'ヘ': 'ペ', 'ホ': 'ポ',
};

const HIRAGANA_MIN = 0x3041;
const HIRAGANA_MAX = 0x3096;
const HIRAGANA_TO_KATAKANA_OFFSET = 0x60;
const HALF_DAKUTEN = 'ﾞ';
const HALF_HANDAKUTEN = 'ﾟ';

export function normalizeQuery(query) {
  if (query === '') return '';

  let result = '';
  for (let i = 0; i < query.length; i++) {
    const ch = query[i];
    const code = ch.charCodeAt(0);

    if (code >= HIRAGANA_MIN && code <= HIRAGANA_MAX) {
      result += String.fromCharCode(code + HIRAGANA_TO_KATAKANA_OFFSET);
      continue;
    }

    const fullKana = HALFWIDTH_KANA[ch];
    if (fullKana !== undefined) {
      const next = query[i + 1];
      if (next === HALF_DAKUTEN && DAKUTEN_PAIR[fullKana]) {
        result += DAKUTEN_PAIR[fullKana];
        i++;
        continue;
      }
      if (next === HALF_HANDAKUTEN && HANDAKUTEN_PAIR[fullKana]) {
        result += HANDAKUTEN_PAIR[fullKana];
        i++;
        continue;
      }
      result += fullKana;
      continue;
    }

    result += ch;
  }
  return result;
}

export function searchByName(query, entries) {
  if (query === '') return [];
  const normalized = normalizeQuery(query);
  const matched = [];
  for (const entry of entries) {
    if (normalizeQuery(entry.name).startsWith(normalized)) {
      matched.push(entry);
    }
  }
  matched.sort((a, b) => a.num - b.num);
  return matched.slice(0, MAX_RESULTS);
}
