using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Npgsql;
using ForgeTales.Classes;
using System.Threading.Tasks;

namespace ForgeTales.Model
{
    public class ReaderFromDb
    {
        private readonly string _connectionString;

        public ReaderFromDb()
        {
            _connectionString = DbConnection.connectionStr;
        }
        public bool CheckPassword(int readerId, string currentPassword)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    string sql = "SELECT password_hash FROM readers WHERE reader_id = @readerId";
                    var command = new NpgsqlCommand(sql, connection);
                    command.Parameters.AddWithValue("@readerId", readerId);

                    string storedHash = (string)command.ExecuteScalar();
                    return Verification.VerifySHA512Hash(currentPassword, storedHash);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка проверки пароля: {ex.Message}");
                return false;
            }
        }
        public bool UpdatePassword(int readerId, string newPasswordHash)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    string sql = "UPDATE readers SET password_hash = @passwordHash WHERE reader_id = @readerId";
                    var command = new NpgsqlCommand(sql, connection);
                    command.Parameters.AddWithValue("@passwordHash", newPasswordHash);
                    command.Parameters.AddWithValue("@readerId", readerId);

                    return command.ExecuteNonQuery() > 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления пароля: {ex.Message}");
                return false;
            }
        }
        public SubscriptionInfo GetCurrentSubscription(int readerId)
        {
            try
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(
                        "SELECT s.name, us.end_date " +
                        "FROM users_subscriptions us " +
                        "JOIN subscriptions s ON us.sub_id = s.subscription_id " +
                        "WHERE us.reader_id = @readerId AND us.end_date > CURRENT_TIMESTAMP " +
                        "ORDER BY us.end_date DESC LIMIT 1", conn))
                    {
                        cmd.Parameters.AddWithValue("@readerId", readerId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new SubscriptionInfo
                                {
                                    Name = reader.GetString(0),
                                    EndDate = reader.GetDateTime(1),
                                    HeartsBalance = GetHeartsBalance(readerId)
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка получения подписки: {ex.Message}");
            }
            return null;
        }

        public bool HasUnlimitedSubscription(int readerId)
        {
            try
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(
                        "SELECT 1 FROM users_subscriptions " +
                        "WHERE reader_id = @readerId AND end_date > CURRENT_TIMESTAMP " +
                        "AND sub_id = 1", conn))
                    {
                        cmd.Parameters.AddWithValue("@readerId", readerId);
                        return cmd.ExecuteScalar() != null;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка проверки подписки: {ex.Message}");
                return false;
            }
        }

        public DateTime? GetLastReadTime(int readerId)
        {
            try
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(
                        "SELECT start_time FROM reading_sessions " +
                        "WHERE reader_id = @readerId AND action_type = 'chapter_read' " +
                        "ORDER BY start_time DESC LIMIT 1", conn))
                    {
                        cmd.Parameters.AddWithValue("@readerId", readerId);
                        var result = cmd.ExecuteScalar();
                        return result != DBNull.Value && result != null ? (DateTime?)result : null;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка получения времени чтения: {ex.Message}");
                return null;
            }
        }

        public bool CanReadNow(int readerId)
        {
            if (HasUnlimitedSubscription(readerId))
                return true;

            var lastReadTime = GetLastReadTime(readerId);
            if (!lastReadTime.HasValue)
                return true;

            return (DateTime.Now - lastReadTime.Value).TotalMinutes >= 15;
        }

        public int GetRemainingTime(int readerId)
        {
            var lastReadTime = GetLastReadTime(readerId);
            if (!lastReadTime.HasValue)
                return 0;

            var elapsed = DateTime.Now - lastReadTime.Value;
            return Math.Max(0, 15 - (int)elapsed.TotalMinutes);
        }

        public bool DeductHearts(int readerId, int amount)
        {
            try
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    conn.Open();

                    int currentBalance = GetHeartsBalance(readerId);
                    if (currentBalance < amount)
                        return false;

                    using (var cmd = new NpgsqlCommand(
                        "INSERT INTO hearts (reader_id, amount, operation_type) " +
                        "VALUES (@readerId, @amount, 'DEDUCT')", conn))
                    {
                        cmd.Parameters.AddWithValue("@readerId", readerId);
                        cmd.Parameters.AddWithValue("@amount", amount);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка списания сердечек: {ex.Message}");
                return false;
            }
        }

        public void LogReadingSession(int readerId, string actionType)
        {
            try
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(
                        "INSERT INTO reading_sessions (reader_id, action_type) " +
                        "VALUES (@readerId, @actionType)", conn))
                    {
                        cmd.Parameters.AddWithValue("@readerId", readerId);
                        cmd.Parameters.AddWithValue("@actionType", actionType);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка логирования: {ex.Message}");
            }
        }

        public int AddSubscription(int readerId, int subId, int months, decimal amount)
        {
            try
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(
                        "SELECT add_user_subscription(@readerId, @subId, @months, @amount)", conn))
                    {
                        cmd.Parameters.AddWithValue("@readerId", readerId);
                        cmd.Parameters.AddWithValue("@subId", subId);
                        cmd.Parameters.AddWithValue("@months", months);
                        cmd.Parameters.AddWithValue("@amount", amount);
                        return Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка добавления подписки: {ex.Message}");
                return -1;
            }
        }

        public bool AddHearts(int readerId, int amount, string operationType)
        {
            try
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(
                        "INSERT INTO hearts (reader_id, amount, operation_type) " +
                        "VALUES (@readerId, @amount, @operationType)", conn))
                    {
                        cmd.Parameters.AddWithValue("@readerId", readerId);
                        cmd.Parameters.AddWithValue("@amount", amount);
                        cmd.Parameters.AddWithValue("@operationType", operationType);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public int GetHeartsBalance(int readerId)
        {
            try
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(
                        "SELECT COALESCE(SUM(CASE WHEN operation_type = 'DEDUCT' THEN -amount ELSE amount END), 0) " +
                        "FROM hearts WHERE reader_id = @readerId", conn))
                    {
                        cmd.Parameters.AddWithValue("@readerId", readerId);
                        return Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка получения баланса сердечек: {ex.Message}");
                return 0;
            }
        }

        public Reader GetReader(string username, string password)
        {
            try
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    conn.Open();
                    string sql = "SELECT reader_id, username, email, password_hash, avatar_url " +
                                "FROM readers WHERE username = @username";
                    var command = new NpgsqlCommand(sql, conn);
                    command.Parameters.AddWithValue("@username", username);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string storedHash = reader.GetString(3);
                            if (Verification.VerifySHA512Hash(password, storedHash))
                            {
                                var readerObj = new Reader(
                                    readerId: reader.GetInt32(0),
                                    username: reader.GetString(1),
                                    email: reader.GetString(2),
                                    passwordHash: storedHash,
                                    avatarUrl: reader.IsDBNull(4) ? null : reader.GetString(4));

                                readerObj.HeartsBalance = GetHeartsBalance(readerObj.ReaderId);
                                return readerObj;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
            return null;
        }

        public bool CheckUsername(string username)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    string sql = "SELECT username FROM readers WHERE username = @username";
                    var command = new NpgsqlCommand(sql, connection);
                    command.Parameters.AddWithValue("@username", username);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            MessageBox.Show("Такой логин уже существует");
                            return false;
                        }
                    }
                }
                return true;
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }

        public void AddReaderWithAvatar(string username, string email, string password, string avatarUrl)
        {
            try
            {
                if (!Verification.ValidatePasswordStrength(password))
                {
                    MessageBox.Show("Пароль должен содержать:\n- минимум 8 символов\n- цифру\n- заглавную и строчную буквы\n- спецсимвол");
                    return;
                }

                string passwordHash = Verification.GetSHA512Hash(password);

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    string sql = "INSERT INTO readers (username, email, password_hash, avatar_url) " +
                                "VALUES (@username, @email, @passwordHash, @avatarUrl)";

                    using (var command = new NpgsqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@username", username);
                        command.Parameters.AddWithValue("@email", email);
                        command.Parameters.AddWithValue("@passwordHash", passwordHash);
                        command.Parameters.AddWithValue("@avatarUrl",
                            string.IsNullOrEmpty(avatarUrl) ? (object)DBNull.Value : avatarUrl);

                        command.ExecuteNonQuery();
                        MessageBox.Show("Регистрация прошла успешно!");
                    }
                }
            }
            catch (NpgsqlException ex) when (ex.Message.Contains("уже существует"))
            {
                MessageBox.Show("Пользователь с таким логином или email уже существует");
            }
            catch (NpgsqlException ex)
            {
                MessageBox.Show($"Ошибка регистрации: {ex.Message}");
            }
        }

        public async Task<bool> UpdateAvatarAsync(int readerId, string avatarUrl)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var command = new NpgsqlCommand(
                        "UPDATE readers SET avatar_url = @avatarUrl WHERE reader_id = @readerId",
                        connection);

                    command.Parameters.AddWithValue("@avatarUrl",
                        string.IsNullOrEmpty(avatarUrl) ? (object)DBNull.Value : avatarUrl);
                    command.Parameters.AddWithValue("@readerId", readerId);

                    return await command.ExecuteNonQueryAsync() > 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления аватара: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateReaderProfileAsync(Reader reader, string newPassword = null)
        {
            try
            {
                if (!string.IsNullOrEmpty(newPassword))
                {
                    if (!Verification.ValidatePasswordStrength(newPassword))
                    {
                        MessageBox.Show("Новый пароль должен содержать:\n- минимум 8 символов\n- цифру\n- заглавную и строчную буквы\n- спецсимвол");
                        return false;
                    }
                }

                string passwordHash = string.IsNullOrEmpty(newPassword) ?
                    reader.PasswordHash :
                    Verification.GetSHA512Hash(newPassword);

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var command = new NpgsqlCommand(
                        "UPDATE readers SET username = @username, email = @email, " +
                        "password_hash = @passwordHash, avatar_url = @avatarUrl " +
                        "WHERE reader_id = @readerId", connection);

                    command.Parameters.AddWithValue("@readerId", reader.ReaderId);
                    command.Parameters.AddWithValue("@username", reader.Username);
                    command.Parameters.AddWithValue("@email", reader.Email);
                    command.Parameters.AddWithValue("@passwordHash", passwordHash);
                    command.Parameters.AddWithValue("@avatarUrl",
                        string.IsNullOrEmpty(reader.AvatarUrl) ? (object)DBNull.Value : reader.AvatarUrl);

                    return await command.ExecuteNonQueryAsync() > 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления читателя: {ex.Message}");
                return false;
            }
        }
    }
}