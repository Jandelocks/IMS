namespace IMS.Models
{
    public class SessionModel
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public string? SessionId { get; set; }
        public string? Token { get; set; }
        public string? DeviceInfo { get; set; }
        public string? IP { get; set; }
        public DateTime LoginTime { get; set; } = DateTime.UtcNow;

        public UsersModel Users { get; set; }
    }
}
