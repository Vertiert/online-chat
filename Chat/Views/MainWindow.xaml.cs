using ChatApp.Models;
using ChatApp.Services;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ChatApp.Views
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        // Вложенный класс ContactView для отображения контактов
        // Вложенный класс ContactView для отображения контактов
        public class ContactView : INotifyPropertyChanged
        {
            private bool _isSelected;
            private int _unreadCount;
            private string _status;
            private DateTime? _lastSeen;
            public int UnreadCount
            {
                get => _unreadCount;
                set
                {
                    if (_unreadCount != value)
                    {
                        _unreadCount = value;
                        OnPropertyChanged(nameof(UnreadCount));
                        OnPropertyChanged(nameof(UnreadCountVisibility));
                    }
                }
            }
            public string LastSeenText
            {
                get
                {
                    if (!LastSeen.HasValue || Status?.ToLower() == "online")
                        return string.Empty;

                    return $"Последний раз в сети: {LastSeen.Value:HH:mm}";
                }
            }
            public DateTime? LastSeen
            {
                get => _lastSeen;
                set
                {
                    if (_lastSeen != value)
                    {
                        _lastSeen = value;
                        OnPropertyChanged(nameof(LastSeen));
                        OnPropertyChanged(nameof(LastSeenText));
                        OnPropertyChanged(nameof(StatusText));
                    }
                }
            }
            public string Status
            {
                get => _status;
                set
                {
                    if (_status != value)
                    {
                        _status = value;
                        OnPropertyChanged(nameof(Status));
                        OnPropertyChanged(nameof(StatusText));
                        OnPropertyChanged(nameof(StatusColor));
                        OnPropertyChanged(nameof(LastSeenText));
                    }
                }
            }


            public int Id { get; set; }
            public string Username { get; set; }

            public string StatusText
            {
                get
                {
                    if (Status?.ToLower() == "online")
                        return "🟢 В сети";

                    if (Status?.ToLower() == "dnd")
                        return "⛔ Не беспокоить";

                    // Для оффлайн показываем "Был(а) в ..."
                    if (LastSeen.HasValue)
                    {
                        var timeAgo = DateTime.Now - LastSeen.Value;

                        if (timeAgo.TotalMinutes < 1)
                            return "Только что";
                        if (timeAgo.TotalHours < 1)
                            return $"Был(а) {timeAgo.Minutes} мин. назад";
                        if (timeAgo.TotalDays < 1)
                            return $"Был(а) {timeAgo.Hours} ч. назад";
                        if (timeAgo.TotalDays < 7)
                            return $"Был(а) {timeAgo.Days} дн. назад";

                        return $"Был(а) {LastSeen.Value:dd.MM.yyyy}";
                    }

                    return "⚫ Не в сети";
                }
            }

            public Brush StatusColor
            {
                get
                {
                    switch (Status?.ToLower())
                    {
                        case "online":
                            return Brushes.LimeGreen;
                        case "dnd":
                            return Brushes.Red;
                        default:
                            // Для оффлайн - прозрачный или серый
                            return Brushes.Transparent;
                    }
                }
            }

            public ImageSource AvatarImage { get; set; }
            public string LastMessagePreview { get; set; }
            public Visibility UnreadCountVisibility
            {
                get => UnreadCount > 0 ? Visibility.Visible : Visibility.Collapsed;
            }

            public bool IsSelected
            {
                get => _isSelected;
                set
                {
                    if (_isSelected != value)
                    {
                        _isSelected = value;
                        OnPropertyChanged(nameof(IsSelected));
                    }
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        // Методы для получения статуса (должны быть в MainWindow)
        private static Brush GetStatusBrush(string status)
        {
            switch (status?.ToLower())
            {
                case "online": return Brushes.Green;
                case "dnd": return Brushes.Red;
                case "offline":
                default: return Brushes.Transparent;
            }
        }

        private static string GetStatusText(string status)
        {
            switch (status?.ToLower())
            {
                case "online": return "В сети";
                case "dnd": return "Не беспокоить";
                case "offline":
                default: return "Не в сети";
            }
        }

        
        private DatabaseService _dbService;
        private TcpClientService _tcpClient;
        private User _currentUser;
        private List<User> _allContacts;
        private ObservableCollection<MessageView> _messages;
        private ContactView _selectedContact;
        private bool _isTcpConnected;

        public MainWindow(User user)
        {
            InitializeComponent();

            Console.WriteLine("=== ИНИЦИАЛИЗАЦИЯ MAIN WINDOW ===");
            Console.WriteLine($"Пользователь: {user.Username} (ID: {user.Id})");

            _currentUser = user;
            _dbService = new DatabaseService();
            _notificationService = new NotificationService(_dbService, this, _currentUser.Id);

            this.Title = $"ChatApp - {user.Username}";

            _messages = new ObservableCollection<MessageView>();
            MessagesItemsControl.ItemsSource = _messages;

            _allContacts = new List<User>();
            _contacts = new ObservableCollection<ContactView>();
            ContactsItemsControl.ItemsSource = _contacts;
            _dbService.UpdateUserStatus(_currentUser.Id, "online");
            LoadContacts();
            InitializeTcpClient();

            DataContext = this;

            Console.WriteLine("=== ИНИЦИАЛИЗАЦИЯ ЗАВЕРШЕНА ===");
        }
        private NotificationService _notificationService;
        private void ContactBorder_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Console.WriteLine($"[MAIN] === ContactBorder_MouseDown ===");

            try
            {
                if (sender is Border border && border.Tag is ContactView clickedContact)
                {
                    Console.WriteLine($"[MAIN] Выбран контакт: {clickedContact.Username} (ID: {clickedContact.Id})");

                    // Снимаем выделение со всех контактов
                    foreach (var contact in _contacts)
                    {
                        contact.IsSelected = false;
                    }

                    // Выделяем текущий контакт
                    clickedContact.IsSelected = true;
                    _selectedContact = clickedContact;

                    // Обновляем UI
                    UpdateSelectedContactUI(clickedContact);

                    // Загружаем сообщения
                    Console.WriteLine($"[MAIN] Загружаю сообщения для контакта {clickedContact.Id}");
                    LoadMessages(clickedContact.Id);

                    // Сбрасываем счетчик непрочитанных
                    Console.WriteLine($"[MAIN] Сбрасываю счетчик непрочитанных");
                    ResetUnreadCount(clickedContact.Id);

                    e.Handled = true;

                    Console.WriteLine($"[MAIN] === ContactBorder_MouseDown завершен ===");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MAIN] Ошибка при выборе контакта: {ex.Message}");
            }
        }

        private void UpdateSelectedContactUI(ContactView contact)
        {
            SelectedContactName.Text = contact.Username;
            SelectedContactStatus.Text = contact.StatusText;
            SelectedContactStatusCircle.Fill = contact.StatusColor;
            SelectedContactAvatar.ImageSource = contact.AvatarImage ?? null;
        }

        // Свойство для индикатора подключения TCP
        public bool IsTcpConnected
        {
            get => _isTcpConnected;
            set
            {
                if (_isTcpConnected != value)
                {
                    _isTcpConnected = value;
                    OnPropertyChanged(nameof(IsTcpConnected));
                }
            }
        }
        private ObservableCollection<ContactView> _contacts;
        private Border _lastSelectedContactBorder;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void InitializeTcpClient()
        {
            Console.WriteLine("=== ИНИЦИАЛИЗАЦИЯ TCP КЛИЕНТА ===");

            _tcpClient = new TcpClientService();

            Console.WriteLine("Подписываюсь на события TCP...");

            // Подписываемся с логами
            _tcpClient.MessageReceived += (msg) =>
            {
                Console.WriteLine($"[MAIN] СОБЫТИЕ: MessageReceived вызвано! ID={msg.Id}");
                OnTcpMessageReceived(msg);
            };

            _tcpClient.MessageStatusChanged += (id, status) =>
            {
                Console.WriteLine($"[MAIN] СОБЫТИЕ: MessageStatusChanged {id}->{status}");
                OnMessageStatusChanged(id, status);
            };

            _tcpClient.UserStatusChanged += (id, status) =>
            {
                Console.WriteLine($"[MAIN] СОБЫТИЕ: UserStatusChanged {id}->{status}");
                OnTcpUserStatusChanged(id, status);
            };

            _tcpClient.ConnectionStatusChanged += (status) =>
            {
                Console.WriteLine($"[MAIN] СОБЫТИЕ: ConnectionStatusChanged {status}");
                OnTcpConnectionStatusChanged(status);
            };

            _tcpClient.MessageDeleted += (id) =>
            {
                Console.WriteLine($"[MAIN] СОБЫТИЕ: MessageDeleted {id}");
                OnMessageDeleted(id);
            };

            Console.WriteLine("Все события TCP подписаны");

            // Подключаемся к TCP-серверу
            Console.WriteLine($"Подключение к TCP серверу для пользователя {_currentUser.Id}");
            _tcpClient.Connect(_currentUser.Id);

            // Проверяем подключение
            Task.Delay(1000).ContinueWith(t =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (_tcpClient.IsConnected())
                    {
                        Console.WriteLine("[MAIN] TCP подключение установлено");
                        IsTcpConnected = true;
                    }
                    else
                    {
                        Console.WriteLine("[MAIN] TCP подключение не установлено");
                        IsTcpConnected = false;
                    }
                });
            });

            // Отправляем текущий статус
            _tcpClient.UpdateStatus(_currentUser.Id, _currentUser.Status);

            Console.WriteLine("=== ИНИЦИАЛИЗАЦИЯ TCP ЗАВЕРШЕНА ===");
        }

        private async void OnTcpMessageReceived(Message message)
        {
            Console.WriteLine($"[MAIN] === НАЧАЛО OnTcpMessageReceived ===");
            Console.WriteLine($"[MAIN] Сообщение: ID={message.Id}, От={message.SenderId}, Кому={message.ReceiverId}, Контент='{message.Content}'");
            Console.WriteLine($"[MAIN] Я: {_currentUser.Id}, Имя: {_currentUser.Username}");

            await Dispatcher.Invoke(async () =>
            {
                try
                {
                    Console.WriteLine($"[MAIN] Проверка в БД...");
                    var existingMessage = _dbService.GetMessageById(message.Id);

                    if (existingMessage == null)
                    {
                        Console.WriteLine($"[MAIN] Сообщение новое, сохраняю в БД");
                        int messageId = _dbService.SaveReceivedMessage(message);
                        message.Id = messageId;
                        Console.WriteLine($"[MAIN] Сообщение сохранено с ID: {messageId}");
                    }

                    // Определяем ID контакта (от кого пришло сообщение для нас)
                    int contactId = 0;
                    bool isIncomingMessage = false;

                    if (message.SenderId == _currentUser.Id)
                    {
                        // Это исходящее сообщение (мы отправили)
                        contactId = message.ReceiverId;
                        isIncomingMessage = false;
                        Console.WriteLine($"[MAIN] Исходящее сообщение для контакта: {contactId}");
                    }
                    else if (message.ReceiverId == _currentUser.Id)
                    {
                        // Это входящее сообщение (нам)
                        contactId = message.SenderId;
                        isIncomingMessage = true;
                        Console.WriteLine($"[MAIN] ВХОДЯЩЕЕ сообщение от контакта: {contactId}");
                    }
                    else
                    {
                        Console.WriteLine($"[MAIN] Сообщение не для меня, пропускаю");
                        return;
                    }

                    // Проверяем, для активного ли чата сообщение
                    bool isForCurrentChat = false;
                    if (_selectedContact != null)
                    {
                        isForCurrentChat = (_selectedContact.Id == contactId);
                        Console.WriteLine($"[MAIN] isForCurrentChat: {isForCurrentChat} (selected: {_selectedContact.Id})");
                    }
                    else
                    {
                        Console.WriteLine($"[MAIN] Нет выбранного контакта (_selectedContact == null)");
                    }

                    // Обновляем превью сообщения
                    UpdateContactLastMessage(contactId, GetMessagePreview(message));

                    // Если сообщение для текущего чата - добавляем в UI
                    if (isForCurrentChat)
                    {
                        Console.WriteLine($"[MAIN] Сообщение для текущего чата, добавляем в UI");

                        // Проверяем, есть ли уже в UI
                        var existingInUI = _messages.FirstOrDefault(m => m.Id == message.Id);
                        if (existingInUI != null)
                        {
                            Console.WriteLine($"[MAIN] Сообщение уже в UI, обновляю статус");
                            existingInUI.Status = message.DeliveryStatus ?? "sent";
                        }
                        else
                        {
                            Console.WriteLine($"[MAIN] Добавляю новое сообщение в UI");
                            AddMessageToUICollection(message, true);
                        }

                        // Если это входящее сообщение, помечаем как прочитанное
                        if (isIncomingMessage)
                        {
                            try
                            {
                                Console.WriteLine($"[MAIN] Помечаю как прочитанное в БД");
                                _dbService.MarkMessageAsRead(message.Id);

                                Console.WriteLine($"[MAIN] Отправляю статус 'read'");
                                SendDeliveryStatus(message.Id, message.SenderId, "read");

                                // Сбрасываем счетчик непрочитанных
                                ResetUnreadCount(contactId);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[MAIN] Ошибка пометки как прочитанного: {ex.Message}");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[MAIN] Сообщение НЕ для текущего чата");

                        // Если сообщение входящее - увеличиваем счетчик и показываем уведомление
                        if (isIncomingMessage)
                        {
                            Console.WriteLine($"[MAIN] ВХОДЯЩЕЕ сообщение, увеличиваю счетчик");
                            IncrementUnreadCount(contactId);

                            // Показываем уведомление
                            var senderUser = _dbService.GetUserById(message.SenderId);
                            if (senderUser != null)
                            {
                                Console.WriteLine($"[MAIN] Отправитель найден: {senderUser.Username}");

                                if (_notificationService != null)
                                {
                                    Console.WriteLine($"[MAIN] Вызываю уведомление для сообщения от {senderUser.Username}");
                                    _notificationService.ShowMessageNotification(message, senderUser);
                                }
                                else
                                {
                                    Console.WriteLine($"[MAIN] NotificationService не инициализирован!");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"[MAIN] Не удалось найти отправителя с ID: {message.SenderId}");
                            }
                        }
                    }

                    // Для входящих сообщений отправляем статус "delivered"
                    if (isIncomingMessage && message.DeliveryStatus != "delivered")
                    {
                        Console.WriteLine($"[MAIN] Отправляю статус 'delivered'");
                        _dbService.UpdateMessageStatus(message.Id, "delivered");
                        SendDeliveryStatus(message.Id, message.SenderId, "delivered");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MAIN] Критическая ошибка: {ex.Message}");
                }

                Console.WriteLine($"[MAIN] === КОНЕЦ OnTcpMessageReceived ===");
            });
        }

        private void AddMessageToUI(Message message)
        {
            AddMessageToUICollection(message, true);
        }
        private void AddMessageToActiveChatIfNeeded(Message message)
        {
            // Если нет выбранного контакта - выходим
            if (_selectedContact == null) return;

            // Проверяем, относится ли сообщение к текущему чату
            bool isForCurrentChat =
                (message.SenderId == _currentUser.Id && message.ReceiverId == _selectedContact.Id) ||
                (message.SenderId == _selectedContact.Id && message.ReceiverId == _currentUser.Id);

            if (isForCurrentChat)
            {
                // Проверяем, нет ли уже такого сообщения
                var existing = _messages.FirstOrDefault(m => m.Id == message.Id);
                if (existing == null)
                {
                    AddMessageToUICollection(message, true);
                    Console.WriteLine($"Сообщение {message.Id} добавлено в активный чат");
                }
                else
                {
                    Console.WriteLine($"Сообщение {message.Id} уже есть в чате");
                }
            }
        }

        private void AddMessageToUICollection(Message message, bool isCurrentChat)
        {
            Console.WriteLine($"[MAIN] === AddMessageToUICollection ===");
            Console.WriteLine($"[MAIN] Параметры: ID={message.Id}, isCurrentChat={isCurrentChat}");
            Console.WriteLine($"[MAIN] От: {message.SenderId}, Кому: {message.ReceiverId}");
            Console.WriteLine($"[MAIN] Выбранный контакт: {_selectedContact?.Id} (я: {_currentUser.Id})");

            // Прямая проверка для диагностики
            bool diagnosticShouldAdd = false;
            if (_selectedContact != null)
            {
                diagnosticShouldAdd = (message.SenderId == _currentUser.Id && message.ReceiverId == _selectedContact.Id) ||
                                      (message.SenderId == _selectedContact.Id && message.ReceiverId == _currentUser.Id);
                Console.WriteLine($"[MAIN] Диагностика shouldAdd: {diagnosticShouldAdd}");
            }

            Console.WriteLine($"[MAIN] Сообщений в коллекции до: {_messages.Count}");

            // Проверяем на дубликаты
            if (_messages.Any(m => m.Id == message.Id))
            {
                Console.WriteLine($"[MAIN] Сообщение {message.Id} уже есть в коллекции, пропускаем");
                Console.WriteLine($"[MAIN] === КОНЕЦ AddMessageToUICollection (дубликат) ===");
                return;
            }

            bool isMyMessage = message.SenderId == _currentUser.Id;
            string senderName = isMyMessage ? _currentUser.Username : GetUserNameById(message.SenderId);

            var messageView = new MessageView
            {
                Id = message.Id,
                SenderId = message.SenderId,
                SenderName = senderName,
                Content = message.Content,
                Time = message.CreatedAt.ToString("HH:mm"),
                Status = message.DeliveryStatus ?? "sent",
                IsMyMessage = isMyMessage,
                IsFile = message.FileData != null && message.FileData.Length > 0,
                FileName = message.FileName,
                FileData = message.FileData,
                FileSize = message.FileData != null ? FormatFileSize(message.FileData.Length) : "",
                IsDeleted = message.IsDeleted
            };

            Console.WriteLine($"[MAIN] Создан MessageView: ID={messageView.Id}, IsMyMessage={messageView.IsMyMessage}");

            // Устанавливаем стиль сообщения
            if (messageView.IsFile)
            {
                messageView.MessageStyle = messageView.IsMyMessage
                    ? (Style)FindResource("FileMessageStyle")
                    : (Style)FindResource("OtherFileMessageStyle");
            }
            else
            {
                messageView.MessageStyle = messageView.IsMyMessage
                    ? (Style)FindResource("MyMessageStyle")
                    : (Style)FindResource("OtherMessageStyle");
            }

            messageView.ShowSenderName = messageView.IsMyMessage
                ? Visibility.Collapsed
                : Visibility.Visible;

            messageView.ShowStatus = messageView.IsMyMessage
                ? Visibility.Visible
                : Visibility.Collapsed;

            // Добавляем в UI если это текущий чат
            if (isCurrentChat)
            {
                _messages.Add(messageView);
                ScrollToBottom();
                Console.WriteLine($"[MAIN] Сообщение ДОБАВЛЕНО в коллекцию");
                Console.WriteLine($"[MAIN] Сообщений в коллекции после: {_messages.Count}");
            }
            else
            {
                Console.WriteLine($"[MAIN] Сообщение НЕ добавлено (не текущий чат)");
            }

            Console.WriteLine($"[MAIN] === КОНЕЦ AddMessageToUICollection ===");
        }

        private void OnMessageStatusChanged(int messageId, string status)
        {
            Console.WriteLine($"[MAIN] === OnMessageStatusChanged ===");
            Console.WriteLine($"[MAIN] messageId: {messageId}, status: {status}");

            Dispatcher.Invoke(() =>
            {
                _dbService.UpdateMessageStatus(messageId, status);
                Console.WriteLine($"[MAIN] Статус обновлен в БД");

                var messageView = _messages.FirstOrDefault(m => m.Id == messageId);
                if (messageView != null)
                {
                    messageView.Status = status;
                    Console.WriteLine($"[MAIN] Статус обновлен в UI");
                }
                else
                {
                    Console.WriteLine($"[MAIN] Сообщение {messageId} не найдено в UI");
                }

                Console.WriteLine($"[MAIN] === OnMessageStatusChanged завершен ===");
            });
        }
        private void UpdateAllUnreadCounts()
        {
            try
            {
                foreach (var contact in _contacts)
                {
                    int unreadCount = _dbService.GetUnreadCount(_currentUser.Id, contact.Id);
                    if (contact.UnreadCount != unreadCount)
                    {
                        contact.UnreadCount = unreadCount;
                        Console.WriteLine($"Обновлен счетчик для {contact.Username}: {unreadCount}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления счетчиков: {ex.Message}");
            }
        }


        private void LoadMessages(int contactId)
        {
            try
            {
                Console.WriteLine($"[MAIN] === LoadMessages для контакта {contactId} ===");

                _messages.Clear();
                Console.WriteLine($"[MAIN] Коллекция сообщений очищена");

                var messages = _dbService.GetMessages(_currentUser.Id, contactId, 50);
                messages.Reverse();

                Console.WriteLine($"[MAIN] Получено {messages.Count} сообщений из БД");

                foreach (var msg in messages)
                {
                    bool isMyMessage = msg.SenderId == _currentUser.Id;
                    string senderName = isMyMessage ? _currentUser.Username : GetUserNameById(msg.SenderId);

                    var messageView = new MessageView
                    {
                        Id = msg.Id,
                        SenderId = msg.SenderId,
                        SenderName = senderName,
                        Content = msg.Content,
                        Time = msg.CreatedAt.ToString("HH:mm"),
                        Status = msg.DeliveryStatus ?? "sent",
                        IsMyMessage = isMyMessage,
                        IsFile = msg.FileData != null && msg.FileData.Length > 0,
                        FileName = msg.FileName,
                        FileData = msg.FileData,
                        FileSize = msg.FileData != null ? FormatFileSize(msg.FileData.Length) : "",
                        IsEdited = msg.IsEdited,
                        EditedAt = msg.EditedAt,
                        IsDeleted = msg.IsDeleted
                    };

                    // Устанавливаем стиль сообщения
                    if (messageView.IsFile)
                    {
                        messageView.MessageStyle = messageView.IsMyMessage
                            ? (Style)FindResource("FileMessageStyle")
                            : (Style)FindResource("OtherMessageStyle");
                    }
                    else
                    {
                        messageView.MessageStyle = messageView.IsMyMessage
                            ? (Style)FindResource("MyMessageStyle")
                            : (Style)FindResource("OtherMessageStyle");
                    }

                    messageView.ShowSenderName = messageView.IsMyMessage
                        ? Visibility.Collapsed
                        : Visibility.Visible;

                    messageView.ShowStatus = messageView.IsMyMessage
                        ? Visibility.Visible
                        : Visibility.Collapsed;

                    _messages.Add(messageView);
                }

                ScrollToBottom();
                Console.WriteLine($"[MAIN] Добавлено {_messages.Count} сообщений в UI");

                // Помечаем сообщения как прочитанные
                if (contactId != _currentUser.Id)
                {
                    Console.WriteLine($"[MAIN] Помечаю все сообщения как прочитанные в БД");
                    Task.Run(() => MarkAllMessagesAsRead(contactId));
                }

                Console.WriteLine($"[MAIN] === LoadMessages завершен ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MAIN] Ошибка загрузки сообщений: {ex.Message}");
                MessageBox.Show($"Ошибка загрузки сообщений: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MarkAllMessagesAsRead(int contactId)
        {
            try
            {
                Console.WriteLine($"Помечаем все сообщения от {contactId} как прочитанные");

                // Помечаем сообщения как прочитанные в БД
                _dbService.MarkMessagesAsRead(_currentUser.Id, contactId);

                // Обновляем статусы в UI
                foreach (var messageView in _messages)
                {
                    if (messageView.SenderId == contactId && messageView.Status != "read")
                    {
                        messageView.Status = "read";

                        // Отправляем статус "read" отправителю
                        SendReadStatus(messageView.Id, contactId);
                    }
                }

                // Сбрасываем счетчик непрочитанных
                ResetUnreadCount(contactId);

                Console.WriteLine($"Все сообщения от {contactId} помечены как прочитанные");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при пометке сообщений как прочитанные: {ex.Message}");
            }
        }

        private void SendReadStatus(int messageId, int contactId)
        {
            if (_tcpClient != null && _tcpClient.IsConnected())
            {
                try
                {
                    var statusData = new
                    {
                        type = "delivery_status",
                        messageId = messageId,
                        receiverId = contactId,
                        status = "read"
                    };

                    string json = JsonConvert.SerializeObject(statusData);
                    _tcpClient.SendStatusUpdate(json);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка отправки статуса read: {ex.Message}");
                }
            }
        }

        private void SendDeliveryStatus(int messageId, int receiverId, string status)
        {
            Console.WriteLine($"[MAIN] === SendDeliveryStatus ===");
            Console.WriteLine($"[MAIN] messageId: {messageId}, receiverId: {receiverId}, status: {status}");

            if (_tcpClient != null && _tcpClient.IsConnected())
            {
                try
                {
                    var statusData = new
                    {
                        type = "delivery_status",
                        messageId = messageId,
                        receiverId = receiverId,
                        status = status
                    };

                    string json = JsonConvert.SerializeObject(statusData);
                    Console.WriteLine($"[MAIN] Отправляю статус: {json}");
                    _tcpClient.SendStatusUpdate(json);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MAIN] Ошибка отправки статуса: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"[MAIN] TCP не подключен, статус не отправлен");
            }

            Console.WriteLine($"[MAIN] === SendDeliveryStatus завершен ===");
        }


        private void ResetUnreadCount(int contactId)
        {
            try
            {
                var contact = _contacts.FirstOrDefault(c => c.Id == contactId);
                if (contact != null)
                {
                    contact.UnreadCount = 0;
                    Console.WriteLine($"Счетчик сброшен для контакта {contact.Username}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сброса счетчика: {ex.Message}");
            }
        }


        private void IncrementUnreadCount(int contactId)
        {
            try
            {
                var contact = _contacts.FirstOrDefault(c => c.Id == contactId);
                if (contact != null)
                {
                    // Запрашиваем актуальное количество из БД
                    int unreadCount = _dbService.GetUnreadCount(_currentUser.Id, contactId);
                    contact.UnreadCount = unreadCount;
                    Console.WriteLine($"Счетчик обновлен для контакта {contact.Username}: {unreadCount}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка увеличения счетчика: {ex.Message}");
            }
        }

        private void UpdateContactLastMessage(int contactId, string preview)
        {
            try
            {
                // Находим контакт в ObservableCollection
                var contact = _contacts.FirstOrDefault(c => c.Id == contactId);
                if (contact != null)
                {
                    contact.LastMessagePreview = preview.Length > 40 ?
                        preview.Substring(0, 40) + "..." : preview;

                    // Принудительно обновляем UI
                    ContactsItemsControl.Items.Refresh();
                }
                else
                {
                    // Если контакта нет в списке, перезагружаем контакты
                    Dispatcher.BeginInvoke(new Action(async () =>
                    {
                        await ReloadContactsAsync();
                    }));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления превью: {ex.Message}");
            }
        }
        private void ForceUpdateUI()
        {
            // Обновляем счетчики непрочитанных для всех контактов
            foreach (var contact in _contacts)
            {
                int unreadCount = _dbService.GetUnreadCount(_currentUser.Id, contact.Id);
                contact.UnreadCount = unreadCount;
            }

            // Принудительно обновляем список контактов
            ContactsItemsControl.Items.Refresh();
        }
        private async Task AddToContactsAsync(int userId)
        {
            try
            {
                bool isContact = _dbService.IsContact(_currentUser.Id, userId);
                if (!isContact)
                {
                    bool added = _dbService.AddContact(_currentUser.Id, userId);
                    if (added)
                    {
                        Console.WriteLine($"Пользователь {userId} добавлен в контакты");
                        await ReloadContactsAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при добавлении в контакты: {ex.Message}");
            }
        }

        private async Task ReloadContactsAsync()
        {
            await Task.Run(() =>
            {
                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        var freshContacts = _dbService.GetContacts(_currentUser.Id);
                        _allContacts = freshContacts;  // <-- Теперь совместимые типы

                        // Восстанавливаем выделение
                        int? selectedContactId = _selectedContact?.Id;
                        RefreshContactsUI();

                        // Восстанавливаем выделение
                        if (selectedContactId.HasValue)
                        {
                            var contactToSelect = _contacts.FirstOrDefault(c => c.Id == selectedContactId.Value);
                            if (contactToSelect != null)
                            {
                                // Устанавливаем выделение через свойство IsSelected
                                contactToSelect.IsSelected = true;
                            }
                        }

                        Console.WriteLine($"Контакты обновлены. Всего: {_allContacts.Count}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка обновления контактов: {ex.Message}");
                    }
                });
            });
        }
        // Обработчик редактирования сообщения
        private void EditMessageMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.DataContext is MessageView messageView)
            {
                // Проверяем, что сообщение наше
                if (!messageView.IsMyMessage) return;

                // Проверяем, что сообщение не файл
                if (messageView.IsFile)
                {
                    MessageBox.Show("Файлы нельзя редактировать", "Информация",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Открываем диалог редактирования
                var editDialog = new EditMessageDialog(messageView.Content);
                if (editDialog.ShowDialog() == true)
                {
                    string newContent = editDialog.NewContent;

                    // Проверяем, что текст не пустой и не состоит только из пробелов
                    if (string.IsNullOrWhiteSpace(newContent))
                    {
                        MessageBox.Show("Сообщение не может быть пустым", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Проверяем, изменился ли текст
                    if (newContent.Trim() == messageView.Content.Trim())
                    {
                        // Текст не изменился
                        return;
                    }

                    try
                    {
                        // Обновляем в БД
                        bool updated = _dbService.UpdateMessage(messageView.Id, newContent);

                        if (updated)
                        {
                            // Обновляем в UI
                            messageView.Content = newContent;
                            messageView.IsEdited = true;
                            messageView.EditedAt = DateTime.Now;

                            // Обновляем превью последнего сообщения
                            if (_selectedContact != null)
                            {
                                UpdateLastMessagePreview(_selectedContact.Id, $"Вы: {newContent}");
                            }

                            MessageBox.Show("Сообщение изменено", "Успех",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show("Текст сообщения не изменился", "Информация",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка редактирования: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        // Обработчик копирования текста
        private void CopyMessageMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.DataContext is MessageView messageView)
            {
                if (messageView.IsDeleted)
                {
                    MessageBox.Show("Нельзя скопировать удаленное сообщение", "Информация",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                try
                {
                    Clipboard.SetText(messageView.Content);
                    MessageBox.Show("Текст скопирован в буфер обмена", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка копирования: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Обработчик удаления сообщения (заглушка, можно реализовать позже)

        private void RefreshContactsUI()
        {
            try
            {
                var selectedId = _selectedContact?.Id;

                // Получаем свежие данные
                var freshContacts = _dbService.GetContacts(_currentUser.Id);
                _allContacts = freshContacts;

                // Обновляем ObservableCollection
                _contacts.Clear();

                foreach (var contact in freshContacts)
                {
                    var lastMessages = _dbService.GetMessages(_currentUser.Id, contact.Id, 1);
                    string lastMessagePreview = "";

                    if (lastMessages.Count > 0)
                    {
                        var lastMsg = lastMessages[0];
                        string lastMessageSender = lastMsg.SenderId == _currentUser.Id ? "Вы: " : $"{contact.Username}: ";

                        lastMessagePreview = !string.IsNullOrEmpty(lastMsg.FileName)
                            ? $"{lastMessageSender}📎 {lastMsg.FileName}"
                            : lastMsg.Content.Length > 20
                                ? $"{lastMessageSender}{lastMsg.Content.Substring(0, 20)}..."
                                : $"{lastMessageSender}{lastMsg.Content}";
                    }

                    int unreadCount = _dbService.GetUnreadCount(_currentUser.Id, contact.Id);

                    _contacts.Add(new ContactView
                    {
                        Id = contact.Id,
                        Username = contact.Username,
                        Status = contact.Status ?? "offline",
                        AvatarImage = contact.Avatar != null ? LoadImageFromBytes(contact.Avatar) : null,
                        LastMessagePreview = lastMessagePreview,
                        UnreadCount = unreadCount,
                        IsSelected = (selectedId.HasValue && contact.Id == selectedId.Value)
                    });
                }

                Console.WriteLine($"Контакты обновлены. Всего: {_allContacts.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления контактов: {ex.Message}");
            }
        }

        private void UpdateUnreadCountForAllContacts()
        {
            try
            {
                foreach (var contact in _contacts)
                {
                    int unreadCount = _dbService.GetUnreadCount(_currentUser.Id, contact.Id);
                    if (contact.UnreadCount != unreadCount)
                    {
                        contact.UnreadCount = unreadCount;
                        Console.WriteLine($"Обновлен счетчик непрочитанных для {contact.Username}: {unreadCount}");
                    }
                }
                // Обновляем UI
                ContactsItemsControl.Items.Refresh();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления счетчиков непрочитанных: {ex.Message}");
            }
        }

        private void UpdateMessageInUI(int messageId, Action<MessageView> updateAction)
        {
            var messageView = _messages.FirstOrDefault(m => m.Id == messageId);
            if (messageView != null)
            {
                updateAction(messageView);
                Console.WriteLine($"Сообщение {messageId} обновлено в UI");
            }
        }

        private void LoadContacts()
        {
            try
            {
                _allContacts = _dbService.GetContacts(_currentUser.Id);
                _contacts.Clear();

                foreach (var contact in _allContacts)
                {
                    var lastMessages = _dbService.GetMessages(_currentUser.Id, contact.Id, 1);
                    string lastMessagePreview = "";

                    if (lastMessages.Count > 0)
                    {
                        var lastMsg = lastMessages[0];
                        string lastMessageSender = lastMsg.SenderId == _currentUser.Id ? "Вы: " : $"{contact.Username}: ";

                        lastMessagePreview = !string.IsNullOrEmpty(lastMsg.FileName)
                            ? $"{lastMessageSender}📎 {lastMsg.FileName}"
                            : lastMsg.Content.Length > 20
                                ? $"{lastMessageSender}{lastMsg.Content.Substring(0, 20)}..."
                                : $"{lastMessageSender}{lastMsg.Content}";
                    }

                    int unreadCount = _dbService.GetUnreadCount(_currentUser.Id, contact.Id);

                    _contacts.Add(new ContactView
                    {
                        Id = contact.Id,
                        Username = contact.Username,
                        Status = contact.Status ?? "offline",
                        LastSeen = contact.LastSeen,
                        AvatarImage = contact.Avatar != null ? LoadImageFromBytes(contact.Avatar) : null,
                        LastMessagePreview = lastMessagePreview,
                        UnreadCount = unreadCount
                    });
                }

                Console.WriteLine($"Загружено контактов: {_contacts.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки контактов: {ex.Message}");
            }
        }

        private string GetMessagePreview(Message message)
        {
            if (!string.IsNullOrEmpty(message.FileName))
            {
                return $"📎 {message.FileName}";
            }
            return message.Content.Length > 20 ?
                message.Content.Substring(0, 20) + "..." :
                message.Content;
        }

        private void ShowNotification(Message message)
        {
            string senderName = GetUserNameById(message.SenderId);
            string preview = GetMessagePreview(message);
            Console.WriteLine($"Новое сообщение от {senderName}: {preview}");
        }

        private ImageSource LoadImageFromBytes(byte[] bytes)
        {
            try
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.StreamSource = new System.IO.MemoryStream(bytes);
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();
                return image;
            }
            catch
            {
                return null;
            }
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double len = bytes;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }

        private string GetUserNameById(int userId)
        {
            if (userId == _currentUser.Id)
            {
                return _currentUser.Username;
            }

            var user = _allContacts.FirstOrDefault(u => u.Id == userId);
            return user?.Username ?? "Пользователь";
        }

        private void UpdateUnreadCount(int contactId)
        {
            // TODO: Реализовать
        }

        protected override void OnClosed(EventArgs e)
        {
            // При закрытии окна меняем статус на "offline"
            if (_currentUser != null)
            {
                Console.WriteLine($"=== Пользователь {_currentUser.Username} выходит из системы ===");
                _dbService.UpdateUserStatus(_currentUser.Id, "offline");

                // Отправляем статус через TCP
                if (_tcpClient != null && _tcpClient.IsConnected())
                {
                    _tcpClient.UpdateStatus(_currentUser.Id, "offline");
                }
            }

            _tcpClient?.Disconnect();
            base.OnClosed(e);
        }

        private void ScrollToBottom()
        {
            if (MessagesScrollViewer != null)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    MessagesScrollViewer.ScrollToEnd();
                }));
            }
        }

        // ================ ОБРАБОТЧИКИ СОБЫТИЙ ================

        // 1. Обработка выбора контакта
       

        // 2. Обработка изменения текста поиска
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = SearchTextBox.Text.ToLower();

            if (string.IsNullOrEmpty(searchText))
            {
                LoadContacts();
                return;
            }

            var filtered = _allContacts
                .Where(c => c.Username.ToLower().Contains(searchText))
                .Select(c => new ContactView
                {
                    Id = c.Id,
                    Username = c.Username,
                    Status = c.Status ?? "offline",
                    AvatarImage = c.Avatar != null ? LoadImageFromBytes(c.Avatar) : null,
                    LastMessagePreview = "",
                    UnreadCount = 0,
                    IsSelected = false
                })
                .ToList();

            _contacts.Clear();
            foreach (var contact in filtered)
            {
                _contacts.Add(contact);
            }
        }


        // 3. Обработка нажатия кнопки "Добавить контакт"
        private async void AddContactButton_Click(object sender, RoutedEventArgs e)
        {
            var addContactWindow = new AddContactWindow(_currentUser.Id, _dbService);
            if (addContactWindow.ShowDialog() == true)
            {
                await ReloadContactsAsync();
                MessageBox.Show("Контакт успешно добавлен", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // 4. Обработка удаления контакта
        private void RemoveContactMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedContact != null)
            {
                var result = MessageBox.Show(
                    $"Вы уверены, что хотите удалить {_selectedContact.Username} из контактов?",
                    "Удаление контакта",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        bool success = _dbService.RemoveContact(_currentUser.Id, _selectedContact.Id);

                        if (success)
                        {
                            MessageBox.Show("Контакт удален", "Успех",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                            LoadContacts();

                            if (_selectedContact != null && _selectedContact.Id == _selectedContact.Id)
                            {
                                _selectedContact = null;
                                SelectedContactName.Text = "Выберите контакт для общения";
                                SelectedContactStatus.Text = "";
                                SelectedContactAvatar.ImageSource = null;
                                SelectedContactStatusCircle.Fill = Brushes.Transparent;
                                _messages.Clear();
                            }
                        }
                        else
                        {
                            MessageBox.Show("Не удалось удалить контакт", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите контакт для удаления", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // 5. Обработка отправки сообщения
        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine($"[MAIN] === SendButton_Click ===");

            if (_selectedContact == null)
            {
                Console.WriteLine($"[MAIN] Нет выбранного контакта");
                MessageBox.Show("Выберите контакт для отправки сообщения", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string messageText = MessageTextBox.Text.Trim();
            Console.WriteLine($"[MAIN] Текст сообщения: '{messageText}'");

            if (string.IsNullOrWhiteSpace(messageText))
            {
                Console.WriteLine($"[MAIN] Пустое сообщение");
                return;
            }

            var message = new Message
            {
                SenderId = _currentUser.Id,
                ReceiverId = _selectedContact.Id,
                Content = messageText,
                CreatedAt = DateTime.Now
            };

            try
            {
                Console.WriteLine($"[MAIN] Сохраняю в БД...");
                int messageId = _dbService.SendMessage(message);
                message.Id = messageId;
                Console.WriteLine($"[MAIN] Сохранено с ID: {messageId}");

                // Добавляем в UI
                Console.WriteLine($"[MAIN] Добавляю в UI...");
                AddMessageToUICollection(message, true);
                MessageTextBox.Clear();
                Console.WriteLine($"[MAIN] UI обновлен");

                // Отправляем через TCP
                if (_tcpClient != null && _tcpClient.IsConnected())
                {
                    Console.WriteLine($"[MAIN] Отправляю через TCP...");
                    _tcpClient.SendMessage(message);
                }
                else
                {
                    Console.WriteLine($"[MAIN] TCP не подключен!");
                }

                // Обновляем превью
                UpdateLastMessagePreview(_selectedContact.Id, $"Вы: {messageText}");

                Console.WriteLine($"[MAIN] === SendButton_Click завершен ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MAIN] Ошибка отправки: {ex.Message}");
                MessageBox.Show($"Ошибка отправки: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 6. Обработка изменения текста в поле сообщения
        private void MessageTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SendButton.IsEnabled = !string.IsNullOrWhiteSpace(MessageTextBox.Text);
        }

        // 7. Обработка нажатия клавиши Enter для отправки
        private void MessageTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !Keyboard.IsKeyDown(Key.LeftShift))
            {
                SendButton_Click(sender, e);
                e.Handled = true;
            }
        }
        private void UpdateMessageStatusInUI(int messageId, string status)
        {
            var messageView = _messages.FirstOrDefault(m => m.Id == messageId);
            if (messageView != null)
            {
                messageView.Status = status;

                // Если нужно обновить отображение списка сообщений
                int index = _messages.IndexOf(messageView);
                if (index >= 0)
                {
                    _messages.RemoveAt(index);
                    _messages.Insert(index, messageView);
                }
            }
        }

        // 8. Обработка прикрепления файла
        private void AttachFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedContact == null)
            {
                MessageBox.Show("Выберите контакт для отправки файла", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Title = "Выберите файл для отправки";
            dialog.Filter = "Все файлы (*.*)|*.*|Изображения (*.jpg;*.jpeg;*.png;*.gif)|*.jpg;*.jpeg;*.png;*.gif|Документы (*.pdf;*.doc;*.docx;*.txt)|*.pdf;*.doc;*.docx;*.txt";

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    string fileName = System.IO.Path.GetFileName(dialog.FileName);
                    byte[] fileBytes = System.IO.File.ReadAllBytes(dialog.FileName);

                    if (fileBytes.Length > 10 * 1024 * 1024)
                    {
                        MessageBox.Show("Файл слишком большой (максимум 10MB)", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var message = new Message
                    {
                        SenderId = _currentUser.Id,
                        ReceiverId = _selectedContact.Id,
                        Content = $"📎 {fileName}",
                        MessageType = "document",
                        FileData = fileBytes,
                        FileName = fileName,
                        CreatedAt = DateTime.Now
                    };

                    int messageId = _dbService.SendMessage(message);
                    message.Id = messageId;

                    if (_tcpClient != null && _tcpClient.IsConnected())
                    {
                        _tcpClient.SendMessage(message);
                    }

                    AddMessageToUI(message);
                    UpdateLastMessagePreview(_selectedContact.Id, $"Вы: 📎 {fileName}");

                    MessageBox.Show($"Файл '{fileName}' отправлен", "Успешно",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка отправки файла: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // 9. Обработчик статуса пользователя
        private void OnTcpUserStatusChanged(int userId, string status)
        {
            Dispatcher.Invoke(() =>
            {
                // Обновляем статус пользователя в списке контактов
                if (ContactsItemsControl.ItemsSource is List<ContactView> contacts)
                {
                    var contact = contacts.FirstOrDefault(c => c.Id == userId);
                    if (contact != null)
                    {
                        contact.Status = status;
                        ContactsItemsControl.Items.Refresh();

                        // Если это выбранный контакт, обновляем его индикатор
                        if (_selectedContact != null && _selectedContact.Id == userId)
                        {
                            SelectedContactStatusCircle.Fill = MainWindow.GetStatusBrush(status);
                            SelectedContactStatus.Text = MainWindow.GetStatusText(status);
                        }
                    }
                }
            });
        }

        // 10. Обработчик статуса подключения TCP
        private void OnTcpConnectionStatusChanged(string status)
        {
            Dispatcher.Invoke(() =>
            {
                if (status == "connected")
                {
                    IsTcpConnected = true;
                    Console.WriteLine("TCP подключение установлено");
                }
                else if (status == "disconnected")
                {
                    IsTcpConnected = false;
                    Console.WriteLine("TCP соединение разорвано");
                }
            });
        }

        // 11. Обработка клика на профиле
        private void ProfileMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var profileWindow = new ProfileWindow(_currentUser, _dbService);
            if (profileWindow.ShowDialog() == true)
            {
                var updatedUser = _dbService.GetUserById(_currentUser.Id);
                if (updatedUser != null)
                {
                    _currentUser = updatedUser;
                    this.Title = $"ChatApp - {_currentUser.Username}";

                    if (_tcpClient != null && _tcpClient.IsConnected())
                    {
                        _tcpClient.UpdateStatus(_currentUser.Id, _currentUser.Status);
                    }
                }
            }
        }

        // 12. Обработка выхода
        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // 13. Обработка уведомлений
        private void NotificationsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new NotificationSettingsDialog(_notificationService, _currentUser);
            dialog.Owner = this;
            dialog.ShowDialog();
        }

        public ContactView SelectedContact => _selectedContact;

        // 14. Обработка смены пароля
        private void ChangePasswordMenuItem_Click(object sender, RoutedEventArgs e)
        {
            InputDialog oldPassDialog = new InputDialog("Введите старый пароль:", "Смена пароля");
            if (oldPassDialog.ShowDialog() != true) return;
            string oldPass = oldPassDialog.Answer;

            InputDialog newPassDialog = new InputDialog("Введите новый пароль:", "Смена пароля");
            if (newPassDialog.ShowDialog() != true) return;
            string newPass = newPassDialog.Answer;

            InputDialog confirmPassDialog = new InputDialog("Повторите новый пароль:", "Смена пароля");
            if (confirmPassDialog.ShowDialog() != true) return;
            string confirmPass = confirmPassDialog.Answer;

            if (newPass != confirmPass)
            {
                MessageBox.Show("Пароли не совпадают", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                bool success = _dbService.ChangePassword(_currentUser.Id, oldPass, newPass);
                if (success)
                {
                    MessageBox.Show("Пароль успешно изменен", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Неверный старый пароль", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 15. Обработка смены email
        private void ChangeEmailMenuItem_Click(object sender, RoutedEventArgs e)
        {
            InputDialog emailDialog = new InputDialog("Введите новый email:", "Смена Email");
            if (emailDialog.ShowDialog() != true) return;
            string newEmail = emailDialog.Answer;

            InputDialog passwordDialog = new InputDialog("Введите пароль для подтверждения:", "Смена Email");
            if (passwordDialog.ShowDialog() != true) return;
            string password = passwordDialog.Answer;

            MessageBox.Show("Смена email будет реализована позже", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // 16. Просмотр профиля контакта
        private void ViewProfileMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedContact != null)
            {
                var user = _dbService.GetUserById(_selectedContact.Id);
                if (user != null)
                {
                    var profileWindow = new UserProfileWindow(user);
                    profileWindow.ShowDialog();
                }
                else
                {
                    MessageBox.Show("Не удалось загрузить данные пользователя", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void DeleteMessageMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.DataContext is MessageView messageView)
            {
                // Проверяем, что сообщение наше и не удалено
                if (!messageView.IsMyMessage || messageView.IsDeleted) return;

                var result = MessageBox.Show(
                    "Вы уверены, что хотите удалить это сообщение?\n\n" +
                    "Сообщение будет удалено для всех участников чата.",
                    "Удаление сообщения",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        // Удаляем в БД
                        bool deleted = _dbService.SoftDeleteMessage(messageView.Id, _currentUser.Id);

                        if (deleted)
                        {
                            // Обновляем в UI
                            messageView.IsDeleted = true;
                            messageView.Content = "[Сообщение удалено]";

                            // Отправляем через TCP собеседнику
                            if (_selectedContact != null && _tcpClient != null)
                            {
                                _tcpClient.SendDeleteMessage(messageView.Id, _selectedContact.Id);
                            }

                            // Обновляем превью последнего сообщения
                            if (_selectedContact != null)
                            {
                                UpdateLastMessagePreview(_selectedContact.Id, $"Вы: [Сообщение удалено]");
                            }

                            MessageBox.Show("Сообщение удалено", "Успех",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show("Не удалось удалить сообщение. Возможно, оно уже удалено или вы не автор.",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
        private void OnMessageDeleted(int messageId)
        {
            Dispatcher.Invoke(() =>
            {
                Console.WriteLine($"Получено удаление сообщения {messageId}");

                // Обновляем в БД
                try
                {
                    _dbService.SoftDeleteMessage(messageId, -1); // -1 означает "отправитель уже проверил права"
                }
                catch { }

                // Если сообщение в текущем чате, обновляем UI
                var messageView = _messages.FirstOrDefault(m => m.Id == messageId);
                if (messageView != null)
                {
                    messageView.IsDeleted = true;
                    messageView.Content = "[Сообщение удалено]";
                }
            });
        }

        // Обработчик восстановления сообщения
        private void RestoreMessageMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.DataContext is MessageView messageView)
            {
                // Проверяем, что сообщение наше и удалено
                if (!messageView.IsMyMessage || !messageView.IsDeleted) return;

                try
                {
                    // Восстанавливаем в БД
                    bool restored = _dbService.RestoreMessage(messageView.Id, _currentUser.Id);

                    if (restored)
                    {
                        // Перезагружаем сообщения для восстановления контента
                        if (_selectedContact != null)
                        {
                            // Получаем обновленное сообщение из БД
                            var restoredMessage = _dbService.GetMessageById(messageView.Id);
                            if (restoredMessage != null)
                            {
                                messageView.Content = restoredMessage.Content;
                                messageView.IsDeleted = false;
                                messageView.IsFile = restoredMessage.FileData != null && restoredMessage.FileData.Length > 0;
                                messageView.FileName = restoredMessage.FileName;

                                // Обновляем превью
                                UpdateLastMessagePreview(_selectedContact.Id, $"Вы: {restoredMessage.Content}");
                            }
                        }

                        MessageBox.Show("Сообщение восстановлено", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Не удалось восстановить сообщение", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка восстановления: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        // 17. Обновление списка контактов
        private void RefreshContactsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            LoadContacts();
            MessageBox.Show("Список контактов обновлен", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // 18. Обработка клика на сообщении для скачивания файла
        private void MessageBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is MessageView messageView)
            {
                if (messageView.IsFile && messageView.FileData != null && messageView.FileData.Length > 0)
                {
                    DownloadFile(messageView.FileName, messageView.FileData);
                }
            }
        }

        // 19. Скачивание файла
        private void DownloadFile(string fileName, byte[] fileData)
        {
            var saveDialog = new SaveFileDialog
            {
                FileName = fileName,
                Filter = "Все файлы (*.*)|*.*"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    System.IO.File.WriteAllBytes(saveDialog.FileName, fileData);
                    MessageBox.Show($"Файл '{fileName}' успешно сохранен", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сохранении файла: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // 20. Обновление превью последнего сообщения
        private void UpdateLastMessagePreview(int contactId, string preview)
        {
            if (ContactsItemsControl.ItemsSource is List<ContactView> contacts)
            {
                var contact = contacts.FirstOrDefault(c => c.Id == contactId);
                if (contact != null)
                {
                    contact.LastMessagePreview = preview.Length > 40 ? preview.Substring(0, 40) + "..." : preview;
                    ContactsItemsControl.Items.Refresh();
                }
            }
        }
        // 21. Пометить сообщение как прочитанное
        private void MarkMessageAsRead(Message message)
        {
            if (message.ReceiverId == _currentUser.Id && message.DeliveryStatus != "read")
            {
                try
                {
                    // Обновляем в БД
                    _dbService.UpdateMessageStatus(message.Id, "read");

                    // Отправляем статус через TCP отправителю
                    if (_tcpClient != null && _tcpClient.IsConnected())
                    {
                        var statusData = new
                        {
                            type = "delivery_status",
                            messageId = message.Id,
                            receiverId = message.SenderId,
                            status = "read"
                        };

                        string json = JsonConvert.SerializeObject(statusData);
                        _tcpClient.SendStatusUpdate(json);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при пометке сообщения как прочитанное: {ex.Message}");
                }
            }
        }
        // В MainWindow.xaml.cs добавьте обработчик
       
    }
}