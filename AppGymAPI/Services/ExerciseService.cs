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

    public async Task<ExerciseDto?> AddExerciseAsync(ExerciseAddDto dto, CancellationToken ct = default)
    {
        
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
                var photo = await _photoService.AddGifAsync(dto.Gif);
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

        return new ExerciseDto
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

        if (!string.IsNullOrEmpty(result.GifUrl))
        {
            try
            {
                await _photoService.DeletePhotoByUrlAsync(result.GifUrl);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete old image {ImageUrl} for exercise {ExerciseId}", result.GifUrl, id);
            }
        }
        
        _dbContext.Exercises.Remove(result);

        try
        {
            await _dbContext.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Failed to delete exercise {ExerciseId} due to database update constraint or conflict", id);
            return false;
        }
    }

    public async Task<ExerciseDto?> SearchExerciseByNameAsync(string name, CancellationToken ct = default)
    {
        var result = await _dbContext.Exercises
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Name == name, ct);
        
        if (result is null) return null;

        return new ExerciseDto
        {
            Id = result.Id,
            Name = result.Name,
            Description = result.Description,
            GifUrl = result.GifUrl,
            Category = result.Category,
            TargetedMuscle = result.TargetedMuscle
        };
    } 

    public async Task<List<ExerciseDto>> AllExercisesAsync(int pageSize, int pageNumber,
        CancellationToken ct = default)
    {
        if (pageSize <= 0) return new List<ExerciseDto>();
        var safePageNumber = Math.Max(1, pageNumber);

        var result = await _dbContext.Exercises
            .AsNoTracking()
            .Skip((safePageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new ExerciseDto
            {
                Id = e.Id,
                Name = e.Name,
                Description = e.Description,
                GifUrl = e.GifUrl,
                Category = e.Category,
                TargetedMuscle = e.TargetedMuscle
            })
            .ToListAsync(ct);

        return result;
    }

    public async Task<List<ExerciseDto>> SearchExercisesByCategoryAsync(ExerciseType category,
        CancellationToken ct = default)
    {
        var result = await _dbContext.Exercises
            .AsNoTracking()
            .Where(e => e.Category == category)
            .Select(e => new ExerciseDto
            {
                Id = e.Id,
                Name = e.Name,
                Description = e.Description,
                GifUrl = e.GifUrl,
                Category = e.Category,
                TargetedMuscle = e.TargetedMuscle
            })
            .ToListAsync(ct);
        
        return result;
    }//refactor mai tz sa specific si nr exercitii

    public async Task<List<ExerciseDto>> SearchExercisesByTargetedMuscleAsync(TargetedMuscle targetedMuscle,
        CancellationToken ct = default)
    {
        var result = await _dbContext.Exercises
            .AsNoTracking()
            .Where(e => e.TargetedMuscle == targetedMuscle)
            .Select(e => new ExerciseDto
            {
                Id = e.Id,
                Name = e.Name,
                Description = e.Description,
                GifUrl = e.GifUrl,
                Category = e.Category,
                TargetedMuscle = e.TargetedMuscle
            })
            .ToListAsync(ct);

        return result;
    }//si aici tot

    public async Task<ExerciseDto?> EditExerciseAsync(Guid id, ExerciseAddDto dto, CancellationToken ct = default)
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
                    await _photoService.DeletePhotoByUrlAsync(exercise.GifUrl);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete old image {ImageUrl} for exercise {ExerciseId}", exercise.GifUrl, id);
                }
            }

            var gifUrl = await _photoService.AddGifAsync(dto.Gif);

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

        return new ExerciseDto
        {
            Id = exercise.Id,
            Name = exercise.Name,
            Description = exercise.Description,
            GifUrl = exercise.GifUrl,
            Category = exercise.Category,
            TargetedMuscle = exercise.TargetedMuscle
        };
    }

    public async Task<ExerciseDto?> SearchExerciseByIdAsync(Guid id, CancellationToken ct = default)
    {
        var result = await _dbContext.Exercises
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id,ct);
        
        if (result is null) return null;

        return new ExerciseDto
        {
            Id = result.Id,
            Name = result.Name,
            Description = result.Description,
            GifUrl = result.GifUrl,
            Category = result.Category,
            TargetedMuscle = result.TargetedMuscle
        };
    }
}