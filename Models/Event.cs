namespace AppGymAPI.Models;

public enum AccessType
{
    Public,
    Private
};
public class Event
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;
    
    public AccessType AccessType { get; set; } = AccessType.Private;

    public string Password { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime ScheduledTime { get; set; }

    public Guid? HostId { get; set; }

    public User? Host { get; set; }

    public Guid WorkoutTemplateId { get; set; }

    public Workout? WorkoutTemplate { get; set; }

    public List<EventParticipant> Participants { get; set; } = new();
}