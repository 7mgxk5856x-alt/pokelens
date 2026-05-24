import { describe, it, expect } from 'vitest';
import { calcSpeedPatterns } from '../../src/logic/speed-calc.js';

describe('calcSpeedPatterns', () => {
  it('種族値90のポケモンで素早さ4パターンを正しく計算する', () => {
    // Given
    const baseSpe = 90;

    // When
    const result = calcSpeedPatterns(baseSpe);

    // Then
    // floor((90 + 能力ポイント + 20) × 性格補正)
    expect(result.fastest).toBe(156); // floor((90+32+20)*1.1)
    expect(result.fast).toBe(142); // floor((90+32+20)*1.0)
    expect(result.neutral).toBe(110); // floor((90+0+20)*1.0)
    expect(result.slowest).toBe(99); // floor((90+0+20)*0.9)
  });

  it('種族値1（境界値・最小）でも計算が破綻しない', () => {
    // Given
    const baseSpe = 1;

    // When
    const result = calcSpeedPatterns(baseSpe);

    // Then
    expect(result.fastest).toBe(Math.floor((1 + 32 + 20) * 1.1));
    expect(result.fast).toBe(1 + 32 + 20);
    expect(result.neutral).toBe(1 + 0 + 20);
    expect(result.slowest).toBe(Math.floor((1 + 0 + 20) * 0.9));
  });

  it('種族値200（大きい値）でも計算が破綻しない', () => {
    // Given
    const baseSpe = 200;

    // When
    const result = calcSpeedPatterns(baseSpe);

    // Then
    expect(result.fastest).toBe(Math.floor((200 + 32 + 20) * 1.1));
    expect(result.fast).toBe(200 + 32 + 20);
    expect(result.neutral).toBe(220);
    expect(result.slowest).toBe(Math.floor(220 * 0.9));
  });

  it('最遅は性格補正0.9の floor を適用する', () => {
    // Given
    // 種族値100: slowest = floor((100+0+20)*0.9) = floor(108) = 108
    const baseSpe = 100;

    // When
    const result = calcSpeedPatterns(baseSpe);

    // Then
    expect(result.slowest).toBe(108);
  });

  it('スカーフ補正キー（fastestScarf/fastScarf）は戻り値に含まれない', () => {
    // 機能 15 で 6 パターン→ 4 パターンに簡素化。スカーフ補正は機能 16 で UI 側に集約済み。
    // Given
    const baseSpe = 90;

    // When
    const result = calcSpeedPatterns(baseSpe);

    // Then
    expect(result).not.toHaveProperty('fastestScarf');
    expect(result).not.toHaveProperty('fastScarf');
    expect(Object.keys(result)).toEqual(['fastest', 'fast', 'neutral', 'slowest']);
  });

  it('種族値0を渡すと RangeError を投げる', () => {
    // Given
    const baseSpe = 0;

    // When / Then
    expect(() => calcSpeedPatterns(baseSpe)).toThrow(RangeError);
    expect(() => calcSpeedPatterns(baseSpe)).toThrow(/baseSpe は正の整数/);
  });

  it('負の種族値を渡すと RangeError を投げる', () => {
    // Given
    const baseSpe = -1;

    // When / Then
    expect(() => calcSpeedPatterns(baseSpe)).toThrow(RangeError);
    expect(() => calcSpeedPatterns(baseSpe)).toThrow(/baseSpe は正の整数/);
  });

  it('NaN を渡すと RangeError を投げる', () => {
    // Given
    const baseSpe = NaN;

    // When / Then
    expect(() => calcSpeedPatterns(baseSpe)).toThrow(RangeError);
    expect(() => calcSpeedPatterns(baseSpe)).toThrow(/baseSpe は正の整数/);
  });

  it('非整数 (小数) を渡すと RangeError を投げる', () => {
    // Given
    const baseSpe = 90.5;

    // When / Then
    expect(() => calcSpeedPatterns(baseSpe)).toThrow(RangeError);
    expect(() => calcSpeedPatterns(baseSpe)).toThrow(/baseSpe は正の整数/);
  });

  it('文字列を渡すと RangeError を投げる', () => {
    // Given
    const baseSpe = '90';

    // When / Then
    expect(() => calcSpeedPatterns(baseSpe)).toThrow(RangeError);
    expect(() => calcSpeedPatterns(baseSpe)).toThrow(/baseSpe は正の整数/);
  });
});
