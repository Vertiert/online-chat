using ChatApp.Services;
using System;

namespace ChatServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Chat TCP Server";
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔════════════════════════════════════════╗");
            Console.WriteLine("║         ЧАТ СЕРВЕР TCP (v1.0)         ║");
            Console.WriteLine("╚════════════════════════════════════════╝");
            Console.ResetColor();

            try
            {
                // Запускаем TCP-сервер
                var tcpServer = new TcpServerService();
                tcpServer.Start(8888);

                // Ожидаем нажатия клавиши для остановки
                Console.ReadKey();

                tcpServer.Stop();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Критическая ошибка: {ex.Message}");
                Console.ResetColor();
                Console.ReadKey();
            }
        }
    }
}