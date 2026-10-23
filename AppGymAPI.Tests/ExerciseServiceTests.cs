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
using Xunit;

namespace AppGymAPI.Tests;

public class ExerciseServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _dbContext;
    private readonly Mock<IPhotoService> _photoServiceMock;
    private readonly Mock<ILogger<ExerciseService>> _loggerMock;
    private readonly ConcurrencyExceptionInterceptor _concurrencyInterceptor;
    private readonly ExerciseService _exerciseService;

    public ExerciseServiceTests()
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
        _loggerMock = new Mock<ILogger<ExerciseService>>();
        _exerciseService = new ExerciseService(_dbContext, _loggerMock.Object, _photoServiceMock.Object);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Close();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    #region Helpers

    private static Exercise CreateTestExercise(
        Guid? id = null,
        string name = "Bench Press",
        string description = "Chest compound exercise",
        string gifUrl = "",
        ExerciseType category = ExerciseType.Weightlifting,
        TargetedMuscle targetedMuscle = TargetedMuscle.Chest)
    {
        return new Exercise
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
            Description = description,
            GifUrl = gifUrl,
            Category = category,
            TargetedMuscle = targetedMuscle
        };
    }

    private static IFormFile CreateDummyFormFile(
        string fileName = "exercise.gif",
        string contentType = "image/gif",
        byte[]? content = null)
    {
        content ??= Encoding.UTF8.GetBytes("dummy gif content");
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, content.Length, "Gif", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    #endregion

    #region AddExerciseAsync Tests

    [Fact]
    public async Task AddExerciseAsync_WhenGifIsNull_AddsExerciseAndReturnsDto()
    {
        // Arrange
        var dto = new ExerciseAddDto
        {
            Name = "Push Up",
            Description = "Basic bodyweight chest movement",
            Category = ExerciseType.Bodyweight,
            TargetedMuscle = TargetedMuscle.Chest,
            Gif = null
        };

        // Act
        var result = await _exerciseService.AddExerciseAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().NotBeEmpty();
        result.Name.Should().Be(dto.Name);
        result.Description.Should().Be(dto.Description);
        result.Category.Should().Be(dto.Category);
        result.TargetedMuscle.Should().Be(dto.TargetedMuscle);
        result.GifUrl.Should().BeEmpty();

        _photoServiceMock.Verify(p => p.AddGifAsync(It.IsAny<IFormFile>()), Times.Never);

        var savedExercise = await _dbContext.Exercises.FirstOrDefaultAsync(e => e.Id == result.Id);
        savedExercise.Should().NotBeNull();
        savedExercise!.Name.Should().Be(dto.Name);
    }

    [Fact]
    public async Task AddExerciseAsync_WhenGifHasZeroLength_AddsExerciseAndReturnsDtoWithoutCallingPhotoService()
    {
        // Arrange
        var emptyFile = CreateDummyFormFile(content: Array.Empty<byte>());
        var dto = new ExerciseAddDto
        {
            Name = "Bodyweight Squat",
            Description = "Lower body exercise",
            Category = ExerciseType.Bodyweight,
            TargetedMuscle = TargetedMuscle.Quadriceps,
            Gif = emptyFile
        };

        // Act
        var result = await _exerciseService.AddExerciseAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be(dto.Name);
        result.GifUrl.Should().BeEmpty();
        _photoServiceMock.Verify(p => p.AddGifAsync(It.IsAny<IFormFile>()), Times.Never);
    }

    [Fact]
    public async Task AddExerciseAsync_WhenGifProvidedAndUploadSucceeds_AddsExerciseWithGifUrl()
    {
        // Arrange
        var file = CreateDummyFormFile();
        var dto = new ExerciseAddDto
        {
            Name = "Pull Up",
            Description = "Upper body back exercise",
            Category = ExerciseType.Bodyweight,
            TargetedMuscle = TargetedMuscle.Lats,
            Gif = file
        };
        const string expectedUrl = "https://cloudinary.com/appgymapi/pullup.gif";

        _photoServiceMock
            .Setup(p => p.AddGifAsync(file))
            .ReturnsAsync(expectedUrl);

        // Act
        var result = await _exerciseService.AddExerciseAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be(dto.Name);
        result.GifUrl.Should().Be(expectedUrl);

        _photoServiceMock.Verify(p => p.AddGifAsync(file), Times.Once);

        var savedExercise = await _dbContext.Exercises.FirstOrDefaultAsync(e => e.Id == result.Id);
        savedExercise.Should().NotBeNull();
        savedExercise!.GifUrl.Should().Be(expectedUrl);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task AddExerciseAsync_WhenPhotoUploadReturnsNullOrEmpty_ReturnsNullAndDoesNotSave(string? uploadedUrl)
    {
        // Arrange
        var file = CreateDummyFormFile();
        var dto = new ExerciseAddDto
        {
            Name = "Deadlift",
            Description = "Compound posterior chain exercise",
            Category = ExerciseType.Weightlifting,
            TargetedMuscle = TargetedMuscle.Lowerback,
            Gif = file
        };

        _photoServiceMock
            .Setup(p => p.AddGifAsync(file))
            .ReturnsAsync(uploadedUrl);

        // Act
        var result = await _exerciseService.AddExerciseAsync(dto);

        // Assert
        result.Should().BeNull();
        var exercises = await _dbContext.Exercises.ToListAsync();
        exercises.Should().BeEmpty();
    }

    [Fact]
    public async Task AddExerciseAsync_WhenPhotoServiceThrows_LogsWarningAndRethrows()
    {
        // Arrange
        var file = CreateDummyFormFile();
        var dto = new ExerciseAddDto
        {
            Name = "Overhead Press",
            Description = "Shoulder press exercise",
            Category = ExerciseType.Weightlifting,
            TargetedMuscle = TargetedMuscle.Shoulders,
            Gif = file
        };

        _photoServiceMock
            .Setup(p => p.AddGifAsync(file))
            .ThrowsAsync(new InvalidOperationException("Upload failed"));

        // Act & Assert
        await FluentActions.Invoking(() => _exerciseService.AddExerciseAsync(dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Upload failed");

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v != null && v.ToString()!.Contains("Failed to save the image")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        var exercises = await _dbContext.Exercises.ToListAsync();
        exercises.Should().BeEmpty();
    }

    #endregion

    #region DeleteExerciseAsync Tests

    [Fact]
    public async Task DeleteExerciseAsync_WhenExerciseExists_DeletesFromDbAndReturnsTrue()
    {
        // Arrange
        var exercise = CreateTestExercise(name: "Barbell Curl");
        await _dbContext.Exercises.AddAsync(exercise);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act
        var result = await _exerciseService.DeleteExerciseAsync(exercise.Id);

        // Assert
        result.Should().BeTrue();
        var inDb = await _dbContext.Exercises.FirstOrDefaultAsync(e => e.Id == exercise.Id);
        inDb.Should().BeNull();
    }

    [Fact]
    public async Task DeleteExerciseAsync_WhenExerciseDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _exerciseService.DeleteExerciseAsync(nonExistentId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteExerciseAsync_WhenExerciseHasGifUrl_DeletesPhotoFromCloudinary()
    {
        var exercise = CreateTestExercise(
            name: "Barbell Curl",
            gifUrl: "https://cloudinary.com/appgymapi/curl_exercise.gif");
        await _dbContext.Exercises.AddAsync(exercise);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        _photoServiceMock.Setup(p => p.DeletePhotoAsync("appgymapi/curl_exercise")).ReturnsAsync(true);

        var result = await _exerciseService.DeleteExerciseAsync(exercise.Id);

        result.Should().BeTrue();
        _photoServiceMock.Verify(p => p.DeletePhotoAsync("appgymapi/curl_exercise"), Times.Once);
        var inDb = await _dbContext.Exercises.FirstOrDefaultAsync(e => e.Id == exercise.Id);
        inDb.Should().BeNull();
    }

    [Fact]
    public async Task DeleteExerciseAsync_WhenPhotoDeletionFails_LogsWarningAndStillDeletesFromDb()
    {
        var exercise = CreateTestExercise(
            name: "Barbell Curl",
            gifUrl: "https://cloudinary.com/appgymapi/curl_exercise.gif");
        await _dbContext.Exercises.AddAsync(exercise);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        _photoServiceMock.Setup(p => p.DeletePhotoAsync("appgymapi/curl_exercise")).ThrowsAsync(new Exception("Network error"));

        var result = await _exerciseService.DeleteExerciseAsync(exercise.Id);

        result.Should().BeTrue();
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v != null && v.ToString()!.Contains("Failed to delete old image")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        var inDb = await _dbContext.Exercises.FirstOrDefaultAsync(e => e.Id == exercise.Id);
        inDb.Should().BeNull();
    }

    #endregion

    #region SearchExerciseByNameAsync Tests

    [Fact]
    public async Task SearchExerciseByNameAsync_WhenExerciseExists_ReturnsMappedExerciseDTO()
    {
        // Arrange
        var exercise = CreateTestExercise(
            name: "Triceps Pushdown",
            description: "Cable triceps exercise",
            category: ExerciseType.Weightlifting,
            targetedMuscle: TargetedMuscle.Triceps);
        await _dbContext.Exercises.AddAsync(exercise);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act
        var result = await _exerciseService.SearchExerciseByNameAsync("Triceps Pushdown");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(exercise.Id);
        result.Name.Should().Be(exercise.Name);
        result.Description.Should().Be(exercise.Description);
        result.GifUrl.Should().Be(exercise.GifUrl);
        result.Category.Should().Be(exercise.Category);
        result.TargetedMuscle.Should().Be(exercise.TargetedMuscle);
    }

    [Fact]
    public async Task SearchExerciseByNameAsync_WhenExerciseDoesNotExist_ReturnsNull()
    {
        // Arrange & Act
        var result = await _exerciseService.SearchExerciseByNameAsync("NonExistentExercise");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region AllExercisesAsync Tests

    [Fact]
    public async Task AllExercisesAsync_WhenPageSizeIsZero_ReturnsEmptyList()
    {
        // Arrange
        var exercise = CreateTestExercise();
        await _dbContext.Exercises.AddAsync(exercise);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _exerciseService.AllExercisesAsync(pageSize: 0, pageNumber: 1);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task AllExercisesAsync_WithValidPagination_ReturnsCorrectPagedList()
    {
        // Arrange
        var exercises = Enumerable.Range(1, 5)
            .Select(i => CreateTestExercise(
                id: new Guid($"00000000-0000-0000-0000-{i:D12}"),
                name: $"Exercise {i}"))
            .ToList();
        await _dbContext.Exercises.AddRangeAsync(exercises);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act: Page 1, Page 2, Page 3 with pageSize 2
        var page1 = await _exerciseService.AllExercisesAsync(pageSize: 2, pageNumber: 1);
        var page2 = await _exerciseService.AllExercisesAsync(pageSize: 2, pageNumber: 2);
        var page3 = await _exerciseService.AllExercisesAsync(pageSize: 2, pageNumber: 3);

        // Assert
        page1.Should().HaveCount(2);
        page2.Should().HaveCount(2);
        page3.Should().HaveCount(1);

        page1.Should().AllSatisfy(e =>
        {
            e.Id.Should().NotBeEmpty();
            e.GifUrl.Should().NotBeNull();
        });

        var allRetrieved = page1.Concat(page2).Concat(page3).Select(e => e.Name).ToList();
        allRetrieved.Should().HaveCount(5);
        allRetrieved.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task AllExercisesAsync_WhenPageNumberOutOfBounds_ReturnsEmptyList()
    {
        // Arrange
        var exercise = CreateTestExercise();
        await _dbContext.Exercises.AddAsync(exercise);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act
        var result = await _exerciseService.AllExercisesAsync(pageSize: 10, pageNumber: 5);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region SearchExercisesByCategoryAsync Tests

    [Fact]
    public async Task SearchExercisesByCategoryAsync_ReturnsMatchingExercises()
    {
        // Arrange
        var ex1 = CreateTestExercise(name: "Squat", category: ExerciseType.Weightlifting);
        var ex2 = CreateTestExercise(name: "Bench", category: ExerciseType.Weightlifting);
        var ex3 = CreateTestExercise(name: "Running", category: ExerciseType.Cardio);

        await _dbContext.Exercises.AddRangeAsync(ex1, ex2, ex3);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act
        var result = await _exerciseService.SearchExercisesByCategoryAsync(ExerciseType.Weightlifting);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(e => e.Category == ExerciseType.Weightlifting);
        result.Should().AllSatisfy(e => e.Id.Should().NotBeEmpty());
        result.Select(e => e.Name).Should().Contain(new[] { "Squat", "Bench" });
    }

    [Fact]
    public async Task SearchExercisesByCategoryAsync_WhenNoneMatch_ReturnsEmptyList()
    {
        // Arrange
        var ex = CreateTestExercise(category: ExerciseType.Cardio);
        await _dbContext.Exercises.AddAsync(ex);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act
        var result = await _exerciseService.SearchExercisesByCategoryAsync(ExerciseType.Stretching);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region SearchExercisesByTargetedMuscleAsync Tests

    [Fact]
    public async Task SearchExercisesByTargetedMuscleAsync_ReturnsMatchingExercises()
    {
        // Arrange
        var ex1 = CreateTestExercise(name: "Incline Bench", targetedMuscle: TargetedMuscle.Chest);
        var ex2 = CreateTestExercise(name: "Cable Fly", targetedMuscle: TargetedMuscle.Chest);
        var ex3 = CreateTestExercise(name: "Hamstring Curl", targetedMuscle: TargetedMuscle.Hamstrings);

        await _dbContext.Exercises.AddRangeAsync(ex1, ex2, ex3);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act
        var result = await _exerciseService.SearchExercisesByTargetedMuscleAsync(TargetedMuscle.Chest);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(e => e.TargetedMuscle == TargetedMuscle.Chest);
        result.Should().AllSatisfy(e => e.Id.Should().NotBeEmpty());
        result.Select(e => e.Name).Should().Contain(new[] { "Incline Bench", "Cable Fly" });
    }

    [Fact]
    public async Task SearchExercisesByTargetedMuscleAsync_WhenNoneMatch_ReturnsEmptyList()
    {
        // Arrange
        var ex = CreateTestExercise(targetedMuscle: TargetedMuscle.Biceps);
        await _dbContext.Exercises.AddAsync(ex);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act
        var result = await _exerciseService.SearchExercisesByTargetedMuscleAsync(TargetedMuscle.Traps);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region EditExerciseAsync Tests

    [Fact]
    public async Task EditExerciseAsync_WhenExerciseDoesNotExist_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var dto = new ExerciseAddDto
        {
            Name = "Any Name",
            Description = "Any Description",
            Category = ExerciseType.Weightlifting,
            TargetedMuscle = TargetedMuscle.Chest
        };

        // Act
        var result = await _exerciseService.EditExerciseAsync(nonExistentId, dto);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task EditExerciseAsync_WhenNoNewGifProvided_UpdatesBasicFieldsAndPreservesGifUrl()
    {
        // Arrange
        var exercise = CreateTestExercise(
            name: "Old Name",
            description: "Old Desc",
            gifUrl: "https://example.com/existing.gif",
            category: ExerciseType.Bodyweight,
            targetedMuscle: TargetedMuscle.Abs);

        await _dbContext.Exercises.AddAsync(exercise);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var editDto = new ExerciseAddDto
        {
            Name = "New Name",
            Description = "New Desc",
            Category = ExerciseType.Weightlifting,
            TargetedMuscle = TargetedMuscle.Glutes,
            Gif = null
        };

        // Act
        var result = await _exerciseService.EditExerciseAsync(exercise.Id, editDto);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(exercise.Id);
        result.Name.Should().Be("New Name");
        result.Description.Should().Be("New Desc");
        result.GifUrl.Should().Be("https://example.com/existing.gif");
        result.Category.Should().Be(ExerciseType.Weightlifting);
        result.TargetedMuscle.Should().Be(TargetedMuscle.Glutes);

        _photoServiceMock.Verify(p => p.DeletePhotoAsync(It.IsAny<string>()), Times.Never);
        _photoServiceMock.Verify(p => p.AddGifAsync(It.IsAny<IFormFile>()), Times.Never);

        _dbContext.ChangeTracker.Clear();
        var updatedInDb = await _dbContext.Exercises.FirstOrDefaultAsync(e => e.Id == exercise.Id);
        updatedInDb.Should().NotBeNull();
        updatedInDb!.Name.Should().Be("New Name");
        updatedInDb.GifUrl.Should().Be("https://example.com/existing.gif");
    }

    [Fact]
    public async Task EditExerciseAsync_WhenPriorGifIsEmptyAndNewGifProvided_UploadsAndSetsGifUrl()
    {
        // Arrange
        var exercise = CreateTestExercise(gifUrl: "");
        await _dbContext.Exercises.AddAsync(exercise);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var file = CreateDummyFormFile();
        var editDto = new ExerciseAddDto
        {
            Name = "Updated Name",
            Description = "Updated Desc",
            Category = ExerciseType.Weightlifting,
            TargetedMuscle = TargetedMuscle.Forearms,
            Gif = file
        };

        const string newUrl = "https://example.com/appgymapi/new_image.gif";
        _photoServiceMock.Setup(p => p.AddGifAsync(file)).ReturnsAsync(newUrl);

        // Act
        var result = await _exerciseService.EditExerciseAsync(exercise.Id, editDto);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(exercise.Id);
        result.GifUrl.Should().Be(newUrl);
        _photoServiceMock.Verify(p => p.DeletePhotoAsync(It.IsAny<string>()), Times.Never);
        _photoServiceMock.Verify(p => p.AddGifAsync(file), Times.Once);

        _dbContext.ChangeTracker.Clear();
        var updatedInDb = await _dbContext.Exercises.FirstOrDefaultAsync(e => e.Id == exercise.Id);
        updatedInDb!.GifUrl.Should().Be(newUrl);
    }

    [Fact]
    public async Task EditExerciseAsync_WhenPriorGifExistsAndNewGifProvided_DeletesOldPhotoAndUploadsNew()
    {
        // Arrange
        var exercise = CreateTestExercise(gifUrl: "https://cloudinary.com/appgymapi/old_exercise_photo.gif");
        await _dbContext.Exercises.AddAsync(exercise);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var file = CreateDummyFormFile();
        var editDto = new ExerciseAddDto
        {
            Name = "Updated Exercise",
            Description = "Updated Description",
            Category = ExerciseType.Weightlifting,
            TargetedMuscle = TargetedMuscle.Chest,
            Gif = file
        };

        const string newUrl = "https://cloudinary.com/appgymapi/new_exercise_photo.gif";
        _photoServiceMock.Setup(p => p.DeletePhotoAsync("appgymapi/old_exercise_photo")).ReturnsAsync(true);
        _photoServiceMock.Setup(p => p.AddGifAsync(file)).ReturnsAsync(newUrl);

        // Act
        var result = await _exerciseService.EditExerciseAsync(exercise.Id, editDto);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(exercise.Id);
        result.GifUrl.Should().Be(newUrl);
        _photoServiceMock.Verify(p => p.DeletePhotoAsync("appgymapi/old_exercise_photo"), Times.Once);
        _photoServiceMock.Verify(p => p.AddGifAsync(file), Times.Once);

        _dbContext.ChangeTracker.Clear();
        var updatedInDb = await _dbContext.Exercises.FirstOrDefaultAsync(e => e.Id == exercise.Id);
        updatedInDb!.GifUrl.Should().Be(newUrl);
    }

    [Fact]
    public async Task EditExerciseAsync_WhenOldPhotoDeletionFails_LogsWarningAndStillUploadsNewPhoto()
    {
        // Arrange
        var exercise = CreateTestExercise(gifUrl: "https://cloudinary.com/appgymapi/old_image.gif");
        await _dbContext.Exercises.AddAsync(exercise);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var file = CreateDummyFormFile();
        var editDto = new ExerciseAddDto
        {
            Name = "Exercise Name",
            Description = "Exercise Description",
            Category = ExerciseType.Weightlifting,
            TargetedMuscle = TargetedMuscle.Chest,
            Gif = file
        };

        const string newUrl = "https://cloudinary.com/appgymapi/new_image.gif";
        _photoServiceMock
            .Setup(p => p.DeletePhotoAsync(It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Deletion failed"));

        _photoServiceMock
            .Setup(p => p.AddGifAsync(file))
            .ReturnsAsync(newUrl);

        // Act
        var result = await _exerciseService.EditExerciseAsync(exercise.Id, editDto);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(exercise.Id);
        result.GifUrl.Should().Be(newUrl);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v != null && v.ToString()!.Contains("Failed to delete old image")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _dbContext.ChangeTracker.Clear();
        var updatedInDb = await _dbContext.Exercises.FirstOrDefaultAsync(e => e.Id == exercise.Id);
        updatedInDb!.GifUrl.Should().Be(newUrl);
    }

    [Fact]
    public async Task EditExerciseAsync_UsesIdParameter_ToUpdateCorrectExerciseWhenMultipleExercisesExist()
    {
        // Arrange
        var ex1 = CreateTestExercise(name: "First Exercise");
        var ex2 = CreateTestExercise(name: "Second Exercise");
        await _dbContext.Exercises.AddRangeAsync(ex1, ex2);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var editDto = new ExerciseAddDto
        {
            Name = "Updated First Exercise",
            Description = "Updated Desc",
            Category = ExerciseType.Weightlifting,
            TargetedMuscle = TargetedMuscle.Chest
        };

        // Act
        var result = await _exerciseService.EditExerciseAsync(ex1.Id, editDto);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(ex1.Id);
        result.Name.Should().Be("Updated First Exercise");

        _dbContext.ChangeTracker.Clear();
        var ex1InDb = await _dbContext.Exercises.FirstOrDefaultAsync(e => e.Id == ex1.Id);
        var ex2InDb = await _dbContext.Exercises.FirstOrDefaultAsync(e => e.Id == ex2.Id);

        ex1InDb!.Name.Should().Be("Updated First Exercise");
        ex2InDb!.Name.Should().Be("Second Exercise"); // Must be untouched
    }

    [Fact]
    public async Task EditExerciseAsync_WhenConcurrencyConflictOccurs_LogsErrorAndRethrows()
    {
        // Arrange
        var exercise = CreateTestExercise();
        await _dbContext.Exercises.AddAsync(exercise);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var editDto = new ExerciseAddDto
        {
            Name = "Conflict Exercise",
            Description = "Desc",
            Category = ExerciseType.Weightlifting,
            TargetedMuscle = TargetedMuscle.Chest
        };

        _concurrencyInterceptor.ThrowOnSaving = true;

        // Act & Assert
        await FluentActions.Invoking(() => _exerciseService.EditExerciseAsync(exercise.Id, editDto))
            .Should().ThrowAsync<DbUpdateConcurrencyException>();

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v != null && v.ToString()!.Contains("Concurrency error while updating exercise")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Cancellation Token Tests

    [Fact]
    public async Task Operations_WhenCancelledTokenPassed_ThrowOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var dummyDto = new ExerciseAddDto
        {
            Name = "Name",
            Description = "Description",
            Category = ExerciseType.Weightlifting,
            TargetedMuscle = TargetedMuscle.Chest
        };

        // Act & Assert
        await FluentActions.Invoking(() => _exerciseService.AddExerciseAsync(dummyDto, cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();

        await FluentActions.Invoking(() => _exerciseService.DeleteExerciseAsync(Guid.NewGuid(), cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();

        await FluentActions.Invoking(() => _exerciseService.SearchExerciseByNameAsync("Name", cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();

        await FluentActions.Invoking(() => _exerciseService.AllExercisesAsync(10, 1, cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();

        await FluentActions.Invoking(() => _exerciseService.SearchExercisesByCategoryAsync(ExerciseType.Weightlifting, cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();

        await FluentActions.Invoking(() => _exerciseService.SearchExercisesByTargetedMuscleAsync(TargetedMuscle.Chest, cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();

        await FluentActions.Invoking(() => _exerciseService.EditExerciseAsync(Guid.NewGuid(), dummyDto, cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region Concurrency Exception Interceptor

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

    #endregion
}
