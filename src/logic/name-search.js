const MAX_RESULTS = 10;

const HIRAGANA_MIN = 0x3041;
const HIRAGANA_MAX = 0x3096;
const HIRAGANA_TO_KATAKANA_OFFSET = 0x60;

// ASCII 英字の文字コード境界（A–Z / a–z）。ローマ字判定に使う
const ASCII_UPPER_A = 'A'.charCodeAt(0);
const ASCII_UPPER_Z = 'Z'.charCodeAt(0);
const ASCII_LOWER_A = 'a'.charCodeAt(0);
const ASCII_LOWER_Z = 'z'.charCodeAt(0);

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
  return (
    (c >= ASCII_UPPER_A && c <= ASCII_UPPER_Z) ||
    (c >= ASCII_LOWER_A && c <= ASCII_LOWER_Z)
  );
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

/**
 * 検索クエリを比較用に正規化する。NFKC で半角カナ・濁点結合・全角英数字を一括処理し、
 * 半角ハイフンを長音記号に変換、ローマ字をカタカナへ変換、ひらがなを全角カタカナに揃える。
 * @param {string} query 入力文字列
 * @returns {string} 正規化後の文字列
 */
export function normalizeQuery(query) {
  if (query === '') {
    return '';
  }

  // NFKC で半角カナ→全角カナ・濁点 / 半濁点の結合・全角英数字→ASCII を一括処理する
  // （個別の対応辞書を持たず Unicode 標準に委ねる）
  const nfkc = query.normalize('NFKC');

  // 半角ハイフン `-` を長音記号 `ー` に正規化する。
  // ローマ字入力中（例: `ri-` → `リー`）にキーボードから長音記号を直接入力できない補完。
  // 全角ハイフン `－` (U+FF0D) は NFKC で `-` (U+002D) に正規化されるためここで一緒に拾える。
  // 位置によらず全ての `-` を置換するため `ga-bu` → `ガーブ` のように中間ハイフンも変換されるが、
  // ポケモン名検索の文脈で中間ハイフン入力のユースケースはなく許容する。
  const withLongVowel = nfkc.replace(/-/g, 'ー');

  // ASCII 英字が含まれていればローマ字→カタカナ変換を適用する。
  // 元入力に全角英字（'Ａ' 等）があった場合も NFKC で 'A' になりここで拾える
  const preprocessed = /[a-zA-Z]/.test(withLongVowel)
    ? romajiToKatakana(withLongVowel)
    : withLongVowel;

  // 残るのはひらがな→カタカナのみ（NFKC はスクリプト変換しない）
  let result = '';
  for (let i = 0; i < preprocessed.length; i++) {
    const code = preprocessed.charCodeAt(i);
    if (code >= HIRAGANA_MIN && code <= HIRAGANA_MAX) {
      result += String.fromCharCode(code + HIRAGANA_TO_KATAKANA_OFFSET);
    } else {
      result += preprocessed[i];
    }
  }
  return result;
}

/**
 * 正規化したクエリでエントリ名を前方一致検索する。図鑑番号順にソートし、最大件数で打ち切る。
 * @param {string} query 検索クエリ
 * @param {Array<{name: string, num: number}>} entries 検索対象エントリ
 * @returns {Array} 一致したエントリ（図鑑番号昇順、最大 MAX_RESULTS 件）
 */
export function searchByName(query, entries) {
  if (query === '') {
    return [];
  }
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
