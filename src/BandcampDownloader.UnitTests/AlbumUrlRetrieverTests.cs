using BandcampDownloader.Bandcamp.Download;
using BandcampDownloader.Bandcamp.Extraction;
using BandcampDownloader.Net;
using Moq;

namespace BandcampDownloader.UnitTests;

public sealed class AlbumUrlRetrieverTests
{
    private AlbumUrlRetriever _sut;
    private Mock<IDiscographyService> _discographyService;
    private Mock<IHttpService> _httpService;

    [SetUp]
    public void Setup()
    {
        _discographyService = new Mock<IDiscographyService>();
        _httpService = new Mock<IHttpService>();
        _sut = new AlbumUrlRetriever(_discographyService.Object, _httpService.Object);
    }

    [Test]
    public async Task RetrieveAlbumsUrlsAsync_WithWindowsLineEndings_ReturnsSeparateUrls()
    {
        // Arrange
        var input = "https://artist1.bandcamp.com/album/a\r\nhttps://artist2.bandcamp.com/album/b";

        // Act
        var result = await _sut.RetrieveAlbumsUrlsAsync(input, downloadArtistDiscography: false, CancellationToken.None);

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result, Contains.Item("https://artist1.bandcamp.com/album/a"));
        Assert.That(result, Contains.Item("https://artist2.bandcamp.com/album/b"));
    }

    [Test]
    public async Task RetrieveAlbumsUrlsAsync_WithUnixLineEndings_ReturnsSeparateUrls()
    {
        // Arrange — this is the bug case: clipboard from browser/script uses \n only
        var input = "https://artist1.bandcamp.com/album/a\nhttps://artist2.bandcamp.com/album/b";

        // Act
        var result = await _sut.RetrieveAlbumsUrlsAsync(input, downloadArtistDiscography: false, CancellationToken.None);

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result, Contains.Item("https://artist1.bandcamp.com/album/a"));
        Assert.That(result, Contains.Item("https://artist2.bandcamp.com/album/b"));
    }

    [Test]
    public async Task RetrieveAlbumsUrlsAsync_WithBlankLinesBetweenUrls_SkipsBlanks()
    {
        // Arrange — double newline (blank line separator) is also common from clipboard tools
        var input = "https://artist1.bandcamp.com/album/a\n\nhttps://artist2.bandcamp.com/album/b\n\nhttps://artist3.bandcamp.com/album/c";

        // Act
        var result = await _sut.RetrieveAlbumsUrlsAsync(input, downloadArtistDiscography: false, CancellationToken.None);

        // Assert
        Assert.That(result, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task RetrieveAlbumsUrlsAsync_WithDuplicateUrls_ReturnsDistinctUrls()
    {
        // Arrange
        var input = "https://artist1.bandcamp.com/album/a\nhttps://artist1.bandcamp.com/album/a";

        // Act
        var result = await _sut.RetrieveAlbumsUrlsAsync(input, downloadArtistDiscography: false, CancellationToken.None);

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task RetrieveAlbumsUrlsAsync_WithLeadingAndTrailingWhitespace_TrimsUrls()
    {
        // Arrange
        var input = "  https://artist1.bandcamp.com/album/a  \n  https://artist2.bandcamp.com/album/b  ";

        // Act
        var result = await _sut.RetrieveAlbumsUrlsAsync(input, downloadArtistDiscography: false, CancellationToken.None);

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result, Contains.Item("https://artist1.bandcamp.com/album/a"));
        Assert.That(result, Contains.Item("https://artist2.bandcamp.com/album/b"));
    }

    [Test]
    public async Task RetrieveAlbumsUrlsAsync_WithLargeNumberOfUrls_ParsesAllUrls()
    {
        // Arrange — regression test for the original 56-URL failure
        var urls = Enumerable.Range(1, 56)
            .Select(i => $"https://artist{i}.bandcamp.com/album/release")
            .ToList();
        var input = string.Join("\n\n", urls); // double-newline separated, Unix endings

        // Act
        var result = await _sut.RetrieveAlbumsUrlsAsync(input, downloadArtistDiscography: false, CancellationToken.None);

        // Assert
        Assert.That(result, Has.Count.EqualTo(56));
    }
}
