// テスト用 party fixture 群。各テストは必要な fixture を import して mockParty に渡す。

/**
 * ガブリアス・いじっぱり・こだわりスカーフ・さめはだ（HP32 atk32、他 0）。
 * 物理火力指数の数値検証（AET-010）に使用。
 * じしんの火力指数 = 100 × atk実数値200 × STAB1.5 = 30000
 */
export const GARCHOMP_PHYSICAL = {
  party: [
    {
      species: 'ガブリアス',
      ability: 'さめはだ',
      item: 'こだわりスカーフ',
      nature: 'いじっぱり',
      abilityPoints: { hp: 32, atk: 32, def: 0, spa: 0, spd: 0, spe: 0 },
      moves: [
        { name: 'じしん' },
        { name: 'げきりん' },
        { name: 'みがわり' },
        { name: 'つるぎのまい' },
      ],
    },
  ],
};

/**
 * ガブリアス・いじっぱり・能力ポイント全 0・りゅうせいぐん習得。
 * 特殊火力指数（SpA 経路）の数値検証（AET-017）に使用。
 * spa 実数値 = floor((80+0+20)×0.9) = 90 / りゅうせいぐん火力指数 = 130 × 90 × STAB1.5 = 17550
 */
export const GARCHOMP_SPECIAL = {
  party: [
    {
      species: 'ガブリアス',
      ability: 'さめはだ',
      item: null,
      nature: 'いじっぱり',
      abilityPoints: { hp: 0, atk: 0, def: 0, spa: 0, spd: 0, spe: 0 },
      moves: [
        { name: 'りゅうせいぐん' },
        { name: 'じしん' },
        { name: 'みがわり' },
        { name: 'つるぎのまい' },
      ],
    },
  ],
};

/**
 * 補正あり性格と補正なし性格のペア（AET-008 用）。
 * 0: ガブリアス・いじっぱり（補正あり、atk↑/spa↓）
 * 1: ガブリアス・まじめ（補正なし）
 */
export const NATURE_MIX = {
  party: [
    {
      species: 'ガブリアス',
      ability: 'さめはだ',
      item: null,
      nature: 'いじっぱり',
      abilityPoints: { hp: 0, atk: 0, def: 0, spa: 0, spd: 0, spe: 0 },
      moves: [{ name: 'じしん' }, { name: 'げきりん' }, { name: 'みがわり' }, { name: 'まもる' }],
    },
    {
      species: 'ガブリアス',
      ability: 'さめはだ',
      item: null,
      nature: 'まじめ',
      abilityPoints: { hp: 0, atk: 0, def: 0, spa: 0, spd: 0, spe: 0 },
      moves: [{ name: 'じしん' }, { name: 'げきりん' }, { name: 'みがわり' }, { name: 'まもる' }],
    },
  ],
};

/**
 * てつのこぶし持ちポケモン（ゴウカザル）でパンチ技と非パンチ技の比較。
 * AET-016 で「マッハパンチに 1.2 倍補正、ばかぢからは補正なし」を検証。
 */
export const IRONFIST_FIXTURE = {
  party: [
    {
      species: 'ゴウカザル',
      ability: 'てつのこぶし',
      item: null,
      nature: 'まじめ',
      abilityPoints: { hp: 0, atk: 0, def: 0, spa: 0, spd: 0, spe: 0 },
      moves: [
        { name: 'マッハパンチ' },
        { name: 'ばかぢから' },
        { name: 'かえんほうしゃ' },
        { name: 'まもる' },
      ],
    },
  ],
};

/**
 * おやこあい補正（無条件 atk/spa 1.25 倍）を持つガブリアス。
 * AET-016b で「無条件 modifier の UI 結合担保」を検証。
 * ガブリアスは実機でおやこあいを持たないが、party.json は特性名で自由指定でき
 * `DataLoader.getAbilityModifier('おやこあい')` がマスターデータから補正値を返す挙動を検証する。
 * 計算: じしん威力100 × atk 実数値 200（いじっぱり HP32 atk32）× STAB 1.5 × おやこあい 1.25 = 37500
 */
export const PARENTAL_BOND_FIXTURE = {
  party: [
    {
      species: 'ガブリアス',
      ability: 'おやこあい',
      item: null,
      nature: 'いじっぱり',
      abilityPoints: { hp: 32, atk: 32, def: 0, spa: 0, spd: 0, spe: 0 },
      moves: [
        { name: 'じしん' },
        { name: 'げきりん' },
        { name: 'みがわり' },
        { name: 'まもる' },
      ],
    },
  ],
};

/**
 * 変化技・威力不定技・必中技を含む。
 * AET-011（変化技 −）/ AET-012（威力不定技 −）/ AET-013（必中技 −）で使用。
 */
export const MOVE_VARIANTS_FIXTURE = {
  party: [
    {
      species: 'ガブリアス',
      ability: 'さめはだ',
      item: null,
      nature: 'まじめ',
      abilityPoints: { hp: 0, atk: 0, def: 0, spa: 0, spd: 0, spe: 0 },
      moves: [
        { name: 'つるぎのまい' }, // 変化技（power/accuracy 共に null）
        { name: 'カウンター' }, // 威力不定技（power=null, accuracy=100）
        { name: 'つばめがえし' }, // 必中技（accuracy=null, power=60）
        { name: 'じしん' }, // 通常技
      ],
    },
  ],
};

/**
 * multihit 技（おうふくビンタ・最大威力 75）を含む。
 * AET-014 で威力欄に 75 が表示されることを検証。
 */
export const MULTIHIT_FIXTURE = {
  party: [
    {
      species: 'ピカチュウ',
      ability: 'せいでんき',
      item: null,
      nature: 'まじめ',
      abilityPoints: { hp: 0, atk: 0, def: 0, spa: 0, spd: 0, spe: 0 },
      moves: [
        { name: 'おうふくビンタ' },
        { name: 'でんきショック' },
        { name: 'みがわり' },
        { name: 'まもる' },
      ],
    },
  ],
};

/**
 * メガシンカ対応の動作確認用 fixture（機能 7・AET-039/040/042 用）。
 * 0: フシギバナ + フシギバナイト → メガ切替ボタン表示
 * 1: フシギバナ + カメックスナイト → ボタン非表示（持ち物不一致）
 * 2: フシギバナ + こだわりハチマキ → ボタン非表示（メガストーンでない）
 * 3: リザードン + リザードナイトＸ → メガ切替（複数メガを循環、通常→Ｘ→Ｙ→通常）
 * 4: ガブリアス + こだわりスカーフ → ボタン非表示（メガ不可ポケモン）
 * 5: リザードン + 持ち物なし（item: null） → ボタン非表示（メガストーン不一致）
 */
export const MEGA_FIXTURE = {
  party: [
    {
      species: 'フシギバナ',
      ability: 'しんりょく',
      item: 'フシギバナイト',
      nature: 'ひかえめ',
      abilityPoints: { hp: 0, atk: 0, def: 0, spa: 0, spd: 0, spe: 0 },
      moves: [{ name: 'ギガドレイン' }, { name: 'ヘドロばくだん' }, { name: 'まもる' }, { name: 'みがわり' }],
    },
    {
      species: 'フシギバナ',
      ability: 'しんりょく',
      item: 'カメックスナイト',
      nature: 'ひかえめ',
      abilityPoints: { hp: 0, atk: 0, def: 0, spa: 0, spd: 0, spe: 0 },
      moves: [{ name: 'ギガドレイン' }, { name: 'まもる' }, { name: 'みがわり' }, { name: 'たいあたり' }],
    },
    {
      species: 'フシギバナ',
      ability: 'しんりょく',
      item: 'こだわりハチマキ',
      nature: 'いじっぱり',
      abilityPoints: { hp: 0, atk: 0, def: 0, spa: 0, spd: 0, spe: 0 },
      moves: [{ name: 'たいあたり' }, { name: 'まもる' }, { name: 'みがわり' }, { name: 'ギガドレイン' }],
    },
    {
      species: 'リザードン',
      ability: 'もうか',
      item: 'リザードナイトＸ',
      nature: 'いじっぱり',
      abilityPoints: { hp: 0, atk: 0, def: 0, spa: 0, spd: 0, spe: 0 },
      moves: [{ name: 'ひのこ' }, { name: 'まもる' }, { name: 'みがわり' }, { name: 'たいあたり' }],
    },
    {
      species: 'ガブリアス',
      ability: 'さめはだ',
      item: 'こだわりスカーフ',
      nature: 'いじっぱり',
      abilityPoints: { hp: 0, atk: 0, def: 0, spa: 0, spd: 0, spe: 0 },
      moves: [{ name: 'じしん' }, { name: 'まもる' }, { name: 'みがわり' }, { name: 'げきりん' }],
    },
    {
      species: 'リザードン',
      ability: 'もうか',
      item: null,
      nature: 'まじめ',
      abilityPoints: { hp: 0, atk: 0, def: 0, spa: 0, spd: 0, spe: 0 },
      moves: [{ name: 'ひのこ' }, { name: 'まもる' }, { name: 'みがわり' }, { name: 'たいあたり' }],
    },
  ],
};

/**
 * 6 匹分の基本パーティ（AET-001/002/003/006/007/009/015 用の汎用 fixture）。
 */
export const STANDARD_PARTY = {
  party: [
    {
      species: 'ガブリアス',
      ability: 'さめはだ',
      item: 'こだわりスカーフ',
      nature: 'いじっぱり',
      abilityPoints: { hp: 32, atk: 32, def: 0, spa: 0, spd: 0, spe: 0 },
      moves: [{ name: 'じしん' }, { name: 'げきりん' }, { name: 'みがわり' }, { name: 'まもる' }],
    },
    {
      species: 'ピカチュウ',
      ability: 'せいでんき',
      item: 'いのちのたま',
      nature: 'おくびょう',
      abilityPoints: { hp: 0, atk: 0, def: 0, spa: 32, spd: 0, spe: 32 },
      moves: [
        { name: 'でんきショック' },
        { name: 'みがわり' },
        { name: 'まもる' },
        { name: 'おうふくビンタ' },
      ],
    },
    {
      species: 'フシギダネ',
      ability: 'しんりょく',
      item: null,
      nature: 'まじめ',
      abilityPoints: { hp: 0, atk: 0, def: 0, spa: 0, spd: 0, spe: 0 },
      moves: [{ name: 'たいあたり' }, { name: 'みがわり' }, { name: 'まもる' }, { name: 'やどりぎのタネ' }],
    },
    {
      species: 'ヒトカゲ',
      ability: 'もうか',
      item: null,
      nature: 'まじめ',
      abilityPoints: { hp: 0, atk: 0, def: 0, spa: 0, spd: 0, spe: 0 },
      moves: [{ name: 'ひのこ' }, { name: 'みがわり' }, { name: 'まもる' }, { name: 'ひっかく' }],
    },
    {
      species: 'ゼニガメ',
      ability: 'げきりゅう',
      item: null,
      nature: 'まじめ',
      abilityPoints: { hp: 0, atk: 0, def: 0, spa: 0, spd: 0, spe: 0 },
      moves: [{ name: 'みずでっぽう' }, { name: 'みがわり' }, { name: 'まもる' }, { name: 'たいあたり' }],
    },
    {
      species: 'ライチュウ',
      ability: 'せいでんき',
      item: null,
      nature: 'おくびょう',
      abilityPoints: { hp: 0, atk: 0, def: 0, spa: 32, spd: 0, spe: 32 },
      moves: [{ name: '１０まんボルト' }, { name: 'みがわり' }, { name: 'まもる' }, { name: 'でんきショック' }],
    },
  ],
};

/** 不明な species を 1 匹含むパーティ（AET-004 用）。 */
export const UNKNOWN_SPECIES_FIXTURE = {
  party: [
    {
      species: 'ガブリアス',
      ability: 'さめはだ',
      item: null,
      nature: 'まじめ',
      abilityPoints: { hp: 0, atk: 0, def: 0, spa: 0, spd: 0, spe: 0 },
      moves: [{ name: 'じしん' }, { name: 'みがわり' }, { name: 'まもる' }, { name: 'つるぎのまい' }],
    },
    {
      species: 'まぼろしポケモン',
      ability: 'さめはだ',
      item: null,
      nature: 'まじめ',
      abilityPoints: { hp: 0, atk: 0, def: 0, spa: 0, spd: 0, spe: 0 },
      moves: [{ name: 'たいあたり' }, { name: 'みがわり' }, { name: 'まもる' }, { name: 'つるぎのまい' }],
    },
    ...STANDARD_PARTY.party.slice(2),
  ],
};

/** 指定したフィールドを欠落させた party データを返す（AET-031〜034 用）。 */
export function partyMissingField(field) {
  const copy = JSON.parse(JSON.stringify(STANDARD_PARTY));
  delete copy.party[0][field];
  return copy;
}
