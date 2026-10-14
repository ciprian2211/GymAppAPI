using AppGymAPI.DTOs;
using AppGymAPI.Models;
using AppGymAPI.Services;


namespace AppGymAPI.Endpoints;

public static class ExerciseEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/exercises");
        group.MapGet("{id:guid}", GetExerciseByIdAsync);
        group.MapGet("by-name/{name}", GetExerciseByNameAsync);
        group.MapGet("by-category/{exerciseType}", GetAllExercisesByCategoryAsync);
        group.MapGet("by-targeted-muscle/{targetedMuscle}", GetAllExercisesByTargetedMuscleAsync);
        group.MapPost("", PostExerciseAsync);
        group.MapPut("edit/{id:guid}", PutExerciseAsync);
        group.MapDelete("delete/{id:guid}", DeleteExerciseAsync);
    }

    private static async Task<IResult> PostExerciseAsync(ExerciseAddDTO request,
        IExerciseService exerciseService,
        CancellationToken ct = default)
    {
        var result = await exerciseService.AddExerciseAsync(request,ct);
        
        return result is null ? Results.BadRequest() : Results.Ok(result);
    }

    private static async Task<IResult> DeleteExerciseAsync(Guid id,IExerciseService exerciseService,
        CancellationToken ct = default)
    {
        var result = await exerciseService.DeleteExerciseAsync(id, ct);

        return result ? Results.Ok() : Results.NotFound();
    }

    private static async Task<IResult> GetExerciseByNameAsync(string name,IExerciseService exerciseService,
        CancellationToken ct = default)
    {
        var result = await exerciseService.SearchExerciseByNameAsync(name, ct);
        
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> GetAllExercisesByCategoryAsync(ExerciseType exerciseType,
        IExerciseService exerciseService, CancellationToken ct = default)
    {
        var result = await exerciseService
            .SearchExercisesByCategoryAsync(exerciseType, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetAllExercisesByTargetedMuscleAsync(TargetedMuscle targetedMuscle,
        IExerciseService exerciseService, CancellationToken ct = default)
    {
        var result = await exerciseService
            .SearchExercisesByTargetedMuscleAsync(targetedMuscle, ct);
        
        return Results.Ok(result);
    }

    private static async Task<IResult> GetExerciseByIdAsync(Guid id, IExerciseService exerciseService,
        CancellationToken ct = default)
    {
        var result = await exerciseService.SearchExerciseByIdAsync(id, ct);

        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> PutExerciseAsync(Guid id, ExerciseAddDTO request,
        IExerciseService exerciseService, CancellationToken ct = default)
    {
        var result = await exerciseService.EditExerciseAsync(id, request, ct);

        return result is null ? Results.NotFound() : Results.Ok(result);
    }
}