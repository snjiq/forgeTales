using Microsoft.Win32;
using System;
using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ForgeTales.Classes;
using ForgeTales.Model;
using Npgsql;
using System.Globalization;
using ForgeTales.Helpers;
using System.Windows.Input;
using System.Threading.Tasks;
using ForgeTales.Windows;

namespace ForgeTales.Pages
{
    public partial class AddNovelPage : Page
    {
        private readonly Author _currentAuthor;
        private readonly NovelFromDb _novelDb = new NovelFromDb();
        private readonly ChapterFromDb _chapterDb = new ChapterFromDb();
        private string _coverImageBase64;


        public AddNovelPage(Author author)
        {
            InitializeComponent();
            _currentAuthor = author ?? throw new ArgumentNullException(nameof(author));
            LoadGenres();
        }

        private void LoadGenres()
        {
            try
            {
                using (var connection = new NpgsqlConnection(DbConnection.connectionStr))
                {
                    connection.Open();
                    var command = new NpgsqlCommand("SELECT genre_id, name FROM genres ORDER BY name", connection);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            GenreComboBox.Items.Add(new
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1)
                            });
                        }
                    }
                }

                GenreComboBox.DisplayMemberPath = "Name";
                GenreComboBox.SelectedValuePath = "Id";

                if (GenreComboBox.Items.Count > 0)
                    GenreComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки жанров: {ex.Message}");
                GenreComboBox.Items.Add(new { Id = 1, Name = "Фэнтези" });
                GenreComboBox.Items.Add(new { Id = 2, Name = "Научная фантастика" });
            }
        }


       

        private bool ValidateRenPyProject(string folderPath)
        {
            try
            {
                // Проверяем обязательную папку game
                string gameFolder = Path.Combine(folderPath, "game");
                if (!Directory.Exists(gameFolder))
                    return false;

                // Проверяем наличие хотя бы одного .rpy файла
                return Directory.GetFiles(gameFolder, "*.rpy", SearchOption.AllDirectories).Length > 0;
            }
            catch
            {
                return false;
            }
        }

        // Вспомогательный класс для WinForms диалога
        private class Win32Window : System.Windows.Forms.IWin32Window
        {
            private readonly IntPtr _handle;
            public Win32Window(Window window)
            {
                _handle = new System.Windows.Interop.WindowInteropHelper(window).Handle;
            }
            public IntPtr Handle => _handle;
        }

        private (bool isValid, string message) CheckRenPyProject(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                    return (false, "Папка не существует");

                string gamePath = Path.Combine(path, "game");
                if (!Directory.Exists(gamePath))
                    return (false, "Папка 'game' не найдена");

                if (Directory.GetFiles(gamePath, "*.rpy", SearchOption.TopDirectoryOnly).Length == 0)
                    return (false, "Не найдены файлы .rpy в папке game");

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, $"Ошибка проверки: {ex.Message}");
            }
        }
        private string _renPyProjectPath;

        private void UploadRenPyProject_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new System.Windows.Forms.FolderBrowserDialog
                {
                    Description = "Выберите папку с проектом Ren'Py",
                    ShowNewFolderButton = false
                };

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    if (RenPyHelper.ValidateRenPyProject(dialog.SelectedPath))
                    {
                        _renPyProjectPath = dialog.SelectedPath;
                        MessageBox.Show("Проект Ren'Py успешно загружен");
                        ChapterTitleTextBox.Text = Path.GetFileName(_renPyProjectPath);
                    }
                    else
                    {
                        MessageBox.Show("Это не похоже на проект Ren'Py");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Валидация данных
                if (string.IsNullOrWhiteSpace(TitleTextBox.Text) ||
                    string.IsNullOrWhiteSpace(ChapterTitleTextBox.Text))
                {
                    MessageBox.Show("Заполните обязательные поля");
                    return;
                }

                // Создаем объекты
                var novel = new Novel(
                    novelId: 0,
                    title: TitleTextBox.Text.Trim(),
                    description: DescriptionTextBox.Text.Trim(),
                    genreId: (int?)GenreComboBox.SelectedValue,
                    authorId: _currentAuthor.AuthorId,
                    createdAt: DateTime.Now,
                    url: _coverImageBase64,
                    author: _currentAuthor
                );

                string chapterContent;
                bool isRenPy = false;

                if (!string.IsNullOrEmpty(_renPyProjectPath))
                {
                    chapterContent = RenPyHelper.ZipRenPyProject(_renPyProjectPath);
                    isRenPy = true;
                }
                else if (!string.IsNullOrWhiteSpace(ChapterContentTextBox.Text))
                {
                    chapterContent = ChapterContentTextBox.Text;
                }
                else
                {
                    MessageBox.Show("Введите содержание главы или загрузите проект");
                    return;
                }

                var chapter = new Chapter(
                    chapterId: 0,
                    novelId: 0,
                    title: ChapterTitleTextBox.Text.Trim(),
                    content: chapterContent,
                    chapterNumber: 1,
                    createdAt: DateTime.Now,
                    isRenPyProject: isRenPy
                );

                // Сохраняем в БД
                if (_novelDb.AddChapterWithNovel(novel, chapter))
                {
                    MessageBox.Show("Сохранено успешно!");
                    NavigationService?.Navigate(new NovelsPage(_currentAuthor));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}");
            }
        }

        private bool ValidateNovelData()
        {
            if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
            {
                MessageBox.Show("Введите название новеллы");
                return false;
            }

            if (GenreComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите жанр");
                return false;
            }

            if (string.IsNullOrWhiteSpace(ChapterTitleTextBox.Text))
            {
                MessageBox.Show("Введите название главы");
                return false;
            }

            if (string.IsNullOrEmpty(_renPyProjectPath) && string.IsNullOrWhiteSpace(ChapterContentTextBox.Text))
            {
                MessageBox.Show("Введите содержание главы или загрузите проект Ren'Py");
                return false;
            }

            return true;
        }

        private string GetChapterContent()
        {
            return !string.IsNullOrEmpty(_renPyProjectPath)
                ? ZipRenPyProject(_renPyProjectPath)
                : ChapterContentTextBox.Text;
        }
        private string ZipRenPyProject(string projectPath)
        {
            try
            {
                string gameFolder = Path.Combine(projectPath, "game");
                using (var memoryStream = new MemoryStream())
                {
                    using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                    {
                        foreach (var file in Directory.GetFiles(gameFolder, "*", SearchOption.AllDirectories))
                        {
                            var relativePath = file.Substring(gameFolder.Length + 1);
                            var entry = archive.CreateEntry(relativePath);

                            using (var entryStream = entry.Open())
                            using (var fileStream = File.OpenRead(file))
                            {
                                fileStream.CopyTo(entryStream);
                            }
                        }
                    }
                    return Convert.ToBase64String(memoryStream.ToArray());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка архивации: {ex.Message}");
                throw;
            }
        }
        private void AddFilesToArchive(ZipArchive archive, string sourceDir, string entryPrefix)
        {
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var entryName = entryPrefix + Path.GetFileName(file);
                var entry = archive.CreateEntry(entryName);

                using (var entryStream = entry.Open())
                using (var fileStream = File.OpenRead(file))
                {
                    fileStream.CopyTo(entryStream);
                }
            }

            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var dirEntryName = entryPrefix + Path.GetFileName(dir) + "/";
                archive.CreateEntry(dirEntryName);
                AddFilesToArchive(archive, dir, dirEntryName);
            }
        }
        private void UploadCoverButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Images|*.jpg;*.jpeg;*.png;*.bmp|All files|*.*",
                Title = "Выберите изображение обложки"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    byte[] imageBytes = File.ReadAllBytes(openFileDialog.FileName);
                    _coverImageBase64 = Convert.ToBase64String(imageBytes);

                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = new MemoryStream(imageBytes);
                    bitmap.EndInit();
                    CoverImage.Source = bitmap;
                    CoverImage.Visibility = Visibility.Visible;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки изображения: {ex.Message}");
                }
            }
        }

        private void GenerateCoverButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var visual = new DrawingVisual();
                using (var context = visual.RenderOpen())
                {
                    context.DrawRectangle(
                        Brushes.LightGray,
                        null,
                        new Rect(0, 0, 400, 600));

                    var text = !string.IsNullOrWhiteSpace(TitleTextBox.Text)
                        ? TitleTextBox.Text
                        : "Новая новелла";

                    var formattedText = new FormattedText(
                        text,
                        CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        new Typeface("Arial"),
                        36,
                        Brushes.DarkSlateBlue,
                        96);

                    context.DrawText(
                        formattedText,
                        new Point(200 - formattedText.Width / 2, 300 - formattedText.Height / 2));
                }

                var bitmap = new RenderTargetBitmap(400, 600, 96, 96, PixelFormats.Pbgra32);
                bitmap.Render(visual);

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));

                using (var stream = new MemoryStream())
                {
                    encoder.Save(stream);
                    _coverImageBase64 = Convert.ToBase64String(stream.ToArray());
                    CoverImage.Source = bitmap;
                    CoverImage.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка генерации обложки: {ex.Message}");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.GoBack();
        }
    }
}