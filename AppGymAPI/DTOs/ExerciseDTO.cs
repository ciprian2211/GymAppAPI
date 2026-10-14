using System.ComponentModel.DataAnnotations;
using AppGymAPI.Models;

namespace AppGymAPI.DTOs;

public class ExerciseDTO
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string GifUrl { get; set; } = string.Empty;
    
    public ExerciseType Category { get; set; } = ExerciseType.None;
    public TargetedMuscle TargetedMuscle { get; set; } = TargetedMuscle.None;
}