import { describe, it, expect } from 'vitest';
import { calcSpeedPatterns } from '../../src/logic/speed-calc.js';

describe('calcSpeedPatterns', () => {
  it('種族値90のポケモンで素早さ6パターンを正しく計算する', () => {
    // Given
    const baseSpe = 90;

    // When
    const result = calcSpeedPatterns(baseSpe);

    // Then
    // floor((90 + 能力ポイント + 20) × 性格補正)、スカーフはfloor(実数値 × 1.5)
    expect(result.fastestScarf).toBe(234); // floor(156 * 1.5)
    expect(result.fastScarf).toBe(213); // floor(142 * 1.5)
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
    expect(result.fastestScarf).toBe(Math.floor(Math.floor((1 + 32 + 20) * 1.1) * 1.5));
    expect(result.fastScarf).toBe(Math.floor((1 + 32 + 20) * 1.5));
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
    expect(result.fastestScarf).toBe(Math.floor(result.fastest * 1.5));
    expect(result.fastScarf).toBe(Math.floor((200 + 32 + 20) * 1.5));
    expect(result.neutral).toBe(220);
    expect(result.slowest).toBe(Math.floor(220 * 0.9));
  });

  it('スカーフは「ベース実数値floor」のあとに×1.5してさらにfloorを適用する', () => {
    // Given
    // 種族値85 で floor 順序の違いが顕在化する:
    //   最速ベース = floor((85+32+20)*1.1) = floor(150.7) = 150
    //   正しい順:   floor(150 * 1.5) = 225
    //   誤った順:   floor(150.7 * 1.5) = floor(226.05) = 226
    const baseSpe = 85;

    // When
    const result = calcSpeedPatterns(baseSpe);

    // Then
    expect(result.fastestScarf).toBe(225);
  });

  it('準速スカーフもベース実数値floor後に×1.5してfloorする', () => {
    // Given
    // 種族値91: fast = floor((91+32+20)*1.0) = 143、fastScarf = floor(143 * 1.5) = 214
    const baseSpe = 91;

    // When
    const result = calcSpeedPatterns(baseSpe);

    // Then
    expect(result.fastScarf).toBe(214);
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

  it('種族値0を渡すと RangeError を投げる', () => {
    // Given
    const baseSpe = 0;

    // When / Then
    expect(() => calcSpeedPatterns(baseSpe)).toThrow(RangeError);
  });

  it('負の種族値を渡すと RangeError を投げる', () => {
    // Given
    const baseSpe = -1;

    // When / Then
    expect(() => calcSpeedPatterns(baseSpe)).toThrow(RangeError);
  });

  it('NaN を渡すと RangeError を投げる', () => {
    // Given
    const baseSpe = NaN;

    // When / Then
    expect(() => calcSpeedPatterns(baseSpe)).toThrow(RangeError);
  });

  it('非整数 (小数) を渡すと RangeError を投げる', () => {
    // Given
    const baseSpe = 90.5;

    // When / Then
    expect(() => calcSpeedPatterns(baseSpe)).toThrow(RangeError);
  });

  it('文字列を渡すと RangeError を投げる', () => {
    // Given
    const baseSpe = '90';

    // When / Then
    expect(() => calcSpeedPatterns(baseSpe)).toThrow(RangeError);
  });
});
