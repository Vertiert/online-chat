using System.Windows;

namespace ChatApp.Views
{
    public partial class InputDialog : Window
    {
        public string Answer { get; set; }

        public InputDialog(string question, string title)
        {
            InitializeComponent();
            this.Title = title;
            QuestionText.Text = question;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            Answer = AnswerTextBox.Text;
            DialogResult = true;
        }
    }
}