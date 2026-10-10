using System;

namespace CodigoECavaleiros.Entities
{
    /// <summary>Classe base abstrata dos personagens (herói e inimigos).</summary>
    public abstract class Character
    {
        public string Name { get; protected set; }
        public int Atk { get; protected set; }
        public int Defense { get; protected set; }
        public int MaxHp { get; protected set; }
        public int Hp { get; private set; }

        protected Character(string name, int maxHp, int atk, int defense)
        {
            Name = name; MaxHp = maxHp; Hp = maxHp; Atk = atk; Defense = defense;
        }

        public bool Alive { get { return Hp > 0; } }
        public float Ratio { get { return (float)Hp / MaxHp; } }

        /// <summary>Encapsulamento: o HP nunca fica negativo. Retorna o dano aplicado.</summary>
        public int ReceiveDamage(int amount)
        {
            amount = Math.Min(Hp, Math.Max(0, amount));
            Hp -= amount;
            return amount;
        }

        public int Heal(int amount)
        {
            int before = Hp;
            Hp = Math.Min(MaxHp, Hp + Math.Max(0, amount));
            return Hp - before;
        }

        public void Restore() { Hp = MaxHp; }

        protected void RaiseMaxHp(int amount) { MaxHp += amount; }
        protected void RaiseAtk(int amount) { Atk += amount; }
    }
}
