using System;

namespace ChatApp.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string Username { get; set; }
        public byte[] Avatar { get; set; }
        public string Status { get; set; } = "offline";
        public string Bio { get; set; }
        public DateTime LastSeen { get; set; }
        public DateTime CreatedAt { get; set; }

        // Конструктор по умолчанию
        public User()
        {
            LastSeen = DateTime.Now;
            CreatedAt = DateTime.Now;
            Username = "Пользователь";
            Status = "offline";
        }
    }
}