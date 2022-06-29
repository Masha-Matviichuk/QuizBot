using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Xsl;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Args;

namespace QuizTelegramChatBot
{
    public class Program
    {
        private static Quiz quiz;
        private static TelegramBotClient bot;
        private static Dictionary<long, QuestionState> States;
        private static Dictionary<long, int> UserScores;
        private static string StateFileName = "state.json";
        private static string ScoreFileName = "score.json";
        public static void Main(string[] args)
        {
            quiz = new Quiz("data.txt");
            if (File.Exists(StateFileName))
            {
                var json = File.ReadAllText(StateFileName);
                States = JsonConvert.DeserializeObject<Dictionary<long, QuestionState>>(json);
            }
            else
            {
                States = new Dictionary<long, QuestionState>();
            }
            if (File.Exists(ScoreFileName))
            {
                var json = File.ReadAllText(ScoreFileName);
                UserScores = JsonConvert.DeserializeObject<Dictionary<long, int>>(json);
            }
            else
            {
                UserScores = new Dictionary<long, int>();
            }
            var token = "1959780140:AAEAaeNuYreHY4jeeGKOK9yFkUqJ9RWEDLw"; 
            bot = new TelegramBotClient(token);
            bot.OnMessage += BotOnMessage;
            bot.StartReceiving();
            Console.WriteLine("!!!!!");
            Console.ReadLine();
            var stateJson = JsonConvert.SerializeObject(States);
            File.WriteAllText(StateFileName, stateJson);
            var scoreJson = JsonConvert.SerializeObject(UserScores);
            File.WriteAllText(ScoreFileName, scoreJson);

        }

        private static void BotOnMessage(object? sender, MessageEventArgs e)
        {
            var chatId = e.Message.Chat.Id;
            var fromId = e.Message.From.Id;
            if (e.Message.Text == "/start")
            {

                NewRound(chatId);
                UserScores[chatId] = 0;
            }else if (e.Message.Text == "/skipquestion")
            {
                NewRound(chatId);
            }else if(e.Message.Text == "/rating")
            {
                foreach (var user in UserScores.OrderByDescending(i => i.Value))
                {
                     
                    int rating = 1;
                    bot.SendTextMessageAsync(chatId, $"{rating}. Игрок: {user.Key} Очки: {user.Value}");
                    Console.WriteLine($"Key{user.Key} and Value{user.Value}" );
                    rating++;
                }
            }
            else if (e.Message.Text == "/stop")
            {
                bot.SendTextMessageAsync(chatId, $"Игра окончена!\nВаше количество очков - {UserScores[fromId]}.");
                UserScores[chatId] = 0;
                
                /*if (e.Message.Text != "/start")
                {
                    bot.SendTextMessageAsync(chatId, "Чтобы начать новою игру введите  \"/start\""); 
                }*/
            }
            else{


             if (!States.TryGetValue(chatId, out var state))
                    {
                        state = new QuestionState();
                        States[chatId] = state;
                    }

                    if (state.CurrentItem == null)
                    {
                        state.CurrentItem = quiz.NextQuestion();
                    }




                    var question = state.CurrentItem;
                    var tryAnswer = e.Message.Text.ToLower().Replace('ё', 'е');
                    if (tryAnswer == question.Answer)
                    {
                        bot.SendTextMessageAsync(chatId, "Правильно!");
                        
                        if (UserScores.ContainsKey(fromId))
                        {
                            UserScores[fromId]++;
                        }
                        else
                        {
                            UserScores[fromId] = 1;
                        }

                        //////////////////////////////////////////
                        if (UserScores[fromId] == 1)
                        {
                            bot.SendTextMessageAsync(chatId, $"У вас {UserScores[fromId]} очко");
                        }

                        if (UserScores[fromId] >= 2 && UserScores[fromId] <= 4)
                        {
                            bot.SendTextMessageAsync(chatId, $"У вас {UserScores[fromId]} очка");
                        }

                        if (UserScores[fromId] > 4)
                        {
                            bot.SendTextMessageAsync(chatId, $"У вас {UserScores[fromId]} очков");
                        }

                        /////////////////////////////////////////////     
                        NewRound(chatId);
                    }
                    else
                    {
                        state.Opened++;
                        if (state.IsEnd)
                        {
                            bot.SendTextMessageAsync(chatId, $"Вы не угадали! Ответ - {question.Answer}.");
                            NewRound(chatId);

                        }

                        bot.SendTextMessageAsync(chatId, state.DisplayQuestion);
                    }

            }
            
            

        }

        public static void NewRound(long  chatId)
        {
            if (!States.TryGetValue(chatId, out var state))
            {
                state = new QuestionState();
                States[chatId] = state;
            }

            state.CurrentItem = quiz.NextQuestion();
            state.Opened = 0;
            bot.SendTextMessageAsync(chatId, state.DisplayQuestion);
        }
    }
}