using NotEnoughLogs;

namespace AxeHax.Core.Logging;

#nullable disable

public readonly struct AxeHaxLog
{
    public AxeHaxLog(LogLevel level, string category, string content)
    {
        this.Level = level;
        this.Category = category;
        this.Content = content;
    }

    public readonly LogLevel Level;
    public readonly string Category;
    public readonly string Content;
}