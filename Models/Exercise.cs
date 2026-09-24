namespace AppGymAPI.Models;

public enum ExerciseType
{
    None,
    Weightlifting,
    Bodyweight,
    Cardio,
    Stretching
}

public enum TargetedMuscle
{
    None,
    Neck,
    Traps,
    Shoulders,
    Biceps,
    Triceps,
    Forearms,
    Chest,
    Abs,
    Lats,
    Lowerback,
    Upperback,
    Glutes,
    Quadriceps,
    Hamstrings,
    Calves,
}
public class Exercise
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string GifUrl { get; set; } = string.Empty;
    
    public ExerciseType Category { get; set; } = ExerciseType.None;

    public TargetedMuscle TargetedMuscle { get; set; } = TargetedMuscle.None;

}