using System.Text.Json.Nodes;
using PokelensMasterDataBuilder.Fetchers;
using Xunit;

namespace PokelensMasterDataBuilder.Tests;

public class PokeApiNameTests
{
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
        Assert.Equal("rotom-wash", PokeApiName.FindMatchingVariety(species, "rotom-wash"));
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
            PokeApiName.FindMatchingVariety(species, "ogerpon-wellspring"));
    }

    [Fact]
    public void FindMatchingVariety_NoMatch_ReturnsNull()
    {
        var species = JsonNode.Parse("""
            {"varieties": [{"pokemon": {"name": "pikachu"}}]}
            """)!;
        Assert.Null(PokeApiName.FindMatchingVariety(species, "raichu"));
    }

    [Fact]
    public void FindMatchingVariety_NoVarietiesField_ReturnsNull()
    {
        var species = JsonNode.Parse("""{"name": "test"}""")!;
        Assert.Null(PokeApiName.FindMatchingVariety(species, "anything"));
    }

    // ---------- ExtractJa ----------

    [Fact]
    public void ExtractJa_JaPresent_PrefersJaOverJaHrkt()
    {
        var root = JsonNode.Parse("""
            {"names": [
              {"language": {"name": "en"}, "name": "Pikachu"},
              {"language": {"name": "ja-Hrkt"}, "name": "ピカチュウ"},
              {"language": {"name": "ja"}, "name": "ピカチュウ漢字版"}
            ]}
            """)!;
        Assert.Equal("ピカチュウ漢字版", PokeApiName.ExtractJa(root, "names"));
    }

    [Fact]
    public void ExtractJa_OnlyJaHrkt_ReturnsJaHrkt()
    {
        var root = JsonNode.Parse("""
            {"names": [
              {"language": {"name": "en"}, "name": "Pikachu"},
              {"language": {"name": "ja-Hrkt"}, "name": "ピカチュウ"}
            ]}
            """)!;
        Assert.Equal("ピカチュウ", PokeApiName.ExtractJa(root, "names"));
    }

    [Fact]
    public void ExtractJa_NoJapanese_ReturnsNull()
    {
        var root = JsonNode.Parse("""
            {"names": [{"language": {"name": "en"}, "name": "Pikachu"}]}
            """)!;
        Assert.Null(PokeApiName.ExtractJa(root, "names"));
    }

    [Fact]
    public void ExtractJa_ArrayKeyMissing_ReturnsNull()
    {
        var root = JsonNode.Parse("""{"name": "test"}""")!;
        Assert.Null(PokeApiName.ExtractJa(root, "names"));
    }

    [Fact]
    public void ExtractJa_FormNamesArrayKey_Works()
    {
        // pokemon-form エンドポイントは "form_names" 配列を使う
        var root = JsonNode.Parse("""
            {"form_names": [{"language": {"name": "ja"}, "name": "ウォッシュロトム"}]}
            """)!;
        Assert.Equal("ウォッシュロトム", PokeApiName.ExtractJa(root, "form_names"));
    }
}
