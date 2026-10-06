using AppGymAPI.Models;

namespace AppGymAPI.DTOs;

public record UserDTO
{
    public Guid Id { get; set; }
    
    public string Name { get; set; } = string.Empty;

    public string Surname { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public Role Role { get; set; } 
};