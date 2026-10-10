using System.Collections.Generic;

namespace CodigoECavaleiros.Entities
{
    /// <summary>Chefão com fases: quanto menos HP, mais ataques e menos tempo.</summary>
    public class Boss : Enemy
    {
        readonly List<EnemyAttack> phase2, phase3;

        public Boss(string name, int hp, int atk, int defense, IEnumerable<EnemyAttack> attacks,
                    IEnumerable<EnemyAttack> phase2, IEnumerable<EnemyAttack> phase3, int xpReward = 0)
            : base(name, hp, atk, defense, attacks, xpReward)
        {
            this.phase2 = new List<EnemyAttack>(phase2);
            this.phase3 = new List<EnemyAttack>(phase3);
        }

        public override int Phase
        {
            get { float r = Ratio; return r > 0.6f ? 1 : (r > 0.3f ? 2 : 3); }
        }

        public override IReadOnlyList<EnemyAttack> Attacks
        {
            get
            {
                var pool = new List<EnemyAttack>(base.Attacks);
                if (Phase >= 2) pool.AddRange(phase2);
                if (Phase >= 3) pool.AddRange(phase3);
                return pool;
            }
        }

        public override float QuestionTime
        {
            get { return Phase == 3 ? Settings.BossFinalPhaseTime : Settings.QuestionTime; }
        }
    }

    public class CompiladorSombrio : Boss
    {
        public CompiladorSombrio() : base("Compilador Sombrio", 125, 17, 4,
            new[] { new EnemyAttack("Erro de Sintaxe", 1.0f), new EnemyAttack("Warning", 0.8f), new EnemyAttack("Stack Overflow", 1.3f) },
            new[] { new EnemyAttack("Segfault", 1.6f) },
            new[] { new EnemyAttack("Kernel Panic", 1.9f), new EnemyAttack("Loop de Bugs", 0.8f, 2) })
        {
            Topics = new[] { "variaveis", "repeticao", "funcoes", "poo", "ia" };
            TopicLabel = "Todos os tópicos";
        }
    }
}
