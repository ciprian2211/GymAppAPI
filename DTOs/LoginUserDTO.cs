using System.ComponentModel.DataAnnotations;

namespace AppGymAPI.DTOs;

public class LoginUserDTO
{
    [Required]
    [EmailAddress(ErrorMessage = "Must be a valid email.")]
    [MaxLength(256,ErrorMessage = "Email length must not be greater than {1}")]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [DataType(DataType.Password)]
    [StringLength(256,MinimumLength = 8,ErrorMessage = "Password must not be greater than {1} or smaller than {2}")]
    public string Password { get; set; } = string.Empty;
}