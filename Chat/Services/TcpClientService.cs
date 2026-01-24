using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using ChatApp.Models;

namespace ChatApp.Services
{
    public class TcpClientService
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private Thread _receiveThread;
        private bool _isConnected;
        private int _userId;

        public event Action<int> MessageDeleted;
        public event Action<Message> MessageReceived;
        public event Action<int, string> UserStatusChanged;
        public event Action<string> ConnectionStatusChanged;
        public event Action<int, string> MessageStatusChanged;

        public void Connect(int userId, string serverIp = "127.0.0.1", int port = 8888)
        {
            try
            {
                _userId = userId;
                _client = new TcpClient();
                _client.Connect(serverIp, port);
                _stream = _client.GetStream();
                _isConnected = true;

                Console.WriteLine($"[TCP-CLIENT] Подключение к серверу {serverIp}:{port} для пользователя {userId}");

                // Отправляем авторизацию
                Authenticate(userId);

                _receiveThread = new Thread(ReceiveMessages);
                _receiveThread.IsBackground = true;
                _receiveThread.Start();

                ConnectionStatusChanged?.Invoke("connected");
                Console.WriteLine($"[TCP-CLIENT] Успешно подключен к серверу");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TCP-CLIENT] Ошибка подключения: {ex.Message}");
                ConnectionStatusChanged?.Invoke("disconnected");
            }
        }

        public void Disconnect()
        {
            _isConnected = false;
            try
            {
                _stream?.Close();
                _client?.Close();
                _receiveThread?.Join(1000);
            }
            catch { }

            ConnectionStatusChanged?.Invoke("disconnected");
            Console.WriteLine("[TCP-CLIENT] Отключен");
        }

        private void Authenticate(int userId)
        {
            var authData = new { type = "auth", userId };
            string json = JsonConvert.SerializeObject(authData);
            Send(json);
        }

        public void SendMessage(Message message)
        {
            try
            {
                var messageData = new
                {
                    type = "message",
                    id = message.Id,
                    senderId = message.SenderId,
                    receiverId = message.ReceiverId,
                    content = message.Content,
                    messageType = message.MessageType,
                    fileName = message.FileName,
                    createdAt = message.CreatedAt,
                    fileData = message.FileData
                };

                string json = JsonConvert.SerializeObject(messageData);
                Console.WriteLine($"[TCP-CLIENT] Отправка сообщения: ID={message.Id}, От={message.SenderId}, Кому={message.ReceiverId}");
                Send(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TCP-CLIENT] Ошибка отправки сообщения: {ex.Message}");
            }
        }

        public void SendDeleteMessage(int messageId, int receiverId)
        {
            if (!IsConnected()) return;

            try
            {
                var deleteData = new
                {
                    type = "message_delete",
                    messageId = messageId,
                    receiverId = receiverId,
                    timestamp = DateTime.Now
                };

                string json = JsonConvert.SerializeObject(deleteData);
                Console.WriteLine($"[TCP-CLIENT] Отправка удаления: MessageID={messageId}");
                Send(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TCP-CLIENT] Ошибка отправки удаления: {ex.Message}");
            }
        }

        public void UpdateStatus(int userId, string status)
        {
            var statusData = new { type = "status", userId, status };
            string json = JsonConvert.SerializeObject(statusData);
            Send(json);
        }

        private void Send(string json)
        {
            if (!_isConnected || _stream == null)
            {
                Console.WriteLine("[TCP-CLIENT] Не подключен, отправка невозможна");
                return;
            }

            try
            {
                string messageWithDelimiter = json + Environment.NewLine;
                byte[] data = Encoding.UTF8.GetBytes(messageWithDelimiter);
                _stream.Write(data, 0, data.Length);
                Console.WriteLine($"[TCP-CLIENT] Данные отправлены ({data.Length} байт)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TCP-CLIENT] Ошибка отправки: {ex.Message}");
                Disconnect();
            }
        }

        private void ReceiveMessages()
        {
            byte[] buffer = new byte[4096];
            StringBuilder messageBuilder = new StringBuilder();

            Console.WriteLine("[TCP-CLIENT] Поток приема сообщений запущен");

            while (_isConnected)
            {
                try
                {
                    int bytesRead = _stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        Console.WriteLine("[TCP-CLIENT] Поток закрыт (0 байт)");
                        break;
                    }

                    string chunk = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    messageBuilder.Append(chunk);

                    string allData = messageBuilder.ToString();
                    int newLineIndex;

                    while ((newLineIndex = allData.IndexOf(Environment.NewLine)) >= 0)
                    {
                        string json = allData.Substring(0, newLineIndex);
                        allData = allData.Substring(newLineIndex + Environment.NewLine.Length);

                        if (!string.IsNullOrEmpty(json))
                        {
                            Console.WriteLine($"[TCP-CLIENT] Получен JSON: {json.Substring(0, Math.Min(json.Length, 100))}...");
                            HandleReceivedData(json);
                        }
                    }

                    messageBuilder.Clear();
                    messageBuilder.Append(allData);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[TCP-CLIENT] Ошибка чтения: {ex.Message}");
                    break;
                }
            }

            Console.WriteLine("[TCP-CLIENT] Поток приема завершен");
            Disconnect();
        }

        private void HandleReceivedData(string json)
        {
            try
            {
                Console.WriteLine($"[TCP-CLIENT] =========== ПРИНЯТО СООБЩЕНИЕ ===========");
                Console.WriteLine($"[TCP-CLIENT] JSON: {json}");

                dynamic data = JsonConvert.DeserializeObject(json);
                string type = data.type;

                Console.WriteLine($"[TCP-CLIENT] Тип: {type}");

                switch (type)
                {
                    case "message":
                        try
                        {
                            var message = JsonConvert.DeserializeObject<Message>(json);
                            Console.WriteLine($"[TCP-CLIENT] Десериализовано сообщение:");
                            Console.WriteLine($"[TCP-CLIENT]   ID: {message.Id}");
                            Console.WriteLine($"[TCP-CLIENT]   От: {message.SenderId}");
                            Console.WriteLine($"[TCP-CLIENT]   Кому: {message.ReceiverId}");
                            Console.WriteLine($"[TCP-CLIENT]   Текст: {message.Content}");

                            // Проверяем подписки
                            if (MessageReceived == null)
                            {
                                Console.WriteLine($"[TCP-CLIENT] ОШИБКА: Нет подписчиков на MessageReceived!");
                                return;
                            }

                            Console.WriteLine($"[TCP-CLIENT] Вызываю событие MessageReceived...");
                            MessageReceived?.Invoke(message);
                            Console.WriteLine($"[TCP-CLIENT] Событие MessageReceived вызвано");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[TCP-CLIENT] Ошибка десериализации: {ex.Message}");
                        }
                        break;

                        // ... остальные case
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TCP-CLIENT] Критическая ошибка: {ex.Message}");
            }
        }

        public void SendStatusUpdate(string json)
        {
            Console.WriteLine($"[TCP-CLIENT] Отправка статуса: {json}");
            Send(json);
        }

        public bool IsConnected()
        {
            return _isConnected && _client != null && _client.Connected;
        }
    }
}