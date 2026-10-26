namespace project.users.dto
{
    public class userDto
    {
        public string userName { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public Boolean isActive { get; set; }
        public List<string> roles { get; set; } = [];
    }
}
