using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ChatApp.Models;
using ChatApp.Services;

namespace ChatApp.Views
{
    public partial class AddContactWindow : Window
    {
        private DatabaseService _dbService;
        private int _currentUserId;
        private List<SearchUserView> _searchResults;

        public AddContactWindow(int userId, DatabaseService dbService)
        {
            InitializeComponent();
            _currentUserId = userId;
            _dbService = dbService;
            _searchResults = new List<SearchUserView>();
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string searchTerm = SearchTextBox.Text.Trim();

            if (string.IsNullOrEmpty(searchTerm))
            {
                MessageBox.Show("Введите имя или email для поиска", "Поиск",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SearchUsers(searchTerm);
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Автопоиск при вводе
            string searchTerm = SearchTextBox.Text.Trim();
            if (searchTerm.Length >= 2)
            {
                SearchUsers(searchTerm);
            }
        }

        private void SearchUsers(string searchTerm)
        {
            var users = _dbService.SearchUsersNotInContacts(_currentUserId, searchTerm);

            _searchResults.Clear();

            foreach (var user in users)
            {
                _searchResults.Add(new SearchUserView
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    Status = user.Status,
                    StatusText = GetStatusText(user.Status),
                    AvatarImage = user.Avatar != null ? LoadImageFromBytes(user.Avatar) : null
                });
            }

            SearchResultsListBox.ItemsSource = _searchResults;

            if (_searchResults.Count == 0)
            {
                MessageBox.Show("Пользователи не найдены", "Результат поиска",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private string GetStatusText(string status)
        {
            if (status == "online") return "🟢 В сети";
            if (status == "offline") return "⚪ Не в сети";
            if (status == "dnd") return "⛔ Не беспокоить";
            return "⚪ Не в сети";
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

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int contactId)
            {
                try
                {
                    bool success = _dbService.AddContact(_currentUserId, contactId);

                    if (success)
                    {
                        MessageBox.Show("Контакт успешно добавлен", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        // Обновляем список результатов поиска
                        var userToRemove = _searchResults.FirstOrDefault(u => u.Id == contactId);
                        if (userToRemove != null)
                        {
                            _searchResults.Remove(userToRemove);
                            SearchResultsListBox.ItemsSource = _searchResults.ToList();
                        }
                    }
                    else
                    {
                        MessageBox.Show("Не удалось добавить контакт", "Ошибка",
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

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void SearchResultsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Можно добавить просмотр профиля при выборе
        }
    }

    // Класс для отображения результатов поиска
    public class SearchUserView
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Status { get; set; }
        public string StatusText { get; set; }
        public ImageSource AvatarImage { get; set; }
    }
}