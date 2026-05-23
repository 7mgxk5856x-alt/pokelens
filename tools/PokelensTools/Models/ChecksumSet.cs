namespace PokelensTools.Models;

/// <summary>パイプラインの各入力ファイルのチェックサム（ハッシュ）を、ソースごとの名前付きプロパティで保持する。</summary>
/// <remarks>
/// マジック文字列キーの Dictionary に代えて型で表すことで、前回値と今回値が同じ項目集合を持つことをコンパイル時に保証する。
/// <see cref="PokelensTools.Pipeline.IncrementalRunner.DetermineSteps"/> の差分判定と checksums.json への保存／読み込みに用いる。
/// </remarks>
/// <param name="ShowdownPokedex">Showdown ポケデックスキャッシュのハッシュ。</param>
/// <param name="ShowdownMoves">Showdown 技キャッシュのハッシュ。</param>
/// <param name="ShowdownItems">Showdown アイテムキャッシュのハッシュ。</param>
/// <param name="ShowdownAbilities">Showdown 特性キャッシュのハッシュ。</param>
/// <param name="PokeApiTranslations">PokéAPI 翻訳辞書（Step2 産物）のハッシュ。</param>
/// <param name="ChampionsPatch">champions-patch.json（Step3 トリガ）のハッシュ。</param>
/// <param name="MovesPowerPatch">moves-power-patch.json（Step4 専用）のハッシュ。</param>
/// <param name="ItemsModifiers">items-modifiers.json（Step4 専用）のハッシュ。</param>
/// <param name="AbilitiesModifiers">abilities-modifiers.json（Step4 専用）のハッシュ。</param>
/// <param name="PokemonNamePatch">pokemon-name-patch.json（Step4 専用）のハッシュ。</param>
/// <param name="ItemNamePatch">item-name-patch.json（Step4 専用）のハッシュ。</param>
internal sealed record ChecksumSet(
    string ShowdownPokedex,
    string ShowdownMoves,
    string ShowdownItems,
    string ShowdownAbilities,
    string PokeApiTranslations,
    string ChampionsPatch,
    string MovesPowerPatch,
    string ItemsModifiers,
    string AbilitiesModifiers,
    string PokemonNamePatch,
    string ItemNamePatch);
