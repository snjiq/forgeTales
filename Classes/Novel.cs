using System;
using System.Collections.Generic;
using ForgeTales.Classes;

namespace ForgeTales.Model
{
    public class Novel
    {
        public int NovelId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int? GenreId { get; set; }
        public int AuthorId { get; set; }
        public DateTime CreatedAt { get; set; }
        private string _coverImageUrl;
        public string CoverImageUrl
        {
            get => string.IsNullOrWhiteSpace(_coverImageUrl)
                ? "pack://application:,,,/Images/back.jpg"
                : _coverImageUrl;
            set => _coverImageUrl = value;
        }
        public Author Author { get; set; }
        public GenreGroup Genre { get; set; }
        public double AverageRating { get; set; }
        public int ChapterCount { get; set; }
        public int ViewCount { get; set; }
        public List<Review> Reviews { get; set; } = new List<Review>();

        public Novel() { }

        public Novel(int novelId, string title, string description, int? genreId,
                    int authorId, DateTime createdAt, string url, Author author)
        {
            NovelId = novelId;
            Title = title;
            Description = description;
            GenreId = genreId;
            AuthorId = authorId;
            CreatedAt = createdAt;
            CoverImageUrl = url;
            Author = author;
        }
    }
}