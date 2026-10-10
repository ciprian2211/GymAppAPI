using AppGymAPI.DTOs;
using AppGymAPI.Models;

namespace AppGymAPI.Services;

public interface IExerciseService
{
    Task<ExerciseDTO?> AddExerciseAsync(ExerciseAddDTO dto, CancellationToken ct = default);
    
    Task<bool> DeleteExerciseAsync(Guid id, CancellationToken ct = default);
    
    Task<ExerciseDTO?> SearchExerciseByNameAsync(string name, CancellationToken ct = default);

    Task<List<ExerciseDTO>> AllExercisesAsync(int pageSize,int pageNumber,
        CancellationToken ct = default);

    Task<List<ExerciseDTO>> SearchExercisesByCategoryAsync(ExerciseType category, CancellationToken ct = default);

    Task<List<ExerciseDTO>> SearchExercisesByTargetedMuscleAsync(TargetedMuscle targetedMuscle,
        CancellationToken ct = default);

    Task<ExerciseDTO?> EditExerciseAsync(Guid id, ExerciseAddDTO dto,
        CancellationToken ct = default);

}