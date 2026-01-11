using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

public class ClientBot
{
    private readonly TelegramBotClient _botClient;
    private readonly List<IUpdateHandler> _handlers;

    public ClientBot(TelegramBotClient botClient, IEnumerable<IUpdateHandler> handlers)
    {
        _botClient = botClient;
        _handlers = handlers.ToList();
    }

    public void Start()
    {
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>() 
        };
        _botClient.StartReceiving(HandleUpdateAsync, HandlePollingErrorAsync, receiverOptions);

        Console.WriteLine("🤖 Client bot started");
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken ct)
    {
        if (update.Message != null)
        {
            Console.WriteLine($"Chat ID: {update.Message.Chat.Id}");
            Console.WriteLine($"Chat Type: {update.Message.Chat.Type}");
            Console.WriteLine($"From Group: {update.Message.Chat.Title}");
        }
        foreach (var handler in _handlers)
        {
            if (handler.CanHandle(update))
            {
                if (update.Type == UpdateType.CallbackQuery)
                {
                    await handler.HandleCallbackQueryAsync(_botClient, update.CallbackQuery, ct);
                }
                else
                {
                    await handler.HandleAsync(_botClient, update, ct);
                }
                return;
            }
        }
        
    }

    private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken ct)
    {
        Console.WriteLine(exception);
        return Task.CompletedTask;
    }
}
public interface IUpdateHandler
{
    bool CanHandle(Update update);
    Task HandleAsync(ITelegramBotClient botClient, Update update, CancellationToken ct);
    Task HandleCallbackQueryAsync(ITelegramBotClient botClient, CallbackQuery query, CancellationToken ct);
}