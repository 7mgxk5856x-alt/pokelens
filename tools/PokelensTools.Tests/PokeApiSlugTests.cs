using PokelensTools;
using Xunit;

namespace PokelensTools.Tests;

public class PokeApiSlugTests
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
    public void PokemonFormSlug_HandlesCommonNames(string showdownName, string expected)
    {
        Assert.Equal(expected, PokeApiSlug.PokemonFormSlug(showdownName));
    }

    [Theory]
    [InlineData("Choice Scarf", "choice-scarf")]
    [InlineData("Life Orb", "life-orb")]
    [InlineData("Wellspring Mask", "wellspring-mask")]
    [InlineData("Ice Stone", "ice-stone")]
    [InlineData("Auspicious Armor", "auspicious-armor")]
    [InlineData("Metal Alloy", "metal-alloy")]
    public void ItemSlug_HandlesCommonNames(string showdownName, string expected)
    {
        Assert.Equal(expected, PokeApiSlug.ItemSlug(showdownName));
    }
}
