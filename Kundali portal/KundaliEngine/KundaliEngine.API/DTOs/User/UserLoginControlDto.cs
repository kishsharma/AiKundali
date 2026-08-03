namespace KundaliEngine.API.DTOs.User
{
    public class UserLoginControlDto
    {
        public Guid Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }

        public bool IsActive { get; set; }

        public int KundaliSaveLimit { get; set; }
    }
}
