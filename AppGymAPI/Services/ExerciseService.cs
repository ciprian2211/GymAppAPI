using AppGymAPI.Data;
using AppGymAPI.DTOs;
using AppGymAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace AppGymAPI.Services;

public class ExerciseService : IExerciseService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<ExerciseService> _logger;
    private readonly IPhotoService _photoService;
    
    public ExerciseService(AppDbContext dbContext, ILogger<ExerciseService> logger, IPhotoService photoService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _photoService = photoService;
    }

    public async Task<ExerciseDTO?> AddExerciseAsync(ExerciseAddDTO dto, CancellationToken ct = default)
    {
        string gifUrl = string.Empty;

        
        var newExercise = new Exercise
        {
            Name = dto.Name,
            Description = dto.Description,
            Category = dto.Category,
            TargetedMuscle = dto.TargetedMuscle
        };
        
        if (dto.Gif is not null && dto.Gif.Length > 0)
        {
            try
            {
                var photo = await _photoService.AddPhotoAsync(dto.Gif);
                if (string.IsNullOrEmpty(photo)) return null;
                newExercise.GifUrl = photo;
            }
            catch (Exception e)
            {
                _logger.LogWarning(e,$"Failed to save the image for the exercise {newExercise.Id}.");
                throw;
            }
        }
        _dbContext.Exercises.Add(newExercise);
        await _dbContext.SaveChangesAsync(ct);

        return new ExerciseDTO
        {
            Id = newExercise.Id,
            Name = newExercise.Name,
            Description = newExercise.Description,
            GifUrl = newExercise.GifUrl,
            Category = newExercise.Category,
            TargetedMuscle = newExercise.TargetedMuscle
        };
    }

    public async Task<bool> DeleteExerciseAsync(Guid id, CancellationToken ct = default)
    {
        var result = await _dbContext.Exercises
            .FirstOrDefaultAsync(exercise => exercise.Id == id,ct);
        
        if (result is null) return false;
        
        _dbContext.Exercises.Remove(result);
        await _dbContext.SaveChangesAsync(ct);

        return true;
    }

    public async Task<ExerciseDTO?> SearchExerciseByNameAsync(string name, CancellationToken ct = default)
    {
        var result = await _dbContext.Exercises
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Name == name, ct);
        
        if (result is null) return null;

        return new ExerciseDTO
        {
            Name = result.Name,
            Description = result.Description,
            Category = result.Category,
            TargetedMuscle = result.TargetedMuscle
        };
    } 

    public async Task<List<ExerciseDTO>> AllExercisesAsync(int pageSize,int pageNumber,
        CancellationToken ct = default)
    {
        if (pageSize == 0) return new List<ExerciseDTO>();
        
        var result = await _dbContext.Exercises
            .AsNoTracking()
            .Skip((pageNumber-1)*pageSize)
            .Take(pageSize)
            .Select(e => new ExerciseDTO
            {
                Name = e.Name,
                Description = e.Description,
                Category = e.Category,
                TargetedMuscle = e.TargetedMuscle
            })
            .ToListAsync(ct);

        return result;
    }

    public async Task<List<ExerciseDTO>> SearchExercisesByCategoryAsync(ExerciseType category,
        CancellationToken ct = default)
    {
        var result = await _dbContext.Exercises
            .AsNoTracking()
            .Where(e => e.Category == category)
            .Select(e => new ExerciseDTO
            {
                Name = e.Name,
                Description = e.Description,
                Category = e.Category,
                TargetedMuscle = e.TargetedMuscle
            })
            .ToListAsync(ct);
        
        return result;
    }//refactor mai tz sa specific si nr exercitii

    public async Task<List<ExerciseDTO>> SearchExercisesByTargetedMuscleAsync(TargetedMuscle targetedMuscle,
        CancellationToken ct = default)
    {
        var result = await _dbContext.Exercises
            .AsNoTracking()
            .Where(e => e.TargetedMuscle == targetedMuscle)
            .Select(e => new ExerciseDTO
            {
                Name = e.Name,
                Description = e.Description,
                Category = e.Category,
                TargetedMuscle = e.TargetedMuscle
            })
            .ToListAsync(ct);

        return result;
    }//si aici tot

    public async Task<ExerciseDTO?> EditExerciseAsync(Guid id, ExerciseAddDTO dto, CancellationToken ct = default)
    {
        var exercise = await _dbContext.Exercises
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        if (exercise is null) return null;

        exercise.Name = dto.Name;
        exercise.Description = dto.Description;
        exercise.Category = dto.Category;
        exercise.TargetedMuscle = dto.TargetedMuscle;

        if (dto.Gif is not null && dto.Gif.Length > 0)
        {

            if (!string.IsNullOrEmpty(exercise.GifUrl))
            {
                try
                {

                    var uri = new Uri(exercise.GifUrl);
                    var fileName = System.IO.Path.GetFileNameWithoutExtension(uri.LocalPath);
                    var publicId = $"appgymapi/{fileName}";
                    await _photoService.DeletePhotoAsync(publicId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete old image {ImageUrl} for user {UserId}", exercise.GifUrl,
                        id);
                }
            }

            var gifUrl = await _photoService.AddPhotoAsync(dto.Gif);

            if (!string.IsNullOrEmpty(gifUrl))
            {
                exercise.GifUrl = gifUrl;
            }

        }

        try
        {
            await _dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error while updating exercise {ExerciseId}", id);
            throw;
        }

        return new ExerciseDTO
        {
            Name = exercise.Name,
            Description = exercise.Description,
            Category = exercise.Category,
            TargetedMuscle = exercise.TargetedMuscle
        };
    }
}