using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ChatApp.Notifications
{
    public class TrayNotificationManager : IDisposable
    {
        private NotifyIcon _notifyIcon;
        private bool _isInitialized = false;
        private IntPtr _mainWindowHandle;

        // События
        public event EventHandler NotificationClicked;
        public event EventHandler ApplicationExitRequested;

        // Импорт для фокуса окна
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool FlashWindow(IntPtr hWnd, bool bInvert);

        public void Initialize(IntPtr mainWindowHandle)
        {
            if (_isInitialized) return;

            try
            {
                _mainWindowHandle = mainWindowHandle;

                // Создаем NotifyIcon
                _notifyIcon = new NotifyIcon
                {
                    Icon = GetApplicationIcon(),
                    Visible = true,
                    Text = "ChatApp",
                    BalloonTipTitle = "ChatApp",
                    BalloonTipIcon = ToolTipIcon.Info
                };

                // Обработчики событий
                _notifyIcon.MouseClick += NotifyIcon_MouseClick;
                _notifyIcon.BalloonTipClicked += NotifyIcon_BalloonTipClicked;
                _notifyIcon.BalloonTipClosed += NotifyIcon_BalloonTipClosed;

                // Создаем контекстное меню
                CreateContextMenu();

                // Убедимся, что иконка создана
                Application.AddMessageFilter(new MessageFilter());

                _isInitialized = true;
                Console.WriteLine("TrayNotificationManager инициализирован");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка инициализации TrayNotificationManager: {ex.Message}");
            }
        }

        private void NotifyIcon_BalloonTipClicked(object sender, EventArgs e)
        {
            Console.WriteLine("BalloonTipClicked");
            BringMainWindowToFront();
            NotificationClicked?.Invoke(this, EventArgs.Empty);
        }

        private void NotifyIcon_BalloonTipClosed(object sender, EventArgs e)
        {
            Console.WriteLine("BalloonTipClosed");
        }

        private void NotifyIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Console.WriteLine("NotifyIcon кликнут левой кнопкой");
                BringMainWindowToFront();
                NotificationClicked?.Invoke(this, EventArgs.Empty);
            }
        }

        private void CreateContextMenu()
        {
            try
            {
                var contextMenu = new ContextMenuStrip();

                var showItem = new ToolStripMenuItem("Показать чат");
                showItem.Click += (s, e) =>
                {
                    Console.WriteLine("Меню: Показать чат");
                    BringMainWindowToFront();
                    NotificationClicked?.Invoke(this, EventArgs.Empty);
                };

                var testItem = new ToolStripMenuItem("Тест уведомления");
                testItem.Click += (s, e) =>
                {
                    Console.WriteLine("Меню: Тест уведомления");
                    ShowNotification("Тест", "Это тестовое уведомление из меню", 3000);
                };

                var exitItem = new ToolStripMenuItem("Выход");
                exitItem.Click += (s, e) =>
                {
                    Console.WriteLine("Меню: Выход");
                    ApplicationExitRequested?.Invoke(this, EventArgs.Empty);
                };

                contextMenu.Items.Add(showItem);
                contextMenu.Items.Add(testItem);
                contextMenu.Items.Add(new ToolStripSeparator());
                contextMenu.Items.Add(exitItem);

                _notifyIcon.ContextMenuStrip = contextMenu;
                Console.WriteLine("Контекстное меню создано");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка создания контекстного меню: {ex.Message}");
            }
        }

        private void BringMainWindowToFront()
        {
            try
            {
                if (_mainWindowHandle != IntPtr.Zero)
                {
                    SetForegroundWindow(_mainWindowHandle);
                    FlashWindow(_mainWindowHandle, false);
                }
                else
                {
                    Console.WriteLine("MainWindow handle не установлен");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка приведения окна в передний план: {ex.Message}");
            }
        }

        private Icon GetApplicationIcon()
        {
            try
            {
                // Используем иконку приложения WPF
                var icon = System.Drawing.Icon.ExtractAssociatedIcon(
                    System.Reflection.Assembly.GetEntryAssembly().Location);
                return icon ?? SystemIcons.Application;
            }
            catch
            {
                // Создаем простую иконку программно
                Bitmap bmp = new Bitmap(32, 32);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.SteelBlue);
                    using (Font font = new Font("Arial", 14, FontStyle.Bold))
                    {
                        g.DrawString("C", font, Brushes.White, 8, 6);
                    }
                }
                return Icon.FromHandle(bmp.GetHicon());
            }
        }

        public void ShowNotification(string title, string message, int timeout = 5000)
        {
            if (!_isInitialized || _notifyIcon == null)
            {
                Console.WriteLine("TrayNotificationManager не инициализирован");
                return;
            }

            try
            {
                Console.WriteLine($"Показ уведомления: {title} - {message}");

                // Убедимся, что иконка видима
                if (!_notifyIcon.Visible)
                    _notifyIcon.Visible = true;

                // Показываем уведомление
                _notifyIcon.ShowBalloonTip(timeout, title, message, ToolTipIcon.Info);

                Console.WriteLine("Уведомление показано");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка показа уведомления: {ex.Message}");
            }
        }

        public void Dispose()
        {
            try
            {
                if (_notifyIcon != null)
                {
                    _notifyIcon.Visible = false;
                    _notifyIcon.Dispose();
                    _notifyIcon = null;
                    Console.WriteLine("TrayNotificationManager очищен");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка очистки TrayNotificationManager: {ex.Message}");
            }
        }

        // Класс для обработки сообщений
        private class MessageFilter : IMessageFilter
        {
            public bool PreFilterMessage(ref Message m)
            {
                // Фильтр сообщений для корректной работы NotifyIcon
                return false;
            }
        }
    }
}