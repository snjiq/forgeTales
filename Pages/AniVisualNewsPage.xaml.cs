using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace ForgeTales.Pages
{
    public partial class AniVisualNewsPage : Page
    {
        private const string NewsUrl = "https://www.goha.ru/anime/news";

        public AniVisualNewsPage()
        {
            InitializeComponent();
            this.Loaded += (s, e) => {
                HideScriptErrors();
                NewsBrowser.Navigate("https://www.goha.ru/anime/news");
            };
        }

        private void NewsBrowser_LoadCompleted(object sender, NavigationEventArgs e)
        {
            // Скрытие скриптовых ошибок
            HideScriptErrors();
        }

        private void HideScriptErrors()
        {
            // Получаем COM-объект браузера
            dynamic activeX = this.NewsBrowser.GetType().InvokeMember("ActiveXInstance",
                System.Reflection.BindingFlags.GetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null, this.NewsBrowser, new object[] { });

            // Отключаем отображение ошибок
            activeX.Silent = true;
            activeX.RegisterAsBrowser = true;

        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (NewsBrowser.CanGoBack)
                NewsBrowser.GoBack();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            NewsBrowser.Refresh();
        }

        private void OpenInBrowser_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = NewsBrowser.Source?.ToString() ?? NewsUrl,
                UseShellExecute = true
            });
        }
    }
}