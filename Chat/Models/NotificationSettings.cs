using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ChatApp.Models
{
    public class NotificationSettings : INotifyPropertyChanged
    {
        private int _userId;
        private bool _enabled = true;
        private bool _showPreview = true;
        private bool _playSound = true;
        private bool _onlyBanner = false;
        private bool _smartNotifications = true;

        public int UserId
        {
            get => _userId;
            set { _userId = value; OnPropertyChanged(); }
        }

        public bool Enabled
        {
            get => _enabled;
            set { _enabled = value; OnPropertyChanged(); }
        }

        public bool ShowPreview
        {
            get => _showPreview;
            set { _showPreview = value; OnPropertyChanged(); }
        }

        public bool PlaySound
        {
            get => _playSound;
            set { _playSound = value; OnPropertyChanged(); }
        }

        public bool OnlyBanner
        {
            get => _onlyBanner;
            set { _onlyBanner = value; OnPropertyChanged(); }
        }

        public bool SmartNotifications
        {
            get => _smartNotifications;
            set { _smartNotifications = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}