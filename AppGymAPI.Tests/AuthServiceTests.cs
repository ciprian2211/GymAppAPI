using System.Text;
using AppGymAPI.DTOs;
using AppGymAPI.Models;
using AppGymAPI.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace AppGymAPI.Tests;

public class AuthServiceTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly Mock<IPhotoService> _photoServiceMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        var storeMock = new Mock<IUserStore<User>>();

        _userManagerMock = new Mock<UserManager<User>>(storeMock.Object,
            null!, null!, null!, null!, null!, null!, null!, null!);

        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<AuthService>>();
        _photoServiceMock = new Mock<IPhotoService>();

        _configurationMock
            .Setup(c => c["JwtSettings:SecretKey"])
            .Returns("ThisIsMySuperSecretKeyForTestingPurposesOnly123456!");

        _configurationMock
            .Setup(c => c["JwtSettings:Issuer"]).Returns("TestIssuer");

        _configurationMock
            .Setup(c => c["JwtSettings:Audience"]).Returns("TestAudience");

        _authService = new AuthService(_userManagerMock.Object, _configurationMock.Object, _loggerMock.Object, _photoServiceMock.Object);
    }

    private static IFormFile CreateTestFormFile(string fileName = "avatar.png", string contentType = "image/png", byte[]? bytes = null)
    {
        bytes ??= Encoding.UTF8.GetBytes("fake image data");
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "ProfileImage", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    [Fact]
    public async Task LoginUserAsync_WithValidCredentials_ReturnsAuthResponseDTO()
    {
        var loginUserDto = new LoginUserDto
        {
            Email = "test@gmail.com",
            Password = "Password123!"
        };

        var fakeUser = new User
        {
            Id = Guid.NewGuid(),
            Email = loginUserDto.Email,
        };

        _userManagerMock
            .Setup(mgr => mgr.FindByEmailAsync(loginUserDto.Email))
            .ReturnsAsync(fakeUser);
        _userManagerMock
            .Setup(mgr => mgr.CheckPasswordAsync(fakeUser, loginUserDto.Password))
            .ReturnsAsync(true);

        var result = await _authService.LoginUserAsync(loginUserDto);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task LoginUserAsync_WithInvalidEmail_ReturnsNull()
    {
        var loginDto = new LoginUserDto
        {
            Email = "salut123",
            Password = "123Boss!"
        };
        _userManagerMock
            .Setup(mgr => mgr.FindByEmailAsync(loginDto.Email))
            .ReturnsAsync((User?)null);

        var result = await _authService.LoginUserAsync(loginDto);

        Assert.Null(result);
    }

    [Fact]
    public async Task LoginUserAsync_WithInvalidPassword_ReturnsNull()
    {
        var loginDto = new LoginUserDto
        {
            Email = "test@gmail.com",
            Password = "123Boss!"
        };
        var fakeUser = new User
        {
            Id = Guid.NewGuid(),
            Email = loginDto.Email
        };

        _userManagerMock
            .Setup(mgr => mgr.FindByEmailAsync(loginDto.Email))
            .ReturnsAsync(fakeUser);
        _userManagerMock
            .Setup(mgr => mgr.CheckPasswordAsync(fakeUser, loginDto.Password))
            .ReturnsAsync(false);

        var result = await _authService.LoginUserAsync(loginDto);

        Assert.Null(result);
    }

    [Fact]
    public async Task RegisterUserAsync_WithValidUserDetails_ReturnsTrue()
    {
        var registerDto = new RegisterUserDto
        {
            Name = "Valid name",
            Surname = "Valid surname",
            Email = "test@gmail.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        User? capturedUser = null;
        _userManagerMock
            .Setup(mgr => mgr.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .Callback<User, string>((u, _) => capturedUser = u)
            .ReturnsAsync(IdentityResult.Success);

        var result = await _authService.RegisterUserAsync(registerDto);

        Assert.True(result);
        capturedUser.Should().NotBeNull();
        capturedUser!.ImageUrl.Should().Be("https://res.cloudinary.com/n7iduzxq/image/upload/v1792748390/user-profile.png");
    }

    [Fact]
    public async Task RegisterUserAsync_WithProfileImage_UploadsPhotoAndSavesUrl()
    {
        var file = CreateTestFormFile();
        var registerDto = new RegisterUserDto
        {
            Name = "Valid name",
            Surname = "Valid surname",
            Email = "test@gmail.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            ProfileImage = file
        };

        const string uploadedUrl = "https://res.cloudinary.com/test/image/upload/custom-avatar.png";
        _photoServiceMock
            .Setup(p => p.AddPhotoAsync(file))
            .ReturnsAsync(uploadedUrl);

        User? capturedUser = null;
        _userManagerMock
            .Setup(mgr => mgr.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .Callback<User, string>((u, _) => capturedUser = u)
            .ReturnsAsync(IdentityResult.Success);

        var result = await _authService.RegisterUserAsync(registerDto);

        Assert.True(result);
        capturedUser.Should().NotBeNull();
        capturedUser!.ImageUrl.Should().Be(uploadedUrl);
        _photoServiceMock.Verify(p => p.AddPhotoAsync(file), Times.Once);
    }

    [Fact]
    public async Task RegisterUserAsync_WhenPhotoUploadReturnsNull_UsesFallbackDefaultUrl()
    {
        var file = CreateTestFormFile();
        var registerDto = new RegisterUserDto
        {
            Name = "Valid name",
            Surname = "Valid surname",
            Email = "test@gmail.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            ProfileImage = file
        };

        _photoServiceMock
            .Setup(p => p.AddPhotoAsync(file))
            .ReturnsAsync((string?)null);

        User? capturedUser = null;
        _userManagerMock
            .Setup(mgr => mgr.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .Callback<User, string>((u, _) => capturedUser = u)
            .ReturnsAsync(IdentityResult.Success);

        var result = await _authService.RegisterUserAsync(registerDto);

        Assert.True(result);
        capturedUser.Should().NotBeNull();
        capturedUser!.ImageUrl.Should().Be("https://res.cloudinary.com/n7iduzxq/image/upload/v1792748390/user-profile.png");
    }

    [Fact]
    public async Task RegisterUserAsync_WhenEmailAlreadyExists_ReturnsFalse()
    {
        var registerDto = new RegisterUserDto
        {
            Name = "Valid name",
            Surname = "Valid surname",
            Email = "test@gmail.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        _userManagerMock
            .Setup(mgr => mgr.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Email already taken!" }));

        var result = await _authService.RegisterUserAsync(registerDto);

        Assert.False(result);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithValidTokens_ReturnsNewTokens()
    {
        var email = "test@gmail.com";
        var fakeUser = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            RefreshToken = "ValidToken123",
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(5)
        };

        _userManagerMock.Setup(mgr => mgr.FindByEmailAsync(email))
            .ReturnsAsync(fakeUser);

        _userManagerMock.Setup(mgr => mgr.CheckPasswordAsync(fakeUser, "password"))
            .ReturnsAsync(true);

        var loginResult = await _authService
            .LoginUserAsync(new LoginUserDto { Email = email, Password = "password" });
        Assert.NotNull(loginResult);
        var validJwtToken = loginResult!.Token;

        var refreshDto = new RefreshTokenRequestDto
        {
            Token = validJwtToken,
            RefreshToken = loginResult.RefreshToken
        };

        var result = await _authService.RefreshTokenAsync(refreshDto);

        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result!.Token));
        Assert.False(string.IsNullOrEmpty(result.RefreshToken));
    }

    [Fact]
    public async Task RefreshTokenAsync_WhenUserNotFound_ReturnsNull()
    {
        var email = "test@gmail.com";
        var fakeUser = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            RefreshToken = "ValidToken123",
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(5)
        };

        _userManagerMock.Setup(mgr => mgr.FindByEmailAsync(email))
            .ReturnsAsync(fakeUser);

        _userManagerMock.Setup(mgr => mgr.CheckPasswordAsync(fakeUser, "password"))
            .ReturnsAsync(true);

        var loginResult = await _authService
            .LoginUserAsync(new LoginUserDto { Email = email, Password = "password" });
        Assert.NotNull(loginResult);
        var validJwtToken = loginResult!.Token;

        var refreshDto = new RefreshTokenRequestDto
        {
            Token = validJwtToken,
            RefreshToken = loginResult.RefreshToken
        };

        _userManagerMock.Setup(mgr => mgr.FindByEmailAsync(email))
            .ReturnsAsync((User?)null);

        var result = await _authService.RefreshTokenAsync(refreshDto);

        Assert.Null(result);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithIncorrectRefreshToken_ReturnsNull()
    {
        var email = "test@gmail.com";
        var fakeUser = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            RefreshToken = "ValidToken123",
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(5)
        };

        _userManagerMock.Setup(mgr => mgr.FindByEmailAsync(email))
            .ReturnsAsync(fakeUser);

        _userManagerMock.Setup(mgr => mgr.CheckPasswordAsync(fakeUser, "password"))
            .ReturnsAsync(true);

        var loginResult = await _authService
            .LoginUserAsync(new LoginUserDto { Email = email, Password = "password" });
        Assert.NotNull(loginResult);
        var validJwtToken = loginResult!.Token;

        var refreshDto = new RefreshTokenRequestDto
        {
            Token = validJwtToken,
            RefreshToken = "InvalidToken123"
        };

        var result = await _authService.RefreshTokenAsync(refreshDto);

        Assert.Null(result);
    }

    [Fact]
    public async Task RefreshTokenAsync_WhenRefreshTokenIsExpired_ReturnsNull()
    {
        var email = "test@gmail.com";
        var fakeUser = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            RefreshToken = "ValidToken123",
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(-1)
        };

        _userManagerMock.Setup(mgr => mgr.FindByEmailAsync(email))
            .ReturnsAsync(fakeUser);

        _userManagerMock.Setup(mgr => mgr.CheckPasswordAsync(fakeUser, "password"))
            .ReturnsAsync(true);

        var loginResult = await _authService
            .LoginUserAsync(new LoginUserDto { Email = email, Password = "password" });
        Assert.NotNull(loginResult);
        var validJwtToken = loginResult!.Token;

        var refreshDto = new RefreshTokenRequestDto
        {
            Token = validJwtToken,
            RefreshToken = loginResult.RefreshToken
        };
        fakeUser.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(-1);

        var result = await _authService.RefreshTokenAsync(refreshDto);

        Assert.Null(result);
    }
}
