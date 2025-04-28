using System;
using System.Diagnostics;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ForgeTales.Classes;
using ForgeTales.Model;
using ForgeTales.Windows;
using Npgsql;
using Npgsql.PostgresTypes;
using ForgeTales.Helpers;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ForgeTales.Pages
{
    public partial class NovelDetailsPage : Page
    {
        private readonly int _novelId;
        private readonly NovelFromDb _novelDb = new NovelFromDb();
        private readonly ChapterFromDb _chapterDb = new ChapterFromDb();
        private readonly ReaderFromDb _readerDb = new ReaderFromDb();
        private readonly ReviewFromDb _reviewDb = new ReviewFromDb();
        private Reader _currentReader;
        private Author _currentAuthor;
        private Novel _currentNovel;

        public NovelDetailsPage(int novelId)
        {
            InitializeComponent();
            _novelId = novelId;
            Loaded += NovelDetailsPage_Loaded;
        }

        private void NovelDetailsPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Получаем текущего пользователя
                _currentReader = Application.Current.Properties["CurrentUser"] as Reader;
                _currentAuthor = Application.Current.Properties["CurrentAuthor"] as Author;

                // Логируем просмотр новеллы
                if (_currentReader != null)
                {
                    _readerDb.LogReadingSession(_currentReader.ReaderId, "novel_view");
                }

                // Загружаем данные
                LoadNovelDetails();
                CheckReviewPermissions();
                LoadReviews();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке страницы: {ex.Message}");
                NavigationService?.GoBack();
            }
        }

        private void LoadNovelDetails()
        {
            try
            {
                _currentNovel = _novelDb.GetNovelById(_novelId);
                if (_currentNovel != null)
                {
                    if (string.IsNullOrEmpty(_currentNovel.CoverImageUrl))
                    {
                        _currentNovel.CoverImageUrl = "pack://application:,,,/Images/back.jpg";
                    }
                    DataContext = _currentNovel;
                    ChaptersListView.ItemsSource = _chapterDb.GetChaptersByNovelId(_novelId);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки деталей новеллы: {ex.Message}");
                throw;
            }
        }

        private void CheckReviewPermissions()
        {
            try
            {
                if (_currentNovel == null) return;

                bool isAuthor = _currentAuthor != null && _currentAuthor.AuthorId == _currentNovel.AuthorId;
                bool isGuest = _currentReader == null;

                AddReviewPanel.Visibility = isAuthor || isGuest ? Visibility.Collapsed : Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка проверки прав на отзыв: {ex.Message}");
                AddReviewPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void LoadReviews()
        {
            try
            {
                var reviews = _reviewDb.GetReviewsForNovel(_novelId);

                if (reviews.Count == 0)
                {
                    // Создаем список с одним элементом-заглушкой
                    ReviewsList.ItemsSource = new List<string> { "Пока нет отзывов. Будьте первым!" };
                }
                else
                {
                    ReviewsList.ItemsSource = reviews;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки отзывов: {ex.Message}");
            }
        }
        private void SubmitReview_Click(object sender, RoutedEventArgs e)
        {
            if (_currentReader == null) return;

            string comment = ReviewCommentTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(comment))
            {
                MessageBox.Show("Пожалуйста, напишите отзыв");
                return;
            }

            if (comment.Length > 1000)
            {
                MessageBox.Show("Отзыв не должен превышать 1000 символов");
                return;
            }

            int rating = RatingComboBox.SelectedIndex + 1;

            try
            {
                bool success = _reviewDb.AddReview(_novelId, _currentReader.ReaderId, rating, comment);
                if (success)
                {
                    MessageBox.Show("Спасибо за ваш отзыв!");
                    ReviewCommentTextBox.Text = "";
                    RatingComboBox.SelectedIndex = 4;
                    LoadReviews();
                }
                else
                {
                    MessageBox.Show("Не удалось добавить отзыв");
                }
            }
            catch(PostgresException ex) when(ex.SqlState == "23505")// Ошибка уникальности
            {
                MessageBox.Show("Вы уже оставляли отзыв на эту новеллу");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении отзыва: {ex.Message}");
            }
        }

        private async void ReadChapter_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int chapterId)
            {
                try
                {
                    // Проверяем возможность чтения
                    if (!_readerDb.CanReadNow(_currentReader.ReaderId))
                    {
                        MessageBox.Show($"Вы сможете прочитать следующую главу через {_readerDb.GetRemainingTime(_currentReader.ReaderId)} минут");
                        return;
                    }

                    // Проверяем баланс, если нет подписки "Взахлеб"
                    if (!_readerDb.HasUnlimitedSubscription(_currentReader.ReaderId) &&
                        _currentReader.HeartsBalance < 3)
                    {
                        MessageBox.Show("Недостаточно сердечек для чтения главы");
                        return;
                    }

                    var chapter = _chapterDb.GetChapterById(chapterId);
                    if (chapter == null)
                    {
                        MessageBox.Show("Глава не найдена");
                        return;
                    }

                    // Списываем сердечки, если нет подписки "Взахлеб"
                    if (!_readerDb.HasUnlimitedSubscription(_currentReader.ReaderId))
                    {
                        if (!await Task.Run(() => _readerDb.DeductHearts(_currentReader.ReaderId, 3)))
                        {
                            MessageBox.Show("Ошибка при списании сердечек");
                            return;
                        }
                        _currentReader.HeartsBalance -= 3;
                    }

                    // Логируем время чтения
                    _readerDb.LogReadingSession(_currentReader.ReaderId, "chapter_read");

                    if (chapter.IsRenPyProject)
                    {
                        LaunchRenPyChapter(chapter.Content);
                    }

                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при открытии главы: {ex.Message}");
                }
            }
        }

        private void LaunchRenPyChapter(string base64Content)
        {
            string tempFolder = "";
            try
            {
                tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempFolder);

                byte[] zipData = Convert.FromBase64String(base64Content);
                string zipPath = Path.Combine(tempFolder, "project.zip");
                File.WriteAllBytes(zipPath, zipData);

                string extractPath = Path.Combine(tempFolder, "game");
                ZipFile.ExtractToDirectory(zipPath, extractPath);

                if (!Directory.Exists(extractPath) ||
                    Directory.GetFiles(extractPath, "*.rpy", SearchOption.AllDirectories).Length == 0)
                {
                    throw new Exception("Извлеченный проект Ren'Py недействителен");
                }

                RenPyHelper.LaunchRenPyProject(extractPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка запуска Ren'Py: {ex.Message}");

                try
                {
                    if (Directory.Exists(tempFolder))
                    {
                        Directory.Delete(tempFolder, true);
                    }
                }
                catch { }
            }
        }


        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}