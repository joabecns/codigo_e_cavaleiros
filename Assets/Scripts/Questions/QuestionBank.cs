using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CodigoECavaleiros.Questions
{
    [Serializable]
    public class Question
    {
        public string id;
        public string topic;
        public string text;
        public string[] options;
        public int answer;
        public string explanation;

        /// <summary>Cópia com alternativas embaralhadas (a posição da correta muda).</summary>
        public Question Shuffled(System.Random rng)
        {
            var order = Enumerable.Range(0, options.Length).OrderBy(_ => rng.Next()).ToArray();
            return new Question
            {
                id = id, topic = topic, text = text, explanation = explanation,
                options = order.Select(i => options[i]).ToArray(),
                answer = Array.IndexOf(order, answer)
            };
        }
    }

    [Serializable]
    class QuestionList { public Question[] items; }

    /// <summary>Banco de questões: carrega o JSON, valida e sorteia sem repetir.</summary>
    public class QuestionBank
    {
        readonly System.Random rng;
        readonly HashSet<string> used = new HashSet<string>();
        public IReadOnlyList<Question> Questions { get; private set; }

        public QuestionBank(string json, System.Random rng = null)
        {
            this.rng = rng ?? new System.Random();
            Questions = Parse(json);
        }

        /// <summary>Lê Assets/Resources/questions.json.</summary>
        public static QuestionBank FromResources(System.Random rng = null)
        {
            var asset = Resources.Load<TextAsset>("questions");
            if (asset == null) throw new InvalidOperationException("Resources/questions.json não encontrado");
            return new QuestionBank(asset.text, rng);
        }

        static Question[] Parse(string json)
        {
            // JsonUtility não lê arrays na raiz: embrulha num objeto.
            var list = JsonUtility.FromJson<QuestionList>("{\"items\":" + json + "}");
            foreach (var q in list.items)
                if (q.options == null || q.options.Length != 4 || q.answer < 0 || q.answer >= 4)
                    throw new InvalidOperationException("Questão inválida: " + q.id);
            return list.items;
        }

        /// <summary>Sorteia uma questão dos tópicos indicados, sem repetir até esgotar o grupo.</summary>
        public Question Draw(IEnumerable<string> topics)
        {
            var set = new HashSet<string>(topics);
            var pool = Questions.Where(q => set.Contains(q.topic)).ToList();
            if (pool.Count == 0) throw new ArgumentException("Nenhuma questão para os tópicos informados");
            var fresh = pool.Where(q => !used.Contains(q.id)).ToList();
            if (fresh.Count == 0)
            {
                foreach (var q in pool) used.Remove(q.id);
                fresh = pool;
            }
            var choice = fresh[rng.Next(fresh.Count)];
            used.Add(choice.id);
            return choice.Shuffled(rng);
        }
    }
}
