using System.ComponentModel.DataAnnotations;
using AppGymAPI.Models;

namespace AppGymAPI.DTOs;

public class ExerciseAddDTO
{
    [Required]
    [StringLength(maximumLength:60,MinimumLength = 8,ErrorMessage = "Name length should not be greater than {1} or less than {2}")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(maximumLength:400,MinimumLength = 8,ErrorMessage = "Description length should not be greater than {1} or less than {2}")]
    public string Description { get; set; } = string.Empty;
    
    public IFormFile? Gif { get; set; }
    
    [Required]
    public ExerciseType Category { get; set; } = ExerciseType.None;
    [Required]
    public TargetedMuscle TargetedMuscle { get; set; } = TargetedMuscle.None;
}