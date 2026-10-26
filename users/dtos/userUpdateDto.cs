using System.ComponentModel.DataAnnotations;
namespace project.users.dto;

public class userUpdateDto
{
    public bool status { get; set; }
    [Required] public List<string> roles { get; set; } = [];
}
