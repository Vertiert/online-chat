using ChatApp.Models;
using ChatApp.Services;
using MySql.Data.MySqlClient;
using System;
using System.Text.RegularExpressions;
using System.Windows;

namespace ChatApp.Views
{
    public partial class ChangeEmailWindow : Window
    {
        private DatabaseService _dbService;
        private User _currentUser;

        public ChangeEmailWindow(User user, DatabaseService dbService)
        {
            InitializeComponent();
            _currentUser = user;
            _dbService = dbService;

            CurrentEmailTextBlock.Text = user.Email;
            NewEmailTextBox.Focus();
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
                return Regex.IsMatch(email, pattern, RegexOptions.IgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private bool IsEmailAvailable(string email)
        {
            try
            {
                // Используем метод из DatabaseService
                if (_dbService.IsEmailAvailable(email))
                    return true;

                // Если email занят, проверяем, не занят ли он текущим пользователем
                using (var conn = new MySqlConnection(_dbService.ConnectionString))
                {
                    conn.Open();
                    string query = "SELECT COUNT(*) FROM users WHERE email = @email AND id = @userId";
                    var cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@email", email);
                    cmd.Parameters.AddWithValue("@userId", _currentUser.Id);

                    int count = Convert.ToInt32(cmd.ExecuteScalar());
                    return count > 0; // Если count > 0, значит это email текущего пользователя
                }
            }
            catch
            {
                return false;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string newEmail = NewEmailTextBox.Text.Trim();
            string password = PasswordBox.Password;

            // Скрываем предыдущие ошибки
            ErrorMessageTextBlock.Visibility = Visibility.Collapsed;

            // Проверка на пустые поля
            if (string.IsNullOrEmpty(newEmail) || string.IsNullOrEmpty(password))
            {
                ShowError("Все поля должны быть заполнены");
                return;
            }

            // Проверка, изменился ли email
            if (newEmail == _currentUser.Email)
            {
                ShowError("Новый email совпадает с текущим");
                return;
            }

            // Валидация email
            if (!IsValidEmail(newEmail))
            {
                ShowError("Некорректный формат email. Пример: example@mail.com");
                return;
            }

            // Проверка доступности email
            if (!IsEmailAvailable(newEmail))
            {
                ShowError("Этот email уже используется другим пользователем");
                return;
            }

            // Проверка пароля
            if (!_dbService.VerifyUserPassword(_currentUser.Id, password))
            {
                ShowError("Неверный пароль");
                return;
            }

            // Обновление email в базе данных
            try
            {
                bool success = _dbService.UpdateUserEmail(_currentUser.Id, newEmail);

                if (success)
                {
                    MessageBox.Show("Email успешно изменён", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
                else
                {
                    ShowError("Ошибка при изменении email");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка: {ex.Message}");
            }
        }

        private void ShowError(string message)
        {
            ErrorMessageTextBlock.Text = message;
            ErrorMessageTextBlock.Visibility = Visibility.Visible;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void NewEmailTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            ErrorMessageTextBlock.Visibility = Visibility.Collapsed;
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ErrorMessageTextBlock.Visibility = Visibility.Collapsed;
        }
    }
}