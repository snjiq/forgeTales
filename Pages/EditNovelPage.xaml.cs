using System;
using System.Windows;
using System.Windows.Controls;
using ForgeTales.Classes;
using ForgeTales.Model;
using System.Collections.Generic;
using Npgsql;
using System.Windows.Media.Imaging;

namespace ForgeTales.Pages
{
    public partial class EditNovelPage : Page
    {
        private readonly Author _currentAuthor;
        private readonly Novel _novelToEdit;
        private readonly NovelFromDb _novelDb = new NovelFromDb();

        public EditNovelPage(Author author, Novel novel)
        {
            InitializeComponent();
            _currentAuthor = author ?? throw new ArgumentNullException(nameof(author));
            _novelToEdit = novel ?? throw new ArgumentNullException(nameof(novel));

            DataContext = _novelToEdit;
            LoadGenres();
        }

        private void LoadGenres()
        {
            try
            {
                List<GenreGroup> genres = new List<GenreGroup>();
                using (var connection = new NpgsqlConnection(DbConnection.connectionStr))
                {
                    connection.Open();
                    var command = new NpgsqlCommand("SELECT genre_id, name FROM genres ORDER BY name", connection);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            genres.Add(new GenreGroup(reader.GetInt32(0), reader.GetString(1)));
                        }
                    }
                }

                GenreComboBox.ItemsSource = genres;
                if (_novelToEdit.GenreId.HasValue)
                {
                    GenreComboBox.SelectedValue = _novelToEdit.GenreId.Value;
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка загрузки жанров: {ex.Message}");
            }
        }

        private void CoverUrlTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(CoverUrlTextBox.Text))
                {
                    CoverImage.Source = new BitmapImage(new Uri(CoverUrlTextBox.Text, UriKind.RelativeOrAbsolute));
                }
            }
            catch
            {
                // Ошибка загрузки изображения - оставляем текущее
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_novelToEdit.Title))
            {
                ShowError("Введите название новеллы");
                TitleTextBox.Focus();
                return;
            }

            try
            {
                if (_novelDb.UpdateNovel(_novelToEdit))
                {
                    MessageBox.Show("Изменения сохранены успешно!");
                    NavigationService?.Navigate(new NovelsPage(_currentAuthor));
                }
                else
                {
                    ShowError("Не удалось сохранить изменения");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при сохранении: {ex.Message}");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.GoBack();
        }

        private void ShowError(string message)
        {
            MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public class GenreGroup
    {
        public int GenreId { get; set; }
        public string Name { get; set; }

        public GenreGroup(int genreId, string name)
        {
            GenreId = genreId;
            Name = name;
        }
    }
}