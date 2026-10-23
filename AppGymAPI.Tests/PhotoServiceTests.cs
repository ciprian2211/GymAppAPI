using System.Text;
using AppGymAPI.Services;
using AppGymAPI.Settings;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace AppGymAPI.Tests;

public class PhotoServiceTests
{
    private readonly PhotoService _photoService;

    public PhotoServiceTests()
    {
        var options = Options.Create(new CloudinarySettings
        {
            CloudName = "test-cloud",
            ApiKey = "test-api-key",
            ApiSecret = "test-api-secret"
        });

        _photoService = new PhotoService(options);
    }

    private static IFormFile CreateFormFile(long length, string contentType, string fileName = "test.jpg")
    {
        var content = length > 0 ? new byte[length] : Array.Empty<byte>();
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    [Fact]
    public async Task AddPhotoAsync_WhenFileIsEmpty_ReturnsNull()
    {
        var file = CreateFormFile(0, "image/jpeg");

        var result = await _photoService.AddPhotoAsync(file);

        result.Should().BeNull();
    }

    [Theory]
    [InlineData("application/pdf")]
    [InlineData("text/plain")]
    [InlineData("video/mp4")]
    [InlineData("image/gif")]
    [InlineData("image/webp")]
    public async Task AddPhotoAsync_WhenContentTypeNotAllowed_ReturnsNull(string invalidContentType)
    {
        var file = CreateFormFile(1024, invalidContentType);

        var result = await _photoService.AddPhotoAsync(file);

        result.Should().BeNull();
    }

    [Fact]
    public async Task AddPhotoAsync_WhenFileSizeExceeds10MB_ReturnsNull()
    {
        long elevenMb = 11L * 1024 * 1024;
        var file = CreateFormFile(elevenMb, "image/jpeg");

        var result = await _photoService.AddPhotoAsync(file);

        result.Should().BeNull();
    }

    [Fact]
    public async Task AddGifAsync_WhenFileIsEmpty_ReturnsNull()
    {
        var file = CreateFormFile(0, "image/gif", "test.gif");

        var result = await _photoService.AddGifAsync(file);

        result.Should().BeNull();
    }

    [Theory]
    [InlineData("application/pdf")]
    [InlineData("text/plain")]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("video/mp4")]
    public async Task AddGifAsync_WhenContentTypeNotAllowed_ReturnsNull(string invalidContentType)
    {
        var file = CreateFormFile(1024, invalidContentType, "test.gif");

        var result = await _photoService.AddGifAsync(file);

        result.Should().BeNull();
    }

    [Fact]
    public async Task AddGifAsync_WhenFileSizeExceeds15MB_ReturnsNull()
    {
        long sixteenMb = 16L * 1024 * 1024;
        var file = CreateFormFile(sixteenMb, "image/gif", "test.gif");

        var result = await _photoService.AddGifAsync(file);

        result.Should().BeNull();
    }
}
