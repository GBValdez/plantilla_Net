using System.ComponentModel.DataAnnotations;
namespace project.utils.dto;

public class emailDto
{
    [Required, EmailAddress] public string email { get; set; } = string.Empty;
}
