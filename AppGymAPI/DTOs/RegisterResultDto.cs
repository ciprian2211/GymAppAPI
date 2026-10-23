namespace AppGymAPI.DTOs;

public record RegisterResultDto
{
    public bool Succeeded { get; init; }
    public IEnumerable<string> Errors { get; init; } = Array.Empty<string>();
}
