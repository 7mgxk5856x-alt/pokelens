import { describe, it, expect } from 'vitest';
import { resolveModifier } from '../../src/logic/resolve-modifier.js';

const physical = (overrides = {}) => ({
  type: 'Ground',
  category: 'Physical',
  power: 100,
  tags: [],
  ...overrides,
});
const special = (overrides = {}) => ({
  type: 'Fire',
  category: 'Special',
  power: 90,
  tags: [],
  ...overrides,
});
const status = (overrides = {}) => ({
  type: 'Normal',
  category: 'Status',
  power: null,
  tags: [],
  ...overrides,
});

describe('resolveModifier()', () => {
  describe('modifier がない場合', () => {
    it('null のとき multiplier 1.0, typesForCalc は pokemonTypes', () => {
      expect(resolveModifier(null, physical(), ['Ground'], 'ability')).toEqual({
        multiplier: 1.0,
        typesForCalc: ['Ground'],
      });
    });

    it('undefined のときも同様にフォールバック', () => {
      expect(resolveModifier(undefined, physical(), ['Fire'], 'item')).toEqual({
        multiplier: 1.0,
        typesForCalc: ['Fire'],
      });
    });
  });

  describe('condition なし（無条件補正）', () => {
    it('Physical 技は modifier.atk を採用', () => {
      const r = resolveModifier({ atk: 1.3, spa: 1.3 }, physical(), ['Ground'], 'item');
      expect(r).toEqual({ multiplier: 1.3, typesForCalc: ['Ground'] });
    });

    it('Special 技は modifier.spa を採用', () => {
      const r = resolveModifier({ atk: 1.3, spa: 1.3 }, special(), ['Fire'], 'item');
      expect(r).toEqual({ multiplier: 1.3, typesForCalc: ['Fire'] });
    });

    it('Status 技は 1.0', () => {
      const r = resolveModifier({ atk: 1.3, spa: 1.3 }, status(), [], 'item');
      expect(r.multiplier).toBe(1.0);
    });

    it('atk / spa が未定義のステータスは 1.0', () => {
      expect(
        resolveModifier({ atk: 1.5 }, special(), ['Fire'], 'item').multiplier
      ).toBe(1.0);
    });

    it('modifier に atk / spa いずれも未定義の場合 Physical 技で 1.0', () => {
      expect(resolveModifier({}, physical(), ['Ground'], 'item').multiplier).toBe(1.0);
    });

    it('modifier に atk / spa いずれも未定義の場合 Special 技で 1.0', () => {
      expect(resolveModifier({}, special(), ['Fire'], 'item').multiplier).toBe(1.0);
    });
  });

  describe('isStab + ability（特性のタイプ一致補正、STAB倍率置換）', () => {
    it('技タイプ一致なら modifier.stab を返し、typesForCalc は空配列', () => {
      const r = resolveModifier(
        { stab: 2.0, condition: 'isStab' },
        physical({ type: 'Ground' }),
        ['Ground'],
        'ability'
      );
      expect(r).toEqual({ multiplier: 2.0, typesForCalc: [] });
    });

    it('技タイプ不一致なら multiplier 1.0、typesForCalc は pokemonTypes', () => {
      const r = resolveModifier(
        { stab: 2.0, condition: 'isStab' },
        physical({ type: 'Fire' }),
        ['Ground'],
        'ability'
      );
      expect(r).toEqual({ multiplier: 1.0, typesForCalc: ['Ground'] });
    });

    it('stab フィールド未指定なら 2.0 がデフォルト', () => {
      const r = resolveModifier(
        { condition: 'isStab' },
        physical(),
        ['Ground'],
        'ability'
      );
      expect(r.multiplier).toBe(2.0);
    });
  });

  describe('isStab + item（アイテムのタイプ一致時補正、たつじんのおび等）', () => {
    it('技タイプ一致なら pickStatMultiplier を返し、typesForCalc は pokemonTypes', () => {
      const r = resolveModifier(
        { atk: 1.2, spa: 1.2, condition: 'isStab' },
        physical({ type: 'Ground' }),
        ['Ground'],
        'item'
      );
      expect(r).toEqual({ multiplier: 1.2, typesForCalc: ['Ground'] });
    });

    it('技タイプ不一致なら multiplier 1.0、typesForCalc は pokemonTypes', () => {
      const r = resolveModifier(
        { atk: 1.2, spa: 1.2, condition: 'isStab' },
        physical({ type: 'Fire' }),
        ['Ground'],
        'item'
      );
      expect(r).toEqual({ multiplier: 1.0, typesForCalc: ['Ground'] });
    });

    it('Special 技でも spa を採用してタイプ一致補正が機能する', () => {
      const r = resolveModifier(
        { atk: 1.2, spa: 1.2, condition: 'isStab' },
        special({ type: 'Fire' }),
        ['Fire'],
        'item'
      );
      expect(r.multiplier).toBe(1.2);
    });
  });

  describe('isType（特定タイプ補正）', () => {
    it('moveType と一致するなら multiplier を返す', () => {
      const r = resolveModifier(
        { spa: 1.5, condition: 'isType', moveType: 'Fire' },
        special({ type: 'Fire' }),
        ['Fire'],
        'ability'
      );
      expect(r.multiplier).toBe(1.5);
    });

    it('moveType と一致しなければ 1.0', () => {
      const r = resolveModifier(
        { spa: 1.5, condition: 'isType', moveType: 'Fire' },
        special({ type: 'Water' }),
        ['Water'],
        'ability'
      );
      expect(r.multiplier).toBe(1.0);
    });

    it('moveType が未指定なら 1.0', () => {
      const r = resolveModifier(
        { spa: 1.5, condition: 'isType' },
        special({ type: 'Fire' }),
        ['Fire'],
        'ability'
      );
      expect(r.multiplier).toBe(1.0);
    });
  });

  describe('タグ条件（isPunch / isPulse / isBite / isRecoil / isSlice / isContact / isSound / hasSecondary）', () => {
    it('isPunch + 該当タグありで multiplier を返す', () => {
      const r = resolveModifier(
        { atk: 1.2, condition: 'isPunch' },
        physical({ tags: ['isPunch', 'isContact'] }),
        ['Ground'],
        'ability'
      );
      expect(r.multiplier).toBe(1.2);
    });

    it('isPunch + 該当タグなしで 1.0', () => {
      const r = resolveModifier(
        { atk: 1.2, condition: 'isPunch' },
        physical({ tags: ['isContact'] }),
        ['Ground'],
        'ability'
      );
      expect(r.multiplier).toBe(1.0);
    });

    it.each(['isPulse', 'isBite', 'isRecoil', 'isSlice', 'isContact', 'isSound', 'hasSecondary'])(
      '%s + 該当タグありで multiplier を返す',
      (condName) => {
        const r = resolveModifier(
          { atk: 1.2, condition: condName },
          physical({ tags: [condName] }),
          ['Ground'],
          'ability'
        );
        expect(r.multiplier).toBe(1.2);
      }
    );

    it('isContact + 該当タグなしで 1.0', () => {
      const r = resolveModifier(
        { atk: 1.3, condition: 'isContact' },
        physical({ tags: ['isProtect'] }),
        ['Ground'],
        'ability'
      );
      expect(r.multiplier).toBe(1.0);
    });

    it('isSound + 該当タグありで Special 技に spa が適用される', () => {
      const r = resolveModifier(
        { atk: 1.3, spa: 1.3, condition: 'isSound' },
        special({ tags: ['isSound'] }),
        ['Normal'],
        'ability'
      );
      expect(r.multiplier).toBe(1.3);
    });

    it('hasSecondary + 該当タグなしで 1.0', () => {
      const r = resolveModifier(
        { atk: 1.3, condition: 'hasSecondary' },
        physical({ tags: [] }),
        ['Ground'],
        'ability'
      );
      expect(r.multiplier).toBe(1.0);
    });

    it('move.tags が undefined でも安全に 1.0 を返す', () => {
      const r = resolveModifier(
        { atk: 1.2, condition: 'isPunch' },
        { type: 'Ground', category: 'Physical', power: 80 },
        ['Ground'],
        'ability'
      );
      expect(r.multiplier).toBe(1.0);
    });
  });

  describe('powerMax60（低威力技補正）', () => {
    it('power = 40 で multiplier を返す', () => {
      const r = resolveModifier(
        { atk: 1.5, condition: 'powerMax60' },
        physical({ power: 40 }),
        ['Ground'],
        'ability'
      );
      expect(r.multiplier).toBe(1.5);
    });

    it('power = 60（境界値）でも multiplier を返す', () => {
      const r = resolveModifier(
        { atk: 1.5, condition: 'powerMax60' },
        physical({ power: 60 }),
        ['Ground'],
        'ability'
      );
      expect(r.multiplier).toBe(1.5);
    });

    it('power = 61 で 1.0', () => {
      const r = resolveModifier(
        { atk: 1.5, condition: 'powerMax60' },
        physical({ power: 61 }),
        ['Ground'],
        'ability'
      );
      expect(r.multiplier).toBe(1.0);
    });

    it('power = null で 1.0（威力不定技）', () => {
      const r = resolveModifier(
        { atk: 1.5, condition: 'powerMax60' },
        physical({ power: null }),
        ['Ground'],
        'ability'
      );
      expect(r.multiplier).toBe(1.0);
    });
  });

  describe('convertNormalTo（スキン系: Normal技を別タイプに変換 + 倍率）', () => {
    const skinModifier = {
      atk: 1.2,
      spa: 1.2,
      condition: 'convertNormalTo',
      convertedType: 'Fairy',
    };

    it('Normal 技は moveTypeOverride を伴って multiplier を返す', () => {
      const r = resolveModifier(skinModifier, physical({ type: 'Normal' }), ['Normal'], 'ability');
      expect(r).toEqual({
        multiplier: 1.2,
        typesForCalc: ['Normal'],
        moveTypeOverride: 'Fairy',
      });
    });

    it('Normal 以外の技には適用されず moveTypeOverride も付かない', () => {
      const r = resolveModifier(skinModifier, physical({ type: 'Ground' }), ['Normal'], 'ability');
      expect(r).toEqual({ multiplier: 1.0, typesForCalc: ['Normal'] });
      expect(r.moveTypeOverride).toBeUndefined();
    });

    it('convertedType 未指定なら 1.0 を返し moveTypeOverride も付かない', () => {
      const r = resolveModifier(
        { atk: 1.2, condition: 'convertNormalTo' },
        physical({ type: 'Normal' }),
        ['Normal'],
        'ability'
      );
      expect(r).toEqual({ multiplier: 1.0, typesForCalc: ['Normal'] });
    });

    it('Special 技でも moveTypeOverride を返す', () => {
      const r = resolveModifier(skinModifier, special({ type: 'Normal' }), ['Normal'], 'ability');
      expect(r.moveTypeOverride).toBe('Fairy');
      expect(r.multiplier).toBe(1.2);
    });
  });

  describe('convertAllTo（ノーマライズ系: 全技を別タイプに変換 + 倍率）', () => {
    const normalize = {
      atk: 1.2,
      spa: 1.2,
      condition: 'convertAllTo',
      convertedType: 'Normal',
    };

    it('Normal 以外の技も moveTypeOverride を伴って multiplier を返す', () => {
      const r = resolveModifier(normalize, physical({ type: 'Ground' }), ['Ground'], 'ability');
      expect(r).toEqual({
        multiplier: 1.2,
        typesForCalc: ['Ground'],
        moveTypeOverride: 'Normal',
      });
    });

    it('Normal 技にも moveTypeOverride を返す（既に Normal でも上書きで一貫）', () => {
      const r = resolveModifier(normalize, physical({ type: 'Normal' }), ['Normal'], 'ability');
      expect(r.moveTypeOverride).toBe('Normal');
    });

    it('convertedType 未指定なら 1.0 を返す', () => {
      const r = resolveModifier(
        { atk: 1.2, condition: 'convertAllTo' },
        physical(),
        ['Ground'],
        'ability'
      );
      expect(r).toEqual({ multiplier: 1.0, typesForCalc: ['Ground'] });
    });
  });

  describe('フォールバック', () => {
    it('未知の condition は multiplier 1.0', () => {
      const r = resolveModifier(
        { atk: 1.5, condition: 'isUnknownCondition' },
        physical(),
        ['Ground'],
        'ability'
      );
      expect(r).toEqual({ multiplier: 1.0, typesForCalc: ['Ground'] });
    });
  });

  describe('純粋関数性', () => {
    it('引数オブジェクト・配列を変更しない', () => {
      const modifier = { atk: 1.3, spa: 1.3, condition: 'isStab' };
      const move = physical({ type: 'Ground' });
      const types = ['Ground', 'Dragon'];
      const modSnap = JSON.parse(JSON.stringify(modifier));
      const moveSnap = JSON.parse(JSON.stringify(move));
      const typesSnap = [...types];

      resolveModifier(modifier, move, types, 'item');

      expect(modifier).toEqual(modSnap);
      expect(move).toEqual(moveSnap);
      expect(types).toEqual(typesSnap);
    });
  });
});
