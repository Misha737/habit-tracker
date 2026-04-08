namespace Modules.Core.Application.Ports;

public interface IUserValidationClient
{
    Task<bool> UserExistsAsync(Guid userId, CancellationToken ct = default);
}

public class UserServiceUnavailableException : Exception
{
    public UserServiceUnavailableException(string message, Exception? inner = null)
        : base(message, inner) { }
}
