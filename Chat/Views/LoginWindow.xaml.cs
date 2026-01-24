using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using ChatApp.Services;

namespace ChatApp.Views
{
    public partial class LoginWindow : Window
    {
        private DatabaseService _dbService;

        public LoginWindow()
        {
            InitializeComponent();
            _dbService = new DatabaseService();

            // Очищаем ошибки при переключении вкладок
            LoginTabRadio.Checked += ClearErrors;
            RegisterTabRadio.Checked += ClearErrors;

            // Подписываемся на события изменения текста для валидации
            RegisterEmailTextBox.TextChanged += ValidateRegisterFields;
            UsernameTextBox.TextChanged += ValidateRegisterFields;
            RegisterPasswordBox.PasswordChanged += ValidateRegisterFields;
            ConfirmPasswordBox.PasswordChanged += ValidateRegisterFields;
        }

        private void ClearErrors(object sender, RoutedEventArgs e)
        {
            LoginErrorText.Visibility = Visibility.Collapsed;
            RegisterErrorText.Visibility = Visibility.Collapsed;
        }

        private bool ContainsOnlySpaces(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            return text.Trim().Length == 0;
        }

        private bool HasLeadingOrTrailingSpaces(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            return text.Length != text.Trim().Length;
        }

        private bool ContainsInnerSpaces(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            return text.Contains(" ");
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

        // Валидация полей регистрации в реальном времени
        private void ValidateRegisterFields(object sender, EventArgs e)
        {
            string email = RegisterEmailTextBox.Text;
            string username = UsernameTextBox.Text;
            string password = RegisterPasswordBox.Password;

            // Сбрасываем ошибку
            RegisterErrorText.Visibility = Visibility.Collapsed;

            // Валидация email
            if (!string.IsNullOrEmpty(email))
            {
                if (HasLeadingOrTrailingSpaces(email))
                {
                    ShowRegisterError("Email не должен содержать пробелы в начале или конце");
                    return;
                }
            }

            // Валидация имени пользователя
            if (!string.IsNullOrEmpty(username))
            {
                if (HasLeadingOrTrailingSpaces(username))
                {
                    ShowRegisterError("Имя пользователя не должно содержать пробелы в начале или конце");
                    return;
                }

                if (ContainsInnerSpaces(username))
                {
                    ShowRegisterError("Имя пользователя не должно содержать пробелы внутри");
                    return;
                }
            }

            // Валидация пароля
            if (!string.IsNullOrEmpty(password))
            {
                if (HasLeadingOrTrailingSpaces(password))
                {
                    ShowRegisterError("Пароль не должен содержать пробелы в начале или конце");
                    return;
                }
            }
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string email = EmailTextBox.Text.Trim();
            string password = PasswordBox.Password;

            // Очищаем предыдущие ошибки
            LoginErrorText.Visibility = Visibility.Collapsed;

            // Проверка на пустые поля
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ShowLoginError("Заполните все поля");
                return;
            }

            // Проверка на только пробелы
            if (ContainsOnlySpaces(email) || ContainsOnlySpaces(password))
            {
                ShowLoginError("Поля не должны содержать только пробелы");
                return;
            }

            // Проверка на пробелы в начале/конце
            if (HasLeadingOrTrailingSpaces(email))
            {
                ShowLoginError("Email не должен содержать пробелы в начале или конце");
                return;
            }

            var user = _dbService.AuthenticateUser(email, password);

            if (user != null)
            {
                MainWindow mainWindow = new MainWindow(user);
                mainWindow.Show();
                this.Close();
            }
            else
            {
                ShowLoginError("Неверный email или пароль");
            }
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            string email = RegisterEmailTextBox.Text.Trim();
            string password = RegisterPasswordBox.Password;
            string confirmPassword = ConfirmPasswordBox.Password;
            string username = UsernameTextBox.Text.Trim();

            // Очищаем предыдущие ошибки
            RegisterErrorText.Visibility = Visibility.Collapsed;

            // Проверка на пустые поля
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) ||
                string.IsNullOrEmpty(confirmPassword) || string.IsNullOrEmpty(username))
            {
                ShowRegisterError("Все поля должны быть заполнены");
                return;
            }

            // Проверка на только пробелы
            if (ContainsOnlySpaces(email) || ContainsOnlySpaces(password) ||
                ContainsOnlySpaces(confirmPassword) || ContainsOnlySpaces(username))
            {
                ShowRegisterError("Поля не должны содержать только пробелы");
                return;
            }

            // Проверка на пробелы в начале/конце
            if (HasLeadingOrTrailingSpaces(RegisterEmailTextBox.Text) ||
                HasLeadingOrTrailingSpaces(UsernameTextBox.Text) ||
                HasLeadingOrTrailingSpaces(password))
            {
                ShowRegisterError("Поля не должны содержать пробелы в начале или конце");
                return;
            }

            // Проверка на пробелы внутри имени пользователя
            if (ContainsInnerSpaces(username))
            {
                ShowRegisterError("Имя пользователя не должно содержать пробелы внутри");
                return;
            }

            // Валидация email
            if (!IsValidEmail(email))
            {
                ShowRegisterError("Некорректный формат email.\nПример: example@mail.com");
                return;
            }

            // Проверка имени пользователя
            if (username.Length < 3 || username.Length > 20)
            {
                ShowRegisterError("Имя пользователя должно быть\nот 3 до 20 символов");
                return;
            }

            // Проверка пароля
            if (password.Length < 6)
            {
                ShowRegisterError("Пароль должен содержать\nминимум 6 символов");
                return;
            }

            // Проверка совпадения паролей
            if (password != confirmPassword)
            {
                ShowRegisterError("Пароли не совпадают");
                return;
            }

            // Проверка уникальности email
            if (_dbService.IsEmailTaken(email))
            {
                ShowRegisterError("Этот email уже зарегистрирован");
                return;
            }

            // Проверка уникальности имени пользователя
            if (_dbService.IsUsernameTaken(username))
            {
                ShowRegisterError("Это имя пользователя уже занято");
                return;
            }

            // Регистрация пользователя
            try
            {
                int userId = _dbService.RegisterUser(email, password, username);

                if (userId > 0)
                {
                    MessageBox.Show("Регистрация успешна!\nТеперь вы можете войти.", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    // Очищаем поля
                    RegisterEmailTextBox.Clear();
                    RegisterPasswordBox.Clear();
                    ConfirmPasswordBox.Clear();
                    UsernameTextBox.Clear();

                    // Переключаем на вкладку входа
                    LoginTabRadio.IsChecked = true;

                    // Заполняем поля входа
                    EmailTextBox.Text = email;
                }
                else
                {
                    ShowRegisterError("Ошибка регистрации. Попробуйте еще раз.");
                }
            }
            catch (Exception ex)
            {
                ShowRegisterError($"Ошибка регистрации:\n{ex.Message}");
            }
        }

        private void ForgotPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            string email = EmailTextBox.Text.Trim();

            if (string.IsNullOrEmpty(email) || ContainsOnlySpaces(email))
            {
                ShowLoginError("Введите email для восстановления");
                return;
            }

            if (!IsValidEmail(email))
            {
                ShowLoginError("Некорректный формат email");
                return;
            }

            MessageBox.Show($"На email {email} отправлена ссылка\nдля восстановления пароля.\n\n" +
                          "(Это заглушка - в реальном приложении\nздесь был бы отправлен email)",
                          "Восстановление пароля",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BackToLoginButton_Click(object sender, RoutedEventArgs e)
        {
            LoginTabRadio.IsChecked = true;
        }

        private void ShowLoginError(string message)
        {
            LoginErrorText.Text = message;
            LoginErrorText.Visibility = Visibility.Visible;
        }

        private void ShowRegisterError(string message)
        {
            RegisterErrorText.Text = message;
            RegisterErrorText.Visibility = Visibility.Visible;
        }

        // Очистка ошибок при вводе текста
        private void EmailTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoginErrorText.Visibility = Visibility.Collapsed;
        }

        private void RegisterEmailTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RegisterErrorText.Visibility = Visibility.Collapsed;
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            LoginErrorText.Visibility = Visibility.Collapsed;
        }

        private void RegisterPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            RegisterErrorText.Visibility = Visibility.Collapsed;
        }
    }
}