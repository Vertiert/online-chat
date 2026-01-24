using System;
using System.Windows;
using System.Windows.Media.Imaging;
using ChatApp.Models;

namespace ChatApp.Views
{
    public partial class UserProfileWindow : Window
    {
        public UserProfileWindow(User user)
        {
            InitializeComponent();
            Title = $"Профиль: {user.Username}";
            LoadUserData(user);
        }

        private void LoadUserData(User user)
        {
            UsernameText.Text = user.Username;
            StatusText.Text = GetStatusText(user.Status);
            BioText.Text = string.IsNullOrEmpty(user.Bio) ? "Пользователь не добавил информацию о себе" : user.Bio;

            // Загружаем аватар
            if (user.Avatar != null && user.Avatar.Length > 0)
            {
                try
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.StreamSource = new System.IO.MemoryStream(user.Avatar);
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.EndInit();
                    UserAvatarImage.ImageSource = image;
                }
                catch
                {
                    // Игнорируем ошибки загрузки аватара
                }
            }
        }

        private string GetStatusText(string status)
        {
            if (status == "online") return "🟢 В сети";
            if (status == "offline") return "⚪ Не в сети";
            if (status == "dnd") return "⛔ Не беспокоить";
            return "⚪ Не в сети";
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}