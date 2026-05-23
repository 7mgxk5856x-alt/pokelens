import { describe, it, expect } from 'vitest';
import { calcPowerIndex } from '../../src/logic/power-index-calc.js';

const STATS = { hp: 215, atk: 200, def: 115, spa: 90, spd: 105, spe: 122 };

function physical(overrides = {}) {
  return { type: 'Ground', category: 'Physical', power: 100, accuracy: 100, ...overrides };
}
function special(overrides = {}) {
  return { type: 'Electric', category: 'Special', power: 90, accuracy: 100, ...overrides };
}

describe('calcPowerIndex()', () => {
  it('変化技は null を返す', () => {
    const move = { type: 'Normal', category: 'Status', power: null, accuracy: null };
    expect(calcPowerIndex(move, STATS, ['Normal'], 1.0, 1.0)).toBeNull();
  });

  it('威力不定技 (power=null) は null を返す', () => {
    const move = physical({ power: null });
    expect(calcPowerIndex(move, STATS, ['Ground'], 1.0, 1.0)).toBeNull();
  });

  it('物理技は atk 実数値を使う', () => {
    expect(calcPowerIndex(physical(), STATS, [], 1.0, 1.0)).toBe(100 * 200 * 1.0);
  });

  it('特殊技は spa 実数値を使う', () => {
    expect(calcPowerIndex(special(), STATS, [], 1.0, 1.0)).toBe(90 * 90 * 1.0);
  });

  it('STAB あり: タイプ一致で 1.5 倍', () => {
    expect(calcPowerIndex(physical(), STATS, ['Ground', 'Dragon'], 1.0, 1.0)).toBe(
      100 * 200 * 1.5
    );
  });

  it('STAB なし: タイプ不一致で 1.0 倍', () => {
    expect(calcPowerIndex(physical(), STATS, ['Fire'], 1.0, 1.0)).toBe(100 * 200 * 1.0);
  });

  it('abilityModifier と itemModifier を反映する', () => {
    expect(calcPowerIndex(physical(), STATS, ['Ground'], 1.3, 1.2)).toBeCloseTo(
      100 * 200 * 1.5 * 1.3 * 1.2
    );
  });

  it('特殊技でも STAB × ability × item の三重補正が乗る (spa 参照)', () => {
    expect(calcPowerIndex(special({ type: 'Fire' }), STATS, ['Fire'], 1.3, 1.2)).toBeCloseTo(
      90 * 90 * 1.5 * 1.3 * 1.2
    );
  });

  it('multihit 最大総威力 (basePower×multihit[1]) の値で火力指数を計算する', () => {
    // doubleslap 等の連続技は MergeConverter が power=basePower×maxHits を格納する。
    // calcPowerIndex は受け取った power をそのまま使うので、ここでは power=75（15×5 相当）で検証する。
    const move = { type: 'Water', category: 'Physical', power: 75, accuracy: 100 };
    expect(calcPowerIndex(move, STATS, ['Water'], 1.0, 1.0)).toBeCloseTo(75 * 200 * 1.5);
  });

  it('威力不定技パッチ適用後の power 値で火力指数を計算する', () => {
    // メトロノーム等の威力不定技は moves-power-patch で補完された値が power に入る。
    // calcPowerIndex はパッチか multihit かを区別せず受け取った power で計算する。
    const move = { type: 'Normal', category: 'Physical', power: 120, accuracy: null };
    expect(calcPowerIndex(move, STATS, ['Normal'], 1.0, 1.0)).toBeCloseTo(120 * 200 * 1.5);
  });

  it('引数オブジェクトを変更しない', () => {
    const move = physical();
    const stats = { ...STATS };
    const types = ['Ground'];
    const moveSnap = { ...move };
    const statsSnap = { ...stats };
    const typesSnap = [...types];

    calcPowerIndex(move, stats, types, 1.2, 1.1);

    expect(move).toEqual(moveSnap);
    expect(stats).toEqual(statsSnap);
    expect(types).toEqual(typesSnap);
  });
});
