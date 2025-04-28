using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using ForgeTales.Classes;
using ForgeTales.Model;
using ForgeTales.Windows;
using Microsoft.Win32;

namespace ForgeTales.Pages
{
    public partial class EditProfilePage : Page, INotifyPropertyChanged
    {
        private Author _author;
        private Reader _reader;
        private bool _isAuthor;
        private bool _showPasswordFields;
        private MainMenu _mainWindow;

        public event PropertyChangedEventHandler PropertyChanged;

        public string Username
        {
            get => _isAuthor ? _author.Name : _reader.Username;
            set
            {
                if (_isAuthor) _author.Name = value;
                else _reader.Username = value;
                OnPropertyChanged(nameof(Username));
            }
        }

        public string Email
        {
            get => _isAuthor ? _author.Email : _reader.Email;
            set
            {
                if (_isAuthor) _author.Email = value;
                else _reader.Email = value;
                OnPropertyChanged(nameof(Email));
            }
        }

        public string Bio
        {
            get => _isAuthor ? _author.Bio : null;
            set
            {
                if (_isAuthor) _author.Bio = value;
                OnPropertyChanged(nameof(Bio));
            }
        }

        public string AvatarSource
        {
            get => _isAuthor ? _author.AvatarUrl : _reader.AvatarUrl;
            set
            {
                if (_isAuthor)
                    _author.AvatarUrl = value;
                else
                    _reader.AvatarUrl = value;
                OnPropertyChanged(nameof(AvatarSource));
            }
        }

        public bool IsAuthor
        {
            get => _isAuthor;
            set
            {
                _isAuthor = value;
                OnPropertyChanged(nameof(IsAuthor));
            }
        }

        public bool ShowPasswordFields
        {
            get => _showPasswordFields;
            set
            {
                _showPasswordFields = value;
                OnPropertyChanged(nameof(ShowPasswordFields));
                OnPropertyChanged(nameof(ShowPasswordButtonText));
            }
        }

        public string ShowPasswordButtonText => _showPasswordFields ? "Скрыть" : "Изменить пароль";

        public EditProfilePage(Author author)
        {
            InitializeComponent();
            _author = author;
            _isAuthor = true;
            _mainWindow = Application.Current.MainWindow as MainMenu;
            DataContext = this;
        }

        public EditProfilePage(Reader reader)
        {
            InitializeComponent();
            _reader = reader;
            _isAuthor = false;
            _mainWindow = Application.Current.MainWindow as MainMenu;
            DataContext = this;
        }

        private void ChangePassword_Click(object sender, RoutedEventArgs e)
        {
            ShowPasswordFields = !ShowPasswordFields;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsAuthor)
            {
                _mainWindow?.UpdateAvatar(_author.AvatarUrl);
                UpdateAuthorProfile();
            }
            else
            {
                _mainWindow?.UpdateAvatar(_reader.AvatarUrl);
                UpdateReaderProfile();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private async void UpdateAuthorProfile()
        {
            if (ShowPasswordFields && !ValidatePasswords())
                return;

            try
            {
                var authorDb = new AuthorFromDb();
                var readerDb = new ReaderFromDb();

                if (ShowPasswordFields)
                {
                    if (!Verification.ValidatePasswordStrength(NewPasswordBox.Password))
                    {
                        MessageBox.Show("Новый пароль должен содержать:\n- минимум 8 символов\n- цифру\n- заглавную и строчную буквы\n- спецсимвол");
                        return;
                    }
                    _author.PasswordHash = Verification.GetSHA512Hash(NewPasswordBox.Password);
                }

                if (await authorDb.UpdateAuthorAsync(_author))
                {
                    await readerDb.UpdateAvatarAsync((int)_author.ReaderId, _author.AvatarUrl);

                    MessageBox.Show("Профиль успешно обновлен");
                    if (Application.Current.MainWindow is MainMenu mainWindow)
                    {
                        mainWindow.UpdateAvatar(_author.AvatarUrl);
                    }
                    Application.Current.Properties["CurrentUser"] = _author;
                    ShowPasswordFields = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private async void UpdateReaderProfile()
        {
            if (ShowPasswordFields)
            {
                if (!ValidatePasswords())
                    return;

                if (!Verification.ValidatePasswordStrength(NewPasswordBox.Password))
                {
                    MessageBox.Show("Новый пароль должен содержать:\n- минимум 8 символов\n- цифру\n- заглавную и строчную буквы\n- спецсимвол");
                    return;
                }

                _reader.PasswordHash = Verification.GetSHA512Hash(NewPasswordBox.Password);
            }

            try
            {
                var readerDb = new ReaderFromDb();
                if (await readerDb.UpdateReaderProfileAsync(_reader))
                {
                    MessageBox.Show("Профиль успешно обновлен");
                    ShowPasswordFields = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private bool ValidatePasswords()
        {
            if (string.IsNullOrEmpty(NewPasswordBox.Password))
            {
                MessageBox.Show("Введите новый пароль");
                return false;
            }

            if (NewPasswordBox.Password != ConfirmPasswordBox.Password)
            {
                MessageBox.Show("Пароли не совпадают");
                return false;
            }

            return true;
        }

        private void ChangeAvatar_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp",
                Title = "Выберите изображение для аватара"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    byte[] imageBytes = File.ReadAllBytes(openFileDialog.FileName);
                    AvatarSource = Convert.ToBase64String(imageBytes);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки изображения: {ex.Message}");
                }
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}