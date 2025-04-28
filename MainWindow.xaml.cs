using System;
using System.Windows;
using System.Windows.Input;
using ForgeTales.Model;
using ForgeTales.Windows;
using ForgeTales.Classes;
using Microsoft.WindowsAPICodePack.Dialogs.Controls;

namespace ForgeTales
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        { 
            InitializeComponent();
            LoginTextBox.Text = "test";
            if (Application.Current.Properties.Contains("CurrentUser"))
            {
                OpenMainMenu();
            }
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = LoginTextBox.Text;
            string password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Введите имя пользователя и пароль");
                return;
            }

            var readerFromDb = new ReaderFromDb();
            var authorFromDb = new AuthorFromDb();

            // Сначала пробуем войти как читатель
            var reader = readerFromDb.GetReader(username, password);
            if (reader != null)
            {
                Application.Current.Properties["CurrentUser"] = reader;

                // Проверяем, является ли читатель автором
                var author = authorFromDb.GetAuthorByReaderId(reader.ReaderId);
                if (author != null)
                {
                    Application.Current.Properties["CurrentAuthor"] = author;
                }

                OpenMainMenu();
                return;
            }

            // Если не читатель, пробуем войти как автор (для старых записей)
            var authorLogin = authorFromDb.AuthenticateAuthor(username, password);
            if (authorLogin != null)
            {
                Application.Current.Properties["CurrentUser"] = authorLogin;
                Application.Current.Properties["CurrentAuthor"] = authorLogin;
                OpenMainMenu();
                return;
            }

            MessageBox.Show("Неверное имя пользователя или пароль");
        }

        private void OpenMainMenu()
        {
            new MainMenu().Show();
            this.Close();
        }

        private void RegisterText_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            new RegistrationWindow().Show();
            this.Close();
        }

        public static void Logout()
        {
            if (Application.Current.Properties.Contains("CurrentUser"))
            {
                Application.Current.Properties.Remove("CurrentUser");
            }
            if (Application.Current.Properties.Contains("CurrentAuthor"))
            {
                Application.Current.Properties.Remove("CurrentAuthor");
            }
            new MainWindow().Show();
        }
    }
}