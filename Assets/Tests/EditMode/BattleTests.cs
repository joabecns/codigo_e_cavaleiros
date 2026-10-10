using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using CodigoECavaleiros.Combat;
using CodigoECavaleiros.Entities;
using CodigoECavaleiros.Questions;

namespace CodigoECavaleiros.Tests
{
    /// <summary>Mesmos casos de teste do protótipo em Python (test_combat.py e test_questions_player.py).</summary>
    public class BattleTests
    {
        QuestionBank NewBank(int seed = 1)
        {
            return QuestionBank.FromResources(new System.Random(seed));
        }

        BattleManager Make(out Player p, out Enemy e)
        {
            p = new Player(); e = new PalhacoBug();
            return new BattleManager(p, e, NewBank());
        }

        [Test]
        public void CorrectAnswer_DamagesEnemyOnly()
        {
            Player p; Enemy e; var bm = Make(out p, out e);
            var res = bm.Answer(bm.NextQuestion().answer);
            Assert.IsTrue(res.Correct);
            Assert.Greater(res.DamageDealt, 0);
            Assert.Less(e.Hp, e.MaxHp);
            Assert.AreEqual(p.MaxHp, p.Hp);
        }

        [Test]
        public void WrongAnswer_UsesRandomChoiceForEnemyAttack()
        {
            Player p; Enemy e; var bm = Make(out p, out e);
            bm.NextQuestion();
            int askedMax = -1;
            e.RandomIndex = max => { askedMax = max; return max - 1; };  // sorteio previsível: último ataque
            var res = bm.Answer(null);                                   // tempo esgotado conta como erro
            Assert.AreEqual(e.Attacks.Count, askedMax);
            Assert.AreEqual(e.Attacks[e.Attacks.Count - 1].Name, res.EnemyAttack.Value.Name);
            Assert.Less(p.Hp, p.MaxHp);
            Assert.AreEqual(e.MaxHp, e.Hp);
        }

        [Test]
        public void Round_HasFiveQuestions()
        {
            Player p; Enemy e; var bm = Make(out p, out e);
            var finished = new bool[5];
            for (int i = 0; i < 5; i++) { bm.NextQuestion(); finished[i] = bm.Answer(null).RoundFinished; }
            CollectionAssert.AreEqual(new[] { false, false, false, false, true }, finished);
            Assert.AreEqual(2, bm.Round);
            Assert.AreEqual(0, bm.AskedInRound);
        }

        [Test]
        public void Skill_CostsFocusAndAddsDamage()
        {
            Player p; Enemy e; var bm = Make(out p, out e);
            Assert.IsTrue(bm.ToggleSkill());
            var res = bm.Answer(bm.NextQuestion().answer);
            Assert.IsTrue(res.SkillUsed);
            Assert.AreEqual(Settings.PlayerFc - Settings.SkillCost + Settings.FcPerHit, p.Fc);
            Assert.AreEqual(bm.CalcDamage(1.0f * Settings.SkillMult), res.DamageDealt);
        }

        [Test]
        public void Skill_NotArmedWithoutFocus()
        {
            Player p; Enemy e; var bm = Make(out p, out e);
            p.SpendFc(p.Fc);
            Assert.IsFalse(bm.ToggleSkill());
        }

        [Test]
        public void Boss_PhasesAndTimer()
        {
            var boss = new CompiladorSombrio();
            Assert.AreEqual(1, boss.Phase);
            Assert.AreEqual(Settings.QuestionTime, boss.QuestionTime);
            boss.ReceiveDamage((int)(boss.MaxHp * 0.75));
            Assert.AreEqual(3, boss.Phase);
            Assert.AreEqual(Settings.BossFinalPhaseTime, boss.QuestionTime);
            Assert.Greater(boss.Attacks.Count, 3);
        }

        [Test]
        public void Combo_Multiplier()
        {
            var c = new ComboSystem();
            Assert.AreEqual(1.0f, c.Multiplier());
            for (int i = 0; i < 3; i++) c.Register(true);
            Assert.AreEqual(1.5f, c.Multiplier());
            c.Register(false);
            Assert.AreEqual(0, c.Streak);
            Assert.AreEqual(3, c.Best);
        }

        [Test]
        public void QuestionBank_IsValidAndCoversAllTopics()
        {
            var bank = NewBank(3);
            Assert.GreaterOrEqual(bank.Questions.Count, 40);
            CollectionAssert.AreEquivalent(Settings.AllTopics, bank.Questions.Select(q => q.topic).Distinct().ToArray());
            Assert.IsTrue(bank.Questions.All(q => q.options.Length == 4 && q.answer >= 0 && q.answer < 4));
        }

        [Test]
        public void Draw_DoesNotRepeatUntilExhausted()
        {
            var bank = NewBank(3);
            var ids = Enumerable.Range(0, 8).Select(_ => bank.Draw(new[] { "poo" }).id).ToList();
            Assert.AreEqual(8, ids.Distinct().Count());
            Assert.IsNotNull(bank.Draw(new[] { "poo" }).id); // depois de esgotar, recomeça sem erro
        }

        [Test]
        public void Shuffle_KeepsCorrectAnswer()
        {
            var bank = NewBank(3);
            var original = bank.Questions.ToDictionary(q => q.id);
            for (int i = 0; i < 50; i++)
            {
                var q = bank.Draw(new[] { "variaveis", "ia" });
                var o = original[q.id];
                Assert.AreEqual(o.options[o.answer], q.options[q.answer]);
            }
        }

        [Test]
        public void Player_LevelUpAndHpBounds()
        {
            var p = new Player();
            Assert.AreEqual(1, p.GainXp(45));
            Assert.AreEqual(2, p.Level);
            Assert.AreEqual(Settings.PlayerAtk + 2, p.Atk);
            p.ReceiveDamage(10000);
            Assert.AreEqual(0, p.Hp);
            Assert.IsFalse(p.Alive);
            p.Restore();
            Assert.AreEqual(0, p.Heal(50));
        }

        [Test]
        public void Player_MaxLevelCap()
        {
            var p = new Player();
            p.GainXp(100000);
            Assert.AreEqual(Settings.MaxLevel, p.Level);
        }
    }
}
