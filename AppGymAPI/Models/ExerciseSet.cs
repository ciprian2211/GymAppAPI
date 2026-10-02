namespace AppGymAPI.Models;

public class ExerciseSet
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid WorkoutExerciseId { get; set; }

    public WorkoutExercise? WorkoutExercise { get; set; }
    
    public int Set { get; set; }

    public int Reps { get; set; }

    public float Weight { get; set; }
    
    public bool IsCompleted { get; set; } = false;
}