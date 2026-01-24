using System;
using System.Collections.Generic;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using MySql.Data.MySqlClient;
using ChatApp.Models;

namespace ChatApp.Services
{
    public class DatabaseService
    {
        private string connectionString = "server=localhost;database=chat_app;uid=root;pwd=qwerty;SslMode=None;AllowPublicKeyRetrieval=true";

        public string ConnectionString => connectionString;

        // Хеширование пароля
        public string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(password);
                byte[] hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
        public bool IsEmailAvailable(string email)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string query = "SELECT COUNT(*) FROM users WHERE email = @email";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@email", email);
                    int count = Convert.ToInt32(cmd.ExecuteScalar());
                    return count == 0;
                }
                catch
                {
                    return false;
                }
            }
        }
        // Аутентификация пользователя (ИСПРАВЛЕННЫЙ)
        public User AuthenticateUser(string email, string password)
        {
            string passwordHash = HashPassword(password);

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT * FROM users WHERE email = @email AND password_hash = @passwordHash";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@email", email);
                cmd.Parameters.AddWithValue("@passwordHash", passwordHash);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new User
                        {
                            Id = Convert.ToInt32(reader["id"]),
                            Email = reader["email"].ToString(),
                            Username = reader["username"]?.ToString() ?? "Пользователь",
                            Avatar = reader["avatar"] as byte[],
                            Status = reader["status"]?.ToString() ?? "offline",
                            Bio = reader["bio"]?.ToString(),
                            LastSeen = reader["last_seen"] == DBNull.Value ? DateTime.Now : Convert.ToDateTime(reader["last_seen"]),
                            CreatedAt = reader["created_at"] == DBNull.Value ? DateTime.Now : Convert.ToDateTime(reader["created_at"])
                        };
                    }
                }
            }
            return null;
        }

        // Регистрация пользователя (ИСПРАВЛЕННЫЙ - устанавливает last_seen)
        public int RegisterUser(string email, string password, string username)
        {
            string passwordHash = HashPassword(password);

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = @"INSERT INTO users (email, password_hash, username, last_seen, status) 
                                VALUES (@email, @passwordHash, @username, NOW(), 'offline'); 
                                SELECT LAST_INSERT_ID();";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@email", email);
                cmd.Parameters.AddWithValue("@passwordHash", passwordHash);
                cmd.Parameters.AddWithValue("@username", username);

                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        // Получение пользователя по ID (ИСПРАВЛЕННЫЙ)
        public User GetUserById(int userId)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT * FROM users WHERE id = @id";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", userId);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new User
                        {
                            Id = Convert.ToInt32(reader["id"]),
                            Email = reader["email"].ToString(),
                            Username = reader["username"]?.ToString() ?? "Пользователь",
                            Avatar = reader["avatar"] as byte[],
                            Status = reader["status"]?.ToString() ?? "offline",
                            Bio = reader["bio"]?.ToString(),
                            LastSeen = reader["last_seen"] == DBNull.Value ? DateTime.Now : Convert.ToDateTime(reader["last_seen"]),
                            CreatedAt = reader["created_at"] == DBNull.Value ? DateTime.Now : Convert.ToDateTime(reader["created_at"])
                        };
                    }
                }
            }
            return null;
        }
        // Метод для проверки, занято ли имя пользователя
        public bool IsUsernameTaken(string username)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string query = "SELECT COUNT(*) FROM users WHERE username = @username";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@username", username);
                    int count = Convert.ToInt32(cmd.ExecuteScalar());
                    return count > 0;
                }
                catch
                {
                    return false;
                }
            }
        }

        // Метод для проверки, занят ли email
        public bool IsEmailTaken(string email)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string query = "SELECT COUNT(*) FROM users WHERE email = @email";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@email", email);
                    int count = Convert.ToInt32(cmd.ExecuteScalar());
                    return count > 0;
                }
                catch
                {
                    return false;
                }
            }
        }

        // Получение списка пользователей (кроме текущего) (ИСПРАВЛЕННЫЙ)
        public List<User> GetUsers(int currentUserId)
        {
            var users = new List<User>();

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT * FROM users WHERE id != @currentUserId";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@currentUserId", currentUserId);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        users.Add(new User
                        {
                            Id = Convert.ToInt32(reader["id"]),
                            Email = reader["email"].ToString(),
                            Username = reader["username"]?.ToString() ?? "Пользователь",
                            Avatar = reader["avatar"] as byte[],
                            Status = reader["status"]?.ToString() ?? "offline",
                            Bio = reader["bio"]?.ToString(),
                            LastSeen = reader["last_seen"] == DBNull.Value ? DateTime.Now : Convert.ToDateTime(reader["last_seen"]),
                            CreatedAt = reader["created_at"] == DBNull.Value ? DateTime.Now : Convert.ToDateTime(reader["created_at"])
                        });
                    }
                }
            }
            return users;
        }

        // Отправка сообщения
        // Исправленный метод SendMessage
        public int SendMessage(Message message)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                // ДОБАВЛЕНО delivery_status
                string query = @"INSERT INTO messages 
                        (sender_id, receiver_id, content, message_type, file_data, file_name, delivery_status) 
                        VALUES (@senderId, @receiverId, @content, @type, @fileData, @fileName, 'sent');
                        SELECT LAST_INSERT_ID();";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@senderId", message.SenderId);
                cmd.Parameters.AddWithValue("@receiverId", message.ReceiverId);
                cmd.Parameters.AddWithValue("@content", message.Content);
                cmd.Parameters.AddWithValue("@type", message.MessageType);
                cmd.Parameters.AddWithValue("@fileData", message.FileData ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@fileName", message.FileName ?? (object)DBNull.Value);

                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        // Получение истории сообщений
        // Исправленный метод GetMessages
        public List<Message> GetMessages(int userId, int contactId, int limit = 50)
        {
            var messages = new List<Message>();

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = @"SELECT * FROM messages 
                WHERE ((sender_id = @userId AND receiver_id = @contactId) 
                       OR (sender_id = @contactId AND receiver_id = @userId))
                AND is_deleted = FALSE  -- ИСКЛЮЧАЕМ УДАЛЕННЫЕ СООБЩЕНИЯ
                ORDER BY created_at DESC
                LIMIT @limit";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@userId", userId);
                cmd.Parameters.AddWithValue("@contactId", contactId);
                cmd.Parameters.AddWithValue("@limit", limit);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        messages.Add(new Message
                        {
                            Id = Convert.ToInt32(reader["id"]),
                            SenderId = Convert.ToInt32(reader["sender_id"]),
                            ReceiverId = Convert.ToInt32(reader["receiver_id"]),
                            Content = reader["content"].ToString(),
                            MessageType = reader["message_type"]?.ToString() ?? "text",
                            FileData = reader["file_data"] as byte[],
                            FileName = reader["file_name"]?.ToString(),
                            IsRead = Convert.ToBoolean(reader["is_read"]),
                            IsDeleted = Convert.ToBoolean(reader["is_deleted"]),
                            DeliveryStatus = reader["delivery_status"]?.ToString() ?? "sent",
                            CreatedAt = reader["created_at"] == DBNull.Value ? DateTime.Now : Convert.ToDateTime(reader["created_at"]),
                            IsEdited = reader["is_edited"] != DBNull.Value && Convert.ToBoolean(reader["is_edited"]),
                            EditedAt = reader["edited_at"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["edited_at"]),
                            OriginalContent = reader["original_content"]?.ToString()
                        });
                    }
                }
            }
            return messages;
        }
        public bool RestoreMessage(int messageId, int userId)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // Проверяем, принадлежит ли сообщение пользователю
                    string checkQuery = "SELECT sender_id FROM messages WHERE id = @id";
                    var checkCommand = new MySqlCommand(checkQuery, connection);
                    checkCommand.Parameters.AddWithValue("@id", messageId);

                    var senderId = checkCommand.ExecuteScalar();
                    if (senderId == null || Convert.ToInt32(senderId) != userId)
                    {
                        return false; // Сообщение не принадлежит пользователю
                    }

                    // Восстанавливаем сообщение
                    string restoreQuery = @"
                UPDATE messages 
                SET is_deleted = FALSE
                WHERE id = @id";

                    var restoreCommand = new MySqlCommand(restoreQuery, connection);
                    restoreCommand.Parameters.AddWithValue("@id", messageId);

                    int rowsAffected = restoreCommand.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка восстановления сообщения: {ex.Message}");
                return false;
            }
        }

        // Обновление статуса "прочитано"
        public void MarkMessagesAsRead(int userId, int contactId)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                // ДОБАВЛЯЕМ обновление delivery_status
                string query = @"UPDATE messages 
                        SET is_read = TRUE, 
                            delivery_status = 'read'
                        WHERE receiver_id = @userId 
                        AND sender_id = @contactId 
                        AND is_read = FALSE";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@userId", userId);
                cmd.Parameters.AddWithValue("@contactId", contactId);
                cmd.ExecuteNonQuery();
            }
        }

        // Обновление профиля пользователя
        public void UpdateUserProfile(User user)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = @"UPDATE users 
                                SET username = @username, 
                                    avatar = @avatar, 
                                    status = @status, 
                                    bio = @bio,
                                    last_seen = NOW()
                                WHERE id = @id";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@username", user.Username);
                cmd.Parameters.AddWithValue("@avatar", user.Avatar ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@status", user.Status);
                cmd.Parameters.AddWithValue("@bio", user.Bio ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@id", user.Id);

                cmd.ExecuteNonQuery();
            }
        }

        // Смена пароля
        public bool ChangePassword(int userId, string oldPassword, string newPassword)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                // Проверка старого пароля
                string checkQuery = "SELECT password_hash FROM users WHERE id = @id";
                MySqlCommand checkCmd = new MySqlCommand(checkQuery, conn);
                checkCmd.Parameters.AddWithValue("@id", userId);

                var currentHash = checkCmd.ExecuteScalar()?.ToString();
                if (currentHash != HashPassword(oldPassword))
                    return false;

                // Обновление пароля
                string newPasswordHash = HashPassword(newPassword);
                string updateQuery = "UPDATE users SET password_hash = @newHash WHERE id = @id";
                MySqlCommand updateCmd = new MySqlCommand(updateQuery, conn);
                updateCmd.Parameters.AddWithValue("@newHash", newPasswordHash);
                updateCmd.Parameters.AddWithValue("@id", userId);

                return updateCmd.ExecuteNonQuery() > 0;
            }
        }

        // Поиск пользователей по имени
        public List<User> SearchUsers(string searchTerm, int currentUserId)
        {
            var users = new List<User>();

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = @"SELECT * FROM users 
                                WHERE (username LIKE @searchTerm OR email LIKE @searchTerm)
                                AND id != @currentUserId";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@searchTerm", $"%{searchTerm}%");
                cmd.Parameters.AddWithValue("@currentUserId", currentUserId);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        users.Add(new User
                        {
                            Id = Convert.ToInt32(reader["id"]),
                            Username = reader["username"]?.ToString() ?? "Пользователь",
                            Email = reader["email"].ToString(),
                            Status = reader["status"]?.ToString() ?? "offline",
                            Avatar = reader["avatar"] as byte[]
                        });
                    }
                }
            }
            return users;
        }
        public int SaveReceivedMessage(Message message)
        {
            Console.WriteLine($"[DB] Сохранение полученного сообщения: ID={message.Id}");

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                // ИСПРАВЛЕННЫЙ ЗАПРОС - добавляем delivery_status
                string query = @"INSERT INTO messages 
                 (sender_id, receiver_id, content, message_type, file_data, file_name, is_read, is_deleted, created_at, delivery_status)
                 VALUES (@sender_id, @receiver_id, @content, @message_type, @file_data, @file_name, @is_read, @is_deleted, @created_at, @delivery_status);
                 SELECT LAST_INSERT_ID();";

                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@sender_id", message.SenderId);
                    command.Parameters.AddWithValue("@receiver_id", message.ReceiverId);
                    command.Parameters.AddWithValue("@content", message.Content);
                    command.Parameters.AddWithValue("@message_type", message.MessageType ?? "text");
                    command.Parameters.AddWithValue("@file_data", message.FileData != null ? (object)message.FileData : DBNull.Value);
                    command.Parameters.AddWithValue("@file_name", message.FileName ?? "");
                    command.Parameters.AddWithValue("@is_read", false);
                    command.Parameters.AddWithValue("@is_deleted", false);
                    command.Parameters.AddWithValue("@created_at", message.CreatedAt);
                    command.Parameters.AddWithValue("@delivery_status", message.DeliveryStatus ?? "sent");

                    int messageId = Convert.ToInt32(command.ExecuteScalar());
                    Console.WriteLine($"[DB] Сообщение сохранено с ID: {messageId}");
                    return messageId;
                }
            }
        }
        public void UpdateUserStatus(int userId, string status)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string query = @"UPDATE users 
                                    SET status = @status, 
                                        last_seen = CASE 
                                            WHEN @status = 'offline' THEN NOW() 
                                            ELSE last_seen 
                                        END
                                    WHERE id = @userId";

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@status", status);
                    cmd.Parameters.AddWithValue("@userId", userId);

                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка обновления статуса: {ex.Message}");
                }
            }
        }
        public bool VerifyUserPassword(int userId, string password)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string query = "SELECT password_hash FROM users WHERE id = @id";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@id", userId);

                    var dbHash = cmd.ExecuteScalar()?.ToString();
                    string inputHash = HashPassword(password);

                    return dbHash == inputHash;
                }
                catch
                {
                    return false;
                }
            }
        }
        public bool UpdateUserEmail(int userId, string newEmail)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string query = "UPDATE users SET email = @email WHERE id = @id";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@email", newEmail);
                    cmd.Parameters.AddWithValue("@id", userId);

                    int rowsAffected = cmd.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
                catch
                {
                    return false;
                }
            }
        }
        // Метод для обновления времени последней активности
        public void UpdateLastSeen(int userId)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "UPDATE users SET last_seen = NOW() WHERE id = @userId";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@userId", userId);
                cmd.ExecuteNonQuery();
            }
        }

        // Метод для получения пользователей онлайн
        public List<User> GetOnlineUsers(int currentUserId)
        {
            var users = new List<User>();

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                // Пользователи онлайн за последние 5 минут
                string query = @"SELECT * FROM users 
                        WHERE id != @currentUserId 
                        AND status = 'online' 
                        AND last_seen > DATE_SUB(NOW(), INTERVAL 5 MINUTE)
                        ORDER BY username";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@currentUserId", currentUserId);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        users.Add(new User
                        {
                            Id = Convert.ToInt32(reader["id"]),
                            Username = reader["username"]?.ToString() ?? "Пользователь",
                            Status = "online",
                            // ... остальные поля
                        });
                    }
                }
            }
            return users;
        }
        public void UpdateMessageStatus(int messageId, string status)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = @"UPDATE messages 
                        SET delivery_status = @status,
                            is_read = @isRead
                        WHERE id = @messageId";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@status", status);
                cmd.Parameters.AddWithValue("@isRead", status == "read"); // При статусе read устанавливаем is_read = true
                cmd.Parameters.AddWithValue("@messageId", messageId);

                cmd.ExecuteNonQuery();
            }
        }

        public Message GetMessageById(int messageId)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT * FROM messages WHERE id = @id";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", messageId);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new Message
                        {
                            Id = Convert.ToInt32(reader["id"]),
                            SenderId = Convert.ToInt32(reader["sender_id"]),
                            ReceiverId = Convert.ToInt32(reader["receiver_id"]),
                            Content = reader["content"].ToString(),
                            MessageType = reader["message_type"]?.ToString() ?? "text",
                            FileData = reader["file_data"] as byte[],
                            FileName = reader["file_name"]?.ToString(),
                            IsRead = Convert.ToBoolean(reader["is_read"]),
                            IsDeleted = Convert.ToBoolean(reader["is_deleted"]),
                            DeliveryStatus = reader["delivery_status"]?.ToString() ?? "sent",
                            CreatedAt = reader["created_at"] == DBNull.Value ? DateTime.Now : Convert.ToDateTime(reader["created_at"]),
                            // Новые поля
                            IsEdited = reader["is_edited"] != DBNull.Value && Convert.ToBoolean(reader["is_edited"]),
                            EditedAt = reader["edited_at"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["edited_at"]),
                            OriginalContent = reader["original_content"]?.ToString()
                        };
                    }
                }
            }
            return null;
        }
        public string GetLastMessagePreview(int userId, int contactId)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = @"SELECT content, file_name, sender_id 
                        FROM messages 
                        WHERE (sender_id = @userId AND receiver_id = @contactId) 
                           OR (sender_id = @contactId AND receiver_id = @userId)
                        AND is_deleted = FALSE
                        ORDER BY created_at DESC 
                        LIMIT 1";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@userId", userId);
                cmd.Parameters.AddWithValue("@contactId", contactId);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        string fileName = reader["file_name"]?.ToString();
                        int senderId = Convert.ToInt32(reader["sender_id"]);
                        string senderPrefix = senderId == userId ? "Вы: " : "";

                        if (!string.IsNullOrEmpty(fileName))
                        {
                            return $"{senderPrefix}📎 {fileName}";
                        }
                        else
                        {
                            string content = reader["content"].ToString();
                            return $"{senderPrefix}{content}";
                        }
                    }
                }
            }
            return "";
        }

        public int GetUnreadCount(int userId, int contactId)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = @"SELECT COUNT(*) 
                        FROM messages 
                        WHERE receiver_id = @userId 
                        AND sender_id = @contactId 
                        AND is_read = FALSE
                        AND is_deleted = FALSE";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@userId", userId);
                cmd.Parameters.AddWithValue("@contactId", contactId);

                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }
        // Получение списка контактов пользователя
        public List<User> GetContacts(int userId)
        {
            var contacts = new List<User>();

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
            SELECT u.* FROM users u
            INNER JOIN contacts c ON u.id = c.contact_id
            WHERE c.user_id = @userId
            ORDER BY u.username";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@userId", userId);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        contacts.Add(new User
                        {
                            Id = Convert.ToInt32(reader["id"]),
                            Email = reader["email"].ToString(),
                            Username = reader["username"]?.ToString() ?? "Пользователь",
                            Avatar = reader["avatar"] as byte[],
                            Status = reader["status"]?.ToString() ?? "offline",
                            Bio = reader["bio"]?.ToString(),
                            LastSeen = reader["last_seen"] == DBNull.Value ? DateTime.Now : Convert.ToDateTime(reader["last_seen"]),
                            CreatedAt = reader["created_at"] == DBNull.Value ? DateTime.Now : Convert.ToDateTime(reader["created_at"])
                        });
                    }
                }
            }
            return contacts;
        }

        // Добавление контакта
        public bool AddContact(int userId, int contactId)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                // Проверяем, не добавляем ли мы самого себя
                if (userId == contactId)
                {
                    return false;
                }

                // Проверяем, существует ли уже контакт
                string checkQuery = "SELECT COUNT(*) FROM contacts WHERE user_id = @userId AND contact_id = @contactId";
                MySqlCommand checkCmd = new MySqlCommand(checkQuery, conn);
                checkCmd.Parameters.AddWithValue("@userId", userId);
                checkCmd.Parameters.AddWithValue("@contactId", contactId);

                int existingCount = Convert.ToInt32(checkCmd.ExecuteScalar());
                if (existingCount > 0)
                {
                    return false; // Контакт уже существует
                }

                // Добавляем контакт
                string query = "INSERT INTO contacts (user_id, contact_id) VALUES (@userId, @contactId)";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@userId", userId);
                cmd.Parameters.AddWithValue("@contactId", contactId);

                return cmd.ExecuteNonQuery() > 0;
            }
        }

        // Удаление контакта
        public bool RemoveContact(int userId, int contactId)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "DELETE FROM contacts WHERE user_id = @userId AND contact_id = @contactId";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@userId", userId);
                cmd.Parameters.AddWithValue("@contactId", contactId);

                return cmd.ExecuteNonQuery() > 0;
            }
        }

        // Поиск пользователей не в контактах
        public List<User> SearchUsersNotInContacts(int userId, string searchTerm)
        {
            var users = new List<User>();

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
            SELECT u.* FROM users u
            WHERE u.id != @userId
            AND (u.username LIKE @searchTerm OR u.email LIKE @searchTerm)
            AND u.id NOT IN (
                SELECT contact_id FROM contacts WHERE user_id = @userId
            )
            ORDER BY u.username";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@userId", userId);
                cmd.Parameters.AddWithValue("@searchTerm", $"%{searchTerm}%");

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        users.Add(new User
                        {
                            Id = Convert.ToInt32(reader["id"]),
                            Email = reader["email"].ToString(),
                            Username = reader["username"]?.ToString() ?? "Пользователь",
                            Avatar = reader["avatar"] as byte[],
                            Status = reader["status"]?.ToString() ?? "offline",
                            Bio = reader["bio"]?.ToString()
                        });
                    }
                }
            }
            return users;
        }
        // Получение настроек уведомлений
        public NotificationSettings GetNotificationSettings(int userId)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT * FROM user_notification_settings WHERE user_id = @userId";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@userId", userId);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new NotificationSettings
                        {
                            UserId = userId,
                            Enabled = Convert.ToBoolean(reader["enabled"]),
                            ShowPreview = Convert.ToBoolean(reader["show_preview"]),
                            PlaySound = Convert.ToBoolean(reader["play_sound"]),
                            OnlyBanner = Convert.ToBoolean(reader["only_banner"]),
                            SmartNotifications = Convert.ToBoolean(reader["smart_notifications"])
                        };
                    }
                    else
                    {
                        // Создаем настройки по умолчанию
                        var defaultSettings = new NotificationSettings { UserId = userId };
                        SaveNotificationSettings(defaultSettings);
                        return defaultSettings;
                    }
                }
            }
        }

        // Сохранение настроек уведомлений
        public void SaveNotificationSettings(NotificationSettings settings)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
            INSERT INTO user_notification_settings 
            (user_id, enabled, show_preview, play_sound, only_banner, smart_notifications) 
            VALUES (@userId, @enabled, @showPreview, @playSound, @onlyBanner, @smartNotifications)
            ON DUPLICATE KEY UPDATE 
            enabled = @enabled,
            show_preview = @showPreview,
            play_sound = @playSound,
            only_banner = @onlyBanner,
            smart_notifications = @smartNotifications";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@userId", settings.UserId);
                cmd.Parameters.AddWithValue("@enabled", settings.Enabled);
                cmd.Parameters.AddWithValue("@showPreview", settings.ShowPreview);
                cmd.Parameters.AddWithValue("@playSound", settings.PlaySound);
                cmd.Parameters.AddWithValue("@onlyBanner", settings.OnlyBanner);
                cmd.Parameters.AddWithValue("@smartNotifications", settings.SmartNotifications);

                cmd.ExecuteNonQuery();
            }
        }
        // Проверка, является ли пользователь контактом
        public bool IsContact(int userId, int contactId)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT COUNT(*) FROM contacts WHERE user_id = @userId AND contact_id = @contactId";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@userId", userId);
                cmd.Parameters.AddWithValue("@contactId", contactId);

                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }
        public bool UpdateMessage(int messageId, string newContent)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // Получаем текущее сообщение
                    string getQuery = "SELECT content, is_edited, original_content FROM messages WHERE id = @id";
                    var getCommand = new MySqlCommand(getQuery, connection);
                    getCommand.Parameters.AddWithValue("@id", messageId);

                    using (var reader = getCommand.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string currentContent = reader["content"].ToString();
                            bool isEdited = reader["is_edited"] != DBNull.Value && Convert.ToBoolean(reader["is_edited"]);
                            string originalContent = reader["original_content"]?.ToString();

                            // Проверяем, изменился ли текст (игнорируем пробелы в начале/конце)
                            if (currentContent?.Trim() == newContent.Trim())
                            {
                                return false; // Текст не изменился
                            }

                            // Проверяем, не пустое ли сообщение
                            if (string.IsNullOrWhiteSpace(newContent))
                            {
                                return false;
                            }

                            reader.Close();

                            // Обновляем сообщение
                            string updateQuery = @"
                        UPDATE messages 
                        SET content = @content, 
                            is_edited = TRUE, 
                            edited_at = @editedAt,
                            original_content = CASE 
                                WHEN @isEdited = FALSE THEN @originalContent 
                                ELSE original_content 
                            END
                        WHERE id = @id";

                            var updateCommand = new MySqlCommand(updateQuery, connection);
                            updateCommand.Parameters.AddWithValue("@content", newContent);
                            updateCommand.Parameters.AddWithValue("@editedAt", DateTime.Now);
                            updateCommand.Parameters.AddWithValue("@isEdited", isEdited);
                            updateCommand.Parameters.AddWithValue("@originalContent", originalContent ?? currentContent);
                            updateCommand.Parameters.AddWithValue("@id", messageId);

                            int rowsAffected = updateCommand.ExecuteNonQuery();
                            return rowsAffected > 0;
                        }
                        else
                        {
                            // Сообщение не найдено
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления сообщения: {ex.Message}");
                return false;
            }
        }
        public bool SoftDeleteMessage(int messageId, int userId)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // ПРОВЕРЯЕМ корректно
                    string checkQuery = "SELECT sender_id FROM messages WHERE id = @id";
                    var checkCommand = new MySqlCommand(checkQuery, connection);
                    checkCommand.Parameters.AddWithValue("@id", messageId);

                    object senderIdObj = checkCommand.ExecuteScalar();
                    if (senderIdObj == null || DBNull.Value.Equals(senderIdObj))
                    {
                        Console.WriteLine("Сообщение не найдено");
                        return false;
                    }

                    int senderId = Convert.ToInt32(senderIdObj);

                    // Разрешаем удалять ТОЛЬКО свои сообщения
                    if (senderId != userId)
                    {
                        Console.WriteLine($"Попытка удалить чужое сообщение: {senderId} != {userId}");
                        return false;
                    }

                    // МЯГКОЕ УДАЛЕНИЕ - скрываем для всех
                    string deleteQuery = @"
                UPDATE messages 
                SET is_deleted = TRUE,
                    content = '[Сообщение удалено]',
                    file_data = NULL,
                    file_name = NULL
                WHERE id = @id";

                    var deleteCommand = new MySqlCommand(deleteQuery, connection);
                    deleteCommand.Parameters.AddWithValue("@id", messageId);

                    int rowsAffected = deleteCommand.ExecuteNonQuery();

                    Console.WriteLine($"Сообщение {messageId} удалено. Затронуто строк: {rowsAffected}");
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка удаления сообщения: {ex.Message}");
                return false;
            }
        }
        public Message GetMessageBySenderReceiver(int senderId, int receiverId, string content)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = @"SELECT * FROM messages 
                WHERE sender_id = @senderId 
                AND receiver_id = @receiverId 
                AND content = @content
                ORDER BY created_at DESC 
                LIMIT 1";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@senderId", senderId);
                cmd.Parameters.AddWithValue("@receiverId", receiverId);
                cmd.Parameters.AddWithValue("@content", content);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new Message
                        {
                            Id = Convert.ToInt32(reader["id"]),
                            SenderId = Convert.ToInt32(reader["sender_id"]),
                            ReceiverId = Convert.ToInt32(reader["receiver_id"]),
                            Content = reader["content"].ToString(),
                            MessageType = reader["message_type"]?.ToString() ?? "text",
                            FileData = reader["file_data"] as byte[],
                            FileName = reader["file_name"]?.ToString(),
                            IsRead = Convert.ToBoolean(reader["is_read"]),
                            IsDeleted = Convert.ToBoolean(reader["is_deleted"]),
                            DeliveryStatus = reader["delivery_status"]?.ToString() ?? "sent",
                            CreatedAt = reader["created_at"] == DBNull.Value ? DateTime.Now : Convert.ToDateTime(reader["created_at"])
                        };
                    }
                }
            }
            return null;
        }
        public void MarkMessageAsRead(int messageId)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = @"UPDATE messages 
                SET is_read = TRUE, 
                    delivery_status = 'read'
                WHERE id = @messageId";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@messageId", messageId);
                cmd.ExecuteNonQuery();
            }
        }

    }
}