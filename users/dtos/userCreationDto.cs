using System.ComponentModel.DataAnnotations;

namespace project.users.dto
{
    public class userCreationDto : credentialsDto
    {
        [Required, StringLength(64)]
        public string userName { get; set; } = null!;
    }
}
