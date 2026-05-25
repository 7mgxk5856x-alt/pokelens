using PokelensMasterDataBuilder.Fetchers;
using Xunit;

namespace PokelensMasterDataBuilder.Tests;

public class ShowdownInternalKeyTests
{
    // メガシンカ対象は items.ts の megaStone フィールドを真実源として網羅的に検証する（D-7）。
    // メガシンカ可能なポケモンは現状約 50 種（X/Y を含めると約 60 メガ形態）と有限のため、
    // すべての (Showdown 表示名 → pokedex.json 内部キー) ペアを Theory で列挙する。
    // 命名規則外のメガ（将来追加される可能性）と pokedex.json と items.ts 間のキー drift を即時検知する。
    [Theory]
    // Gen 6 / Gen 7 全メガ形態（メガストーンが items.ts に登録されているもの）
    [InlineData("Venusaur-Mega", "venusaurmega")]
    [InlineData("Charizard-Mega-X", "charizardmegax")]
    [InlineData("Charizard-Mega-Y", "charizardmegay")]
    [InlineData("Blastoise-Mega", "blastoisemega")]
    [InlineData("Beedrill-Mega", "beedrillmega")]
    [InlineData("Pidgeot-Mega", "pidgeotmega")]
    [InlineData("Raichu-Mega-X", "raichumegax")]
    [InlineData("Raichu-Mega-Y", "raichumegay")]
    [InlineData("Clefable-Mega", "clefablemega")]
    [InlineData("Alakazam-Mega", "alakazammega")]
    [InlineData("Slowbro-Mega", "slowbromega")]
    [InlineData("Gengar-Mega", "gengarmega")]
    [InlineData("Kangaskhan-Mega", "kangaskhanmega")]
    [InlineData("Pinsir-Mega", "pinsirmega")]
    [InlineData("Gyarados-Mega", "gyaradosmega")]
    [InlineData("Aerodactyl-Mega", "aerodactylmega")]
    [InlineData("Mewtwo-Mega-X", "mewtwomegax")]
    [InlineData("Mewtwo-Mega-Y", "mewtwomegay")]
    [InlineData("Ampharos-Mega", "ampharosmega")]
    [InlineData("Steelix-Mega", "steelixmega")]
    [InlineData("Scizor-Mega", "scizormega")]
    [InlineData("Heracross-Mega", "heracrossmega")]
    [InlineData("Houndoom-Mega", "houndoommega")]
    [InlineData("Tyranitar-Mega", "tyranitarmega")]
    [InlineData("Sceptile-Mega", "sceptilemega")]
    [InlineData("Blaziken-Mega", "blazikenmega")]
    [InlineData("Swampert-Mega", "swampertmega")]
    [InlineData("Gardevoir-Mega", "gardevoirmega")]
    [InlineData("Sableye-Mega", "sableyemega")]
    [InlineData("Mawile-Mega", "mawilemega")]
    [InlineData("Aggron-Mega", "aggronmega")]
    [InlineData("Medicham-Mega", "medichammega")]
    [InlineData("Manectric-Mega", "manectricmega")]
    [InlineData("Sharpedo-Mega", "sharpedomega")]
    [InlineData("Camerupt-Mega", "cameruptmega")]
    [InlineData("Altaria-Mega", "altariamega")]
    [InlineData("Banette-Mega", "banettemega")]
    [InlineData("Absol-Mega", "absolmega")]
    [InlineData("Glalie-Mega", "glaliemega")]
    [InlineData("Salamence-Mega", "salamencemega")]
    [InlineData("Metagross-Mega", "metagrossmega")]
    [InlineData("Latias-Mega", "latiasmega")]
    [InlineData("Latios-Mega", "latiosmega")]
    [InlineData("Lopunny-Mega", "lopunnymega")]
    [InlineData("Garchomp-Mega", "garchompmega")]
    [InlineData("Lucario-Mega", "lucariomega")]
    [InlineData("Abomasnow-Mega", "abomasnowmega")]
    [InlineData("Gallade-Mega", "gallademega")]
    [InlineData("Audino-Mega", "audinomega")]
    [InlineData("Diancie-Mega", "dianciemega")]
    public void ForPokemon_ConvertsShowdownNameToInternalKey(string showdownName, string expected)
    {
        Assert.Equal(expected, ShowdownInternalKey.ForPokemon(showdownName));
    }

    [Theory]
    [InlineData("Bulbasaur", "bulbasaur")]
    [InlineData("Rotom-Wash", "rotomwash")]
    [InlineData("Necrozma-Dusk-Mane", "necrozmaduskmane")]
    [InlineData("Tapu Koko", "tapukoko")]
    [InlineData("Type: Null", "typenull")]
    [InlineData("Mr. Mime", "mrmime")]
    [InlineData("Farfetch'd", "farfetchd")]
    [InlineData("Flabébé", "flabebe")]
    [InlineData("Urshifu-Rapid-Strike", "urshifurapidstrike")]
    public void ForPokemon_HandlesNonMegaSpecialNames(string showdownName, string expected)
    {
        Assert.Equal(expected, ShowdownInternalKey.ForPokemon(showdownName));
    }

    [Fact]
    public void ForPokemon_EmptyString_ReturnsEmpty()
    {
        // 境界値: 空文字を渡しても TypeError を起こさず空文字を返すことを担保する。
        // items.ts の megaStone マップが空文字を含むケース等の防御。
        Assert.Equal(string.Empty, ShowdownInternalKey.ForPokemon(string.Empty));
    }
}
