namespace AppGymAPI.Models;

public class Workout
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public string Name { get; set; } = string.Empty;

    public List<WorkoutExercise> WorkoutExercises { get; set; } = new();

    public User? User { get; set; }

    public Guid? UserId { get; set; }

    public Guid? OriginalTemplate { get; set; }
    
    
}