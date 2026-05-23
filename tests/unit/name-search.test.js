import { describe, it, expect } from 'vitest';
import { normalizeQuery, searchByName } from '../../src/logic/name-search.js';

describe('normalizeQuery()', () => {
  it('ひらがなを全角カタカナに変換する', () => {
    expect(normalizeQuery('うーらおす')).toBe('ウーラオス');
  });

  it('半角カタカナを全角カタカナに変換する', () => {
    expect(normalizeQuery('ｳｰﾗｵｽ')).toBe('ウーラオス');
  });

  it('既に全角カタカナの文字列はそのまま返す', () => {
    expect(normalizeQuery('ガブリアス')).toBe('ガブリアス');
  });

  it('半角の濁点を合成する', () => {
    expect(normalizeQuery('ｶﾞﾌﾞﾘｱｽ')).toBe('ガブリアス');
  });

  it('半角の半濁点を合成する', () => {
    expect(normalizeQuery('ﾊﾟﾝﾁ')).toBe('パンチ');
  });

  it('ASCII 括弧・空白・記号はそのまま、かなのみ変換する', () => {
    expect(normalizeQuery('ウーラオス (れ')).toBe('ウーラオス (レ');
  });

  it('空文字は空文字を返す', () => {
    expect(normalizeQuery('')).toBe('');
  });

  it('混在入力を一括で変換する', () => {
    expect(normalizeQuery('ｶﾞブりあス')).toBe('ガブリアス');
  });

  describe('ローマ字入力 → カタカナ変換', () => {
    it('"gabu" → "ガブ"', () => {
      expect(normalizeQuery('gabu')).toBe('ガブ');
    });

    it('"kairyu" → "カイリュ"（拗音）', () => {
      expect(normalizeQuery('kairyu')).toBe('カイリュ');
    });

    it('"shi" / "chi" / "tsu" のヘボン式', () => {
      expect(normalizeQuery('shi')).toBe('シ');
      expect(normalizeQuery('chi')).toBe('チ');
      expect(normalizeQuery('tsu')).toBe('ツ');
    });

    it('大文字 / 大文字小文字混在も小文字として扱う', () => {
      expect(normalizeQuery('GABU')).toBe('ガブ');
      expect(normalizeQuery('Gabu')).toBe('ガブ');
    });

    it('末尾の単独 n は撥音「ン」', () => {
      expect(normalizeQuery('pan')).toBe('パン');
    });

    it('nn は撥音「ン」', () => {
      expect(normalizeQuery('minna')).toBe('ミンナ');
      expect(normalizeQuery('minn')).toBe('ミン');
    });

    it('nn の後に拗音 y が続く場合は撥音 + 拗音として変換される', () => {
      expect(normalizeQuery('nnya')).toBe('ンニャ');
    });

    it('促音は子音重ねで「ッ」', () => {
      expect(normalizeQuery('kka')).toBe('ッカ');
      expect(normalizeQuery('katta')).toBe('カッタ');
    });

    it('半端な子音は無視して途中まで変換する', () => {
      expect(normalizeQuery('gab')).toBe('ガ');
      expect(normalizeQuery('gabur')).toBe('ガブ');
    });

    it('ローマ字とカタカナの混在もOK', () => {
      expect(normalizeQuery('ガbu')).toBe('ガブ');
    });

    it('「n」+「y」は撥音にならず拗音テーブルが効く', () => {
      expect(normalizeQuery('nyaa')).toBe('ニャア');
    });

    it('外来音 fa / vi など', () => {
      expect(normalizeQuery('fairi')).toBe('ファイリ');
      expect(normalizeQuery('vi')).toBe('ヴィ');
    });
  });

  describe('半角ハイフン → 長音記号変換', () => {
    it('ローマ字＋ハイフンで長音記号に変換する', () => {
      // `ri-` → `リー`、`u-raosu` → `ウーラオス`（ローマ字入力時の長音補完）
      expect(normalizeQuery('ri-')).toBe('リー');
      expect(normalizeQuery('u-raosu')).toBe('ウーラオス');
    });

    it('ひらがな・カタカナ＋ハイフンでも長音記号に変換する', () => {
      // ローマ字以外（かな入力中の補完）でも `-` を `ー` として扱う
      expect(normalizeQuery('ぴ-')).toBe('ピー');
      expect(normalizeQuery('ピ-')).toBe('ピー');
    });

    it('全角ハイフン `－` も NFKC 経由で長音記号に変換される', () => {
      // 全角ハイフン (U+FF0D) は NFKC で半角 `-` (U+002D) になり、その後 `ー` に変換される
      expect(normalizeQuery('ri－')).toBe('リー');
    });

    it('ハイフンを含まないローマ字入力は影響を受けない', () => {
      // 回帰確認: 既存のローマ字変換（`gabu` → `ガブ`）が壊れていない
      expect(normalizeQuery('gabu')).toBe('ガブ');
    });

    it('既存の長音記号 `ー` 直接入力は冪等', () => {
      // 回帰確認: 元入力に `ー` が既に含まれる場合もそのまま保たれる
      expect(normalizeQuery('ウーラオス')).toBe('ウーラオス');
    });
  });
});

describe('searchByName()', () => {
  const entries = [
    { num: 892, name: 'ウーラオス' },
    { num: 892, name: 'ウーラオス (れんげきのかた)' },
    { num: 831, name: 'ウールー' },
    { num: 445, name: 'ガブリアス' },
    { num: 25, name: 'ピカチュウ' },
  ];

  it('空文字は空配列を返す', () => {
    expect(searchByName('', entries)).toEqual([]);
  });

  it('前方一致した結果を num 昇順で返す', () => {
    const result = searchByName('ウー', entries);
    expect(result.map((e) => e.num)).toEqual([831, 892, 892]);
    expect(result[0].name).toBe('ウールー');
  });

  it('ひらがな入力でカタカナエントリにヒットする', () => {
    const result = searchByName('うー', entries);
    expect(result.map((e) => e.name)).toEqual([
      'ウールー',
      'ウーラオス',
      'ウーラオス (れんげきのかた)',
    ]);
  });

  it('半角カタカナ入力でエントリにヒットする', () => {
    const result = searchByName('ｶﾞﾌﾞ', entries);
    expect(result.map((e) => e.name)).toEqual(['ガブリアス']);
  });

  it('ローマ字＋ハイフン入力（ri-）でカタカナエントリにヒットする', () => {
    // `ri-` → `リー` に正規化され、リーフィア等に前方一致する（PRD 機能 3 受け入れ条件）
    const leafiaEntries = [...entries, { num: 470, name: 'リーフィア' }];
    const result = searchByName('ri-', leafiaEntries);
    expect(result.map((e) => e.name)).toEqual(['リーフィア']);
  });

  it('該当 0 件で空配列を返す', () => {
    expect(searchByName('ヌル', entries)).toEqual([]);
  });

  it('11 件以上ヒット時は 10 件にカットする', () => {
    const many = Array.from({ length: 15 }, (_, i) => ({ num: i + 1, name: `ウソッキ${i}` }));
    const result = searchByName('ウ', many);
    expect(result).toHaveLength(10);
    expect(result.map((e) => e.num)).toEqual([1, 2, 3, 4, 5, 6, 7, 8, 9, 10]);
  });

  it('entries 配列を変更しない（純粋関数）', () => {
    const snapshot = entries.map((e) => ({ ...e }));
    const original = [...entries];
    searchByName('ウ', entries);
    expect(entries).toEqual(original);
    expect(entries.map((e) => ({ ...e }))).toEqual(snapshot);
  });
});
