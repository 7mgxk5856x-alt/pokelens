import { describe, it, expect } from 'vitest';
import { calcSpeedPatterns } from '../../src/logic/speed-calc.js';

describe('calcSpeedPatterns()', () => {
  it('種族値90のポケモンで素早さ6パターンを正しく計算する', () => {
    expect(calcSpeedPatterns(90)).toEqual({
      fastestScarf: 234,
      fastScarf: 213,
      fastest: 156,
      fast: 142,
      neutral: 110,
      slowest: 99,
    });
  });

  it('種族値1（境界値・最小）でも計算が破綻しない', () => {
    expect(calcSpeedPatterns(1)).toEqual({
      fastestScarf: Math.floor(Math.floor((1 + 32 + 20) * 1.1) * 1.5),
      fastScarf: Math.floor((1 + 32 + 20) * 1.5),
      fastest: Math.floor((1 + 32 + 20) * 1.1),
      fast: 1 + 32 + 20,
      neutral: 1 + 0 + 20,
      slowest: Math.floor((1 + 0 + 20) * 0.9),
    });
  });

  it('種族値200（大きい値）でも計算が破綻しない', () => {
    const r = calcSpeedPatterns(200);
    expect(r.fastest).toBe(Math.floor((200 + 32 + 20) * 1.1));
    expect(r.fastestScarf).toBe(Math.floor(r.fastest * 1.5));
    expect(r.fastScarf).toBe(Math.floor((200 + 32 + 20) * 1.5));
    expect(r.neutral).toBe(220);
    expect(r.slowest).toBe(Math.floor(220 * 0.9));
  });

  it('スカーフは「ベース実数値floor」のあとに×1.5してさらにfloorを適用する', () => {
    // 種族値89: 最速ベース = floor((89+32+20)*1.1) = floor(155.1) = 155
    //          最速スカーフ = floor(155 * 1.5) = 232
    // もしfloor順を誤って先に×1.5すると floor(155.1*1.5)=232 で同じだが、
    // 別ケースで差が出ることを確認するために種族値85を使う:
    //   最速ベース = floor((85+32+20)*1.1) = floor(150.7) = 150
    //   正しい順:   floor(150 * 1.5) = 225
    //   誤った順:   floor(150.7 * 1.5) = floor(226.05) = 226
    expect(calcSpeedPatterns(85).fastestScarf).toBe(225);
  });

  it('準速スカーフもベース実数値floor後に×1.5してfloorする', () => {
    // 種族値91: fast = floor((91+32+20)*1.0) = 143
    //          fastScarf = floor(143 * 1.5) = 214
    expect(calcSpeedPatterns(91).fastScarf).toBe(214);
  });

  it('最遅は性格補正0.9の floor を適用する', () => {
    // 種族値100: slowest = floor((100+0+20)*0.9) = floor(108) = 108
    expect(calcSpeedPatterns(100).slowest).toBe(108);
  });
});
