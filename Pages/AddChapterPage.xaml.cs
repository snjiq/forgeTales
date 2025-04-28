using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using ForgeTales.Classes;
using ForgeTales.Helpers;
using ForgeTales.Model;
using Microsoft.Win32;

namespace ForgeTales.Pages
{
    public partial class AddChapterPage : Page
    {
        private readonly Author _author;
        private readonly int _novelId;
        private readonly ChapterFromDb _chapterDb = new ChapterFromDb();
        private string _renPyProjectPath;

        public AddChapterPage(Author author, int novelId)
        {
            InitializeComponent();
            _author = author;
            _novelId = novelId;
            LoadChapterNumber();
        }

        private void LoadChapterNumber()
        {
            var chapters = _chapterDb.GetChaptersByNovelId(_novelId);
            ChapterNumberTextBox.Text = (chapters.Count + 1).ToString();
        }

        private void UploadRenPyButton_Click(object sender, RoutedEventArgs e)
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
                        RenPyStatusText.Text = $"Загружен проект: {Path.GetFileName(_renPyProjectPath)}";
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

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ChapterTitleTextBox.Text))
            {
                MessageBox.Show("Введите название главы");
                ChapterTitleTextBox.Focus();
                return;
            }

            if (!int.TryParse(ChapterNumberTextBox.Text, out int chapterNumber) || chapterNumber <= 0)
            {
                MessageBox.Show("Введите корректный номер главы");
                ChapterNumberTextBox.Focus();
                return;
            }

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
                MessageBox.Show("Введите содержание главы или загрузите проект Ren'Py");
                return;
            }

            try
            {
                var chapter = new Chapter(
                    chapterId: 0,
                    novelId: _novelId,
                    title: ChapterTitleTextBox.Text.Trim(),
                    content: chapterContent,
                    chapterNumber: chapterNumber,
                    createdAt: DateTime.Now,
                    isRenPyProject: isRenPy
                );

                if (_chapterDb.AddChapter(chapter))
                {
                    MessageBox.Show("Глава успешно добавлена!");
                    NavigationService?.GoBack();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении главы: {ex.Message}");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.GoBack();
        }
    }
}