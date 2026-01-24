using Domain.DataAccessInterfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bots;

namespace BeautySalonBot.ClientBotHandler;

public class ClientStartHandler : IUpdateHandler
{
    private readonly IProcedureRepository _procedureRepository;
    private static readonly Dictionary<long, string> _waitingForComment = new();
    private string _adminText = "";

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

        if (update.Type == UpdateType.Message && update.Message?.Text != null)
        {
            // либо это старт-команда
            if (update.Message.Text == "/start")
                return true;

            // либо пользователь в ожидании комментария
            if (_waitingForComment.ContainsKey(update.Message.From.Id))
                return true;
        }

        return false;
    }


    public async Task HandleAsync(ITelegramBotClient botClient, Update update, CancellationToken ct)
    {
        if (update.Message?.Text?.StartsWith("/start") == true)
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
            var time = now.ToString("HH:mm");
            // Ответ пользователю
            await botClient.SendMessage(
                chatId,
                $"{update.Message.From.FirstName}, спасибо! 🤍\n" +
                "Ваша заявка принята.\n" +
                "Наш администратор свяжется с вами в ближайшее время ✨",
                cancellationToken: ct);

            _adminText =
                $"📥 Запрос на запись:\n" +
                $"👤 Клиент: [{update.Message.From.FirstName}](tg://user?id={update.Message.From.Id})\n" +
                $"💅 Процедура: {procedureName}\n\n" +
                $"💬 Комментарий: {comment}\n\n" +
                $"Сообщение было отправлено в: {time}";
            await botClient.SendMessage(
                chatId: -5031976519,
                text: _adminText,
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
                new[] { InlineKeyboardButton.WithCallbackData(p.Name, $"select_procedure:{p.Id}") }
            )
        );
        

        await botClient.SendMessage(
            chatId,
            $"Здравствуйте, {firstName}!✨\n" +
            $"Спасибо за обращение в BEAUTY ZONE!✨\n\n" +
            $"Вас приветствует виртуальный помощник нашей студии красоты.\n" +
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
            var idPart = data.Split(':')[1];
            var procedureId = Guid.Parse(idPart);

            var procedure = await _procedureRepository.GetByIdAsync(procedureId);
            var procedureName = procedure.Name;

            _waitingForComment[query.From.Id] = procedureName;
            

            await bot.SendMessage(
                chatId,
                $"{query.From.FirstName}🤍\n" +
                "Спасибо за ваш запрос!\n" +
                "Пожалуйста, отправьте в ответном сообщении:\n" +
                "📅 желаемую дату записи\n" +
                "⏰ удобное время\n" +
                "Мы проверим доступность и в ближайшее время подтвердим вашу запись ✨",
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
                text: _adminText + $"\n\n📌 Статус: {statusText}",
                parseMode: ParseMode.Markdown,
                cancellationToken: ct);

            await bot.AnswerCallbackQuery(query.Id);
            return;
        }
    }
}