using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using ChatApp.Models;

namespace ChatApp.Services
{
    public class TcpServerService
    {
        private TcpListener _listener;
        private Dictionary<int, TcpClient> _userClients = new Dictionary<int, TcpClient>();
        private Dictionary<TcpClient, int> _clientUsers = new Dictionary<TcpClient, int>();
        private bool _isRunning;
        private Thread _listenerThread;

        public void Start(int port = 8888)
        {
            try
            {
                _listener = new TcpListener(IPAddress.Any, port);
                _listener.Start();
                _isRunning = true;

                _listenerThread = new Thread(ListenForClients);
                _listenerThread.IsBackground = true;
                _listenerThread.Start();

                Console.WriteLine($"TCP сервер запущен на порту {port}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка запуска TCP сервера: {ex.Message}");
            }
        }

        private void HandleMessage(string json, TcpClient senderClient)
        {
            try
            {
                Console.WriteLine($"[TCP-SERVER] Обработка сообщения: {json.Substring(0, Math.Min(json.Length, 100))}...");

                dynamic data = JsonConvert.DeserializeObject(json);
                string type = data.type;

                Console.WriteLine($"[TCP-SERVER] Тип: {type}");

                switch (type)
                {
                    case "auth":
                        int userId = data.userId;
                        AuthenticateClient(userId, senderClient);
                        break;

                    case "message":
                        int receiverId = data.receiverId;
                        int senderId = data.senderId;

                        Console.WriteLine($"[TCP-SERVER] Сообщение от {senderId} к {receiverId}");

                        // Отправляем получателю
                        if (SendToUser(receiverId, json))
                        {
                            Console.WriteLine($"[TCP-SERVER] Сообщение отправлено получателю {receiverId}");
                        }
                        else
                        {
                            Console.WriteLine($"[TCP-SERVER] Получатель {receiverId} не подключен");
                        }

                        // Отправляем обратно отправителю для подтверждения
                        if (SendToUser(senderId, json))
                        {
                            Console.WriteLine($"[TCP-SERVER] Подтверждение отправлено отправителю {senderId}");
                        }

                        Console.WriteLine($"[TCP-SERVER] Сообщение от {senderId} к {receiverId} обработано");
                        break;

                    case "delivery_status":
                        int statusReceiverId = data.receiverId;
                        Console.WriteLine($"[TCP-SERVER] Статус доставки для {statusReceiverId}");
                        SendToUser(statusReceiverId, json);
                        break;

                    case "message_delete":
                        int deleteReceiverId = data.receiverId;
                        Console.WriteLine($"[TCP-SERVER] Удаление сообщения для {deleteReceiverId}");
                        SendToUser(deleteReceiverId, json);
                        break;

                    case "status":
                        int statusUserId = data.userId;
                        string status = data.status;
                        Console.WriteLine($"[TCP-SERVER] Статус пользователя {statusUserId}: {status}");
                        SendToAllUsersExcept(statusUserId, json);
                        break;

                    default:
                        Console.WriteLine($"[TCP-SERVER] Неизвестный тип: {type}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TCP-SERVER] Ошибка обработки: {ex.Message}");
            }
        }
        public void Stop()
        {
            _isRunning = false;
            try
            {
                _listener?.Stop();

                foreach (var client in _userClients.Values)
                {
                    client.Close();
                }
                _userClients.Clear();
                _clientUsers.Clear();
            }
            catch { }

            Console.WriteLine("TCP сервер остановлен");
        }

        private void ListenForClients()
        {
            while (_isRunning)
            {
                try
                {
                    TcpClient client = _listener.AcceptTcpClient();
                    Thread clientThread = new Thread(() => HandleClient(client));
                    clientThread.IsBackground = true;
                    clientThread.Start();
                }
                catch (SocketException)
                {
                    // Сервер остановлен
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при подключении клиента: {ex.Message}");
                }
            }
        }

        private void HandleClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[4096];
            StringBuilder messageBuilder = new StringBuilder();

            while (_isRunning && client.Connected)
            {
                try
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                        break;

                    string chunk = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    messageBuilder.Append(chunk);

                    // Обрабатываем все полные сообщения
                    string allData = messageBuilder.ToString();
                    int newLineIndex;
                    while ((newLineIndex = allData.IndexOf(Environment.NewLine)) >= 0)
                    {
                        string json = allData.Substring(0, newLineIndex);
                        allData = allData.Substring(newLineIndex + Environment.NewLine.Length);

                        if (!string.IsNullOrEmpty(json))
                        {
                            Console.WriteLine($"Сервер получил JSON: {json}");
                            HandleMessage(json, client);
                        }
                    }

                    // Сохраняем оставшиеся данные
                    messageBuilder.Clear();
                    messageBuilder.Append(allData);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка обработки клиента: {ex.Message}");
                    break;
                }
            }

            // Удаляем клиента при отключении
            DisconnectClient(client);
        }


        private void SendDeliveryStatus(int messageId, int userId, string status)
        {
            var statusData = new
            {
                type = "status_update",
                messageId = messageId,
                userId = userId,
                status = status
            };

            string json = JsonConvert.SerializeObject(statusData);
            SendToUser(userId, json);
        }

        private void AuthenticateClient(int userId, TcpClient client)
        {
            lock (_userClients)
            {
                _userClients[userId] = client;
                _clientUsers[client] = userId;
            }
            Console.WriteLine($"Пользователь {userId} подключен к TCP-серверу");
        }

        private void DisconnectClient(TcpClient client)
        {
            lock (_userClients)
            {
                if (_clientUsers.ContainsKey(client))
                {
                    int userId = _clientUsers[client];
                    _userClients.Remove(userId);
                    _clientUsers.Remove(client);
                    Console.WriteLine($"Пользователь {userId} отключен от TCP-сервера");
                }
            }

            try
            {
                client.Close();
            }
            catch { }
        }

        private bool SendToUser(int userId, string json)
        {
            byte[] data = Encoding.UTF8.GetBytes(json);

            lock (_userClients)
            {
                if (_userClients.ContainsKey(userId))
                {
                    try
                    {
                        TcpClient client = _userClients[userId];
                        NetworkStream stream = client.GetStream();
                        stream.Write(data, 0, data.Length);
                        Console.WriteLine($"Сообщение отправлено пользователю {userId}");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка отправки пользователю {userId}: {ex.Message}");
                        return false;
                    }
                }
                else
                {
                    Console.WriteLine($"Пользователь {userId} не подключен к TCP-серверу");
                    return false;
                }
            }
        }

        public void SendToAllUsersExcept(int exceptUserId, string json)
        {
            byte[] data = Encoding.UTF8.GetBytes(json);

            lock (_userClients)
            {
                foreach (var kvp in _userClients)
                {
                    if (kvp.Key != exceptUserId)
                    {
                        try
                        {
                            NetworkStream stream = kvp.Value.GetStream();
                            stream.Write(data, 0, data.Length);
                        }
                        catch { }
                    }
                }
            }
        }
    }
}