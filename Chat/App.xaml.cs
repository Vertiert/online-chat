using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using ChatApp.Services;
using ChatApp.Views;

namespace ChatApp
{
    public partial class App : Application
    {
        private static Mutex _mutex;
        private Thread _serverThread;
        private bool _isFirstInstance;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Проверяем, не запущено ли уже приложение
            _mutex = new Mutex(true, "ChatApp_TCP_Server", out _isFirstInstance);

            // Показываем окно входа
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
        }

        

        protected override void OnExit(ExitEventArgs e)
        {
            // Останавливаем TCP-сервер при выходе
            try
            {
                _serverThread?.Join(1000); // Ждем завершения потока (макс 1 сек)
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка остановки TCP-сервера: {ex.Message}");
            }
            finally
            {
                if (_isFirstInstance)
                {
                    _mutex?.ReleaseMutex();
                    _mutex?.Dispose();
                }
            }

            base.OnExit(e);
        }
    }
}