using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace ChatApp.Models
{
    public class MessageView : INotifyPropertyChanged
    {
        private int _id;
        private int _senderId;
        private string _senderName;
        private string _content;
        private string _time;
        private string _status;
        private string _statusSymbol;
        private Style _messageStyle;
        private Visibility _showSenderName;
        private Visibility _showStatus;
        private bool _isMyMessage;
        private bool _isFile;
        private string _fileName;
        private byte[] _fileData;
        private string _fileSize;
        private bool _isDeleted;
        private bool _isEdited;
        private DateTime? _editedAt;

        private bool _isRead;
        public bool IsRead
        {
            get => _isRead;
            set
            {
                if (_isRead != value)
                {
                    _isRead = value;
                    OnPropertyChanged();
                }
            }
        }
        public int Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public int SenderId
        {
            get => _senderId;
            set { _senderId = value; OnPropertyChanged(); }
        }

        public string SenderName
        {
            get => _senderName;
            set { _senderName = value; OnPropertyChanged(); }
        }

        public string Content
        {
            get => _content;
            set
            {
                if (_content != value)
                {
                    _content = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DisplayContent));
                }
            }
        }

        public string Time
        {
            get => _time;
            set { _time = value; OnPropertyChanged(); }
        }

        public string Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(StatusSymbol));
                    OnPropertyChanged(nameof(StatusColor));
                    OnPropertyChanged(nameof(DisplayContent));

                    // При установке статуса "read" автоматически устанавливаем IsRead
                    if (value == "read")
                    {
                        IsRead = true;
                    }
                }
            }
        }


        public string StatusSymbol
        {
            get
            {
                switch (_status)
                {
                    case "sent": return "✓";
                    case "delivered": return "✓✓";
                    case "read": return "✓✓";
                    default: return "✓";
                }
            }
        }

        public Brush StatusColor
        {
            get
            {
                switch (_status)
                {
                    case "sent": return Brushes.Gray;
                    case "delivered": return Brushes.Blue;
                    case "read": return Brushes.Green;
                    default: return Brushes.Gray;
                }
            }
        }

        public Style MessageStyle
        {
            get => _messageStyle;
            set { _messageStyle = value; OnPropertyChanged(); }
        }

        public Visibility ShowSenderName
        {
            get => _showSenderName;
            set { _showSenderName = value; OnPropertyChanged(); }
        }

        public Visibility ShowStatus
        {
            get => _showStatus;
            set { _showStatus = value; OnPropertyChanged(); }
        }

        public bool IsMyMessage
        {
            get => _isMyMessage;
            set { _isMyMessage = value; OnPropertyChanged(); }
        }

        public bool IsFile
        {
            get => _isFile;
            set { _isFile = value; OnPropertyChanged(); }
        }

        public string FileName
        {
            get => _fileName;
            set { _fileName = value; OnPropertyChanged(); }
        }

        public byte[] FileData
        {
            get => _fileData;
            set { _fileData = value; OnPropertyChanged(); }
        }

        public string FileSize
        {
            get => _fileSize;
            set { _fileSize = value; OnPropertyChanged(); }
        }

        public bool IsDeleted
        {
            get => _isDeleted;
            set
            {
                if (_isDeleted != value)
                {
                    _isDeleted = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DisplayContent));
                    OnPropertyChanged(nameof(DeletedColor));
                    OnPropertyChanged(nameof(DeletedFontStyle));
                }
            }
        }

        public bool IsEdited
        {
            get => _isEdited;
            set
            {
                if (_isEdited != value)
                {
                    _isEdited = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(EditedInfo));
                }
            }
        }

        public DateTime? EditedAt
        {
            get => _editedAt;
            set
            {
                if (_editedAt != value)
                {
                    _editedAt = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(EditedInfo));
                }
            }
        }

        public string DisplayContent => IsDeleted ? "[Сообщение удалено]" : Content;

        public Brush DeletedColor => IsDeleted ? Brushes.Gray : Brushes.Black;

        public FontStyle DeletedFontStyle => IsDeleted ? FontStyles.Italic : FontStyles.Normal;

        public string EditedInfo => IsEdited ? "Изменено" : "";

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}