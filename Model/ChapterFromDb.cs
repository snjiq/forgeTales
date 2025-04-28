using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ForgeTales.Classes;
using Npgsql;
using System.Windows;

namespace ForgeTales.Model
{
    internal class ChapterFromDb
    {
        private readonly string _connectionString;

        public ChapterFromDb()
        {
            _connectionString = DbConnection.connectionStr;
        }
        public bool AddChapter(Chapter chapter)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    string sql = @"INSERT INTO chapters (novel_id, title, content, chapter_number, created_at)
                         VALUES (@novelId, @title, @content, @chapterNumber, CURRENT_TIMESTAMP)";

                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@novelId", chapter.NovelId);
                        cmd.Parameters.AddWithValue("@title", chapter.Title);
                        cmd.Parameters.AddWithValue("@content", chapter.Content);
                        cmd.Parameters.AddWithValue("@chapterNumber", chapter.ChapterNumber);

                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка добавления главы: {ex.Message}");
                return false;
            }
        }

        public bool AddChapterWithNovel(Novel novel, Chapter chapter)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    string sql = @"INSERT INTO chapters (novel_id, title, content, chapter_number, created_at, is_renpy_project)
                         VALUES (@novelId, @title, @content, @chapterNumber, CURRENT_TIMESTAMP, @isRenPy)";

                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@novelId", novel.NovelId);
                        cmd.Parameters.AddWithValue("@title", chapter.Title);
                        cmd.Parameters.AddWithValue("@content", chapter.Content);
                        cmd.Parameters.AddWithValue("@chapterNumber", chapter.ChapterNumber);
                        cmd.Parameters.AddWithValue("@isRenPy", chapter.IsRenPyProject);

                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка добавления главы: {ex.Message}");
                return false;
            }
        }
        public List<Chapter> GetChaptersByNovelId(int novelId)
        {
            var chapters = new List<Chapter>();
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    string sql = @"SELECT chapter_id, novel_id, title, content, 
                                 chapter_number, created_at
                                 FROM chapters
                                 WHERE novel_id = @novelId
                                 ORDER BY chapter_number";

                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@novelId", novelId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                chapters.Add(new Chapter(
                                    chapterId: reader.GetInt32(0),
                                    novelId: reader.GetInt32(1),
                                    title: reader.GetString(2),
                                    content: reader.GetString(3),
                                    chapterNumber: reader.GetInt32(4),
                                    createdAt: reader.GetDateTime(5)
                                ));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки глав: {ex.Message}");
            }
            return chapters;
        }

        public Chapter GetChapterById(int chapterId)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    string sql = @"SELECT chapter_id, novel_id, title, content, 
                         chapter_number, created_at, is_renpy_project
                         FROM chapters
                         WHERE chapter_id = @chapterId";

                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@chapterId", chapterId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new Chapter(
                                    chapterId: reader.GetInt32(0),
                                    novelId: reader.GetInt32(1),
                                    title: reader.GetString(2),
                                    content: reader.GetString(3),
                                    chapterNumber: reader.GetInt32(4),
                                    createdAt: reader.GetDateTime(5),
                                    isRenPyProject: reader.GetBoolean(6) // Убедитесь, что столбец существует
                                );
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки главы: {ex.Message}");
            }
            return null;
        }

        public bool UpdateChapter(Chapter chapter)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    string sql = @"UPDATE chapters SET 
                                 title = @title,
                                 content = @content,
                                 chapter_number = @chapter_number
                                 WHERE chapter_id = @chapterId";

                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@title", chapter.Title);
                        cmd.Parameters.AddWithValue("@content", chapter.Content);
                        cmd.Parameters.AddWithValue("@chapter_number", chapter.ChapterNumber);
                        cmd.Parameters.AddWithValue("@chapterId", chapter.ChapterId);

                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления главы: {ex.Message}");
                return false;
            }
        }

        public bool DeleteChapter(int chapterId)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    string sql = "DELETE FROM chapters WHERE chapter_id = @chapterId";

                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@chapterId", chapterId);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления главы: {ex.Message}");
                return false;
            }
        }
    }
}
