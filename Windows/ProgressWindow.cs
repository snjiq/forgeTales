using System.Windows;

namespace ForgeTales.Windows
{
    public partial class ProgressWindow : Window
    {
        public ProgressWindow(string message)
        {
            InitializeComponent();
            MessageText.Text = message;
        }

        private void InitializeComponent()
        {
            Width = 300;
            Height = 100;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.NoResize;

            var stackPanel = new System.Windows.Controls.StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var textBlock = new System.Windows.Controls.TextBlock
            {
                Name = "MessageText",
                Margin = new Thickness(10),
                TextAlignment = TextAlignment.Center
            };

            var progressBar = new System.Windows.Controls.ProgressBar
            {
                IsIndeterminate = true,
                Width = 250,
                Height = 20
            };

            stackPanel.Children.Add(textBlock);
            stackPanel.Children.Add(progressBar);
            Content = stackPanel;
        }

        public System.Windows.Controls.TextBlock MessageText { get; set; }
            = new System.Windows.Controls.TextBlock();
    }
}