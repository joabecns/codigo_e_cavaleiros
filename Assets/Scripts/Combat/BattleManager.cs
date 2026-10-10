using System;
using System.Collections.Generic;
using CodigoECavaleiros.Entities;
using CodigoECavaleiros.Questions;

namespace CodigoECavaleiros.Combat
{
    public class TurnResult
    {
        public Question Question;
        public int? Chosen;
        public bool Correct, TimedOut;
        public int Streak, DamageDealt;
        public bool SkillUsed;
        public EnemyAttack? EnemyAttack;
        public List<int> Hits = new List<int>();
        public bool RoundFinished, EnemyDefeated, PlayerDefeated, PhaseChanged;
    }

    /// <summary>Lógica pura de batalha (sem Unity visual): fácil de testar.</summary>
    public class BattleManager
    {
        public Player Player { get; private set; }
        public Enemy Enemy { get; private set; }
        public ComboSystem Combo { get; private set; }
        public int Round { get; private set; }
        public int AskedInRound { get; private set; }
        public int TotalAsked { get; private set; }
        public int CorrectTotal { get; private set; }
        public bool SkillArmed { get; private set; }
        public Question Current { get; private set; }
        /// <summary>tópico -> [acertos, total]</summary>
        public Dictionary<string, int[]> Stats { get; private set; }

        readonly QuestionBank bank;
        readonly int perRound;

        public BattleManager(Player player, Enemy enemy, QuestionBank bank, int perRound = Settings.QuestionsPerRound)
        {
            Player = player; Enemy = enemy; this.bank = bank; this.perRound = perRound;
            Combo = new ComboSystem();
            Round = 1;
            Stats = new Dictionary<string, int[]>();
        }

        public Question NextQuestion()
        {
            Current = bank.Draw(Enemy.Topics);
            return Current;
        }

        /// <summary>Arma/desarma o "Depurar" (só arma se houver Foco suficiente).</summary>
        public bool ToggleSkill()
        {
            if (SkillArmed) SkillArmed = false;
            else if (Player.Fc >= Settings.SkillCost) SkillArmed = true;
            return SkillArmed;
        }

        public int CalcDamage(float mult)
        {
            return Math.Max(1, (int)Math.Round(Player.Atk * mult - Enemy.Defense * 0.5));
        }

        /// <summary>
        /// Processa a resposta (null = tempo esgotado). Acertou: o herói ataca.
        /// Errou: o inimigo sorteia (random) um ataque e contra-ataca.
        /// </summary>
        public TurnResult Answer(int? chosen)
        {
            var q = Current;
            if (q == null) throw new InvalidOperationException("Nenhuma questão ativa");
            bool correct = chosen.HasValue && chosen.Value == q.answer;
            int phaseBefore = Enemy.Phase;
            Combo.Register(correct);
            var res = new TurnResult { Question = q, Chosen = chosen, Correct = correct, TimedOut = !chosen.HasValue, Streak = Combo.Streak };

            int[] stat;
            if (!Stats.TryGetValue(q.topic, out stat)) { stat = new[] { 0, 0 }; Stats[q.topic] = stat; }
            stat[1]++;

            if (correct)
            {
                stat[0]++;
                CorrectTotal++;
                float mult = Combo.Multiplier();
                if (SkillArmed && Player.SpendFc(Settings.SkillCost))
                {
                    res.SkillUsed = true;
                    mult *= Settings.SkillMult;
                }
                res.DamageDealt = Enemy.ReceiveDamage(CalcDamage(mult));
                Player.GainFc(Settings.FcPerHit);
            }
            else
            {
                var attack = Enemy.ChooseAttack();
                res.EnemyAttack = attack;
                for (int i = 0; i < attack.Hits; i++)
                {
                    res.Hits.Add(Player.ReceiveDamage(Enemy.DamageAgainst(attack, Player)));
                    if (!Player.Alive) break;
                }
            }

            SkillArmed = false;
            Current = null;
            AskedInRound++;
            TotalAsked++;
            res.EnemyDefeated = !Enemy.Alive;
            res.PlayerDefeated = !Player.Alive;
            res.PhaseChanged = Enemy.Phase != phaseBefore;
            if (!(res.EnemyDefeated || res.PlayerDefeated) && AskedInRound >= perRound)
            {
                res.RoundFinished = true;
                Round++;
                AskedInRound = 0;
            }
            return res;
        }
    }
}
