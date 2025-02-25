namespace AngularAuth.API.Models.Dto
{
    public class TokenApiDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreeshToken { get; set; } = string.Empty;
    }
}
