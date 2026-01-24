using ChatApp.Models;
using ChatApp.Services;
using System;
using System.Windows;

namespace ChatApp.Views
{
    public partial class NotificationSettingsDialog : Window
    {
        private NotificationService _notificationService;
        private User _currentUser;

        public NotificationSettingsDialog(NotificationService notificationService, User currentUser)
        {
            InitializeComponent();
            _notificationService = notificationService;
            _currentUser = currentUser;
            LoadSettings();
        }

        private void LoadSettings()
        {
            var settings = _notificationService.GetCurrentSettings();
            EnabledCheckBox.IsChecked = settings.Enabled;
            ShowPreviewCheckBox.IsChecked = settings.ShowPreview;
            PlaySoundCheckBox.IsChecked = settings.PlaySound;
            OnlyBannerCheckBox.IsChecked = settings.OnlyBanner;
            SmartNotificationsCheckBox.IsChecked = settings.SmartNotifications;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var settings = new NotificationSettings
            {
                UserId = _currentUser.Id,
                Enabled = EnabledCheckBox.IsChecked ?? true,
                ShowPreview = ShowPreviewCheckBox.IsChecked ?? true,
                PlaySound = PlaySoundCheckBox.IsChecked ?? true,
                OnlyBanner = OnlyBannerCheckBox.IsChecked ?? false,
                SmartNotifications = SmartNotificationsCheckBox.IsChecked ?? true
            };

            _notificationService.UpdateSettings(settings);
            this.DialogResult = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            // Тестовое уведомление
            var testMessage = new Message
            {
                Id = 999,
                SenderId = 0,
                Content = "Это тестовое уведомление для проверки настроек",
                CreatedAt = DateTime.Now
            };

            var testUser = new User
            {
                Id = 0,
                Username = "Тестовый пользователь",
                Email = "test@example.com"
            };

            _notificationService.ShowMessageNotification(testMessage, testUser);
        }
    }
}