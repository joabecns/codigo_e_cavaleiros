using System;

namespace CodigoECavaleiros.Entities
{
    /// <summary>Herói controlado pelo jogador.</summary>
    public class Player : Character
    {
        public int Level { get; private set; }
        public int Xp { get; private set; }
        public int MaxFc { get; private set; }
        public int Fc { get; private set; }

        public Player(string name = "Aria")
            : base(name, Settings.PlayerHp, Settings.PlayerAtk, Settings.PlayerDef)
        {
            Level = 1; Xp = 0;
            MaxFc = Settings.PlayerFc; Fc = Settings.PlayerFc;
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
            MaxFc += 5;
            Heal((int)(MaxHp * 0.3));
        }

        public bool SpendFc(int amount)
        {
            if (Fc < amount) return false;
            Fc -= amount;
            return true;
        }

        public void GainFc(int amount) { Fc = Math.Min(MaxFc, Fc + amount); }

        public void RecoverAfterFight()
        {
            Heal((int)(MaxHp * Settings.HealBetweenFights));
            Fc = MaxFc;
        }

        public void FullRestore() { Restore(); Fc = MaxFc; }
    }
}
