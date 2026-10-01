using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace AppGymAPI.DTOs;

public class RegisterUserDTO
{
    public IFormFile? ProfileImage { get; set; }
    
    [Required]
    [StringLength(maximumLength:60,MinimumLength = 8,ErrorMessage = "Name length should not be greater than {1} or less than {2}")]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [StringLength(60,ErrorMessage = "Name length should not be greater than 60")]
    public string Surname { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress(ErrorMessage = "Not a valid email")]
    [StringLength(256,ErrorMessage = "Email cannot be longer than {1} characters.")]
    public string Email { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    [StringLength(100,MinimumLength = 8, ErrorMessage = "Password must be between {2} and {1} characters.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$", 
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character.")]
    public string Password { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Please confirm your password")]
    [DataType(DataType.Password)]
    [StringLength(100,MinimumLength = 8,ErrorMessage = "Password must be between {2} and {1} characters.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$", 
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character.")]
    [Compare("Password",ErrorMessage = "The passwords do not match.")]
    public string ConfirmPassword { get; set; }
    
}