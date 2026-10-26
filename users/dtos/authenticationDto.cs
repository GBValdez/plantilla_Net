namespace project.users.dto
{
    public class authenticationDto
    {
        public string token { get; set; } = string.Empty;
        public DateTime expiration { get; set; }
    }
}
