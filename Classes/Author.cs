using System;

public class Author
{
    public int AuthorId { get; set; }
    public string Name { get; set; }
    public string Bio { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public int? ReaderId { get; set; }
    public string AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }


    public Author() { }


    public Author(int authorId, string name, string bio, string email,
                string passwordHash, int? readerId = null,
                string avatarUrl = null, DateTime? createdAt = null)
    {
        AuthorId = authorId;
        Name = name;
        Bio = bio;
        Email = email;
        PasswordHash = passwordHash;
        ReaderId = readerId;
        AvatarUrl = avatarUrl;
        CreatedAt = createdAt ?? DateTime.Now;
    }
}