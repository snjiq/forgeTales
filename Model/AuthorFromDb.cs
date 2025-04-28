using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using ForgeTales.Classes;
using Npgsql;

namespace ForgeTales.Model
{
    internal class AuthorFromDb
    {
        private readonly string _connectionString;

        public AuthorFromDb()
        {
            _connectionString = DbConnection.connectionStr;
        }

        public bool AddAuthorWithReader(string name, string bio, string email,
                               string password, string avatarUrl = null)
        {
            try
            {
                if (!Verification.ValidatePasswordStrength(password))
                {
                    MessageBox.Show("Пароль должен содержать:\n- минимум 8 символов\n- цифру\n- заглавную и строчную буквы\n- спецсимвол");
                    return false;
                }

                string passwordHash = Verification.GetSHA512Hash(password);

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();

                    using (var cmd = new NpgsqlCommand("CALL add_author_with_reader(@name, @bio, @email, @passwordHash)", connection))
                    {
                        cmd.Parameters.AddWithValue("@name", name);
                        cmd.Parameters.AddWithValue("@bio", string.IsNullOrEmpty(bio) ? (object)DBNull.Value : bio);
                        cmd.Parameters.AddWithValue("@email", email);
                        cmd.Parameters.AddWithValue("@passwordHash", passwordHash);

                        cmd.ExecuteNonQuery();
                    }

                    if (!string.IsNullOrEmpty(avatarUrl))
                    {
                        int readerId;
                        using (var cmd = new NpgsqlCommand(
                            "SELECT reader_id FROM authors WHERE email = @email", connection))
                        {
                            cmd.Parameters.AddWithValue("@email", email);
                            readerId = (int)cmd.ExecuteScalar();
                        }

                        using (var cmd = new NpgsqlCommand(
                            "UPDATE readers SET avatar_url = @avatarUrl WHERE reader_id = @readerId", connection))
                        {
                            cmd.Parameters.AddWithValue("@avatarUrl", avatarUrl);
                            cmd.Parameters.AddWithValue("@readerId", readerId);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    MessageBox.Show("Автор успешно зарегистрирован!");
                    return true;
                }
            }
            catch (NpgsqlException ex) when (ex.Message.Contains("уже существует"))
            {
                MessageBox.Show("Пользователь с таким именем или email уже существует");
                return false;
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show($"Ошибка регистрации автора: {ex.Message}");
                return false;
            }
        }

        public Author GetAuthorByReaderId(int readerId)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    string sql = @"SELECT author_id, name, bio, email, password_hash, reader_id, avatar_url 
                           FROM authors 
                           WHERE reader_id = @readerId";

                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@readerId", readerId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new Author(
                                    authorId: reader.GetInt32(0),
                                    name: reader.GetString(1),
                                    bio: reader.IsDBNull(2) ? null : reader.GetString(2),
                                    email: reader.GetString(3),
                                    passwordHash: reader.GetString(4),
                                    readerId: reader.GetInt32(5),
                                    avatarUrl: reader.IsDBNull(6) ? null : reader.GetString(6)
                                );
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка получения автора: {ex.Message}");
            }
            return null;
        }

        public List<Author> GetAllAuthors()
        {
            var authors = new List<Author>();

            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    var command = new NpgsqlCommand(
                        "SELECT a.author_id, a.name, a.bio, a.email, a.password_hash, a.reader_id, r.avatar_url " +
                        "FROM authors a " +
                        "LEFT JOIN readers r ON a.reader_id = r.reader_id " +
                        "ORDER BY a.name",
                        connection);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            authors.Add(new Author(
                                authorId: reader.GetInt32(0),
                                name: reader.GetString(1),
                                bio: reader.IsDBNull(2) ? null : reader.GetString(2),
                                email: reader.GetString(3),
                                passwordHash: reader.GetString(4),
                                readerId: reader.IsDBNull(5) ? (int?)null : reader.GetInt32(5),
                                avatarUrl: reader.IsDBNull(6) ? null : reader.GetString(6)
                            ));
                        }
                    }
                }
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show($"Ошибка при получении списка авторов: {ex.Message}");
            }

            return authors;
        }

        public bool UpdateAuthor(Author author)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    var command = new NpgsqlCommand(
                        "UPDATE authors SET name = @name, bio = @bio, email = @email, password_hash = @passwordHash " +
                        "WHERE author_id = @authorId",
                        connection);

                    command.Parameters.AddWithValue("@authorId", author.AuthorId);
                    command.Parameters.AddWithValue("@name", author.Name);
                    command.Parameters.AddWithValue("@bio", string.IsNullOrEmpty(author.Bio) ? (object)DBNull.Value : author.Bio);
                    command.Parameters.AddWithValue("@email", author.Email);
                    command.Parameters.AddWithValue("@passwordHash", author.PasswordHash);

                    int rowsAffected = command.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show($"Ошибка при обновлении автора: {ex.Message}");
                return false;
            }
        }

        public bool DeleteAuthor(int authorId)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            int? readerId = null;
                            var getReaderIdCommand = new NpgsqlCommand(
                                "SELECT reader_id FROM authors WHERE author_id = @authorId",
                                connection, transaction);
                            getReaderIdCommand.Parameters.AddWithValue("@authorId", authorId);

                            var result = getReaderIdCommand.ExecuteScalar();
                            if (result != null && result != DBNull.Value)
                            {
                                readerId = (int)result;
                            }

                            var deleteAuthorCommand = new NpgsqlCommand(
                                "DELETE FROM authors WHERE author_id = @authorId",
                                connection, transaction);
                            deleteAuthorCommand.Parameters.AddWithValue("@authorId", authorId);

                            int rowsAffected = deleteAuthorCommand.ExecuteNonQuery();

                            if (readerId.HasValue && rowsAffected > 0)
                            {
                                var deleteReaderCommand = new NpgsqlCommand(
                                    "DELETE FROM readers WHERE reader_id = @readerId",
                                    connection, transaction);
                                deleteReaderCommand.Parameters.AddWithValue("@readerId", readerId.Value);
                                deleteReaderCommand.ExecuteNonQuery();
                            }

                            transaction.Commit();
                            return rowsAffected > 0;
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show($"Ошибка при удалении автора: {ex.Message}");
                return false;
            }
        }

        public Author AuthenticateAuthor(string email, string password)
        {
            try
            {
                string passwordHash = Verification.GetSHA512Hash(password);

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    var command = new NpgsqlCommand(
                        "SELECT author_id, name, bio, email, password_hash, reader_id " +
                        "FROM authors WHERE email = @email AND password_hash = @passwordHash",
                        connection);

                    command.Parameters.AddWithValue("@email", email);
                    command.Parameters.AddWithValue("@passwordHash", passwordHash);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Author(
                                authorId: reader.GetInt32(0),
                                name: reader.GetString(1),
                                bio: reader.IsDBNull(2) ? null : reader.GetString(2),
                                email: reader.GetString(3),
                                passwordHash: reader.GetString(4),
                                readerId: reader.GetInt32(5)
                            );
                        }
                    }
                }
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show($"Ошибка аутентификации: {ex.Message}");
            }

            return null;
        }

        public bool IsEmailUnique(string email, int? excludeAuthorId = null)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    var command = new NpgsqlCommand(
                        "SELECT COUNT(*) FROM authors WHERE email = @email " +
                        (excludeAuthorId.HasValue ? "AND author_id != @excludeAuthorId" : ""),
                        connection);

                    command.Parameters.AddWithValue("@email", email);
                    if (excludeAuthorId.HasValue)
                    {
                        command.Parameters.AddWithValue("@excludeAuthorId", excludeAuthorId.Value);
                    }

                    long count = (long)command.ExecuteScalar();
                    return count == 0;
                }
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show($"Ошибка проверки email: {ex.Message}");
                return false;
            }
        }

        public bool UpdateAuthorWithReaderPassword(Author author, string newPassword)
        {
            try
            {
                if (!Verification.ValidatePasswordStrength(newPassword))
                {
                    MessageBox.Show("Новый пароль должен содержать:\n- минимум 8 символов\n- цифру\n- заглавную и строчную буквы\n- спецсимвол");
                    return false;
                }

                string passwordHash = Verification.GetSHA512Hash(newPassword);

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            var authorCommand = new NpgsqlCommand(
                                "UPDATE authors SET name = @name, bio = @bio, email = @email, password_hash = @passwordHash " +
                                "WHERE author_id = @authorId",
                                connection, transaction);

                            authorCommand.Parameters.AddWithValue("@authorId", author.AuthorId);
                            authorCommand.Parameters.AddWithValue("@name", author.Name);
                            authorCommand.Parameters.AddWithValue("@bio", string.IsNullOrEmpty(author.Bio) ? (object)DBNull.Value : author.Bio);
                            authorCommand.Parameters.AddWithValue("@email", author.Email);
                            authorCommand.Parameters.AddWithValue("@passwordHash", passwordHash);

                            int authorRows = authorCommand.ExecuteNonQuery();

                            var readerCommand = new NpgsqlCommand(
                                "UPDATE readers SET username = @username, email = @email, password_hash = @passwordHash " +
                                "WHERE reader_id = @readerId",
                                connection, transaction);

                            readerCommand.Parameters.AddWithValue("@readerId", author.ReaderId);
                            readerCommand.Parameters.AddWithValue("@username", author.Name);
                            readerCommand.Parameters.AddWithValue("@email", author.Email);
                            readerCommand.Parameters.AddWithValue("@passwordHash", passwordHash);

                            int readerRows = readerCommand.ExecuteNonQuery();

                            transaction.Commit();
                            return authorRows > 0 && readerRows > 0;
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show($"Ошибка при обновлении профиля: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateAuthorAsync(Author author)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var command = new NpgsqlCommand(
                        "UPDATE authors SET name = @name, bio = @bio, email = @email " +
                        "WHERE author_id = @authorId", connection);

                    command.Parameters.AddWithValue("@authorId", author.AuthorId);
                    command.Parameters.AddWithValue("@name", author.Name);
                    command.Parameters.AddWithValue("@bio", string.IsNullOrEmpty(author.Bio) ? (object)DBNull.Value : author.Bio);
                    command.Parameters.AddWithValue("@email", author.Email);

                    return await command.ExecuteNonQueryAsync() > 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления автора: {ex.Message}");
                return false;
            }
        }
    }
}