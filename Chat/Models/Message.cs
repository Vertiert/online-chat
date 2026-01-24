using System;
using Newtonsoft.Json;

namespace ChatApp.Models
{
    public class Message
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public string Content { get; set; }
        public string MessageType { get; set; } = "text";
        public byte[] FileData { get; set; }
        public string FileName { get; set; }
        public bool IsRead { get; set; }
        public bool IsDeleted { get; set; }

        // Поля для редактирования (ДОБАВЬТЕ ЭТИ СТРОКИ)
        public bool IsEdited { get; set; }
        public DateTime? EditedAt { get; set; }
        public string OriginalContent { get; set; }

        [JsonProperty("deliveryStatus")]
        public string DeliveryStatus { get; set; } = "sent"; // sent, delivered, read

        public DateTime CreatedAt { get; set; }

        public Message()
        {
            CreatedAt = DateTime.Now;
        }
    }
}