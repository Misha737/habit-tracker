namespace UsersService.Domain;

public class User
{
    public Guid Id { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    private User() { }

    public User(string displayName, string email)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new UserDomainException("DisplayName cannot be empty.");

        if (string.IsNullOrWhiteSpace(email))
            throw new UserDomainException("Email cannot be empty.");

        if (!email.Contains('@'))
            throw new UserDomainException("Email must be a valid address.");

        Id = Guid.NewGuid();
        DisplayName = displayName.Trim();
        Email = email.Trim().ToLowerInvariant();
        CreatedAt = DateTime.UtcNow;
    }
}

public class UserDomainException : Exception
{
    public UserDomainException(string message) : base(message) { }
}
