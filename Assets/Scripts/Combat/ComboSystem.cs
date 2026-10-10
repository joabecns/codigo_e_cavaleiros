using System;

namespace CodigoECavaleiros.Combat
{
    /// <summary>Acertos consecutivos aumentam o dano.</summary>
    public class ComboSystem
    {
        public const float Step = 0.25f;   // +25% por acerto extra
        public const int MaxSteps = 3;     // bônus máximo: +75%

        public int Streak { get; private set; }
        public int Best { get; private set; }

        public void Register(bool correct)
        {
            if (correct) { Streak++; Best = Math.Max(Best, Streak); }
            else Streak = 0;
        }

        public float Multiplier()
        {
            int steps = Math.Min(Math.Max(Streak - 1, 0), MaxSteps);
            return 1f + Step * steps;
        }

        public string Label()
        {
            if (Streak < 2) return "";
            return "COMBO x" + Streak + "  (+" + (int)((Multiplier() - 1f) * 100f + 0.5f) + "%)";
        }
    }
}
