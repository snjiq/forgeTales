using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ForgeTales.Classes;
using ForgeTales.Model;
using Npgsql;

namespace ForgeTales.Pages
{
    public partial class NovelsPage : Page
    {
        private Reader _currentReader;
        private Author _currentAuthor;
        private List<Novel> _allNovels;
        private List<Author> _allAuthors;
        private List<GenreGroup> _allGenres;

        public NovelsPage(Reader reader)
        {
            InitializeComponent();
            _currentReader = reader;
            CheckIfReaderIsAuthor();
            LoadData();
            LogAction("novels_view");
        }

        public NovelsPage(Author author)
        {
            InitializeComponent();
            _currentAuthor = author;
            LoadData();
            LogAction("novels_view");
        }

        public NovelsPage()
        {
            InitializeComponent();
            LoadData();
        }

        private void LogAction(string actionType)
        {
            int? userId = _currentReader?.ReaderId ?? _currentAuthor?.AuthorId;
            if (!userId.HasValue) return;

            using (var conn = new NpgsqlConnection(DbConnection.connectionStr))
            {
                try
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(
                        "INSERT INTO reading_sessions (reader_id, action_type, start_time) VALUES (@id, @action, NOW())",
                        conn))
                    {
                        cmd.Parameters.AddWithValue("id", userId.Value);
                        cmd.Parameters.AddWithValue("action", actionType);
                        cmd.ExecuteNonQuery();
                    }
                }
                catch
                {
                    // Игнорируем ошибки логирования
                }
            }
        }

        public async void LoadData()
        {
            try
            {
                LoadingIndicator.Visibility = Visibility.Visible;

                var novelDb = new NovelFromDb();
                var genreDb = new GenreFromDb(DbConnection.connectionStr);

                _allNovels = await Task.Run(() => novelDb.GetAllNovels());
                _allGenres = (await Task.Run(() => genreDb.GetAllGenres()))
                    .Select(g => new GenreGroup(g.GenreId, g.Name))
                    .ToList();

                _allAuthors = await Task.Run(() => new AuthorFromDb().GetAllAuthors());
                AuthorComboBox.ItemsSource = _allAuthors;

                foreach (var novel in _allNovels)
                {
                    if (string.IsNullOrEmpty(novel.CoverImageUrl))
                    {
                        novel.CoverImageUrl = "pack://application:,,,/Images/back.jpg";
                    }
                }

                FilterNovels();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки: {ex}");
                NoResultsText.Visibility = Visibility.Visible;
            }
            finally
            {
                LoadingIndicator.Visibility = Visibility.Collapsed;
            }
        }

        private void FilterNovels()
        {
            try
            {
                string searchText = SearchTextBox?.Text?.ToLower() ?? "";
                if (SearchTextBox?.Foreground == Brushes.Gray && searchText == "поиск новелл...")
                {
                    searchText = "";
                }

                Author selectedAuthor = AuthorComboBox?.SelectedItem as Author;

                var filteredNovels = _allNovels
                    .Where(n => n != null)
                    .Where(n => string.IsNullOrWhiteSpace(searchText) ||
                          (n.Title?.ToLower().Contains(searchText) ?? false) ||
                          (n.Description?.ToLower().Contains(searchText) ?? false))
                    .Where(n => selectedAuthor == null || (n.AuthorId == selectedAuthor.AuthorId))
                    .ToList();

                var groupedNovels = filteredNovels
                    .GroupBy(n =>
                    {
                        if (n == null || n.GenreId == null)
                            return "Без жанра";

                        var genre = _allGenres?.FirstOrDefault(g => g != null && g.GenreId == n.GenreId);
                        return genre?.Name ?? "Без жанра";
                    })
                    .Where(g => g != null && g.Key != null)
                    .Select(g => new NovelGenreGroup
                    {
                        GenreName = g.Key,
                        Novels = g?.Where(n => n != null).ToList() ?? new List<Novel>()
                    })
                    .Where(g => g != null)
                    .ToList();

                if (groupedNovels?.Any() == true)
                {
                    NoResultsText.Visibility = Visibility.Collapsed;
                    GenresItemsControl.Visibility = Visibility.Visible;
                    GenresItemsControl.ItemsSource = groupedNovels;
                }
                else
                {
                    NoResultsText.Visibility = Visibility.Visible;
                    GenresItemsControl.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка фильтрации: {ex}");

            }
        }

        private void CheckIfReaderIsAuthor()
        {
            try
            {
                var authorDb = new AuthorFromDb();
                _currentAuthor = authorDb.GetAuthorByReaderId(_currentReader.ReaderId);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка проверки автора: {ex.Message}");
            }
        }

        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchTextBox.Text == "Поиск новелл...")
            {
                SearchTextBox.Text = "";
                SearchTextBox.Foreground = Brushes.Black;
            }
        }

        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                SearchTextBox.Text = "Поиск новелл...";
                SearchTextBox.Foreground = Brushes.Gray;
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                LogAction("novel_search");
            }
            FilterNovels();
        }

        private void AuthorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterNovels();
        }

        private void NovelCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1 && sender is Border border && border.DataContext is Novel novel)
            {
                NavigationService?.Navigate(new NovelDetailsPage(novel.NovelId));
            }
        }
    }

    public class NovelGenreGroup
    {
        public string GenreName { get; set; }
        public List<Novel> Novels { get; set; } = new List<Novel>();
    }
}