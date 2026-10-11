using System;

namespace CodigoECavaleiros.Entities
{
    /// <summary>BugHunter controlado pelo jogador.</summary>
    public class Player : Character
    {
        public int Level { get; private set; }
        public int Xp { get; private set; }

        public Player(string name = "Aria")
            : base(name, Settings.PlayerHp, Settings.PlayerAtk, Settings.PlayerDef)
        {
            Level = 1; Xp = 0;
        }

        public int XpNeeded { get { return 40 + 20 * (Level - 1); } }

        /// <summary>Soma XP e sobe de nível quando possível. Retorna quantos níveis subiu.</summary>
        public int GainXp(int amount)
        {
            Xp += amount;
            int gained = 0;
            while (Level < Settings.MaxLevel && Xp >= XpNeeded)
            {
                Xp -= XpNeeded;
                LevelUp();
                gained++;
            }
            return gained;
        }

        void LevelUp()
        {
            Level++;
            RaiseMaxHp(15);
            RaiseAtk(2);
            Heal((int)(MaxHp * 0.3));
        }

        public void RecoverAfterFight()
        {
            Heal((int)(MaxHp * Settings.HealBetweenFights));
        }

        public void FullRestore() { Restore(); }
    }
}
