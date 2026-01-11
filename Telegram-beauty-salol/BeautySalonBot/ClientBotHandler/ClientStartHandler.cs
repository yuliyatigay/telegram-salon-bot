using Domain.DataAccessInterfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace BeautySalonBot.ClientBotHandler;

public class ClientStartHandler : IUpdateHandler
{
    private readonly IProcedureRepository _procedureRepository;

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
        }
        else if ( update.Type == UpdateType.CallbackQuery)
        {
            await HandleCallbackQueryAsync(botClient, update.CallbackQuery, ct);
            
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
            $"Здравствуйте, {firstName}! 👋\n\n" +
            $"Вас приветствует виртуальный помощник.\n" +
            $"Для того чтобы отправить заявку, выберите процедуру из списка ниже\n" +
            $"Чтобы выбрать вторую процедуру нажмите на другую процедуру из списка ниже\n" +
            $"Спасибо что выбрали нас✨✨✨",
            replyMarkup: inlineKeyboard,
            cancellationToken: ct);
    }

    public async Task HandleCallbackQueryAsync(ITelegramBotClient bot, CallbackQuery query, CancellationToken ct)
    {
        var chatId = query.Message.Chat.Id;
        var data = query.Data;
        
        var time = query.Message.Date.TimeOfDay;
        var startOfDay = new TimeSpan(9, 0, 0);
        var endOfDay   = new TimeSpan(21, 0, 0); 

        if (data.StartsWith("select_procedure:"))
        {
            var procedureName = data.Split(':')[1];

            await bot.AnswerCallbackQuery(
                query.Id,
                $"Вы выбрали процедуру: {procedureName}"
            );
            if (time < startOfDay || time >= endOfDay)
            {
                await bot.SendMessage(
                    chatId,
                    $"🌙 Сейчас мы вне рабочего времени.\n" +
                    $"Наши часы работы: с 09:00 до 21:00.\n\n" +
                    $"В ближайший рабочий промежуток с вами свяжется наш менеджер\n" +
                    $"чтобы подтвердить запись и помочь с выбором удобного времени.💖\n" +
                    $"Спасибо что выбрали нас✨✨✨",
                    cancellationToken: ct);

                return;
            }

            await bot.SendMessage(
                chatId,
                $"Спасибо! Вы выбрали: *{procedureName}* 😊" +
                $" В ближайшее время с вами свяжется наш администратор для подтверждения записи. Хорошего дня!",
                parseMode: ParseMode.Markdown,
                cancellationToken: ct
            );
            await bot.SendMessage(
                chatId: -5031976519,
                text: $"📥 Запрос на запись:\n" +
                      $"👤 Клиент: [{query.From.FirstName }](tg://user?id={query.From.Id})\n" +
                      $"💅 Процедура: {procedureName}\n\n",
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
                cancellationToken: ct
            );
           
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