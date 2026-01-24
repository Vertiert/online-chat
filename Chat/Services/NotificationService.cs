using ChatApp.Models;
using ChatApp.Notifications;
using ChatApp.Views;
using System;
using System.Windows;
using System.Windows.Interop;

namespace ChatApp.Services
{
    public class NotificationService : IDisposable
    {
        private DatabaseService _dbService;
        private MainWindow _mainWindow;
        private NotificationSettings _settings;
        private TrayNotificationManager _trayManager;
        private int _userId;

        public NotificationService(DatabaseService dbService, MainWindow mainWindow, int userId)
        {
            _dbService = dbService;
            _mainWindow = mainWindow;
            _userId = userId;

            // Получаем настройки
            _settings = _dbService.GetNotificationSettings(userId);
            if (_settings == null)
            {
                _settings = CreateDefaultSettings(userId);
            }

            InitializeTrayManager();
        }

        private NotificationSettings CreateDefaultSettings(int userId)
        {
            return new NotificationSettings
            {
                UserId = userId,
                Enabled = true,
                ShowPreview = true,
                PlaySound = true,
                OnlyBanner = false,
                SmartNotifications = true
            };
        }

        private void InitializeTrayManager()
        {
            try
            {
                // Получаем handle главного окна WPF
                var windowHandle = new WindowInteropHelper(_mainWindow).Handle;

                _trayManager = new TrayNotificationManager();
                _trayManager.NotificationClicked += (s, e) => ShowMainWindow();
                _trayManager.ApplicationExitRequested += (s, e) => ExitApplication();
                _trayManager.Initialize(windowHandle);

                Console.WriteLine("NotificationService инициализирован");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка инициализации NotificationService: {ex.Message}");
            }
        }

        public void ShowMessageNotification(Message message, User sender)
        {
            try
            {
                Console.WriteLine($"Получено сообщение для уведомления от {sender?.Username}");

                // Проверяем настройки
                if (_settings == null || !_settings.Enabled)
                {
                    Console.WriteLine("Уведомления отключены в настройках");
                    return;
                }

                // Проверяем "умные" уведомления
                if (_settings.SmartNotifications && IsUserActiveInDialog(sender?.Id))
                {
                    Console.WriteLine($"Умные уведомления: не показываем, пользователь активен в диалоге с {sender?.Username}");
                    return;
                }

                // Готовим текст уведомления
                string title = $"Новое сообщение от {sender?.Username ?? "Неизвестный"}";
                string text = PrepareNotificationText(message, sender);

                Console.WriteLine($"Подготовка уведомления: {title}");

                // Показываем уведомление
                _trayManager?.ShowNotification(title, text, 5000);

                // Воспроизводим звук
                if (_settings.PlaySound && !_settings.OnlyBanner)
                {
                    PlayNotificationSound();
                }

                Console.WriteLine($"Уведомление показано для {sender?.Username}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка показа уведомления: {ex.Message}");
            }
        }

        private string PrepareNotificationText(Message message, User sender)
        {
            if (!_settings.ShowPreview)
                return "У вас новое сообщение";

            if (!string.IsNullOrEmpty(message.FileName))
                return $"📎 Файл: {message.FileName}";

            if (string.IsNullOrEmpty(message.Content))
                return "Вложение";

            return message.Content.Length > 100
                ? message.Content.Substring(0, 100) + "..."
                : message.Content;
        }

        private bool IsUserActiveInDialog(int? senderId)
        {
            try
            {
                if (senderId == null || _mainWindow == null)
                    return false;

                // Используем Dispatcher для безопасного доступа к UI
                bool isActive = false;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var selectedContact = _mainWindow.SelectedContact;
                    isActive = selectedContact != null && selectedContact.Id == senderId.Value;
                });

                Console.WriteLine($"IsUserActiveInDialog: {isActive} для senderId={senderId}");
                return isActive;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в IsUserActiveInDialog: {ex.Message}");
                return false;
            }
        }

        private void PlayNotificationSound()
        {
            try
            {
                // Используем системный звук
                System.Media.SystemSounds.Beep.Play();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка воспроизведения звука: {ex.Message}");
            }
        }

        private void ShowMainWindow()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    if (_mainWindow != null)
                    {
                        _mainWindow.WindowState = WindowState.Normal;
                        _mainWindow.Show();
                        _mainWindow.Activate();
                        _mainWindow.Topmost = true;
                        _mainWindow.Topmost = false;
                        _mainWindow.Focus();
                        Console.WriteLine("Главное окно показано");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка показа главного окна: {ex.Message}");
                }
            });
        }

        private void ExitApplication()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    Console.WriteLine("Завершение приложения");
                    Application.Current.Shutdown();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка завершения приложения: {ex.Message}");
                }
            });
        }

        public void UpdateSettings(NotificationSettings settings)
        {
            _settings = settings;
            _dbService.SaveNotificationSettings(settings);
            Console.WriteLine("Настройки уведомлений обновлены");
        }

        public NotificationSettings GetCurrentSettings()
        {
            return _settings;
        }

        public void ShowTestNotification()
        {
            Console.WriteLine("Тестовое уведомление");
            _trayManager?.ShowNotification("Тест", "Это тестовое уведомление", 3000);
        }

        public void Dispose()
        {
            Console.WriteLine("Очистка NotificationService");
            _trayManager?.Dispose();
        }
    }
}