using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using ChatApp.Models;
using ChatApp.Services;

namespace ChatApp.Views
{
    public partial class ProfileWindow : Window
    {
        private User _currentUser;
        private DatabaseService _dbService;
        private byte[] _newAvatar;

        public ProfileWindow(User user, DatabaseService dbService)
        {
            InitializeComponent();
            _currentUser = user;
            _dbService = dbService;
            LoadUserData();
        }

        private void LoadUserData()
        {
            UsernameTextBox.Text = _currentUser.Username;
            BioTextBox.Text = _currentUser.Bio;

            // Устанавливаем статус
            foreach (ComboBoxItem item in StatusComboBox.Items)
            {
                if (item.Tag?.ToString() == _currentUser.Status)
                {
                    StatusComboBox.SelectedItem = item;
                    break;
                }
            }

            // Если статус не найден, выбираем первый
            if (StatusComboBox.SelectedItem == null && StatusComboBox.Items.Count > 0)
            {
                StatusComboBox.SelectedIndex = 0;
            }

            // Устанавливаем аватар
            if (_currentUser.Avatar != null && _currentUser.Avatar.Length > 0)
            {
                LoadAvatar(_currentUser.Avatar);
            }
        }

        private void LoadAvatar(byte[] avatarBytes)
        {
            try
            {
                using (var stream = new MemoryStream(avatarBytes))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = stream;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    AvatarImage.ImageSource = bitmap;
                }
            }
            catch { /* Игнорируем ошибки загрузки аватара */ }
        }

        private void ChangeAvatarBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Изображения (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png";

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    _newAvatar = File.ReadAllBytes(dialog.FileName);
                    LoadAvatar(_newAvatar);
                }
                catch
                {
                    MessageBox.Show("Ошибка загрузки изображения", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            // Обновляем данные пользователя
            _currentUser.Username = UsernameTextBox.Text.Trim();
            _currentUser.Bio = BioTextBox.Text.Trim();

            if (StatusComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                _currentUser.Status = selectedItem.Tag?.ToString() ?? "offline";
            }

            if (_newAvatar != null)
            {
                _currentUser.Avatar = _newAvatar;
            }

            // Сохраняем в базу
            try
            {
                _dbService.UpdateUserProfile(_currentUser);
                MessageBox.Show("Профиль обновлен", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}