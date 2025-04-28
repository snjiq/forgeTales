using System.Collections.Generic;
using Npgsql;
using ForgeTales.Classes;


namespace ForgeTales.Model
{
    public class GenreFromDb
    {
        private string _connectionString;

        public GenreFromDb(string connectionString)
        {
            _connectionString = connectionString;
        }

        public List<GenreGroup> GetAllGenres()
        {
            var genres = new List<GenreGroup>();

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = new NpgsqlCommand("SELECT genre_id, name FROM genres", connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var genreId = reader.GetInt32(0);
                        var name = reader.GetString(1);
                        genres.Add(new GenreGroup(genreId, name)); // Используем конструктор
                    }
                }
            }

            return genres;
        }
    }
}