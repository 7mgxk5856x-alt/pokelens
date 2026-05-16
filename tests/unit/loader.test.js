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
  });

  it('存在しないポケモン名で null を返す', async () => {
    setupFetch();
    const loader = new DataLoader();
    await loader.load();
    expect(loader.getPokemonByName('存在しないポケモン')).toBeNull();
  });
});
