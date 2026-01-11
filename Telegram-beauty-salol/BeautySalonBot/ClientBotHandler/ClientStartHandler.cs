using Domain.DataAccessInterfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace BeautySalonBot.ClientBotHandler;

public class ClientStartHandler : IUpdateHandler
{
    private readonly IProcedureRepository _procedureRepository;
    private static readonly Dictionary<long, string> _waitingForComment = new();

    public ClientStartHandler(IProcedureRepository procedureRepository)
    {
        _procedureRepository = procedureRepository;
    }

    public bool CanHandle(Update update)
    {
        if (update.Type == UpdateType.CallbackQuery)
        {
            Console.WriteLine($"[DEBUG] CallbackQuery: {update.CallbackQuery.Data}");
            return true;
        }
        return update.Type == UpdateType.Message && update.Message?.Text == "/start";
    }


    public async Task HandleAsync(ITelegramBotClient botClient, Update update, CancellationToken ct)
    {
        if (update.Type == UpdateType.Message && update.Message?.Text == "/start")
        {
            await HandleStartCommand(botClient, update, ct);
            return;
        }

        if (update.Type == UpdateType.Message &&
            update.Message?.Text != null &&
            _waitingForComment.TryGetValue(update.Message.From.Id, out var procedureName))
        {
            var comment = update.Message.Text;
            var chatId = update.Message.Chat.Id;

            _waitingForComment.Remove(update.Message.From.Id);

            var now = DateTime.UtcNow.AddHours(9);
            var time = now.TimeOfDay;

            // Ответ пользователю
            await botClient.SendMessage(
                chatId,
                $"{update.Message.From.FirstName}, спасибо! 🤍\n" +
                "Ваша заявка принята.\n" +
                "Наш администратор свяжется с вами в ближайшее время ✨",
                cancellationToken: ct);

            // Сообщение админу
            await botClient.SendMessage(
                chatId: -5031976519,
                text: $"📥 Запрос на запись:\n" +
                      $"👤 Клиент: [{update.Message.From.FirstName}](tg://user?id={update.Message.From.Id})\n" +
                      $"💅 Процедура: {procedureName}\n" +
                      $"💬 Комментарий: {comment}\n\n" +
                      $"Сообщение было отправлено в: {time}",
                parseMode: ParseMode.Markdown,
                replyMarkup: new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("✅ Подтвержден", $"reaction:called"),
                        InlineKeyboardButton.WithCallbackData("🔁 Перезвонить", "reaction:retry")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("❌ Не дозвонились", "reaction:missed"),
                        InlineKeyboardButton.WithCallbackData("🚫 Неинтересно", "reaction:ignored")
                    }
                }),
                cancellationToken: ct);
            

            return;
        }
    }

    private async Task HandleStartCommand(ITelegramBotClient botClient, Update update, CancellationToken ct)
    {
        var chatId = long.Parse(update.Message.Chat.Id.ToString());
        var firstName = update.Message.From.FirstName;
        var procedures = await _procedureRepository.GetAllAsync();
        

        var inlineKeyboard = new InlineKeyboardMarkup(
            procedures.Select(p =>
                new[] { InlineKeyboardButton.WithCallbackData(p.Name, $"select_procedure:{p.Name}") }
            )
        );
        

        await botClient.SendMessage(
            chatId,
            $"Здравствуйте, {firstName}!✨\n" +
            $"Спасибо за обращение в BEAUTY ZONE!✨\n\n" +
            $"Вас приветствует виртуальный помощник студии красоты Beauty Zone.\n" +
            $"Пожалуйста, выберите желаемую процедуру из списка ниже 🤍",
            replyMarkup: inlineKeyboard,
            cancellationToken: ct);
    }

    public async Task HandleCallbackQueryAsync(ITelegramBotClient bot, CallbackQuery query, CancellationToken ct)
    {
        var chatId = query.Message.Chat.Id;
        var data = query.Data;
        
        var now = DateTime.UtcNow.AddHours(9);
        var time = now.TimeOfDay;
        var comment = string.Empty;

        
        if (data.StartsWith("select_procedure:"))
        {
            var procedureName = data.Split(':')[1];
            _waitingForComment[query.From.Id] = procedureName;

            await bot.AnswerCallbackQuery(
                query.Id,
                $"Вы выбрали процедуру: {procedureName}"
            );

            await bot.SendMessage(
                chatId,
                $"{query.From.FirstName}, благодарим за выбор 🤍\n" +
                "Отправьте, пожалуйста, сообщение с желаемой датой и временем для записи 🤍\n" +
                "Мы проверим доступность и подтвердим ✨",
                cancellationToken: ct
            );

            return;
        }
        if (data.StartsWith("reaction:"))
        {
            var parts = data.Split(':');
            var status = parts[1];

            var statusText = status switch
            {
                "called" => "✅ Запись подтверждена",
                "retry" => "🔁 Перезвонить",
                "missed" => "❌ Не дозвонились",
                "ignored" => "🚫 Неинтересно",
                _ => "Статус неизвестен"
            };

            var message = query.Message;
            await bot.EditMessageText(
                chatId: message.Chat.Id,
                messageId: message.MessageId,
                text: message.Text + $"\n\n📌 Статус: {statusText}",
                parseMode: ParseMode.Markdown,
                cancellationToken: ct);

            await bot.AnswerCallbackQuery(query.Id);
            return;
        }
    }
}