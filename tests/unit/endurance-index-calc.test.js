import { describe, it, expect } from 'vitest';
import {
  calcEnduranceIndex,
  calcEnduranceIndexPatterns,
} from '../../src/logic/endurance-index-calc.js';

describe('calcEnduranceIndex', () => {
  it('HP × 防御 を積で返す', () => {
    expect(calcEnduranceIndex(215, 115)).toBe(24725);
  });

  it('特殊側（HP × 特防）も同じ式で算出する', () => {
    expect(calcEnduranceIndex(215, 105)).toBe(22575);
  });

  it('境界値 1×1 でも破綻しない', () => {
    expect(calcEnduranceIndex(1, 1)).toBe(1);
  });

  it('大きい値（999 × 999）でも整数として算出', () => {
    expect(calcEnduranceIndex(999, 999)).toBe(998001);
  });
});

describe('calcEnduranceIndexPatterns', () => {
  // ガブリアスの種族値: HP=108, A=130, B=95, C=80, D=85, S=102
  // HP実数値: H32=108+32+75=215 / H0=108+0+75=183
  // B実数値:
  //   B32+補正(×1.1): floor((95+32+20)×1.1) = floor(147×1.1) = floor(161.7) = 161
  //   B0+補正なし:    (95+0+20)×1.0 = 115
  // D実数値:
  //   D32+補正(×1.1): floor((85+32+20)×1.1) = floor(137×1.1) = floor(150.7) = 150
  //   D0+補正なし:    (85+0+20)×1.0 = 105
  const garchompBaseStats = { hp: 108, atk: 130, def: 95, spa: 80, spd: 85, spe: 102 };

  it('ガブリアスの 4 パターン × 2 種類（物理・特殊）= 8 値を完全列挙', () => {
    const result = calcEnduranceIndexPatterns(garchompBaseStats);

    // 耐久特化: H32 × (B32+補正 / D32+補正)
    expect(result.specialized.physical).toBe(215 * 161); // 34615
    expect(result.specialized.special).toBe(215 * 150); // 32250

    // 耐久極振: H0 × (B32+補正 / D32+補正)
    expect(result.defOnly.physical).toBe(183 * 161); // 29463
    expect(result.defOnly.special).toBe(183 * 150); // 27450

    // H極振: H32 × (B0補正なし / D0補正なし)
    expect(result.hpOnly.physical).toBe(215 * 115); // 24725
    expect(result.hpOnly.special).toBe(215 * 105); // 22575

    // 無振り: H0 × (B0補正なし / D0補正なし)
    expect(result.none.physical).toBe(183 * 115); // 21045
    expect(result.none.special).toBe(183 * 105); // 19215
  });

  it('戻り値は 4 パターンキー（specialized / defOnly / hpOnly / none）を持つ', () => {
    const result = calcEnduranceIndexPatterns(garchompBaseStats);
    expect(Object.keys(result)).toEqual(['specialized', 'defOnly', 'hpOnly', 'none']);
  });

  it('各パターンは physical / special の 2 キーを持つ', () => {
    const result = calcEnduranceIndexPatterns(garchompBaseStats);
    for (const patternKey of ['specialized', 'defOnly', 'hpOnly', 'none']) {
      expect(Object.keys(result[patternKey])).toEqual(['physical', 'special']);
    }
  });

  it('種族値最小（HP=1, B=1, D=1）でも算出が破綻しない', () => {
    const minStats = { hp: 1, atk: 1, def: 1, spa: 1, spd: 1, spe: 1 };
    const result = calcEnduranceIndexPatterns(minStats);
    // hpMax=1+32+75=108, hpMin=1+0+75=76
    // defUp=floor((1+32+20)×1.1)=floor(58.3)=58, defNone=1+0+20=21
    expect(result.specialized.physical).toBe(108 * 58);
    expect(result.none.physical).toBe(76 * 21);
  });
});
