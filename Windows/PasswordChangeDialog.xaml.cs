using System.Windows;
using ForgeTales.Model;
using ForgeTales.Classes;

namespace ForgeTales.Windows
{
    public partial class PasswordChangeDialog : Window
    {
        private readonly ReaderFromDb _readerDb = new ReaderFromDb();
        private readonly int _readerId;

        public PasswordChangeDialog(int readerId)
        {
            InitializeComponent();
            _readerId = readerId;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string currentPassword = CurrentPasswordBox.Password;
            string newPassword = NewPasswordBox.Password;
            string confirmPassword = ConfirmPasswordBox.Password;

            // Проверка текущего пароля
            if (!_readerDb.CheckPassword(_readerId, currentPassword))
            {
                MessageBox.Show("Текущий пароль неверен");
                return;
            }

            // Проверка совпадения новых паролей
            if (newPassword != confirmPassword)
            {
                MessageBox.Show("Новые пароли не совпадают");
                return;
            }

            // Проверка сложности пароля
            if (!Verification.ValidatePasswordStrength(newPassword))
            {
                MessageBox.Show("Пароль должен содержать:\n- минимум 8 символов\n- цифру\n- заглавную и строчную буквы\n- спецсимвол");
                return;
            }

            // Обновление пароля
            string hashedPassword = Verification.GetSHA512Hash(newPassword);
            if (_readerDb.UpdatePassword(_readerId, hashedPassword))
            {
                MessageBox.Show("Пароль успешно изменен");
                this.Close();
            }
            else
            {
                MessageBox.Show("Ошибка при изменении пароля");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}