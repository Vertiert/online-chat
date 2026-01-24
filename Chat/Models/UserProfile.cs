using System;

namespace ChatApp.Models
{
    public class UserProfile
    {
        public int UserId { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }
        public byte[] Avatar { get; set; }
        public string Status { get; set; } // online, offline, dnd (do not disturb)
        public string Bio { get; set; }
        public bool NotificationsEnabled { get; set; } = true;
        public bool SoundEnabled { get; set; } = true;
    }
}