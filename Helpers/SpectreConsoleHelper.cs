using Spectre.Console;

namespace Application.Helpers;

internal class SpectreConsoleHelper
{
    public static void Log(string message) => Write($"LOG: {message}", _messageTypes[MessageTypeEnum.Log]);

    public static void Error(string error) => Write($"ERR: {error}", _messageTypes[MessageTypeEnum.Error]);

    public static void Information(string information) => Write($"INF: {information}", _messageTypes[MessageTypeEnum.Information]);

    public static void Success(string success) => Write($"LOG: {success}", _messageTypes[MessageTypeEnum.Success]);

    public static void Warning(string warning) => Write($"WRN: {warning}", _messageTypes[MessageTypeEnum.Warning]);

    private static void Write(string message, string color) => AnsiConsole.MarkupLine(string.Format("[{0}]{1} || {2}[/]", color, DateTime.Now, message));

    public static void WriteHeader(string header, Color color) => AnsiConsole.Write(new FigletText(header).Centered().Color(color));

    #region Privates

    private enum MessageTypeEnum
    {
        Log,
        Error,
        Information,
        Success,
        Warning
    }

    private static readonly Dictionary<MessageTypeEnum, string> _messageTypes = new()
    {
        [MessageTypeEnum.Log] = "white",
        [MessageTypeEnum.Error] = "red",
        [MessageTypeEnum.Information] = "blue",
        [MessageTypeEnum.Success] = "green",
        [MessageTypeEnum.Warning] = "yellow"
    };

    #endregion
}
