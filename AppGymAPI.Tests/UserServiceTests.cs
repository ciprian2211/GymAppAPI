using System.Text;
using AppGymAPI.Data;
using AppGymAPI.DTOs;
using AppGymAPI.Models;
using AppGymAPI.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;

namespace AppGymAPI.Tests;

public class UserServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _dbContext;
    private readonly Mock<IPhotoService> _photoServiceMock;
    private readonly Mock<ILogger<UserService>> _loggerMock;
    private readonly ConcurrencyExceptionInterceptor _concurrencyInterceptor;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _concurrencyInterceptor = new ConcurrencyExceptionInterceptor();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(_concurrencyInterceptor)
            .Options;

        _dbContext = new AppDbContext(options);
        _dbContext.Database.EnsureCreated();

        _photoServiceMock = new Mock<IPhotoService>();
        _loggerMock = new Mock<ILogger<UserService>>();
        _userService = new UserService(_dbContext, _photoServiceMock.Object, _loggerMock.Object);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Close();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    private static User CreateTestUser(
        Guid? id = null,
        string name = "John",
        string surname = "Doe",
        string email = "john.doe@example.com",
        Role role = Role.User,
        float reviewStars = 0.0f,
        string imageUrl = "",
        string description = "")
    {
        return new User
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
            Surname = surname,
            Email = email,
            UserName = email,
            Role = role,
            ReviewStars = reviewStars,
            ImageUrl = imageUrl,
            Description = description
        };
    }

    private static IFormFile CreateDummyFormFile(
        string fileName = "test.jpg",
        string contentType = "image/jpeg",
        byte[]? content = null)
    {
        content ??= Encoding.UTF8.GetBytes("dummy image payload");
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, content.Length, "ProfileImage", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    #region Phase 2: Testing SearchUserByIdAsync

    [Fact]
    public async Task Test_SearchUserById_WhenUserExists_ReturnsMappedUserDTO()
    {
        // Arrange
        var user = CreateTestUser(
            name: "Jane",
            surname: "Austin",
            email: "jane.austin@example.com",
            role: Role.Trainer);
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act
        var result = await _userService.SearchUserByIdAsync(user.Id, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.Name.Should().Be("Jane");
        result.Surname.Should().Be("Austin");
        result.Email.Should().Be("jane.austin@example.com");
        result.Role.Should().Be(Role.Trainer);

        // Boundary Assertion: AsNoTracking must not track entities
        _dbContext.ChangeTracker.Entries().Should().BeEmpty();
    }

    [Fact]
    public async Task Test_SearchUserById_WhenUserDoesNotExist_ReturnsNull()
    {
        // Arrange: DB is empty
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _userService.SearchUserByIdAsync(nonExistentId, CancellationToken.None);

        // Assert
        result.Should().BeNull();
        _dbContext.ChangeTracker.Entries().Should().BeEmpty();
    }

    #endregion

    #region Phase 3: Testing SearchUserByNameAsync

    [Fact]
    public async Task Test_SearchUserByName_WhenMatchesExist_ReturnsMappedUserListDTOs()
    {
        // Arrange: Seed 3 users (2 matching "John", 1 non-matching)
        var user1 = CreateTestUser(name: "John Doe", surname: "One", email: "john1@example.com", reviewStars: 4.5f);
        var user2 = CreateTestUser(name: "Johnny Test", surname: "Two", email: "johnny@example.com", reviewStars: 3.8f);
        var user3 = CreateTestUser(name: "Alice Smith", surname: "Three", email: "alice@example.com", reviewStars: 5.0f);

        await _dbContext.Users.AddRangeAsync(user1, user2, user3);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act
        var results = await _userService.SearchUsersByNameAsync("John", CancellationToken.None);

        // Assert
        results.Should().NotBeNull();
        results.Should().HaveCount(2);

        results.Should().ContainEquivalentOf(new UserListDTO
        {
            Name = "John Doe",
            Surname = "One",
            Email = "john1@example.com",
            ReviewStars = 4.5f
        });

        results.Should().ContainEquivalentOf(new UserListDTO
        {
            Name = "Johnny Test",
            Surname = "Two",
            Email = "johnny@example.com",
            ReviewStars = 3.8f
        });

        // Boundary Assertion: AsNoTracking must not track entities
        _dbContext.ChangeTracker.Entries().Should().BeEmpty();
    }

    [Fact]
    public async Task Test_SearchUserByName_WhenNoMatches_ReturnsEmptyList()
    {
        // Arrange
        var user1 = CreateTestUser(name: "John Doe");
        var user2 = CreateTestUser(name: "Alice Smith");
        await _dbContext.Users.AddRangeAsync(user1, user2);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act
        var results = await _userService.SearchUsersByNameAsync("Zack", CancellationToken.None);

        // Assert
        results.Should().NotBeNull();
        results.Should().BeEmpty();

        // Boundary Assertion: AsNoTracking must not track entities
        _dbContext.ChangeTracker.Entries().Should().BeEmpty();
    }

    #endregion

    #region Phase 4: Testing SearchUsersByRoleAsync

    [Fact]
    public async Task Test_SearchUsersByRole_ReturnsOnlyUsersWithSpecificRole()
    {
        // Arrange: Seed 2 Admins, 1 Trainer, 3 Users
        var admin1 = CreateTestUser(name: "Admin One", role: Role.Admin, reviewStars: 5.0f);
        var admin2 = CreateTestUser(name: "Admin Two", role: Role.Admin, reviewStars: 4.0f);
        var trainer = CreateTestUser(name: "Trainer One", role: Role.Trainer, reviewStars: 4.8f);
        var user1 = CreateTestUser(name: "User One", role: Role.User, reviewStars: 3.0f);
        var user2 = CreateTestUser(name: "User Two", role: Role.User, reviewStars: 2.5f);
        var user3 = CreateTestUser(name: "User Three", role: Role.User, reviewStars: 1.0f);

        await _dbContext.Users.AddRangeAsync(admin1, admin2, trainer, user1, user2, user3);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act
        var results = await _userService.SearchUsersByRoleAsync(Role.Admin, CancellationToken.None);

        // Assert
        results.Should().NotBeNull();
        results.Should().HaveCount(2);
        results.Select(r => r.Name).Should().BeEquivalentTo(new[] { "Admin One", "Admin Two" });

        // Boundary Assertion: AsNoTracking must not track entities
        _dbContext.ChangeTracker.Entries().Should().BeEmpty();
    }

    [Fact]
    public async Task Test_SearchUsersByRole_WhenNoUsersWithRole_ReturnsEmptyList()
    {
        // Arrange: Seed only regular Users
        var user1 = CreateTestUser(name: "User One", role: Role.User);
        var user2 = CreateTestUser(name: "User Two", role: Role.User);
        await _dbContext.Users.AddRangeAsync(user1, user2);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act: Search for Trainer
        var results = await _userService.SearchUsersByRoleAsync(Role.Trainer, CancellationToken.None);

        // Assert
        results.Should().NotBeNull();
        results.Should().BeEmpty();

        // Boundary Assertion: AsNoTracking must not track entities
        _dbContext.ChangeTracker.Entries().Should().BeEmpty();
    }

    #endregion

    #region Phase 5: Testing EditUserAsync

    [Fact]
    public async Task Test_EditUser_WhenUserDoesNotExist_ReturnsNull()
    {
        // Arrange: Empty DB
        var nonExistentId = Guid.NewGuid();
        var editDto = new UserEditDTO
        {
            Id = nonExistentId,
            Name = "NonExistent",
            Surname = "User",
            Description = "Does not exist",
            ProfileImage = null
        };

        // Act
        var result = await _userService.EditUserAsync(nonExistentId, editDto, CancellationToken.None);

        // Assert
        result.Should().BeNull();
        _photoServiceMock.Verify(p => p.AddPhotoAsync(It.IsAny<IFormFile>()), Times.Never);
        _photoServiceMock.Verify(p => p.DeletePhotoAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Test_EditUser_WithNoImage_UpdatesBasicFieldsSuccessfully()
    {
        // Arrange
        var user = CreateTestUser(
            name: "OriginalName",
            surname: "OriginalSurname",
            description: "Original description");
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var editDto = new UserEditDTO
        {
            Id = user.Id,
            Name = "UpdatedFirstName",
            Surname = "UpdatedLastName",
            Description = "Updated description text",
            ProfileImage = null
        };

        // Act
        var result = await _userService.EditUserAsync(user.Id, editDto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(editDto);
        _photoServiceMock.Verify(p => p.AddPhotoAsync(It.IsAny<IFormFile>()), Times.Never);
        _photoServiceMock.Verify(p => p.DeletePhotoAsync(It.IsAny<string>()), Times.Never);

        // Verify changes persisted to DB
        _dbContext.ChangeTracker.Clear();
        var userInDb = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == user.Id);
        userInDb.Should().NotBeNull();
        userInDb!.Name.Should().Be("UpdatedFirstName");
        userInDb.Surname.Should().Be("UpdatedLastName");
        userInDb.Description.Should().Be("Updated description text");
    }

    [Fact]
    public async Task Test_EditUser_WithNewImage_NoOldImage_UploadsImageAndSavesUrl()
    {
        // Arrange
        var user = CreateTestUser(
            name: "John",
            surname: "Doe",
            imageUrl: "");
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var dummyImage = CreateDummyFormFile("avatar.jpg");
        var editDto = new UserEditDTO
        {
            Id = user.Id,
            Name = "JohnUpdated",
            Surname = "DoeUpdated",
            Description = "Has image now",
            ProfileImage = dummyImage
        };

        const string newImageUrl = "https://cloudinary/image.jpg";
        _photoServiceMock
            .Setup(p => p.AddPhotoAsync(dummyImage))
            .ReturnsAsync(newImageUrl);

        // Act
        var result = await _userService.EditUserAsync(user.Id, editDto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _photoServiceMock.Verify(p => p.AddPhotoAsync(dummyImage), Times.Once);
        _photoServiceMock.Verify(p => p.DeletePhotoAsync(It.IsAny<string>()), Times.Never);

        // Verify DB state
        _dbContext.ChangeTracker.Clear();
        var userInDb = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == user.Id);
        userInDb.Should().NotBeNull();
        userInDb!.ImageUrl.Should().Be(newImageUrl);
        userInDb.Name.Should().Be("JohnUpdated");
    }

    [Fact]
    public async Task Test_EditUser_WithNewImage_HasOldImage_DeletesOldImageAndUploadsNewOne()
    {
        // Arrange: User with old Cloudinary image
        const string oldImageUrl = "https://res.cloudinary.com/demo/image/upload/v1234/appgymapi/old_pic.jpg";
        const string newImageUrl = "https://res.cloudinary.com/demo/image/upload/v5678/appgymapi/new_pic.jpg";

        var user = CreateTestUser(
            name: "John",
            surname: "Doe",
            imageUrl: oldImageUrl);
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var dummyImage = CreateDummyFormFile("new_avatar.jpg");
        var editDto = new UserEditDTO
        {
            Id = user.Id,
            Name = "JohnWithNewPhoto",
            Surname = "DoeWithNewPhoto",
            Description = "Photo updated",
            ProfileImage = dummyImage
        };

        _photoServiceMock
            .Setup(p => p.DeletePhotoAsync("appgymapi/old_pic"))
            .ReturnsAsync(true);

        _photoServiceMock
            .Setup(p => p.AddPhotoAsync(dummyImage))
            .ReturnsAsync(newImageUrl);

        // Act
        var result = await _userService.EditUserAsync(user.Id, editDto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _photoServiceMock.Verify(p => p.DeletePhotoAsync("appgymapi/old_pic"), Times.Once);
        _photoServiceMock.Verify(p => p.AddPhotoAsync(dummyImage), Times.Once);

        _dbContext.ChangeTracker.Clear();
        var userInDb = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == user.Id);
        userInDb.Should().NotBeNull();
        userInDb!.ImageUrl.Should().Be(newImageUrl);
    }

    [Fact]
    public async Task Test_EditUser_WhenDeleteOldImageThrows_SwallowsExceptionAndContinues()
    {
        // Arrange: User with old image, DeletePhotoAsync throws
        const string oldImageUrl = "https://res.cloudinary.com/demo/image/upload/v1234/appgymapi/old_pic.jpg";
        const string newImageUrl = "https://res.cloudinary.com/demo/image/upload/v5678/appgymapi/new_pic.jpg";

        var user = CreateTestUser(
            name: "John",
            surname: "Doe",
            imageUrl: oldImageUrl);
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var dummyImage = CreateDummyFormFile("new_avatar.jpg");
        var editDto = new UserEditDTO
        {
            Id = user.Id,
            Name = "John",
            Surname = "Doe",
            Description = "Should succeed even if old delete fails",
            ProfileImage = dummyImage
        };

        _photoServiceMock
            .Setup(p => p.DeletePhotoAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Cloudinary deletion failed"));

        _photoServiceMock
            .Setup(p => p.AddPhotoAsync(dummyImage))
            .ReturnsAsync(newImageUrl);

        // Act: Must not throw
        var result = await _userService.EditUserAsync(user.Id, editDto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _photoServiceMock.Verify(p => p.DeletePhotoAsync("appgymapi/old_pic"), Times.Once);
        _photoServiceMock.Verify(p => p.AddPhotoAsync(dummyImage), Times.Once);

        _dbContext.ChangeTracker.Clear();
        var userInDb = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == user.Id);
        userInDb.Should().NotBeNull();
        userInDb!.ImageUrl.Should().Be(newImageUrl);
    }

    [Fact]
    public async Task Test_EditUser_WhenConcurrencyConflictOccurs_BubblesUpDbUpdateConcurrencyException()
    {
        // Arrange: Seed user, then force DbUpdateConcurrencyException on SaveChangesAsync
        var user = CreateTestUser(name: "ConcurrencyUser");
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var editDto = new UserEditDTO
        {
            Id = user.Id,
            Name = "ConflictName",
            Surname = "ConflictSurname",
            Description = "ConflictDescription"
        };

        _concurrencyInterceptor.ThrowOnSaving = true;

        // Act & Assert
        await FluentActions.Invoking(() => _userService.EditUserAsync(user.Id, editDto, CancellationToken.None))
            .Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    [Fact]
    public async Task Test_EditUser_UsesIdParameter_ToUpdateCorrectUserWhenMultipleUsersExist()
    {
        // Arrange: Seed multiple users
        var user1 = CreateTestUser(name: "User One", surname: "First", description: "Desc One");
        var user2 = CreateTestUser(name: "User Two", surname: "Second", description: "Desc Two");
        await _dbContext.Users.AddRangeAsync(user1, user2);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var editDto = new UserEditDTO
        {
            Id = user1.Id,
            Name = "Updated User One",
            Surname = "Updated First",
            Description = "Updated Desc One"
        };

        // Act: Pass user1.Id as the id parameter
        var result = await _userService.EditUserAsync(user1.Id, editDto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(editDto);

        _dbContext.ChangeTracker.Clear();
        var user1InDb = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == user1.Id);
        var user2InDb = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == user2.Id);

        user1InDb.Should().NotBeNull();
        user1InDb!.Name.Should().Be("Updated User One");
        user1InDb.Surname.Should().Be("Updated First");
        user1InDb.Description.Should().Be("Updated Desc One");

        // user2 must be completely untouched
        user2InDb.Should().NotBeNull();
        user2InDb!.Name.Should().Be("User Two");
        user2InDb.Surname.Should().Be("Second");
        user2InDb.Description.Should().Be("Desc Two");
    }

    [Fact]
    public async Task Test_EditUser_UsesIdParameterToLocateUser_EvenWhenDtoIdIsDifferent()
    {
        // Arrange: Seed two users
        var targetUser = CreateTestUser(name: "Target User", surname: "Target Surname");
        var otherUser = CreateTestUser(name: "Other User", surname: "Other Surname");
        await _dbContext.Users.AddRangeAsync(targetUser, otherUser);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // DTO contains otherUser's Id, but we are editing targetUser via id parameter
        var editDto = new UserEditDTO
        {
            Id = otherUser.Id,
            Name = "Updated Target",
            Surname = "Updated Target Surname",
            Description = "Target user updated"
        };

        // Act: Use targetUser.Id as the id parameter
        var result = await _userService.EditUserAsync(targetUser.Id, editDto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();

        _dbContext.ChangeTracker.Clear();
        var targetUserInDb = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == targetUser.Id);
        var otherUserInDb = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == otherUser.Id);

        // targetUser matched by the id parameter is updated
        targetUserInDb.Should().NotBeNull();
        targetUserInDb!.Name.Should().Be("Updated Target");
        targetUserInDb.Surname.Should().Be("Updated Target Surname");

        // otherUser whose Id was inside editDto remains unchanged
        otherUserInDb.Should().NotBeNull();
        otherUserInDb!.Name.Should().Be("Other User");
        otherUserInDb.Surname.Should().Be("Other Surname");
    }

    [Fact]
    public async Task Test_EditUser_WhenIdParameterNotFound_ReturnsNull_EvenWhenDtoIdMatchesExistingUser()
    {
        // Arrange: Seed an existing user
        var existingUser = CreateTestUser(name: "Existing User", surname: "Original");
        await _dbContext.Users.AddAsync(existingUser);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var nonExistentId = Guid.NewGuid();
        var editDto = new UserEditDTO
        {
            Id = existingUser.Id,
            Name = "Should Not Be Applied",
            Surname = "Should Not Be Applied"
        };

        // Act: Pass nonExistentId as the id parameter
        var result = await _userService.EditUserAsync(nonExistentId, editDto, CancellationToken.None);

        // Assert
        result.Should().BeNull();

        _dbContext.ChangeTracker.Clear();
        var existingUserInDb = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == existingUser.Id);
        existingUserInDb.Should().NotBeNull();
        existingUserInDb!.Name.Should().Be("Existing User");
        existingUserInDb.Surname.Should().Be("Original");
    }

    [Fact]
    public async Task Test_EditUser_WhenIdIsEmptyGuid_ReturnsNullAndDoesNotModifyUsers()
    {
        // Arrange: Seed a user
        var user = CreateTestUser(name: "Original User");
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var editDto = new UserEditDTO
        {
            Id = user.Id,
            Name = "New Name"
        };

        // Act: Pass Guid.Empty
        var result = await _userService.EditUserAsync(Guid.Empty, editDto, CancellationToken.None);

        // Assert
        result.Should().BeNull();

        _dbContext.ChangeTracker.Clear();
        var userInDb = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == user.Id);
        userInDb.Should().NotBeNull();
        userInDb!.Name.Should().Be("Original User");
    }

    #endregion

    #region Phase 6: Testing DeleteUserByIdAsync

    [Fact]
    public async Task Test_DeleteUserById_WhenUserExists_RemovesFromDatabase()
    {
        // Arrange: Seed 1 user
        var user = CreateTestUser(name: "DeleteMe");
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act
        var result = await _userService.DeleteUserByIdAsync(user.Id, CancellationToken.None);

        // Assert
        result.Should().BeTrue();

        _dbContext.ChangeTracker.Clear();
        var deletedUser = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == user.Id);
        deletedUser.Should().BeNull();
    }

    [Fact]
    public async Task Test_DeleteUserById_WhenUserDoesNotExist_ReturnsFalse()
    {
        // Arrange: Empty DB
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _userService.DeleteUserByIdAsync(nonExistentId, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task Test_DeleteUserById_WhenConcurrencyConflictOccurs_ReturnsFalse()
    {
        // Arrange: Seed user, then force DbUpdateConcurrencyException on SaveChangesAsync
        var user = CreateTestUser(name: "DeleteConflictUser");
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        _concurrencyInterceptor.ThrowOnSaving = true;

        // Act
        var result = await _userService.DeleteUserByIdAsync(user.Id, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region General / Boundary Assertions

    [Fact]
    public async Task Operations_WhenCancelledTokenPassed_ThrowOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert: All methods should honor CancellationToken
        await FluentActions.Invoking(() => _userService.SearchUserByIdAsync(Guid.NewGuid(), cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();

        await FluentActions.Invoking(() => _userService.SearchUsersByNameAsync("John", cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();

        await FluentActions.Invoking(() => _userService.SearchUsersByRoleAsync(Role.User, cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();

        await FluentActions.Invoking(() => _userService.EditUserAsync(Guid.NewGuid(), new UserEditDTO { Id = Guid.NewGuid() }, cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();

        await FluentActions.Invoking(() => _userService.DeleteUserByIdAsync(Guid.NewGuid(), cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    private sealed class ConcurrencyExceptionInterceptor : SaveChangesInterceptor
    {
        public bool ThrowOnSaving { get; set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (ThrowOnSaving)
            {
                throw new DbUpdateConcurrencyException("Simulated concurrency conflict during SaveChangesAsync.");
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            if (ThrowOnSaving)
            {
                throw new DbUpdateConcurrencyException("Simulated concurrency conflict during SaveChanges.");
            }

            return base.SavingChanges(eventData, result);
        }
    }
}
