using AppGymAPI.DTOs;
using AppGymAPI.Models;
using AppGymAPI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;

namespace AppGymAPI.Tests;

public class AuthServiceTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly AuthService _authService;
    public AuthServiceTests()
    {
        var storeMock = new Mock<IUserStore<User>>();

        _userManagerMock = new Mock<UserManager<User>>(storeMock.Object,
            null!,null!,null!,null!,null!,null!,null!,null!);
        
        _configurationMock = new Mock<IConfiguration>();
        
        _configurationMock
            .Setup(c => c["JwtSettings:SecretKey"])
            .Returns("ThisIsMySuperSecretKeyForTestingPurposesOnly123456!");
        
        _configurationMock
            .Setup(c => c["JwtSettings:Issuer"]).Returns("TestIssuer");
        
        _configurationMock
            .Setup(c => c["JwtSettings:Audience"]).Returns("TestAudience");
        
        _authService = new AuthService(_userManagerMock.Object, _configurationMock.Object);
    }

    [Fact]
    public async Task LoginUserAsync_WithValidCredentials_ReturnsAuthResponseDTO()
    {
        var loginUserDto = new LoginUserDTO
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
            .Setup(mgr => mgr
                    .FindByEmailAsync(loginUserDto.Email)
            )
            .ReturnsAsync(fakeUser);
        _userManagerMock
            .Setup(mgr =>
                mgr.CheckPasswordAsync(fakeUser, loginUserDto.Password))
            .ReturnsAsync(true);
        var result = await _authService.LoginUserAsync(loginUserDto);
        Assert.NotNull(result);
    }
    [Fact]
    public async Task LoginUserAsync_WithInvalidEmail_ReturnsNull()
    {
        var loginDto = new LoginUserDTO
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
        var loginDto = new LoginUserDTO
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
            .Setup(mgr => mgr
                .FindByEmailAsync(loginDto.Email)
            )
            .ReturnsAsync(fakeUser);
        _userManagerMock
            .Setup(mgr =>
                mgr.CheckPasswordAsync(fakeUser, loginDto.Password))
            .ReturnsAsync(false);

        var result = await _authService.LoginUserAsync(loginDto);
        
        Assert.Null(result);
    }
    [Fact]
    public async Task RegisterUserAsync_WithValidUserDetails_ReturnsTrue()
    {
        var registerDto = new RegisterUserDTO
        {
            Name = "Valid name",
            Surname = "Valid surname",
            Email = "test@gmail.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };
       
        _userManagerMock
            .Setup(mgr => mgr.CreateAsync(It.IsAny<User>(),It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _authService.RegisterUserAsync(registerDto);
        
        Assert.True(result);
    }
    [Fact]
    public async Task RegisterUserAsync_WhenEmailAlreadyExists_ReturnsFalse()
    {
        var registerDto = new RegisterUserDTO
        {
            Name = "Valid name",
            Surname = "Valid surname",
            Email = "test@gmail.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        _userManagerMock
            .Setup(mgr =>
                mgr.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
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
            .LoginUserAsync(new LoginUserDTO{Email = email,Password = "password"});
        var validJwtToken = loginResult.Token;

        var refreshDto = new RefreshTokenRequestDTO
        {
            Token = validJwtToken,
            RefreshToken = loginResult.RefreshToken
        };

        var result = await _authService.RefreshTokenAsync(refreshDto);

        Assert.NotNull(loginResult);
        Assert.False(string.IsNullOrEmpty(result.Token));
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
            .LoginUserAsync(new LoginUserDTO{Email = email,Password = "password"});
        var validJwtToken = loginResult.Token;

        var refreshDto = new RefreshTokenRequestDTO
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
            .LoginUserAsync(new LoginUserDTO{Email = email,Password = "password"});
        var validJwtToken = loginResult.Token;

        var refreshDto = new RefreshTokenRequestDTO
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
            .LoginUserAsync(new LoginUserDTO{Email = email,Password = "password"});
        var validJwtToken = loginResult.Token;
        
        var refreshDto = new RefreshTokenRequestDTO
        {
            Token = validJwtToken,
            RefreshToken = loginResult.RefreshToken
        };
        fakeUser.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(-1);
        
        var result = await _authService.RefreshTokenAsync(refreshDto);

        Assert.Null(result);
    }
}
