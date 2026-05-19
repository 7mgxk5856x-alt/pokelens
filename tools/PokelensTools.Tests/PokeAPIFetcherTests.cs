using System.Net;
using System.Text.Json.Nodes;
using PokelensTools;
using Xunit;

namespace PokelensTools.Tests;

public class PokeAPIFetcherTests
{
    [Theory]
    [InlineData("Bulbasaur", "bulbasaur")]
    [InlineData("Rotom-Wash", "rotom-wash")]
    [InlineData("Necrozma-Dusk-Mane", "necrozma-dusk-mane")]
    [InlineData("Tapu Koko", "tapu-koko")]
    [InlineData("Mr. Mime", "mr-mime")]
    [InlineData("Mime Jr.", "mime-jr")]
    [InlineData("Type: Null", "type-null")]
    [InlineData("Farfetch'd", "farfetchd")]
    [InlineData("Farfetch’d-Galar", "farfetchd-galar")]
    [InlineData("Zygarde-10%", "zygarde-10")]
    [InlineData("Flabébé", "flabebe")]
    [InlineData("Venusaur-Mega", "venusaur-mega")]
    [InlineData("Urshifu-Rapid-Strike", "urshifu-rapid-strike")]
    public void DerivePokemonFormSlug_HandlesCommonNames(string showdownName, string expected)
    {
        Assert.Equal(expected, PokeAPIFetcher.DerivePokemonFormSlug(showdownName));
    }

    [Theory]
    [InlineData("Choice Scarf", "choice-scarf")]
    [InlineData("Life Orb", "life-orb")]
    [InlineData("Wellspring Mask", "wellspring-mask")]
    [InlineData("Ice Stone", "ice-stone")]
    [InlineData("Auspicious Armor", "auspicious-armor")]
    [InlineData("Metal Alloy", "metal-alloy")]
    public void DeriveItemSlug_HandlesCommonNames(string showdownName, string expected)
    {
        Assert.Equal(expected, PokeAPIFetcher.DeriveItemSlug(showdownName));
    }

    // ---------- FindMatchingVariety ----------

    [Fact]
    public void FindMatchingVariety_ExactMatch_ReturnsName()
    {
        var species = JsonNode.Parse("""
            {"varieties": [
              {"pokemon": {"name": "rotom"}},
              {"pokemon": {"name": "rotom-wash"}}
            ]}
            """)!;
        Assert.Equal("rotom-wash", PokeAPIFetcher.FindMatchingVariety(species, "rotom-wash"));
    }

    [Fact]
    public void FindMatchingVariety_LongestPrefixMatch_ReturnsLongest()
    {
        // Showdown "ogerpon-wellspring" → PokéAPI variety "ogerpon-wellspring-mask" (最長一致)
        var species = JsonNode.Parse("""
            {"varieties": [
              {"pokemon": {"name": "ogerpon"}},
              {"pokemon": {"name": "ogerpon-wellspring-mask"}}
            ]}
            """)!;
        Assert.Equal("ogerpon-wellspring-mask",
            PokeAPIFetcher.FindMatchingVariety(species, "ogerpon-wellspring"));
    }

    [Fact]
    public void FindMatchingVariety_NoMatch_ReturnsNull()
    {
        var species = JsonNode.Parse("""
            {"varieties": [{"pokemon": {"name": "pikachu"}}]}
            """)!;
        Assert.Null(PokeAPIFetcher.FindMatchingVariety(species, "raichu"));
    }

    [Fact]
    public void FindMatchingVariety_NoVarietiesField_ReturnsNull()
    {
        var species = JsonNode.Parse("""{"name": "test"}""")!;
        Assert.Null(PokeAPIFetcher.FindMatchingVariety(species, "anything"));
    }

    // ---------- ExtractJaName ----------

    [Fact]
    public void ExtractJaName_JaPresent_PrefersJaOverJaHrkt()
    {
        var root = JsonNode.Parse("""
            {"names": [
              {"language": {"name": "en"}, "name": "Pikachu"},
              {"language": {"name": "ja-Hrkt"}, "name": "ピカチュウ"},
              {"language": {"name": "ja"}, "name": "ピカチュウ漢字版"}
            ]}
            """)!;
        Assert.Equal("ピカチュウ漢字版", PokeAPIFetcher.ExtractJaName(root, "names"));
    }

    [Fact]
    public void ExtractJaName_OnlyJaHrkt_ReturnsJaHrkt()
    {
        var root = JsonNode.Parse("""
            {"names": [
              {"language": {"name": "en"}, "name": "Pikachu"},
              {"language": {"name": "ja-Hrkt"}, "name": "ピカチュウ"}
            ]}
            """)!;
        Assert.Equal("ピカチュウ", PokeAPIFetcher.ExtractJaName(root, "names"));
    }

    [Fact]
    public void ExtractJaName_NoJapanese_ReturnsNull()
    {
        var root = JsonNode.Parse("""
            {"names": [{"language": {"name": "en"}, "name": "Pikachu"}]}
            """)!;
        Assert.Null(PokeAPIFetcher.ExtractJaName(root, "names"));
    }

    [Fact]
    public void ExtractJaName_ArrayKeyMissing_ReturnsNull()
    {
        var root = JsonNode.Parse("""{"name": "test"}""")!;
        Assert.Null(PokeAPIFetcher.ExtractJaName(root, "names"));
    }

    [Fact]
    public void ExtractJaName_FormNamesArrayKey_Works()
    {
        // pokemon-form エンドポイントは "form_names" 配列を使う
        var root = JsonNode.Parse("""
            {"form_names": [{"language": {"name": "ja"}, "name": "ウォッシュロトム"}]}
            """)!;
        Assert.Equal("ウォッシュロトム", PokeAPIFetcher.ExtractJaName(root, "form_names"));
    }

    // ---------- FetchJapaneseNameAsync (HTTP モック経由) ----------

    [Fact]
    public async Task FetchJapaneseNameAsync_OkWithJaName_ReturnsJapanese()
    {
        var fetcher = MakeFetcher((req, _) => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""
                {"names": [
                  {"language": {"name": "en"}, "name": "Pikachu"},
                  {"language": {"name": "ja"}, "name": "ピカチュウ"}
                ]}
                """),
        });
        Assert.Equal("ピカチュウ", await fetcher.FetchJapaneseNameAsync("https://example.com/api"));
    }

    [Fact]
    public async Task FetchJapaneseNameAsync_NotFound_ReturnsNull()
    {
        var fetcher = MakeFetcher((_, __) => new HttpResponseMessage(HttpStatusCode.NotFound));
        Assert.Null(await fetcher.FetchJapaneseNameAsync("https://example.com/api"));
    }

    [Fact]
    public async Task FetchJapaneseNameAsync_BadRequest_ReturnsNull()
    {
        var fetcher = MakeFetcher((_, __) => new HttpResponseMessage(HttpStatusCode.BadRequest));
        Assert.Null(await fetcher.FetchJapaneseNameAsync("https://example.com/api"));
    }

    [Fact]
    public async Task FetchJapaneseNameAsync_MalformedJson_ThrowsJsonException()
    {
        var fetcher = MakeFetcher((_, __) => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("not json"),
        });
        // 現状の実装はパース失敗を握り潰さず例外伝播させる (JsonException 系)。
        await Assert.ThrowsAnyAsync<System.Text.Json.JsonException>(
            () => fetcher.FetchJapaneseNameAsync("https://example.com/api"));
    }

    [Fact]
    public async Task FetchJapaneseNameAsync_NoJapaneseInNames_ReturnsNull()
    {
        var fetcher = MakeFetcher((_, __) => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""
                {"names": [{"language": {"name": "en"}, "name": "Pikachu"}]}
                """),
        });
        Assert.Null(await fetcher.FetchJapaneseNameAsync("https://example.com/api"));
    }

    private static PokeAPIFetcher MakeFetcher(
        Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> responder)
    {
        var http = new HttpClient(new FakeHttpMessageHandler(responder));
        return new PokeAPIFetcher(http);
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> _responder;

        public FakeHttpMessageHandler(
            Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_responder(request, cancellationToken));
        }
    }
}
