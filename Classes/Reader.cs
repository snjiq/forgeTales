public class Reader
{
    public int ReaderId { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public string AvatarUrl { get; set; }

    // Это свойство теперь будет вычисляться, а не храниться в таблице readers
    public int HeartsBalance { get; set; }
    public Reader() { }

    public Reader(int readerId, string username, string email, string passwordHash,
                 string avatarUrl = null)
    {
        ReaderId = readerId;
        Username = username;
        Email = email;
        PasswordHash = passwordHash;
        AvatarUrl = avatarUrl;
        // HeartsBalance будет установлен отдельно
    }
}