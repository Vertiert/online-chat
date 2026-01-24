using System;

namespace ChatApp.Models
{
    public class Contact
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Status { get; set; }
        public byte[] Avatar { get; set; }
        public string LastMessage { get; set; }
        public DateTime LastMessageTime { get; set; }
        public int UnreadCount { get; set; }
    }
}