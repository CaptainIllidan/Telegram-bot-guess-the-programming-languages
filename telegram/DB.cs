using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;
using System.Timers;
using System.Threading;

namespace telegram
{
    public static class DB
    {
        private static List<Question> questions;
        private static List<ProgrammingLanguage> languages;
        private static List<List<int>> answers;
        private static System.Timers.Timer timer = new System.Timers.Timer(5 * 60 * 1000);
        
        public static void SetUp()
        {
            timer.AutoReset = true;
            timer.Elapsed += (sender, e) =>
            {
                bool notServed = true;
                while (notServed)
                    try
                    {
                        lock (questions)
                        {
                            lock (languages)
                            {
                                lock (answers)
                                {
                                    SaveDataToFiles();
                                }
                            }
                        }
                        notServed = false;
                    }
                    catch (Exception ex)
                    {
                        Thread.Sleep(0);
                    }
            };
            timer.Start();
            LoadDataFromFiles();
            //GenerateData();
        }

        private static void GenerateData()
        {
            questions = new List<Question>
            {
                new Question(0, "Интерпретируемый"),
                new Question(1, "Динамически типизируемый"),
                new Question(2, "Статически типизируемый"),
                new Question(3, "Объектно-ориентированный"),
                new Question(4, "Функциональный"),
                new Question(5, "Имеет встроенный сборщик мусора")
            };
            languages = new List<ProgrammingLanguage>
            {
                new ProgrammingLanguage(0, "JavaScript", "https://www.javatpoint.com/images/javascript/javascript_logo.png"),
                new ProgrammingLanguage(1, "Python", "https://www.python.org/static/opengraph-icon-200x200.png"),
                new ProgrammingLanguage(2, "Ruby", "http://bgasparotto.com/wp-content/uploads/2016/03/ruby-logo.png"),
                new ProgrammingLanguage(3, "CoffeeScript", "https://i1.wp.com/devstickers.com/assets/img/pro/t4mv.png?resize=375%2C375"),
                new ProgrammingLanguage(4, "Objective-C", "https://www.thinkingbit.co/blog/wp-content/uploads/2017/10/objective-c-training-croma-campus.png"),
                new ProgrammingLanguage(5, "Java", "https://images.sftcdn.net/images/t_optimized,f_auto/p/eb7e73a0-96da-11e6-b385-00163ed833e7/4013307709/java-for-os-x-screenshot.png"),
                new ProgrammingLanguage(6, "Go", "https://qph.fs.quoracdn.net/main-qimg-584b2d27360ea867c70d14214767574c"),
                new ProgrammingLanguage(7, "Clojure", "http://clojurebridge.lispnyc.org/static/images/clojure-logo.png"),
                new ProgrammingLanguage(8, "Scala", "http://gameover.co.in/wp-content/uploads/2015/04/scala-programming-language.jpg")
            };
            answers = new List<List<int>>
            {
                new List<int>{10, 10, -10, 10, 10, 10},
                new List<int>{10, 10, -10, 10, 10, 10},
                new List<int>{10, 10, -10, 10, 10, 10},
                new List<int>{-10, 10, -10, 10, 10, 10},
                new List<int>{-10, 10, 10, 10, -10, 10},
                new List<int>{-10, -10, 10, 10, -10, 10},
                new List<int>{-10, 10, 10, 10, 10, 10},
                new List<int>{-10, 10, -10, -10, 10, 10},
                new List<int>{-10, 10, -10, 10, 10, 10},
            };
        }

        public static List<Question> GetQuestions()
        {
            return questions;
        }

        public static List<ProgrammingLanguage> GetLanguages()
        {
            return languages;
        }

        public static List<List<int>> GetAnswers()
        {
            return answers;
        }

        private static List<T> LoadDataFromFile<T>(string filename)
        {
            var serializer = new DataContractJsonSerializer(typeof(T[]));
            var list = new List<T>();
            using (var fs = new FileStream(filename, FileMode.Open))
            {
                var data=((T[])serializer.ReadObject(fs));
                list = data.ToList();
            }
            Console.WriteLine($"Данные типа {typeof(T)} загружены из файла {filename}");
            return list;
        }

        private static void SaveDataToFile<T>(string filename, List<T> data)
        {
            var serializer = new DataContractJsonSerializer(typeof(List<T>));
            using (var fs = new FileStream(filename, FileMode.Create))
            {
                serializer.WriteObject(fs, data);
            }
            Console.WriteLine($"Данные типа {typeof(T)} сохранены в файл {filename}");
        }

        private static void SaveDataToFiles()
        {
            SaveDataToFile("questions.data",questions);
            SaveDataToFile("languages.data",languages);
            SaveDataToFile("answers.data",answers);
            Console.WriteLine("Все данные сохранены в файлы");
        }

        public static void LoadDataFromFiles()
        {
            questions = LoadDataFromFile<Question>("questions.data");
            languages = LoadDataFromFile<ProgrammingLanguage>("languages.data");
            answers = LoadDataFromFile<List<int>>("answers.data");
            Console.WriteLine("Все данные загружены из файлов");
        }

        public static void UpdateDB(List<Answer> answs, ProgrammingLanguage lang)
        {
            bool notServed = true;
            while (notServed)
            {
                try
                {
                    lock (answers)
                    {
                        foreach(var answer in answs)
                            answers[lang.Id][answer.questionId] += answer.value;
                    }
                    notServed = false;
                }
                catch (Exception ex)
                {
                    Thread.Sleep(0);
                }
            }
        }

        public static void AddLanguage(string langName, string iconUrl = null)
        {
            var lang = new ProgrammingLanguage(languages.Count, langName, iconUrl);
            bool notServed = true;
            while (notServed)
            {
                try
                {
                    lock (languages)
                    {
                        lock (answers)
                        {
                            lock (questions)
                            {
                                languages.Add(lang);
                                answers.Add(new List<int>());
                                for (int i = 0; i < questions.Count; i++)
                                    answers[lang.Id].Add(0);
                            }
                        }
                    }
                    notServed = false;
                }
                catch (Exception ex)
                {
                    Thread.Sleep(0);
                }
            }
        }

        public static void AddQuestion(string questionText)
        {
            var question = new Question(questions.Count, questionText);
            bool notServed = true;
            while (notServed)
            {
                try
                {
                    lock (questions)
                    {
                        lock (answers)
                        {
                            questions.Add(question);
                            for (int i = 0; i < answers.Count-1; i++)
                                answers[i].Add(0);
                            answers.Last().Add(1);
                        }
                    }
                    notServed = false;
                }
                catch (Exception ex)
                {
                    Thread.Sleep(0);
                }
            }
        }

        public static string TellAboutTheLanguage(ProgrammingLanguage language)
        {
            var languges = GetLanguages();
            if (!languages.Contains(language))
                return "Такого языка нет в базе";
            else
            {
                var result = new StringBuilder();
                result.Append($"{language.Name} ");
                var questions = GetQuestions();
                var answers = GetAnswers();
                for (int i=0; i<questions.Count; i++)
                {
                    if (answers[language.Id][i] > 5)
                        result.Append($", и {questions[i].Text} ");
                    else if(answers[language.Id][i] < -5)
                        result.Append($", и не {questions[i].Text} ");
                }
                if (result.ToString().Contains(','))
                    result.Remove(result.ToString().IndexOf(','), 4);
                return result.ToString();
            }
        }

        public static string TellAboutTheLanguage(string lang)
        {
            var languges = GetLanguages();
            var language = languages.FirstOrDefault(x => x.Name == lang);
            if (language != null)
            {
                if (!languages.Contains(language))
                    return "Такого языка нет в базе";
                else
                {
                    var result = new StringBuilder();
                    result.Append($"{language.Name} ");
                    var questions = GetQuestions();
                    var answers = GetAnswers();
                    for (int i = 0; i < questions.Count; i++)
                    {
                        if (answers[language.Id][i] > 5)
                            result.Append($", и {questions[i].Text} ");
                        else if (answers[language.Id][i] < -5)
                            result.Append($", и не {questions[i].Text} ");
                    }
                    result.Remove(result.ToString().IndexOf(','), 4);
                    return result.ToString();
                }
            }
            return "Такого языка нет в базе";
        }

        public static string TellAboutAllLanguages()
        {
            var result = new StringBuilder();
            var languages = GetLanguages();
            foreach (var language in languages)
                result.AppendLine($"{TellAboutTheLanguage(language)}\n");
            return result.ToString();
        }
    }
}
