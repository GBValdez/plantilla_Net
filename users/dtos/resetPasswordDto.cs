using System.ComponentModel.DataAnnotations;
namespace project.users.dto;

public class resetPasswordDto
{
    [Required] public string email { get; set; } = null!;
    [Required, MinLength(8)] public string password { get; set; } = null!;
    [Required] public string token { get; set; } = null!;
}
