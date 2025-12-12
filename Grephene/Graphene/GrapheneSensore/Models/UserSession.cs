using System;

namespace GrapheneSensore.Models
{
    public class UserSession
    {
        public Guid SessionId { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
        public DateTime LoginTime { get; set; } = DateTime.UtcNow;
        public DateTime LastActivityTime { get; set; } = DateTime.UtcNow;
        public DateTime ExpirationTime { get; set; }
        public string? IpAddress { get; set; }
        public string? MachineName { get; set; }
        public bool IsValid()
        {
            return DateTime.UtcNow < ExpirationTime;
        }
        public void UpdateActivity(int sessionTimeoutMinutes)
        {
            LastActivityTime = DateTime.UtcNow;
            ExpirationTime = DateTime.UtcNow.AddMinutes(sessionTimeoutMinutes);
        }
    }
}
