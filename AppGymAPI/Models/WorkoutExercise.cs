namespace AppGymAPI.Models;

public class WorkoutExercise
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid WorkoutId { get; set; }
    
    public Workout? Workout { get; set; }
    
    public Guid ExerciseId { get; set; }

    public Exercise? Exercise { get; set; }

    public List<ExerciseSet> Sets { get; set; } = new();
    
}