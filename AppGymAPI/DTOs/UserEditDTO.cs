using System.ComponentModel.DataAnnotations;
using AppGymAPI.Models;

namespace AppGymAPI.DTOs;

public class UserEditDTO
{
    public Guid Id { get; set; }
    public IFormFile? ProfileImage { get; set; }
    
    [Required]
    [StringLength(maximumLength:60,MinimumLength = 8,ErrorMessage = "Name length should not be greater than {1} or less than {2}")]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [StringLength(60,ErrorMessage = "Name length should not be greater than 60")]
    public string Surname { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;


}