using innBot.Data;
using innBot.Services;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;


IConfiguration configuration = new ConfigurationBuilder()
    .AddJsonFile("config.json")
    .Build();
var client = new TelegramBotClient(configuration["ApiKeys:TelegramBot"]);
LastCommandRepository lastCommandRepository = new LastCommandRepository();
client.StartReceiving(HandleUpdateAsync, HandlePollingErrorAsync);
Console.ReadLine();


async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    var message = update.Message;

    if (message == null || message.Type != MessageType.Text)
        return;
    
    switch (message.Text.Split(' ').First())
    {
        case "/last":
            string lastCommand = lastCommandRepository.GetLastCommand(message.From.Id);
            if (lastCommand != null)
            {
                await ProcessCommandAsync(botClient, message.Chat.Id, message.From.Id, lastCommand);
            }
            else
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Вы ещё не использовали ни одной команды.");
            }
            break;

        default:
            await ProcessCommandAsync(botClient, message.Chat.Id, message.From.Id, message.Text);
            break;
    }
}

Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
{
    var ErrorMessage = exception switch
    {
        ApiRequestException apiRequestException
            => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
        _ => exception.ToString()
    };

    Console.WriteLine(ErrorMessage);
    return Task.CompletedTask;
}

async Task ProcessCommandAsync(ITelegramBotClient botClient, long chatId, long userId, string command)
{
    lastCommandRepository.SaveCommand(command, userId);
    switch (command.Split(' ').First())
    {
        case "/start":
            await botClient.SendTextMessageAsync(chatId, "Добрый день! Чтобы получить список доступных команд, введите команду /help");
            break;

        case "/help":
            await botClient.SendTextMessageAsync(chatId, "Доступные команды:\n" +
                                                         "/start - начать общение с ботом\n" +
                                                         "/help - вывести справку о доступных командах\n" +
                                                         "/hello - вывести имя и фамилию, email и ссылку на github создателя бота\n" +
                                                         "/inn <ИНН> - получить наименования и адреса компаний по ИНН. Для получения информации о нескольких компаниях, введите их ИНН через пробел\n" +
                                                         "/last - повторить последнее действие бота");
            break;

        case "/hello":
            await botClient.SendTextMessageAsync(chatId, "Создатель бота: Шулепа Яна. \nEmail: ynshulepa@mail.ru\nGitHub: https://github.com/yanulio");
            break;

        case "/inn":
            try
            {
                if (command.Split(' ').Length > 1)
                {
                    for (int i = 1; i < command.Split(' ').Length; ++i)
                    {
                        string answer = await InnInfoProcessor.GetInfoByInn(command.Split(' ')[i]);
                        await botClient.SendTextMessageAsync(chatId, answer);
                    }
                }
                else
                {
                    await botClient.SendTextMessageAsync(chatId, "Укажите ИНН");
                }
            }
            catch (HttpRequestException ex)
            {
                await botClient.SendTextMessageAsync(chatId, "Произошла ошибка при обращении к сайту, попробуйте позже.");
            }
            break;

        default:
            await botClient.SendTextMessageAsync(chatId, "Неизвестная команда. Используйте /help для получения списка команд.");
            break;
    }
}

