using System.Windows;

namespace ChatApp.Views
{
    public partial class EditMessageDialog : Window
    {
        public string NewContent { get; private set; }

        public EditMessageDialog(string currentContent)
        {
            InitializeComponent();
            MessageTextBox.Text = currentContent;
            MessageTextBox.Focus();
            MessageTextBox.SelectAll();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            NewContent = MessageTextBox.Text.Trim();
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}