using System;
using System.Windows;
using System.Windows.Controls;
using ForgeTales.Classes;
using ForgeTales.Model;
using Npgsql;

namespace ForgeTales.Pages
{
    public partial class AuthorNovelsPage : Page
    {
        private readonly Author _author;
        private readonly NovelFromDb _novelDb = new NovelFromDb();

        public AuthorNovelsPage(Author author)
        {
            InitializeComponent();
            _author = author;
            LoadNovels();
            LogAction("author_novels_view");
        }

        private void LogAction(string actionType)
        {
            using (var conn = new NpgsqlConnection(DbConnection.connectionStr))
            {
                try
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(
                        "INSERT INTO reading_sessions (reader_id, action_type, start_time) VALUES (@id, @action, NOW())",
                        conn))
                    {
                        cmd.Parameters.AddWithValue("id", _author.AuthorId);
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

        private void LoadNovels()
        {
            var novels = _novelDb.GetNovelsByAuthor(_author.AuthorId);
            NovelsItemsControl.ItemsSource = novels;
        }

        private void AddNovel_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new AddNovelPage(_author));
            LogAction("add_novel_click");
        }

        private void EditNovel_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int novelId)
            {
                var novel = _novelDb.GetNovelById(novelId);
                if (novel != null)
                {
                    NavigationService.Navigate(new EditNovelPage(_author, novel));
                    LogAction("edit_novel_click");
                }
            }
        }

        private void AddChapter_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int novelId)
            {
                NavigationService.Navigate(new AddChapterPage(_author, novelId));
                LogAction("add_chapter_click");
            }
        }

        private void DeleteNovel_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int novelId)
            {
                if (MessageBox.Show("Удалить эту новеллу и все её главы?",
                    "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    if (_novelDb.DeleteNovel(novelId))
                    {
                        LoadNovels();
                        MessageBox.Show("Новелла удалена");
                        LogAction("delete_novel");
                    }
                }
            }
        }

        private void RefreshList_Click(object sender, RoutedEventArgs e)
        {
            LoadNovels();
            LogAction("refresh_novels");
        }
    }
}