namespace PokelensMasterDataBuilder.Fetchers;

/// <summary>PokéAPI（pokeapi.co）レスポンス JSON のオブジェクトキー名。</summary>
/// <remarks>
/// 日本語名解決のために読み取る PokéAPI レスポンス（pokemon-species / pokemon-form / move / ability / item）の
/// キーを一元化する。同じキー名を複数箇所に直書きすると表記揺れ・キー名ドリフトの温床になるため定数にまとめる。
/// 言語コードの値（"ja" 等）はキーではなく比較対象の値なので、利用側の定数で扱う。
/// Showdown キャッシュのキーは別スキーマであり <see cref="ShowdownKey"/> 側で管理する。
/// </remarks>
internal static class PokeApiKey
{
    /// <summary>リソース名、およびローカライズ名エントリ（names / form_names 要素）の name。</summary>
    internal const string Name = "name";

    /// <summary>ローカライズ名エントリの言語リソース（入れ子に <see cref="Name"/> として "ja" 等を持つ）。</summary>
    internal const string Language = "language";

    /// <summary>日本語名を含むローカライズ名配列（species / move / ability / item レスポンス共通）。</summary>
    internal const string Names = "names";

    /// <summary>フォルム名のローカライズ配列（pokemon-form レスポンス）。</summary>
    internal const string FormNames = "form_names";

    /// <summary>pokemon-species レスポンス固有のキー。</summary>
    /// <remarks>varieties はフォルム違いの一覧で、各要素が <see cref="Pokemon"/> リソース（その <see cref="Name"/> が slug）を持つ。</remarks>
    internal static class Species
    {
        internal const string Varieties = "varieties";
        internal const string Pokemon = "pokemon";
    }
}
