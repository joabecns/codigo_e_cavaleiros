using System;
using System.Collections.Generic;

namespace CodigoECavaleiros.Entities
{
    public struct EnemyAttack
    {
        public readonly string Name;
        public readonly float Power;
        public readonly int Hits;
        public EnemyAttack(string name, float power, int hits = 1) { Name = name; Power = power; Hits = hits; }
    }

    /// <summary>
    /// Inimigo comum. Regra de IA do projeto: quando o jogador erra, o inimigo
    /// escolhe ALEATORIAMENTE qual ataque usar (RandomIndex).
    /// </summary>
    public class Enemy : Character
    {
        static readonly Random Shared = new Random();

        readonly List<EnemyAttack> attacks;
        public int XpReward { get; private set; }
        public string[] Topics { get; protected set; }
        public string TopicLabel { get; protected set; }

        /// <summary>Sorteia um índice em [0, max). Troque nos testes para tornar o sorteio previsível.</summary>
        public Func<int, int> RandomIndex = max => Shared.Next(max);

        public Enemy(string name, int hp, int atk, int defense, IEnumerable<EnemyAttack> attacks, int xpReward)
            : base(name, hp, atk, defense)
        {
            this.attacks = new List<EnemyAttack>(attacks);
            XpReward = xpReward;
            Topics = new string[0];
            TopicLabel = "";
        }

        public virtual IReadOnlyList<EnemyAttack> Attacks { get { return attacks; } }
        public virtual float QuestionTime { get { return Settings.QuestionTime; } }
        public virtual int Phase { get { return 1; } }

        public EnemyAttack ChooseAttack()
        {
            var pool = Attacks;
            return pool[RandomIndex(pool.Count)];
        }

        public int DamageAgainst(EnemyAttack attack, Character target)
        {
            return Math.Max(1, (int)Math.Round(Atk * attack.Power - target.Defense * 0.5));
        }
    }

    public class PalhacoBug : Enemy
    {
        public PalhacoBug() : base("Palhaço Bug", 46, 7, 0,
            new[] { new EnemyAttack("Risada Maligna", 1.0f), new EnemyAttack("Buzina Estridente", 1.3f), new EnemyAttack("Confete de Erro", 0.8f) }, 45)
        { Topics = new[] { "variaveis" }; TopicLabel = "Variáveis e tipos"; }
    }

    public class RatoLoop : Enemy
    {
        public RatoLoop() : base("Rato Loop", 56, 9, 1,
            new[] { new EnemyAttack("Guincho Agudo", 1.0f), new EnemyAttack("Roda Infinita", 0.7f, 2), new EnemyAttack("Laço Apertado", 1.2f) }, 60)
        { Topics = new[] { "repeticao" }; TopicLabel = "Estruturas de repetição"; }
    }

    public class CoelhoNulo : Enemy
    {
        public CoelhoNulo() : base("Coelho Nulo", 66, 12, 2,
            new[] { new EnemyAttack("Olhar Vazio", 1.0f), new EnemyAttack("Exceção!", 1.4f), new EnemyAttack("Vazamento", 0.9f) }, 75)
        { Topics = new[] { "funcoes" }; TopicLabel = "Funções e listas"; }
    }

    public class SargentoHeranca : Enemy
    {
        public SargentoHeranca() : base("Sargento Herança", 76, 14, 3,
            new[] { new EnemyAttack("Grito de Ordem", 1.0f), new EnemyAttack("Sobrescrita", 1.3f), new EnemyAttack("Super()", 1.1f) }, 90)
        { Topics = new[] { "poo" }; TopicLabel = "Classes e POO"; }
    }
}
