using Xunit;

namespace PokelensCore.Tests;

/// <summary>NameSearch の正規化・検索ふるまいを担保するテスト群。<c>tests/unit/name-search.test.js</c> と等価のケースを移植する。</summary>
public class NameSearchTests
{
    // NSC-001: ひらがな → カタカナ
    [Fact]
    public void NSC_001_HiraganaToKatakana()
    {
        Assert.Equal("ガブリアス", NameSearch.NormalizeQuery("がぶりあす"));
    }

    // NSC-002: 半角カナ → 全角カナ（NFKC）
    [Fact]
    public void NSC_002_HalfwidthKatakanaToFullwidth()
    {
        Assert.Equal("ガブ", NameSearch.NormalizeQuery("ｶﾞﾌﾞ"));
    }

    // NSC-003: 全角英字 → ローマ字 → カタカナ
    [Fact]
    public void NSC_003_FullwidthAlphaToKatakana()
    {
        Assert.Equal("ガブ", NameSearch.NormalizeQuery("ｇａｂｕ"));
    }

    // NSC-004: ローマ字 → カタカナ（基本子音）
    [Fact]
    public void NSC_004_BasicRomajiToKatakana()
    {
        Assert.Equal("ガブ", NameSearch.NormalizeQuery("gabu"));
    }

    // NSC-005: ローマ字 → カタカナ（拗音 sh / ch / ts / th）
    [Theory]
    [InlineData("shi", "シ")]
    [InlineData("chi", "チ")]
    [InlineData("tsu", "ツ")]
    [InlineData("thi", "ティ")]
    public void NSC_005_RomajiDigraph(string input, string expected)
    {
        Assert.Equal(expected, NameSearch.NormalizeQuery(input));
    }

    // NSC-006: 促音（同子音連続）
    [Fact]
    public void NSC_006_GeminateRomaji()
    {
        Assert.Equal("ガッキ", NameSearch.NormalizeQuery("gakki"));
    }

    // NSC-007: 撥音 n
    [Theory]
    [InlineData("kan", "カン")]      // n + 文末
    [InlineData("kanji", "カンジ")]  // n + 子音
    [InlineData("nya", "ニャ")]      // ny は二重子音
    public void NSC_007_SyllabicN(string input, string expected)
    {
        Assert.Equal(expected, NameSearch.NormalizeQuery(input));
    }

    // NSC-008: nn は撥音として扱う（"konna" → "コンナ"）
    [Fact]
    public void NSC_008_DoubleN()
    {
        Assert.Equal("コンナ", NameSearch.NormalizeQuery("konna"));
    }

    // NSC-009: 半角ハイフン → 長音記号
    [Fact]
    public void NSC_009_HyphenToProlongedSoundMark()
    {
        Assert.Equal("リーフィア", NameSearch.NormalizeQuery("ri-fia"));
    }

    // NSC-010: 全角ハイフン → NFKC で半角に → 長音記号
    [Fact]
    public void NSC_010_FullwidthHyphenToProlongedSoundMark()
    {
        Assert.Equal("リー", NameSearch.NormalizeQuery("り－"));
    }

    // NSC-011: 空文字
    [Fact]
    public void NSC_011_EmptyQuery()
    {
        Assert.Equal(string.Empty, NameSearch.NormalizeQuery(string.Empty));
    }

    // NSC-012: 半端な子音は無視（"gab" → "ガ" まで確定、b は捨てる）
    [Fact]
    public void NSC_012_PartialConsonantIsDropped()
    {
        Assert.Equal("ガ", NameSearch.NormalizeQuery("gab"));
    }

    // NSC-013: SearchByName 前方一致 + 図鑑番号昇順
    [Fact]
    public void NSC_013_SearchByName_PrefixMatchSortedByNum()
    {
        var entries = new[]
        {
            new NameSearch.Entry("ガブリアス", 445),
            new NameSearch.Entry("ガラガラ", 105),
            new NameSearch.Entry("ピカチュウ", 25),
        };
        var result = NameSearch.SearchByName("ガ", entries);
        Assert.Equal(2, result.Count);
        Assert.Equal("ガラガラ", result[0].Name); // num=105 が先
        Assert.Equal("ガブリアス", result[1].Name);
    }

    // NSC-014: SearchByName 最大 10 件
    [Fact]
    public void NSC_014_SearchByName_MaxResults()
    {
        var entries = Enumerable.Range(1, 15).Select(i => new NameSearch.Entry("ガブ" + i, i)).ToArray();
        var result = NameSearch.SearchByName("ガブ", entries);
        Assert.Equal(10, result.Count);
    }

    // NSC-015: SearchByName 空クエリは空リスト
    [Fact]
    public void NSC_015_SearchByName_EmptyQuery()
    {
        var entries = new[] { new NameSearch.Entry("ガブリアス", 445) };
        Assert.Empty(NameSearch.SearchByName(string.Empty, entries));
    }

    // NSC-016: SearchByName 大文字小文字混在のローマ字でも一致する
    [Fact]
    public void NSC_016_SearchByName_RomajiCaseInsensitive()
    {
        var entries = new[] { new NameSearch.Entry("ガブリアス", 445) };
        var result = NameSearch.SearchByName("GaBu", entries);
        Assert.Single(result);
        Assert.Equal("ガブリアス", result[0].Name);
    }

    // NSC-017: SearchNames（名前のみ・図鑑番号なし）— 入力順を保つ
    [Fact]
    public void NSC_017_SearchNames_PreservesInputOrder()
    {
        var names = new[] { "あついしぼう", "ようりょくそ", "ようきき", "あめふらし" };
        var result = NameSearch.SearchNames("よう", names);
        Assert.Equal(2, result.Count);
        Assert.Equal("ようりょくそ", result[0]);
        Assert.Equal("ようきき", result[1]);
    }

    // NSC-018: SearchNames 最大 10 件で打ち切り
    [Fact]
    public void NSC_018_SearchNames_MaxResults()
    {
        var names = Enumerable.Range(1, 15).Select(i => "あ" + i).ToArray();
        var result = NameSearch.SearchNames("あ", names);
        Assert.Equal(10, result.Count);
    }

    // NSC-019: SearchNames 空クエリは空リスト
    [Fact]
    public void NSC_019_SearchNames_EmptyQuery()
    {
        var names = new[] { "ガブリアス" };
        Assert.Empty(NameSearch.SearchNames(string.Empty, names));
    }

    // NSC-020: NFKC + 長音 + ローマ字 + ひらカナの組み合わせが全て正しく動く（統合）
    [Fact]
    public void NSC_020_CombinedNormalization()
    {
        // "ｇａ-bu" → NFKC → "ga-bu" → 長音化 → "gaーbu" → ローマ字 → "ガーブ"
        Assert.Equal("ガーブ", NameSearch.NormalizeQuery("ｇａ-bu"));
    }
}
