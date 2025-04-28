using System;

namespace ForgeTales.Classes
{
    public class Chapter
    {
        public int ChapterId { get; set; }
        public int NovelId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public int ChapterNumber { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRenPyProject { get; set; }

        public Chapter(int chapterId, int novelId, string title,
                     string content, int chapterNumber, DateTime createdAt,
                     bool isRenPyProject = false)
        {
            ChapterId = chapterId;
            NovelId = novelId;
            Title = title;
            Content = content;
            ChapterNumber = chapterNumber;
            CreatedAt = createdAt;
            IsRenPyProject = isRenPyProject;
        }
    }
}