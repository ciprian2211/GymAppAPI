using AppGymAPI.DTOs;
using AppGymAPI.Models;

namespace AppGymAPI.Services;

public interface IExerciseService
{
    Task<ExerciseDto?> AddExerciseAsync(ExerciseAddDto dto, CancellationToken ct = default);
    
    Task<bool> DeleteExerciseAsync(Guid id, CancellationToken ct = default);
    
    Task<ExerciseDto?> SearchExerciseByNameAsync(string name, CancellationToken ct = default);

    Task<List<ExerciseDto>> AllExercisesAsync(int pageSize,int pageNumber,
        CancellationToken ct = default);

    Task<List<ExerciseDto>> SearchExercisesByCategoryAsync(ExerciseType category, CancellationToken ct = default);

    Task<List<ExerciseDto>> SearchExercisesByTargetedMuscleAsync(TargetedMuscle targetedMuscle,
        CancellationToken ct = default);

    Task<ExerciseDto?> EditExerciseAsync(Guid id, ExerciseAddDto dto,
        CancellationToken ct = default);
    
    Task<ExerciseDto?> SearchExerciseByIdAsync(Guid id, CancellationToken ct = default);
}