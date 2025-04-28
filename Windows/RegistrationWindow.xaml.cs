using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using ForgeTales.Classes;
using ForgeTales.Model;

namespace ForgeTales.Windows
{
    public partial class RegistrationWindow : Window
    {
        private string _readerAvatarPath;
        private string _authorAvatarPath;

        public RegistrationWindow()
        {
            InitializeComponent();
        }

        private void LoginText_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            new MainWindow().Show();
            this.Close();
        }

        private void SelectReaderAvatar_Click(object sender, RoutedEventArgs e)
        {
            _readerAvatarPath = SelectAvatar();
            if (!string.IsNullOrEmpty(_readerAvatarPath))
            {
                try
                {
                    ReaderAvatarImage.Source = new BitmapImage(new Uri(_readerAvatarPath));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке аватара: {ex.Message}");
                }
            }
        }

        private void SelectAuthorAvatar_Click(object sender, RoutedEventArgs e)
        {
            _authorAvatarPath = SelectAvatar();
            if (!string.IsNullOrEmpty(_authorAvatarPath))
            {
                try
                {
                    AuthorAvatarImage.Source = new BitmapImage(new Uri(_authorAvatarPath));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке аватара: {ex.Message}");
                }
            }
        }

        private string SelectAvatar()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.jpg, *.jpeg, *.png)|*.jpg;*.jpeg;*.png",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
            };

            return openFileDialog.ShowDialog() == true ? openFileDialog.FileName : null;
        }

        private string SaveAvatar(string avatarPath)
        {
            if (string.IsNullOrEmpty(avatarPath))
                return null;

            try
            {
                string avatarsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Avatars");
                Directory.CreateDirectory(avatarsDir);
                string destFileName = Path.Combine(avatarsDir, $"{Guid.NewGuid()}{Path.GetExtension(avatarPath)}");
                File.Copy(avatarPath, destFileName);
                return destFileName;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении аватара: {ex.Message}");
                return null;
            }
        }

        private void ReaderRegisterButton_Click(object sender, RoutedEventArgs e)
        {
            string username = ReaderUsernameTextBox.Text;
            string email = ReaderEmailTextBox.Text;
            string password = ReaderPasswordBox.Password;
            string confirmPassword = ReaderRepeatPasswordBox.Password;

            if (password != confirmPassword)
            {
                MessageBox.Show("Пароли не совпадают");
                return;
            }

            var readerDb = new ReaderFromDb();

            if (!readerDb.CheckUsername(username))
            {
                return;
            }

            string avatarUrl = SaveAvatar(_readerAvatarPath);

            try
            {
                readerDb.AddReaderWithAvatar(username, email, password, avatarUrl);
                new MainWindow().Show();
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка регистрации: {ex.Message}");
            }
        }

        private void AuthorRegisterButton_Click(object sender, RoutedEventArgs e)
        {
            string name = AuthorNameTextBox.Text;
            string bio = AuthorBioTextBox.Text;
            string email = AuthorEmailTextBox.Text;
            string password = AuthorPasswordBox.Password;
            string confirmPassword = AuthorRepeatPasswordBox.Password;

            if (password != confirmPassword)
            {
                MessageBox.Show("Пароли не совпадают");
                return;
            }

            string avatarUrl = SaveAvatar(_authorAvatarPath);

            var authorDb = new AuthorFromDb();
            if (authorDb.AddAuthorWithReader(name, bio, email, password, avatarUrl))
            {
                new MainWindow().Show();
                Close();
            }
        }
    }
}