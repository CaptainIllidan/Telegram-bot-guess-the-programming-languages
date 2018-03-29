using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineKeyboardButtons;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using ApiAiSDK;
using ApiAiSDK.Model;
using System.Threading;

namespace telegram
{
    class Program
    {
        static TelegramBotClient Bot;
        static ApiAi ApiAi;
        static Dictionary<string, Game> Games; 

        static void Main(string[] args)
        {
            Bot = new TelegramBotClient("Your key");
            AIConfiguration config = new AIConfiguration("Your key", SupportedLanguage.Russian);
            ApiAi = new ApiAi(config);

            Bot.OnCallbackQuery += BotOnCallbackQueryReceived;
            Bot.OnMessage += BotOnMessageReceived;

            var me = Bot.GetMeAsync().Result;
            DB.SetUp();
            Games=new Dictionary<string, Game>();

            Console.WriteLine(me.FirstName);
            Bot.StartReceiving();
            Console.ReadKey();
            Bot.StopReceiving();
        }

        private static async void BotOnCallbackQueryReceived(object sender, CallbackQueryEventArgs e)
        {
            string buttonText = e.CallbackQuery.Data;
            string name = $"{e.CallbackQuery.From.FirstName} {e.CallbackQuery.From.LastName}";
            Console.WriteLine($"{name} нажал кнопку {buttonText}");

            await Bot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, $"Вы нажали кнопку {buttonText}");
        }

        private static async void BotOnMessageReceived(object sender, MessageEventArgs e)
        {
            var message = e.Message;
            if (message == null || message.Type != MessageType.TextMessage)
                return;

            string name = $"{message.From.FirstName} {message.From.LastName}";

            Console.WriteLine($"{name} отправил сообщение {message.Text}");
            Game game = null;
            bool notServed = true;
            while (notServed)
            {
                try
                {
                    lock (Games)
                    {
                        if(Games.ContainsKey(name))
                            game=Games[name];
                    }
                    notServed = false;
                }
                catch (Exception ex)
                {
                    Thread.Sleep(0);
                }
            }
            if (game==null)
                switch (message.Text.Substring(0,message.Text.Contains(' ')? message.Text.IndexOf(' '):message.Text.Length))
                {
                    case "/start":
                        string text =
    @"Список команд:
        /start - запуск бота
        /showbase - показ всей базы знаний
        /tellAbout {язык} - Информация по выбранному языку; 
        /guess - начать игру по угадыванию языка программирования";
                        await Bot.SendTextMessageAsync(message.From.Id, text);
                        break;
                    case "/showbase":
                        await Bot.SendTextMessageAsync(message.From.Id, DB.TellAboutAllLanguages());
                        break;
                    case "/tellAbout":
                        await Bot.SendTextMessageAsync(message.From.Id, DB.TellAboutTheLanguage(message.Text.Substring(message.Text.IndexOf(' ') + 1)));
                        break;
                    case "/guess":
                        var replyKeyboard = new ReplyKeyboardMarkup(new[]
                        {
                                new[]
                                {
                                    new KeyboardButton("Да"),
                                    new KeyboardButton("Нет")
                                },
                                new[]
                                {
                                    new KeyboardButton("Не знаю / не уверен"),
                                    new KeyboardButton("Объясни выбор")
                                }
                            });
                        await Bot.SendTextMessageAsync(message.Chat.Id, "Сейчас я попробую угадать, какой язык программирования ты загадал(а)",
                            replyMarkup: replyKeyboard);
                        var ngame = new Game();
                        bool ns = true;
                        while (ns)
                        {
                            try
                            {
                                lock (Games)
                                {
                                    Games.Add(name, ngame);
                                }
                                ns = false;
                            }
                            catch (Exception ex)
                            {
                                Thread.Sleep(0);
                            }
                        }
                        var lang = ngame.OfferLanguage(true);
                        await Bot.SendTextMessageAsync(message.From.Id, $"Это - {lang.Name}?");
                        if (lang.IconUrl.Length > 0)
                            await Bot.SendTextMessageAsync(message.From.Id, lang.IconUrl);
                        break;
                    default:
                        var response = ApiAi.TextRequest(message.Text);
                        string answer = response.Result.Fulfillment.Speech;
                        if (answer == "")
                            answer = "Прости, я тебя не понял";
                        await Bot.SendTextMessageAsync(message.From.Id, answer);
                        break;
                }
            else
            {
                if (!game.IsQuestionAsked && !game.IsEnded)
                {
                    Question question = new Question(-1, "");
                    switch (message.Text)
                    {
                        case "Да":
                            game.EndGame(true);
                            bool ns = true;
                            while (ns)
                            {
                                try
                                {
                                    lock (Games)
                                    {
                                        Games.Remove(name);
                                    }
                                    ns = false;
                                }
                                catch (Exception ex)
                                {
                                    Thread.Sleep(0);
                                }
                            }
                            await Bot.SendTextMessageAsync(message.From.Id, "Отлично! Спасибо за игру :)");
                            await Bot.SendTextMessageAsync(message.From.Id, "https://i.imgur.com/1eJ1q0V.jpg");
                            await Bot.SendTextMessageAsync(message.From.Id, "Если хочешь сыграть еще раз, введи в чат команду /guess");
                            break;
                        case "Нет":
                            question = game.GetQuestion();
                            break;
                        case "Объясни выбор":
                            await Bot.SendTextMessageAsync(message.From.Id, game.ExplainChoice());
                            break;
                        default:
                            await Bot.SendTextMessageAsync(message.From.Id, @"Пожалуйста, отвечай на вопросы с помощью специальной клавиатуры:
    На вопросы о языке принимаются ответы да/нет/не знаю,
    На предложенный язык принимаются ответы да/нет");
                            break;
                    }
                    if (question.Id != -1)
                        await Bot.SendTextMessageAsync(message.From.Id, question.Text+'?');
                }
                else if (!game.IsEnded)
                {
                    var lang = new ProgrammingLanguage(-1, "");
                    if (game.QuestionsNotEnded)
                        switch (message.Text)
                        {
                            case "Да":
                                game.AddAnswer(1);
                                break;
                            case "Нет":
                                game.AddAnswer(-1);
                                break;
                            case "Не знаю / не уверен":
                                game.AddAnswer(0);
                                break;
                            case "Объясни выбор":
                                await Bot.SendTextMessageAsync(message.From.Id, game.ExplainChoice());
                                lang = new ProgrammingLanguage(-2, "");
                                break;
                            default:
                                await Bot.SendTextMessageAsync(message.From.Id, @"Пожалуйста, отвечай на вопросы с помощью специальной клавиатуры:
        На вопросы о языке принимаются ответы да/нет/не знаю,
        На предложенный язык принимаются ответы да/нет");
                                break;
                        }
                    else
                        switch (message.Text)
                        {
                            case "Да":
                                game.EndGame(true);
                                bool ns = true;
                                while (ns)
                                {
                                    try
                                    {
                                        lock (Games)
                                        {
                                            Games.Remove(name);
                                        }
                                        ns = false;
                                    }
                                    catch (Exception ex)
                                    {
                                        Thread.Sleep(0);
                                    }
                                }
                                await Bot.SendTextMessageAsync(message.From.Id, "Отлично! Спасибо за игру :)");
                                await Bot.SendTextMessageAsync(message.From.Id, "https://i.imgur.com/1eJ1q0V.jpg");
                                await Bot.SendTextMessageAsync(message.From.Id, "Если хочешь сыграть еще раз, введи в чат команду /guess");
                                break;
                            case "Нет":
                                break;
                            case "Объясни выбор":
                                await Bot.SendTextMessageAsync(message.From.Id, game.ExplainChoice());
                                break;
                            default:
                                await Bot.SendTextMessageAsync(message.From.Id, @"Пожалуйста, отвечай на вопросы с помощью специальной клавиатуры:
    На вопросы о языке принимаются ответы да/нет/не знаю,
    На предложенный язык принимаются ответы да/нет");
                                break;
                        }
                    if (lang.Id != -2)
                    {

                        lang = game.OfferLanguage();
                        if (lang.Id > -1)
                        {
                            await Bot.SendTextMessageAsync(message.From.Id, $"Это - {lang.Name}?");
                            if (lang.IconUrl.Length > 0)
                                await Bot.SendTextMessageAsync(message.From.Id, lang.IconUrl);
                        }
                        else
                        {
                            game.EndGame(false);
                            await Bot.SendTextMessageAsync(message.From.Id, "Я не смог угадать язык. Пожалуйста, подскажи его название. Если у тебя есть ссылка на картинку с его логотипом, добавь ее после символа\'*\'.\nФормат сообщения: {язык*ссылка}");
                        }
                    }
                }
                else if (game.IsQuestionAsked)
                {
                    string langName, iconurl = "";
                    if (message.Text.Contains("*"))
                    {
                        var parse = message.Text.Split('*');
                        langName = parse[0];
                        iconurl = parse[1];
                    }
                    else
                        langName = message.Text;
                    if (iconurl == "")
                        DB.AddLanguage(langName);
                    else
                        DB.AddLanguage(langName, iconurl);
                    game.ChangeState();
                    await Bot.SendTextMessageAsync(message.From.Id, "Спасибо! Если ты знаешь, какой можно задать вопрос, на который можно ответить да/нет, чтобы отгадать этот язык, пришли мне его. Если не знаешь или не хочешь, нажми на клавиатуре \"Нет\"");
                }
                else
                {
                    if (message.Text != "Нет")
                        DB.AddQuestion(message.Text.TrimEnd('?'));
                    game.SaveData();
                    await Bot.SendTextMessageAsync(message.From.Id,"Благодарю. При следующих играх твои ответы будут учтены. Если хочешь сыграть еще раз, введи в чат команду /guess");
                    await Bot.SendTextMessageAsync(message.From.Id, "https://s.tcdn.co/bf4/93e/bf493ef3-930b-3769-9e1d-fc5832693e60/192/2.png");
                    bool ns = true;
                    while (ns)
                    {
                        try
                        {
                            lock (Games)
                            {
                                Games.Remove(name);
                            }
                            ns = false;
                        }
                        catch (Exception ex)
                        {
                            Thread.Sleep(0);
                        }
                    }
                }

            }
        }
    }
}
