namespace Bookify.Core.Extensions;

public static class StringExtensions
{
    public static string GenerateBookingNumber()
    {
        return $"BK{DateTime.UtcNow:yyyyMMdd}{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
    }
}

