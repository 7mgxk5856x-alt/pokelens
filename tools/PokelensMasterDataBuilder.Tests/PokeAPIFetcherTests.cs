using System.Net;
using PokelensMasterDataBuilder.Fetchers;
using Xunit;

namespace PokelensMasterDataBuilder.Tests;

public class PokeAPIFetcherTests
{
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
