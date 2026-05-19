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

const VOWELS = new Set(['a', 'i', 'u', 'e', 'o']);

const ROMAJI_TABLE = {
  kya: 'キャ', kyi: 'キィ', kyu: 'キュ', kye: 'キェ', kyo: 'キョ',
  gya: 'ギャ', gyi: 'ギィ', gyu: 'ギュ', gye: 'ギェ', gyo: 'ギョ',
  sya: 'シャ', syi: 'シィ', syu: 'シュ', sye: 'シェ', syo: 'ショ',
  sha: 'シャ', shi: 'シ', shu: 'シュ', she: 'シェ', sho: 'ショ',
  zya: 'ジャ', zyi: 'ジィ', zyu: 'ジュ', zye: 'ジェ', zyo: 'ジョ',
  jya: 'ジャ', jyi: 'ジィ', jyu: 'ジュ', jye: 'ジェ', jyo: 'ジョ',
  tya: 'チャ', tyi: 'チィ', tyu: 'チュ', tye: 'チェ', tyo: 'チョ',
  cha: 'チャ', chi: 'チ', chu: 'チュ', che: 'チェ', cho: 'チョ',
  cya: 'チャ', cyi: 'チィ', cyu: 'チュ', cye: 'チェ', cyo: 'チョ',
  dya: 'ヂャ', dyi: 'ヂィ', dyu: 'ヂュ', dye: 'ヂェ', dyo: 'ヂョ',
  nya: 'ニャ', nyi: 'ニィ', nyu: 'ニュ', nye: 'ニェ', nyo: 'ニョ',
  hya: 'ヒャ', hyi: 'ヒィ', hyu: 'ヒュ', hye: 'ヒェ', hyo: 'ヒョ',
  bya: 'ビャ', byi: 'ビィ', byu: 'ビュ', bye: 'ビェ', byo: 'ビョ',
  pya: 'ピャ', pyi: 'ピィ', pyu: 'ピュ', pye: 'ピェ', pyo: 'ピョ',
  mya: 'ミャ', myi: 'ミィ', myu: 'ミュ', mye: 'ミェ', myo: 'ミョ',
  rya: 'リャ', ryi: 'リィ', ryu: 'リュ', rye: 'リェ', ryo: 'リョ',
  fya: 'フャ', fyu: 'フュ', fyo: 'フョ',
  tsu: 'ツ', thi: 'ティ',

  ka: 'カ', ki: 'キ', ku: 'ク', ke: 'ケ', ko: 'コ',
  ga: 'ガ', gi: 'ギ', gu: 'グ', ge: 'ゲ', go: 'ゴ',
  sa: 'サ', si: 'シ', su: 'ス', se: 'セ', so: 'ソ',
  za: 'ザ', zi: 'ジ', zu: 'ズ', ze: 'ゼ', zo: 'ゾ',
  ja: 'ジャ', ji: 'ジ', ju: 'ジュ', je: 'ジェ', jo: 'ジョ',
  ta: 'タ', ti: 'チ', tu: 'ツ', te: 'テ', to: 'ト',
  da: 'ダ', di: 'ヂ', du: 'ヅ', de: 'デ', do: 'ド',
  na: 'ナ', ni: 'ニ', nu: 'ヌ', ne: 'ネ', no: 'ノ',
  ha: 'ハ', hi: 'ヒ', hu: 'フ', he: 'ヘ', ho: 'ホ',
  fa: 'ファ', fi: 'フィ', fu: 'フ', fe: 'フェ', fo: 'フォ',
  ba: 'バ', bi: 'ビ', bu: 'ブ', be: 'ベ', bo: 'ボ',
  pa: 'パ', pi: 'ピ', pu: 'プ', pe: 'ペ', po: 'ポ',
  ma: 'マ', mi: 'ミ', mu: 'ム', me: 'メ', mo: 'モ',
  ya: 'ヤ', yu: 'ユ', yo: 'ヨ',
  ra: 'ラ', ri: 'リ', ru: 'ル', re: 'レ', ro: 'ロ',
  wa: 'ワ', wi: 'ウィ', we: 'ウェ', wo: 'ヲ',
  va: 'ヴァ', vi: 'ヴィ', vu: 'ヴ', ve: 'ヴェ', vo: 'ヴォ',

  a: 'ア', i: 'イ', u: 'ウ', e: 'エ', o: 'オ',
};

function isAsciiAlpha(ch) {
  const c = ch.charCodeAt(0);
  return (c >= 0x41 && c <= 0x5a) || (c >= 0x61 && c <= 0x7a);
}

function romajiToKatakana(input) {
  const lower = input.toLowerCase();
  let result = '';
  let i = 0;
  while (i < lower.length) {
    const ch = lower[i];

    if (!isAsciiAlpha(ch)) {
      result += input[i];
      i++;
      continue;
    }

    const next = lower[i + 1];

    if (next && ch === next && !VOWELS.has(ch) && ch !== 'n') {
      result += 'ッ';
      i++;
      continue;
    }

    if (ch === 'n' && next === 'n') {
      // "nna" 等は最初の n のみ撥音とし、後続の "na" を別音節として処理する
      const third = lower[i + 2];
      if (third && (VOWELS.has(third) || third === 'y')) {
        result += 'ン';
        i++;
        continue;
      }
      result += 'ン';
      i += 2;
      continue;
    }

    if (ch === 'n' && (next == null || (!VOWELS.has(next) && next !== 'y'))) {
      result += 'ン';
      i++;
      continue;
    }

    const three = lower.slice(i, i + 3);
    if (ROMAJI_TABLE[three]) {
      result += ROMAJI_TABLE[three];
      i += 3;
      continue;
    }

    const two = lower.slice(i, i + 2);
    if (ROMAJI_TABLE[two]) {
      result += ROMAJI_TABLE[two];
      i += 2;
      continue;
    }

    if (ROMAJI_TABLE[ch]) {
      result += ROMAJI_TABLE[ch];
      i++;
      continue;
    }

    // 半端な子音はスキップ（入力途中とみなす）
    i++;
  }
  return result;
}

export function normalizeQuery(query) {
  if (query === '') return '';

  // ASCII 英字が含まれない純粋カナ入力はローマ字変換をスキップする（テーブル走査によるオーバーヘッドを避けるため）
  const preprocessed = /[a-zA-Z]/.test(query) ? romajiToKatakana(query) : query;

  let result = '';
  for (let i = 0; i < preprocessed.length; i++) {
    const ch = preprocessed[i];
    const code = ch.charCodeAt(0);

    if (code >= HIRAGANA_MIN && code <= HIRAGANA_MAX) {
      result += String.fromCharCode(code + HIRAGANA_TO_KATAKANA_OFFSET);
      continue;
    }

    const fullKana = HALFWIDTH_KANA[ch];
    if (fullKana !== undefined) {
      const next = preprocessed[i + 1];
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
