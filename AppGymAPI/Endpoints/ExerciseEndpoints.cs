using AppGymAPI.DTOs;
using AppGymAPI.Extensions;
using AppGymAPI.Models;
using AppGymAPI.Services;

namespace AppGymAPI.Endpoints;

public static class ExerciseEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/exercises").RequireAuthorization();
        group.MapGet("", GetAllExercisesAsync);
        group.MapGet("{id:guid}", GetExerciseByIdAsync);
        group.MapGet("by-name/{name}", GetExerciseByNameAsync);
        group.MapGet("by-category/{exerciseType}", GetAllExercisesByCategoryAsync);
        group.MapGet("by-targeted-muscle/{targetedMuscle}", GetAllExercisesByTargetedMuscleAsync);
        group.MapPost("", PostExerciseAsync)
            .RequireRoles(Role.Admin);
        group.MapPut("{id:guid}", PutExerciseAsync)
            .RequireRoles(Role.Admin);
        group.MapDelete("{id:guid}", DeleteExerciseAsync)
            .RequireRoles(Role.Admin);
    }

    private static async Task<IResult> GetAllExercisesAsync(
        IExerciseService exerciseService,
        int pageSize = 10,
        int pageNumber = 1,
        CancellationToken ct = default)
    {
        var result = await exerciseService.AllExercisesAsync(pageSize, pageNumber, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> PostExerciseAsync(ExerciseAddDto request,
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

    private static async Task<IResult> GetAllExercisesByCategoryAsync(ExerciseType exerciseType,int? pageSize,
        int? pageNumber, IExerciseService exerciseService, CancellationToken ct = default)
    {
        int size = pageSize.GetValueOrDefault(10);
        int number = pageNumber.GetValueOrDefault(1);
        
        var result = await exerciseService
            .SearchExercisesByCategoryAsync(exerciseType,size,number, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetAllExercisesByTargetedMuscleAsync(TargetedMuscle targetedMuscle,int? pageSize,
        int? pageNumber, IExerciseService exerciseService, CancellationToken ct = default)
    {
        int size = pageSize.GetValueOrDefault(10);
        int number = pageNumber.GetValueOrDefault(1);
        
        var result = await exerciseService
            .SearchExercisesByTargetedMuscleAsync(targetedMuscle,size,number, ct);
        
        return Results.Ok(result);
    }

    private static async Task<IResult> GetExerciseByIdAsync(Guid id, IExerciseService exerciseService,
        CancellationToken ct = default)
    {
        var result = await exerciseService.SearchExerciseByIdAsync(id, ct);

        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> PutExerciseAsync(Guid id, ExerciseAddDto request,
        IExerciseService exerciseService, CancellationToken ct = default)
    {
        var result = await exerciseService.EditExerciseAsync(id, request, ct);

        return result is null ? Results.NotFound() : Results.Ok(result);
    }
}