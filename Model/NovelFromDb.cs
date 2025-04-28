using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ForgeTales.Classes;
using Npgsql;
using System.Windows;
using System.Diagnostics;

namespace ForgeTales.Model
{
    public class NovelFromDb
    {
        private readonly string _connectionString;

        public NovelFromDb()
        {
            _connectionString = DbConnection.connectionStr;
        }
        public bool AddChapterWithNovel(Novel novel, Chapter chapter)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var command = new NpgsqlCommand(
                        "CALL add_chapter_with_novel(@title, @description, @genre, @author_id, @cover_image, " +
                        "@chapter_title, @chapter_content, @chapter_number, @is_renpy_project)",
                        connection))
                    {
                        command.Parameters.AddWithValue("title", novel.Title);
                        command.Parameters.AddWithValue("description", novel.Description ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("genre", novel.GenreId ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("author_id", novel.AuthorId);
                        command.Parameters.AddWithValue("cover_image", novel.CoverImageUrl ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("chapter_title", chapter.Title);
                        command.Parameters.AddWithValue("chapter_content", chapter.Content);
                        command.Parameters.AddWithValue("chapter_number", chapter.ChapterNumber);
                        command.Parameters.AddWithValue("is_renpy_project", chapter.IsRenPyProject);

                        command.ExecuteNonQuery();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка сохранения: {ex}");
                throw;
            }
        }
        public bool UpdateNovel(Novel novel)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    string sql = @"UPDATE novels SET 
                                  title = @title,
                                  description = @description,
                                  genre = @genre,
                                  url = @url
                                  WHERE novel_id = @novelId AND author_id = @authorId";

                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@title", novel.Title);
                        cmd.Parameters.AddWithValue("@description", novel.Description ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@genre", novel.GenreId ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@url", novel.CoverImageUrl ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@novelId", novel.NovelId);
                        cmd.Parameters.AddWithValue("@authorId", novel.AuthorId);

                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления новеллы: {ex.Message}");
                return false;
            }
        }


        public List<Novel> GetAllNovels()
        {
            var novels = new List<Novel>();
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    string sql = @"SELECT n.novel_id, n.title, n.description, n.genre, 
                                  n.author_id, n.created_at, n.url,
                                  a.name as author_name
                           FROM novels n
                           JOIN authors a ON n.author_id = a.author_id
                           ORDER BY n.created_at DESC";

                    using (var cmd = new NpgsqlCommand(sql, connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var novelId = reader.GetInt32(0);
                            var title = reader.IsDBNull(1) ? "Без названия" : reader.GetString(1);
                            var description = reader.IsDBNull(2) ? "Без описания" : reader.GetString(2);
                            var genreId = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3);
                            var authorId = reader.GetInt32(4);
                            var createdAt = reader.GetDateTime(5);
                            var url = reader.IsDBNull(6) ? "Нет URL" : reader.GetString(6);
                            var authorName = reader.IsDBNull(7) ? "Неизвестный автор" : reader.GetString(7);

                            // Создаем объект новеллы
                            var author = new Author(authorId, authorName, null, string.Empty, null);
                            var novel = new Novel(novelId, title, description, genreId, authorId, createdAt, url, author);
                            novels.Add(novel);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки новелл: {ex.Message}");
            }

            return novels;
        }



        public Novel GetNovelById(int novelId)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    string sql = @"SELECT n.novel_id, n.title, n.description, n.genre, 
                                          n.author_id, n.created_at, n.url,
                                          a.name as author_name
                                   FROM novels n
                                   JOIN authors a ON n.author_id = a.author_id
                                   WHERE n.novel_id = @novelId";

                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@novelId", novelId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new Novel(
                                    novelId: reader.GetInt32(0),
                                    title: reader.GetString(1),
                                    description: reader.IsDBNull(2) ? null : reader.GetString(2),
                                    genreId: reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3),
                                    authorId: reader.GetInt32(4),
                                    createdAt: reader.GetDateTime(5),
                                    url: reader.IsDBNull(6) ? null : reader.GetString(6),
                                    author: new Author(
                                        authorId: reader.GetInt32(4),
                                        name: reader.GetString(7),
                                        bio: null,
                                        email: null,
                                        passwordHash: null
                                    )
                                );
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки новеллы: {ex.Message}");
            }
            return null;
        }

        public bool DeleteNovel(int novelId)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    string sql = "DELETE FROM novels WHERE novel_id = @novelId";

                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@novelId", novelId);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления новеллы: {ex.Message}");
                return false;
            }
        }

        public List<Novel> GetNovelsByAuthor(int authorId)
        {
            var novels = new List<Novel>();
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    string sql = @"SELECT n.novel_id, n.title, n.description, n.genre, 
                                          n.author_id, n.created_at, n.url,
                                          a.name as author_name
                                   FROM novels n
                                   JOIN authors a ON n.author_id = a.author_id
                                   WHERE n.author_id = @authorId
                                   ORDER BY n.title";

                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@authorId", authorId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                novels.Add(new Novel(
                                    novelId: reader.GetInt32(0),
                                    title: reader.GetString(1),
                                    description: reader.IsDBNull(2) ? null : reader.GetString(2),
                                    genreId: reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3),
                                    authorId: reader.GetInt32(4),
                                    createdAt: reader.GetDateTime(5),
                                    url: reader.IsDBNull(6) ? null : reader.GetString(6),
                                    author: new Author(
                                        authorId: reader.GetInt32(4),
                                        name: reader.GetString(7),
                                        bio: null,
                                        email: null,
                                        passwordHash: null,
                                        readerId: null
                                    )
                                ));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки новелл автора: {ex.Message}");
            }
            return novels;
        }
    }
}