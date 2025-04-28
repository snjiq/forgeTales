using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Npgsql;
using ForgeTales.Model;
using ZXing;
using ZXing.Common;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace ForgeTales.Pages
{
    public partial class SubscriptionPage : Page
    {
        public class SubscriptionItem
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public decimal MonthlyPrice { get; set; }
            public int HeartsBonus { get; set; }
        }

        private readonly Reader _currentReader;
        private readonly ReaderFromDb _readerDb = new ReaderFromDb();
        private string _currentPaymentId;
        private decimal _currentAmount;
        private int _selectedSubId;

        public SubscriptionPage(Reader reader)
        {
            InitializeComponent();
            _currentReader = reader;
            Loaded += SubscriptionPage_Loaded;
        }

        private void SubscriptionPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadUserData();
            LoadSubscriptions();
        }

        private void LoadUserData()
        {
            UsernameTextBlock.Text = _currentReader.Username;
            HeartsTextBlock.Text = $"Сердечки: {_currentReader.HeartsBalance}";

            var subscription = _readerDb.GetCurrentSubscription(_currentReader.ReaderId);
            CurrentSubscriptionText.Text = subscription != null
                ? $"{subscription.Name} (до {subscription.EndDate:dd.MM.yyyy})"
                : "Нет активной подписки";
        }

        private void LoadSubscriptions()
        {
            try
            {
                using (var conn = new NpgsqlConnection(DbConnection.connectionStr))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(
                        "SELECT subscription_id, name, description, price_monthly FROM subscriptions", conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            SubscriptionsListBox.Items.Clear();
                            while (reader.Read())
                            {
                                var sub = new SubscriptionItem
                                {
                                    Id = reader.GetInt32(0),
                                    Name = reader.GetString(1),
                                    Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                    MonthlyPrice = reader.GetDecimal(3),
                                    HeartsBonus = GetHeartsBonus(reader.GetInt32(0))
                                };

                                SubscriptionsListBox.Items.Add(sub);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки подписок: {ex.Message}");
            }
        }

        private int GetHeartsBonus(int subscriptionId)
        {
            switch (subscriptionId)
            {
                case 1: return 0;   // Взахлеб
                case 2: return 30;  // Большое сердце
                case 3: return 30;  // Набор новичка
                case 4: return 90;  // Все вместе
                default: return 0;
            }
        }

        private void SubscribeButton_Click(object sender, RoutedEventArgs e)
        {
            if (SubscriptionsListBox.SelectedItem is SubscriptionItem selectedSub)
            {
                _selectedSubId = selectedSub.Id;
                _currentAmount = selectedSub.MonthlyPrice;
                _currentPaymentId = Guid.NewGuid().ToString();

                GenerateQRCode($"pay:{_currentPaymentId}:{_currentAmount}");

                PaymentPanel.Visibility = Visibility.Visible;
                AmountTextBlock.Text = $"Сумма: {_currentAmount} руб.";
                SubscriptionDetailsText.Text = $"{selectedSub.Name}\n{selectedSub.Description}";
                HeartsBonusText.Text = $"Бонус: {selectedSub.HeartsBonus} сердечек";
            }
            else
            {
                MessageBox.Show("Выберите подписку");
            }
        }

        private void GenerateQRCode(string content)
        {
            var writer = new BarcodeWriter
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new EncodingOptions
                {
                    Width = 300,
                    Height = 300,
                    Margin = 0
                }
            };

            var bitmap = writer.Write(content);
            QrCodeImage.Source = ConvertBitmapToBitmapImage(bitmap);
        }

        private BitmapImage ConvertBitmapToBitmapImage(Bitmap bitmap)
        {
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                return bitmapImage;
            }
        }

        private void CheckPaymentButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PaymentPanel.Visibility = Visibility.Collapsed;

                // Добавляем подписку
                var result = _readerDb.AddSubscription(
                    _currentReader.ReaderId,
                    _selectedSubId,
                    1, // 1 месяц
                    _currentAmount);

                if (result > 0)
                {
                    // Начисляем бонусные сердечки
                    int heartsBonus = GetHeartsBonus(_selectedSubId);
                    if (heartsBonus > 0)
                    {
                        bool heartsAdded = _readerDb.AddHearts(
                            _currentReader.ReaderId,
                            heartsBonus,
                            "subscription_bonus");


                    }

                    // Обновляем данные пользователя
                    _currentReader.HeartsBalance = _readerDb.GetHeartsBalance(_currentReader.ReaderId);
                    LoadUserData();

                    MessageBox.Show($"Оплата прошла успешно!\nПодписка активирована.\nНачислено бонусных сердечек: {heartsBonus}");
                }
                else
                {
                    MessageBox.Show("Ошибка активации подписки");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обработки платежа: {ex.Message}");
            }
        }

        private void CancelPaymentButton_Click(object sender, RoutedEventArgs e)
        {
            PaymentPanel.Visibility = Visibility.Collapsed;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}