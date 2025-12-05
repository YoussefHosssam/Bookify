namespace Bookify.Services.Interfaces;

public interface IJwtService
{
    string GenerateToken(string userId, string email, IList<string> roles);
    string? ValidateToken(string token);
    string? GetUserIdFromToken(string token);
}

