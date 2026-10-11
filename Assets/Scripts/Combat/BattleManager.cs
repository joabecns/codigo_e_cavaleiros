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
        public bool SkillUsed, SkillBecameReady;
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
        /// <summary>Acertos acumulados para o Depurar (0 até HitsForSkill).</summary>
        public int SkillCharge { get; private set; }
        public bool SkillReady { get { return SkillCharge >= Settings.HitsForSkill; } }
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

        /// <summary>Arma/desarma o "Depurar" (só arma quando a carga de 3 acertos estiver completa).</summary>
        public bool ToggleSkill()
        {
            if (SkillArmed) SkillArmed = false;
            else if (SkillReady) SkillArmed = true;
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
            bool readyBefore = SkillReady;
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
                if (SkillArmed && SkillReady)
                {
                    res.SkillUsed = true;
                    mult *= Settings.SkillMult;
                    SkillCharge = 0;
                }
                res.DamageDealt = Enemy.ReceiveDamage(CalcDamage(mult));
                if (!res.SkillUsed) SkillCharge = Math.Min(Settings.HitsForSkill, SkillCharge + 1);
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
            res.SkillBecameReady = !readyBefore && SkillReady;
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
