import { describe, it, expect, vi, beforeEach } from 'vitest';
import { DataLoader } from '../../src/data/loader.js';

const SAMPLE_POKEDEX = {
  ガブリアス: {
    num: 445,
    name: 'ガブリアス',
    types: ['Dragon', 'Ground'],
    baseStats: { hp: 108, atk: 130, def: 95, spa: 80, spd: 85, spe: 102 },
    abilities: ['さめはだ', 'すながくれ', 'かたやぶり'],
  },
  // 機能 7（P0.5 リファクタ）テスト用: メガフォームを親エントリの `megaForms[]` にネストする
  // 新スキーマで pokedex.json を構成する。loader のメガ関連 API（getMegaFormByName /
  // getMegaFormByItem / isMegaForm / getPokemonByName の null 返却 / searchByName のメガ除外）の
  // 検証で参照される。
  フシギバナ: {
    num: 3,
    name: 'フシギバナ',
    types: ['Grass', 'Poison'],
    baseStats: { hp: 80, atk: 82, def: 83, spa: 100, spd: 100, spe: 80 },
    abilities: ['しんりょく', 'ようりょくそ'],
    megaForms: [
      {
        key: 'venusaurmega',
        name: 'メガフシギバナ',
        item: 'フシギバナイト',
        types: ['Grass', 'Poison'],
        baseStats: { hp: 80, atk: 100, def: 123, spa: 122, spd: 120, spe: 80 },
        abilities: ['あついしぼう'],
      },
    ],
  },
  リザードン: {
    num: 6,
    name: 'リザードン',
    types: ['Fire', 'Flying'],
    baseStats: { hp: 78, atk: 84, def: 78, spa: 109, spd: 85, spe: 100 },
    abilities: ['もうか', 'サンパワー'],
    megaForms: [
      {
        key: 'charizardmegax',
        name: 'メガリザードンＸ',
        item: 'リザードナイトＸ',
        types: ['Fire', 'Dragon'],
        baseStats: { hp: 78, atk: 130, def: 111, spa: 130, spd: 85, spe: 100 },
        abilities: ['かたいツメ'],
      },
      {
        key: 'charizardmegay',
        name: 'メガリザードンＹ',
        item: 'リザードナイトＹ',
        types: ['Fire', 'Flying'],
        baseStats: { hp: 78, atk: 104, def: 78, spa: 159, spd: 115, spe: 100 },
        abilities: ['ひでり'],
      },
    ],
  },
  // メガシンカ非対応ポケモンの代表として、Champions のメガストーン対応外であるメタモンを使用する
  メタモン: {
    num: 132,
    name: 'メタモン',
    types: ['Normal'],
    baseStats: { hp: 48, atk: 48, def: 48, spa: 48, spd: 48, spe: 48 },
    abilities: ['へんしょく', 'じゅうなん'],
  },
};

const SAMPLE_MOVES = {
  じしん: { type: 'Ground', category: 'Physical', power: 100, accuracy: 100 },
};

const SAMPLE_ITEMS = {
  こだわりスカーフ: { modifier: { spe: 1.5 } },
};

const SAMPLE_ABILITIES = {
  さめはだ: { modifier: { condition: 'isContact', atk: 1.0 } },
};

const SAMPLE_TYPES = {
  Normal: 'ノーマル',
  Fire: 'ほのお',
  Dragon: 'ドラゴン',
  Ground: 'じめん',
};

const SAMPLE_MOVE_CATEGORIES = {
  Physical: '物理',
  Special: '特殊',
  Status: '変化',
};

const SAMPLE_NATURES = {
  いじっぱり: { modifiers: { atk: 1.1, spa: 0.9 } },
  がんばりや: { modifiers: {} },
};

const SAMPLE_PARTY = {
  party: [
    {
      species: 'ガブリアス',
      ability: 'さめはだ',
      item: 'こだわりスカーフ',
      nature: 'いじっぱり',
      abilityPoints: { hp: 32, atk: 32, def: 0, spa: 0, spd: 0, spe: 0 },
      moves: [{ name: 'じしん' }],
    },
  ],
};

function makeOkResponse(data) {
  return { ok: true, json: () => Promise.resolve(data) };
}

function makeNotFoundResponse() {
  return { ok: false };
}

function setupFetch(overrides = {}) {
  const defaults = {
    './data/pokedex.json': makeOkResponse(SAMPLE_POKEDEX),
    './data/moves.json': makeOkResponse(SAMPLE_MOVES),
    './data/items.json': makeOkResponse(SAMPLE_ITEMS),
    './data/abilities.json': makeOkResponse(SAMPLE_ABILITIES),
    './data/types.json': makeOkResponse(SAMPLE_TYPES),
    './data/move-categories.json': makeOkResponse(SAMPLE_MOVE_CATEGORIES),
    './data/natures.json': makeOkResponse(SAMPLE_NATURES),
    './data/party.json': makeOkResponse(SAMPLE_PARTY),
  };
  const map = { ...defaults, ...overrides };
  vi.stubGlobal('fetch', vi.fn((url) => Promise.resolve(map[url] ?? makeNotFoundResponse())));
}

beforeEach(() => {
  vi.unstubAllGlobals();
});

describe('DataLoader.load()', () => {
  it('正常系: 全データを返す', async () => {
    setupFetch();
    const loader = new DataLoader();
    const data = await loader.load();

    expect(data.pokedex).toEqual(SAMPLE_POKEDEX);
    expect(data.moves).toEqual(SAMPLE_MOVES);
    expect(data.items).toEqual(SAMPLE_ITEMS);
    expect(data.abilities).toEqual(SAMPLE_ABILITIES);
    expect(data.typeNames).toEqual(SAMPLE_TYPES);
    expect(data.moveCategories).toEqual(SAMPLE_MOVE_CATEGORIES);
    expect(data.natures).toEqual(SAMPLE_NATURES);
    expect(data.userParty).toEqual(SAMPLE_PARTY);
  });

  it('pokedex.json が存在しない場合は適切なメッセージで throw する', async () => {
    setupFetch({ './data/pokedex.json': makeNotFoundResponse() });
    const loader = new DataLoader();
    await expect(loader.load()).rejects.toThrow(
      'データファイルが見つかりません。C# ツールを実行してください'
    );
  });

  it('party.json が存在しない場合は適切なメッセージで throw する', async () => {
    setupFetch({ './data/party.json': makeNotFoundResponse() });
    const loader = new DataLoader();
    await expect(loader.load()).rejects.toThrow('party.json が見つかりません');
  });

  it('party.json の構文が不正な場合は throw する', async () => {
    setupFetch({
      './data/party.json': {
        ok: true,
        json: () => Promise.reject(new SyntaxError('Unexpected token')),
      },
    });
    const loader = new DataLoader();
    await expect(loader.load()).rejects.toThrow(
      'party.json の形式が正しくありません。JSONを確認してください'
    );
  });

  it('party.json の party キーが配列でない場合は throw する', async () => {
    setupFetch({ './data/party.json': makeOkResponse({ party: {} }) });
    const loader = new DataLoader();
    await expect(loader.load()).rejects.toThrow(
      'party.json の形式が正しくありません。JSONを確認してください'
    );
  });

  it('party.json に必須フィールド species が欠落している場合は throw する', async () => {
    const invalidParty = {
      party: [{ ability: 'さめはだ', nature: 'いじっぱり', abilityPoints: {}, moves: [] }],
    };
    setupFetch({ './data/party.json': makeOkResponse(invalidParty) });
    const loader = new DataLoader();
    await expect(loader.load()).rejects.toThrow(
      'party.json の形式が正しくありません。JSONを確認してください'
    );
  });

  it('party.json に必須フィールド nature が欠落している場合は throw する', async () => {
    const invalidParty = {
      party: [{ species: 'ガブリアス', abilityPoints: {}, moves: [] }],
    };
    setupFetch({ './data/party.json': makeOkResponse(invalidParty) });
    const loader = new DataLoader();
    await expect(loader.load()).rejects.toThrow(
      'party.json の形式が正しくありません。JSONを確認してください'
    );
  });

  it('party.json に必須フィールド abilityPoints が欠落している場合は throw する', async () => {
    const invalidParty = {
      party: [{ species: 'ガブリアス', nature: 'いじっぱり', moves: [] }],
    };
    setupFetch({ './data/party.json': makeOkResponse(invalidParty) });
    const loader = new DataLoader();
    await expect(loader.load()).rejects.toThrow(
      'party.json の形式が正しくありません。JSONを確認してください'
    );
  });

  it('party.json に必須フィールド moves が欠落している場合は throw する', async () => {
    const invalidParty = {
      party: [{ species: 'ガブリアス', nature: 'いじっぱり', abilityPoints: {} }],
    };
    setupFetch({ './data/party.json': makeOkResponse(invalidParty) });
    const loader = new DataLoader();
    await expect(loader.load()).rejects.toThrow(
      'party.json の形式が正しくありません。JSONを確認してください'
    );
  });

  it('types.json が存在しない場合は適切なメッセージで throw する', async () => {
    setupFetch({ './data/types.json': makeNotFoundResponse() });
    const loader = new DataLoader();
    await expect(loader.load()).rejects.toThrow(
      'types.json が見つかりません。リポジトリを確認してください'
    );
  });

  it('party.json の species が pokedex.json に未登録でも load() は throw せず継続する', async () => {
    // UI 側で null チェックして「不明なポケモン: <species>」を表示する設計のため、
    // データ層では species の pokedex 存在検証はせず throw もしない
    const unknownSpeciesParty = {
      party: [
        {
          species: '未知のポケモン',
          ability: 'さめはだ',
          item: 'こだわりスカーフ',
          nature: 'いじっぱり',
          abilityPoints: { hp: 0, atk: 0, def: 0, spa: 0, spd: 0, spe: 0 },
          moves: [{ name: 'じしん' }],
        },
      ],
    };
    setupFetch({ './data/party.json': makeOkResponse(unknownSpeciesParty) });
    const loader = new DataLoader();
    const data = await loader.load();
    expect(data.userParty.party[0].species).toBe('未知のポケモン');
    expect(loader.getPokemonByName('未知のポケモン')).toBeNull();
  });
});

describe('DataLoader.getTypeName()', () => {
  it('既知のタイプを日本語に変換する', async () => {
    setupFetch();
    const loader = new DataLoader();
    await loader.load();
    expect(loader.getTypeName('Fire')).toBe('ほのお');
    expect(loader.getTypeName('Dragon')).toBe('ドラゴン');
  });

  it('未知のタイプは英語表記をそのまま返す', async () => {
    setupFetch();
    const loader = new DataLoader();
    await loader.load();
    expect(loader.getTypeName('Unknown')).toBe('Unknown');
  });
});

describe('DataLoader.getMoveCategory()', () => {
  it('カテゴリ値を日本語に変換する', async () => {
    setupFetch();
    const loader = new DataLoader();
    await loader.load();
    expect(loader.getMoveCategory('Physical')).toBe('物理');
    expect(loader.getMoveCategory('Status')).toBe('変化');
  });

  it('Special を日本語に変換する', async () => {
    setupFetch();
    const loader = new DataLoader();
    await loader.load();
    expect(loader.getMoveCategory('Special')).toBe('特殊');
  });
});

describe('DataLoader.getNatureModifiers()', () => {
  it('補正ありの性格で補正倍率マップを返す', async () => {
    setupFetch();
    const loader = new DataLoader();
    await loader.load();
    expect(loader.getNatureModifiers('いじっぱり')).toEqual({ atk: 1.1, spa: 0.9 });
  });

  it('補正なしの性格で空オブジェクトを返す', async () => {
    setupFetch();
    const loader = new DataLoader();
    await loader.load();
    expect(loader.getNatureModifiers('がんばりや')).toEqual({});
  });
});

describe('DataLoader.getMove()', () => {
  it('存在する技名で Move オブジェクトを返す', async () => {
    setupFetch();
    const loader = new DataLoader();
    await loader.load();
    const move = loader.getMove('じしん');
    expect(move).not.toBeNull();
    expect(move.type).toBe('Ground');
    expect(move.category).toBe('Physical');
    expect(move.power).toBe(100);
  });

  it('存在しない技名で null を返す', async () => {
    setupFetch();
    const loader = new DataLoader();
    await loader.load();
    expect(loader.getMove('存在しない技')).toBeNull();
  });
});

describe('DataLoader.getItemModifier()', () => {
  it('登録済み持ち物で Modifier オブジェクトを返す（modifier ラッパーを除去した形で）', async () => {
    setupFetch();
    const loader = new DataLoader();
    await loader.load();
    const modifier = loader.getItemModifier('こだわりスカーフ');
    expect(modifier).not.toBeNull();
    expect(modifier.spe).toBe(1.5);
  });

  it('未登録の持ち物で null を返す', async () => {
    setupFetch();
    const loader = new DataLoader();
    await loader.load();
    expect(loader.getItemModifier('存在しない持ち物')).toBeNull();
  });
});

describe('DataLoader.getAbilityModifier()', () => {
  it('登録済み特性で Modifier オブジェクトを返す（modifier ラッパーを除去した形で）', async () => {
    setupFetch();
    const loader = new DataLoader();
    await loader.load();
    const modifier = loader.getAbilityModifier('さめはだ');
    expect(modifier).not.toBeNull();
    expect(modifier.condition).toBe('isContact');
  });

  it('未登録の特性で null を返す', async () => {
    setupFetch();
    const loader = new DataLoader();
    await loader.load();
    expect(loader.getAbilityModifier('存在しない特性')).toBeNull();
  });
});

describe('DataLoader.searchByName()', () => {
  it('クエリで前方一致した PokedexEntry を返す', async () => {
    setupFetch();
    const loader = new DataLoader();
    await loader.load();
    const result = loader.searchByName('ガブ');
    expect(result.map((e) => e.name)).toEqual(['ガブリアス']);
  });

  it('load() 前は空配列を返す', () => {
    const loader = new DataLoader();
    expect(loader.searchByName('ガブ')).toEqual([]);
  });

  it('空文字クエリで空配列を返す', async () => {
    setupFetch();
    const loader = new DataLoader();
    await loader.load();
    expect(loader.searchByName('')).toEqual([]);
  });
});

describe('DataLoader.getPokemonByName()', () => {
  it('存在するポケモン名で PokedexEntry を返す', async () => {
    setupFetch();
    const loader = new DataLoader();
    await loader.load();
    const entry = loader.getPokemonByName('ガブリアス');
    expect(entry).not.toBeNull();
    expect(entry.name).toBe('ガブリアス');
    expect(entry.types).toEqual(['Dragon', 'Ground']);
    // calcActualStats / calcSpeedPatterns へ渡す前提となる構造を担保する
    expect(entry.baseStats.spe).toBe(102);
    expect(Array.isArray(entry.abilities)).toBe(true);
    expect(entry.abilities.length).toBeGreaterThanOrEqual(1);
  });

  it('メガシンカ可能な親ポケモンは megaForms[] フィールドを持つ（機能 7 リファクタ）', async () => {
    setupFetch();
    const loader = new DataLoader();
    await loader.load();
    const entry = loader.getPokemonByName('フシギバナ');
    expect(entry.megaForms).toHaveLength(1);
    expect(entry.megaForms[0].key).toBe('venusaurmega');
    expect(entry.megaForms[0].name).toBe('メガフシギバナ');
    expect(entry.megaForms[0].item).toBe('フシギバナイト');
  });

  it('メガシンカ不可のポケモンは megaForms フィールドを持たない', async () => {
    setupFetch();
    const loader = new DataLoader();
    await loader.load();
    expect(loader.getPokemonByName('メタモン').megaForms).toBeUndefined();
  });

  it('存在しないポケモン名で null を返す', async () => {
    setupFetch();
    const loader = new DataLoader();
    await loader.load();
    expect(loader.getPokemonByName('存在しないポケモン')).toBeNull();
  });

  it('メガフォーム名で null を返す（機能 7: party.json でメガ名指定は不明なポケモン扱い）', async () => {
    setupFetch();
    const loader = new DataLoader();
    await loader.load();
    expect(loader.getPokemonByName('メガフシギバナ')).toBeNull();
    expect(loader.getPokemonByName('メガリザードンＸ')).toBeNull();
    expect(loader.getPokemonByName('メガリザードンＹ')).toBeNull();
  });
});

describe('DataLoader メガシンカ関連 API（機能 7、P0.5 リファクタ）', () => {
  it('isMegaForm: メガフォーム名で true', async () => {
    setupFetch();
    const loader = new DataLoader();
    await loader.load();
    expect(loader.isMegaForm('メガフシギバナ')).toBe(true);
    expect(loader.isMegaForm('メガリザードンＸ')).toBe(true);
    expect(loader.isMegaForm('メガリザードンＹ')).toBe(true);
  });

  it('isMegaForm: 通常ポケモン名・未登録名で false', async () => {
    setupFetch();
    const loader = new DataLoader();
    await loader.load();
    expect(loader.isMegaForm('フシギバナ')).toBe(false);
    expect(loader.isMegaForm('メタモン')).toBe(false);
    expect(loader.isMegaForm('存在しないポケモン')).toBe(false);
  });

  it('getMegaFormByName: メガフォーム名で MegaFormData を返す', async () => {
    setupFetch();
    const loader = new DataLoader();
    await loader.load();
    const data = loader.getMegaFormByName('メガフシギバナ');
    expect(data).not.toBeNull();
    expect(data.key).toBe('venusaurmega');
    expect(data.name).toBe('メガフシギバナ');
    expect(data.item).toBe('フシギバナイト');
    expect(data.baseStats.atk).toBe(100);
    expect(data.abilities).toEqual(['あついしぼう']);
  });

  it('getMegaFormByName: 複数メガから個別形態を引ける', async () => {
    setupFetch();
    const loader = new DataLoader();
    await loader.load();
    const x = loader.getMegaFormByName('メガリザードンＸ');
    const y = loader.getMegaFormByName('メガリザードンＹ');
    expect(x.types).toEqual(['Fire', 'Dragon']);
    expect(y.types).toEqual(['Fire', 'Flying']);
    expect(x.item).toBe('リザードナイトＸ');
    expect(y.item).toBe('リザードナイトＹ');
  });

  it('getMegaFormByName: 通常ポケモン名・未登録名で null', async () => {
    setupFetch();
    const loader = new DataLoader();
    await loader.load();
    expect(loader.getMegaFormByName('フシギバナ')).toBeNull();
    expect(loader.getMegaFormByName('メタモン')).toBeNull();
    expect(loader.getMegaFormByName('存在しないポケモン')).toBeNull();
  });

  it('getMegaFormByItem: 親名 + 対応メガストーンで MegaFormData を返す', async () => {
    setupFetch();
    const loader = new DataLoader();
    await loader.load();
    const data = loader.getMegaFormByItem('フシギバナ', 'フシギバナイト');
    expect(data).not.toBeNull();
    expect(data.name).toBe('メガフシギバナ');
  });

  it('getMegaFormByItem: 複数メガ（リザードン）で持ち物に応じた形態を引ける', async () => {
    setupFetch();
    const loader = new DataLoader();
    await loader.load();
    const x = loader.getMegaFormByItem('リザードン', 'リザードナイトＸ');
    const y = loader.getMegaFormByItem('リザードン', 'リザードナイトＹ');
    expect(x.name).toBe('メガリザードンＸ');
    expect(y.name).toBe('メガリザードンＹ');
  });

  it('getMegaFormByItem: 対応外メガストーン（例: フシギバナ + カメックスナイト）で null', async () => {
    setupFetch();
    const loader = new DataLoader();
    await loader.load();
    expect(loader.getMegaFormByItem('フシギバナ', 'カメックスナイト')).toBeNull();
  });

  it('getMegaFormByItem: メガシンカ用アイテムでない持ち物（例: こだわりハチマキ）で null', async () => {
    setupFetch();
    const loader = new DataLoader();
    await loader.load();
    expect(loader.getMegaFormByItem('フシギバナ', 'こだわりハチマキ')).toBeNull();
  });

  it('getMegaFormByItem: メガシンカ不可のポケモン名で null', async () => {
    setupFetch();
    const loader = new DataLoader();
    await loader.load();
    expect(loader.getMegaFormByItem('メタモン', 'フシギバナイト')).toBeNull();
  });

  it('searchByName: メガフォームはサジェスト候補から除外される（トップレベル走査による自然な除外）', async () => {
    setupFetch();
    const loader = new DataLoader();
    await loader.load();
    const result = loader.searchByName('メガ');
    expect(result).toEqual([]);
  });

  it('searchByName: 親ポケモン名はメガシンカ可能でも通常通り候補に含まれる', async () => {
    setupFetch();
    const loader = new DataLoader();
    await loader.load();
    const result = loader.searchByName('リザード');
    expect(result.map((e) => e.name)).toEqual(['リザードン']);
  });
});
