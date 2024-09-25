using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace InterviewBuddy
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var questions = new Dictionary<string, string>
{
    { "Что такое SOLID?", "SOLID — это пять принципов объектно-ориентированного программирования, которые помогают делать код более поддерживаемым." },
    { "Что такое интерфейс в C#?", "Интерфейс — это контракт, который должен реализовать класс. В интерфейсе определяются сигнатуры методов, но нет реализации." },
    { "Что такое LINQ?", "LINQ (Language Integrated Query) — это язык запросов, интегрированный в C#, для работы с коллекциями данных." },
};

            var botClient = new TelegramBotClient("YOUR_TELEGRAM_BOT_API_TOKEN");

            // Хранилище для режима тестирования и результатов
            Dictionary<long, (int currentQuestion, int score, List<string> questionsOrder)> testSessions = new Dictionary<long, (int, int, List<string>)>();

            botClient.StartReceiving(
                updateHandler: HandleUpdateAsync,
                pollingErrorHandler: HandlePollingErrorAsync
            );

            Console.WriteLine("Bot is running...");

            Console.ReadLine();  // Для того, чтобы программа не завершалась

            async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
            {
                if (update.Type != UpdateType.Message || update.Message!.Text == null)
                    return;

                var chatId = update.Message.Chat.Id;
                var messageText = update.Message.Text.ToLower();

                if (messageText == "/start")
                {
                    await botClient.SendTextMessageAsync(chatId, "Привет! Я помогу тебе подготовиться к собеседованию на .NET разработчика.");
                    await ShowMenuAsync(botClient, chatId);
                }
                else if (messageText == "режим тестирования")
                {
                    StartTestSession(chatId);
                    await SendTestQuestionAsync(botClient, chatId);
                }
                else if (testSessions.ContainsKey(chatId))
                {
                    await CheckTestAnswerAsync(botClient, chatId, messageText);
                }
                else if (messageText == "следующий вопрос")
                {
                    await SendQuestionAsync(botClient, chatId);
                }
            }

            async Task ShowMenuAsync(ITelegramBotClient botClient, long chatId)
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Выберите режим:",
                    replyMarkup: new ReplyKeyboardMarkup(new[]
                    {
            new KeyboardButton("Режим тестирования"),
            new KeyboardButton("Следующий вопрос")
                    })
                    {
                        ResizeKeyboard = true,
                        OneTimeKeyboard = true
                    }
                );
            }

            void StartTestSession(long chatId)
            {
                var questionOrder = new List<string>(questions.Keys); // Создаем порядок вопросов
                testSessions[chatId] = (0, 0, questionOrder);  // Начало теста: текущий вопрос = 0, счет = 0
            }

            async Task SendTestQuestionAsync(ITelegramBotClient botClient, long chatId)
            {
                var session = testSessions[chatId];
                if (session.currentQuestion >= session.questionsOrder.Count)
                {
                    // Завершаем тест, если вопросы закончились
                    await botClient.SendTextMessageAsync(chatId, $"Тест завершен! Ваш результат: {session.score} из {session.questionsOrder.Count}");
                    testSessions.Remove(chatId); // Удаляем сессию
                    await ShowMenuAsync(botClient, chatId);  // Возвращаемся к главному меню
                    return;
                }

                var question = session.questionsOrder[session.currentQuestion];
                await botClient.SendTextMessageAsync(chatId, $"Вопрос {session.currentQuestion + 1}: {question}");
            }

            async Task CheckTestAnswerAsync(ITelegramBotClient botClient, long chatId, string userAnswer)
            {
                var session = testSessions[chatId];
                var currentQuestionText = session.questionsOrder[session.currentQuestion];
                var correctAnswer = questions[currentQuestionText];

                if (userAnswer.ToLower().Contains(correctAnswer.ToLower()))
                {
                    session.score++;  // Увеличиваем счет, если ответ правильный
                    await botClient.SendTextMessageAsync(chatId, "Правильно!");
                }
                else
                {
                    await botClient.SendTextMessageAsync(chatId, $"Неправильно. Правильный ответ: {correctAnswer}");
                }

                // Переходим к следующему вопросу
                session.currentQuestion++;
                testSessions[chatId] = session;
                await SendTestQuestionAsync(botClient, chatId);
            }

            async Task SendQuestionAsync(ITelegramBotClient botClient, long chatId)
            {
                var random = new Random();
                var questionIndex = random.Next(questions.Count);
                var question = new List<string>(questions.Keys)[questionIndex];

                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"Вопрос: {question}",
                    replyMarkup: new ReplyKeyboardMarkup(new[]
                    {
            new KeyboardButton("Следующий вопрос")
                    })
                    {
                        ResizeKeyboard = true,
                        OneTimeKeyboard = true
                    }
                );
            }

            Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
            {
                var errorMessage = exception switch
                {
                    ApiRequestException apiRequestException
                        => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                    _ => exception.ToString()
                };

                Console.WriteLine(errorMessage);
                return Task.CompletedTask;
            }
        }
    }
}
