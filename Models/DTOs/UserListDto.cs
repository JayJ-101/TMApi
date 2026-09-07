namespace TMApi.Models  
{
    public class UserListDto
    {
        public string Id { get; set; } = string.Empty;
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? FullName { get; set; }
        public bool IsActive { get; set; }
        public IList<string> Roles { get; set; } = new List<string>();
    }
}
