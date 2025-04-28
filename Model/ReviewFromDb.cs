using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ForgeTales.Classes;
using Npgsql;

namespace ForgeTales.Model
{
    public class ReviewFromDb
    {
        private readonly string _connectionString;

        public ReviewFromDb()
        {
            _connectionString = DbConnection.connectionStr;
        }

        public bool AddReview(int novelId, int readerId, int rating, string comment)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(
                    "INSERT INTO reviews (novel_id, reader_id, rating, comment) " +
                    "VALUES (@novelId, @readerId, @rating, @comment)", conn))
                {
                    cmd.Parameters.AddWithValue("@novelId", novelId);
                    cmd.Parameters.AddWithValue("@readerId", readerId);
                    cmd.Parameters.AddWithValue("@rating", rating);
                    cmd.Parameters.AddWithValue("@comment", comment);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public List<Review> GetReviewsForNovel(int novelId)
        {
            var reviews = new List<Review>();
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(
                    "SELECT r.*, re.username, re.avatar_url FROM reviews r " +
                    "JOIN readers re ON r.reader_id = re.reader_id " +
                    "WHERE r.novel_id = @novelId ORDER BY r.created_at DESC", conn))
                {
                    cmd.Parameters.AddWithValue("@novelId", novelId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            reviews.Add(new Review
                            {
                                ReviewId = reader.GetInt32(0),
                                NovelId = reader.GetInt32(1),
                                ReaderId = reader.GetInt32(2),
                                Rating = reader.GetInt32(3),
                                Comment = reader.GetString(4),
                                CreatedAt = reader.GetDateTime(5),
                                Reader = new Reader
                                {
                                    ReaderId = reader.GetInt32(2),
                                    Username = reader.GetString(6),
                                    AvatarUrl = reader.IsDBNull(7) ? null : reader.GetString(7)
                                }
                            });
                        }
                    }
                }
            }
            return reviews;
        }
    }
}
