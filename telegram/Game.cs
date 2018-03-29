using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace telegram
{
    public struct Answer
    {
        public readonly int questionId;
        public readonly int value;
        public Answer(int questionId, int value)
        {
            this.questionId = questionId;
            this.value = value;
        }
        public override bool Equals(object obj)
        {
            return ((Answer)obj).questionId.Equals(questionId);
        }
    }

    class Game
    {
        List<Answer> currentUserAnswers;
        List<Question> questions;
        List<List<int>> answers;
        List<ProgrammingLanguage> languages;
        Dictionary<ProgrammingLanguage, int> languageChances;
        Random random;
        Question currentQuestion;
        ProgrammingLanguage currentLanguage;
        bool isEnded;
        bool isQuestionAsked;

        public Game()
        {
            currentUserAnswers = new List<Answer>();
            questions = new List<Question>(DB.GetQuestions());
            languageChances = new Dictionary<ProgrammingLanguage, int>();
            languages = new List<ProgrammingLanguage>(DB.GetLanguages());
            foreach (var language in languages)
                languageChances.Add(language, 0);
            answers = new List<List<int>>(DB.GetAnswers());
            random = new Random();
            isEnded = false;
            isQuestionAsked = true;
        }

        public bool IsEnded
        {
            get { return isEnded; }
        }
        public Question GetCurrentQuestion
        {
            get { return currentQuestion; }
        }
        public bool IsQuestionAsked
        {
            get { return isQuestionAsked; }
        }
        public bool QuestionsNotEnded
        {
            get { return questions.Count > 0; }
        }

        public void ChangeState()
        {
            isQuestionAsked = !isQuestionAsked;
        }
        public void AddAnswer(int value)
        {
            if (!currentUserAnswers.Contains(new Answer(currentQuestion.Id,0)))
                currentUserAnswers.Add(new Answer(currentQuestion.Id,value));
            UpdateChances();
        }

        public Question GetQuestion()
        {
            var rnd = random.Next(questions.Count);
            currentQuestion = questions[rnd];
            questions.Remove(currentQuestion);
            isQuestionAsked = !isQuestionAsked;
            return currentQuestion;
        }

        private void UpdateChances()
        {
            var newChances = new Dictionary<ProgrammingLanguage, int>(languageChances);
            foreach (var language in languageChances)
            {
                var currentUserAnswer = currentUserAnswers.Last();
                if ((currentUserAnswer.value > 0 && answers[language.Key.Id][currentUserAnswer.questionId] > 5) ||
                    (currentUserAnswer.value < 0 && answers[language.Key.Id][currentUserAnswer.questionId] < -5))
                    newChances[language.Key]++;
                else if ((currentUserAnswer.value < 0 && answers[language.Key.Id][currentUserAnswer.questionId] > 5) ||
                    (currentUserAnswer.value > 0 && answers[language.Key.Id][currentUserAnswer.questionId] < -5))
                    newChances[language.Key]--;
            }
            languageChances = newChances;
        }

        private void RemoveLanguage(ProgrammingLanguage language)
        {
            languages.Remove(language);
            languageChances.Remove(languageChances.Select(s => s.Key)
                .Select(z => z)
                .Where(x=>x == language)
                .FirstOrDefault());
        }

        public ProgrammingLanguage OfferLanguage()
        {
            if (languageChances.Count == 0)
                return new ProgrammingLanguage(-1, "");
            currentLanguage = languageChances.OrderByDescending(x=>x.Value).FirstOrDefault().Key;
            RemoveLanguage(currentLanguage);
            if (questions.Count>0)
                isQuestionAsked = !isQuestionAsked;
            return currentLanguage;
        }

        public ProgrammingLanguage OfferLanguage(bool rnd)
        {
            if (!rnd)
                return OfferLanguage();
            currentLanguage = languageChances.ElementAt(random.Next(languageChances.Count)).Key;
            RemoveLanguage(currentLanguage);
            isQuestionAsked = !isQuestionAsked;
            return currentLanguage;
        }

        public void EndGame(bool success)
        {
            isEnded = true;
            if (success)
                SaveData();
        }

        public void SaveData()
        {
            DB.UpdateDB(currentUserAnswers, DB.GetLanguages().Last());
        }

        public string ExplainChoice()
        {
            if (currentUserAnswers.Count > 0)
            {
                var result = new StringBuilder();
                var quests = DB.GetQuestions();
                result.Append("Потому что ");

                foreach (var userAnswer in currentUserAnswers)
                {
                    result.Append(", и ");
                    if (userAnswer.value < 0)
                        result.Append("не ");
                    result.Append(quests[userAnswer.questionId].Text);
                }
                result.Remove(result.ToString().IndexOf(','), 4);
                return result.ToString();
            }
            else return "Вы еще не отвечали на вопросы";


        }
    }
}
