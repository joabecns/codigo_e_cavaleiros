using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using CodigoECavaleiros.Audio;
using CodigoECavaleiros.Characters;
using CodigoECavaleiros.Entities;
using CodigoECavaleiros.Questions;
using CodigoECavaleiros.UI;

namespace CodigoECavaleiros.Combat
{
    /// <summary>
    /// Liga a lógica (BattleManager) à cena: personagens 3D, animações e interface.
    /// Regra do jogo: ninguém ataca diretamente; quem apanha faz cara de dor,
    /// quem "ataca" faz cara de raiva. No fim, o vencedor comemora e o perdedor se lamenta.
    /// </summary>
    public class BattleController : MonoBehaviour
    {
        [Header("Prefabs (ordem: Leo, Theo, Bruno, Max, Caio)")]
        public GameObject[] heroPrefabs;
        [Header("Prefabs dos inimigos (uma por fase)")]
        public GameObject[] enemyPrefabs;
        [Header("Posições")]
        public Transform heroSpot;
        public Transform enemySpot;

        Player player;
        Enemy enemy;
        BattleManager bm;
        QuestionBank bank;
        CharacterActor heroActor, enemyActor;
        GameObject enemyObject;
        BattleUI ui;
        int stage;

        int? picked;
        int lastTick = 99;
        bool answerReceived, continuePressed;

        void Start()
        {
            bank = QuestionBank.FromResources();
            ui = gameObject.AddComponent<BattleUI>();
            ui.Build();
            ui.OptionChosen += i => { if (!answerReceived) { picked = i; answerReceived = true; AudioManager.Sfx("sfx_click"); } };
            ui.SkillToggled += () => { if (bm != null) { bm.ToggleSkill(); RefreshSkill(); if (bm.SkillArmed) AudioManager.Sfx("sfx_skill"); else AudioManager.Sfx("sfx_click"); } };
            ui.Continue += () => continuePressed = true;

            SpawnHero();
            StartCoroutine(RunGame());
        }

        // ------------------------------------------------------------------ spawn
        void SpawnHero()
        {
            int idx = Mathf.Clamp(GameSession.HeroIndex, 0, heroPrefabs.Length - 1);
            var go = Instantiate(heroPrefabs[idx], heroSpot.position, heroSpot.rotation);
            heroActor = go.GetComponent<CharacterActor>();
            player = new Player(heroActor.displayName);
        }

        void SpawnEnemy(int stageIndex)
        {
            if (enemyObject != null) Destroy(enemyObject);
            enemy = EnemyCatalog.Create(stageIndex);
            enemyObject = Instantiate(enemyPrefabs[stageIndex], enemySpot.position, enemySpot.rotation);
            enemyActor = enemyObject.GetComponent<CharacterActor>();
            bm = new BattleManager(player, enemy, bank);
        }

        // ------------------------------------------------------------------ HUD
        void RefreshHud(bool instant = false)
        {
            string info = "Fase " + (stage + 1) + "/" + Settings.TotalStages + "   Rodada " + bm.Round + "   " + enemy.TopicLabel;
            if (enemy is Boss) info += "   Fase do chefe " + enemy.Phase;
            ui.SetEnemy(enemy.Name, info, enemy.Hp, enemy.MaxHp, instant);
            ui.SetPlayer(player.Name, player.Level, player.Hp, player.MaxHp, player.Fc, player.MaxFc, bm.Combo.Label(), instant);
            RefreshSkill();
        }

        void RefreshSkill()
        {
            ui.SetSkill(bm.SkillArmed, player.Fc >= Settings.SkillCost, Settings.SkillCost);
        }

        // ------------------------------------------------------------------ fluxo do jogo
        IEnumerator RunGame()
        {
            while (stage < Settings.TotalStages)
            {
                SpawnEnemy(stage);
                AudioManager.Music("music_battle");
                heroActor.PlayIdle();
                enemyActor.PlayIdle();
                RefreshHud(true);

                ui.ShowBanner(enemy.Name + " apareceu!\n<size=60%>Tema: " + enemy.TopicLabel + "</size>");
                yield return new WaitForSeconds(2.2f);
                ui.HideBanner();

                bool playerWon = false;
                while (true)
                {
                    var q = bm.NextQuestion();
                    float limit = enemy.QuestionTime;
                    float t = limit;
                    answerReceived = false; picked = null; lastTick = 99;
                    RefreshHud();
                    ui.ShowQuestion(q, bm.AskedInRound + 1, Settings.QuestionsPerRound);

                    while (!answerReceived && t > 0f)
                    {
                        t -= Time.deltaTime;
                        ui.SetTimer(t / limit);
                        int sec = Mathf.CeilToInt(t);
                        if (t > 0f && t < 3.2f && sec != lastTick) { lastTick = sec; AudioManager.Sfx("sfx_tick"); }
                        yield return null;
                    }
                    ui.HideQuestion();

                    var res = bm.Answer(answerReceived ? picked : null);
                    yield return ResolveTurn(res);

                    if (res.EnemyDefeated) { playerWon = true; break; }
                    if (res.PlayerDefeated) break;
                    if (res.RoundFinished)
                    {
                        ui.ShowBanner("Rodada " + (bm.Round - 1) + " concluída!");
                        yield return new WaitForSeconds(1.2f);
                        ui.HideBanner();
                    }
                }

                if (playerWon)
                {
                    enemyActor.PlayLament();
                    heroActor.PlayCheer();
                    AudioManager.StopMusic();
                    AudioManager.Sfx("sfx_victory");
                    int levels = player.GainXp(enemy.XpReward);
                    string extra = levels > 0 ? "\nSubiu para o nível " + player.Level + "!" : "";
                    yield return new WaitForSeconds(2.5f);
                    if (stage == Settings.TotalStages - 1)
                    {
                        ui.ShowEnd("VITÓRIA!", SummaryText(), "Voltar ao menu");
                        yield return WaitContinue();
                        ui.HideEnd();
                        SceneManager.LoadScene(Settings.MenuScene);
                        yield break;
                    }
                    ui.ShowEnd("Vitória!", enemy.Name + " foi derrotado." + extra, "Próxima luta");
                    yield return WaitContinue();
                    ui.HideEnd();
                    player.RecoverAfterFight();
                    stage++;
                }
                else
                {
                    heroActor.PlayLament();
                    enemyActor.PlayCheer();
                    AudioManager.StopMusic();
                    AudioManager.Sfx("sfx_defeat");
                    yield return new WaitForSeconds(2.5f);
                    ui.ShowEnd("Derrota", SummaryText(), "Tentar de novo");
                    yield return WaitContinue();
                    ui.HideEnd();
                    Restart();
                    yield break;
                }
            }
        }

        IEnumerator ResolveTurn(TurnResult r)
        {
            string text;
            if (r.Correct)
            {
                heroActor.SetEmotion(Emotion.Feliz);
                enemyActor.PlayPain();
                AudioManager.Sfx("sfx_correct");
                AudioManager.Sfx("sfx_hit", 0.8f);
                if (r.Streak >= 2) AudioManager.Sfx("sfx_combo");
                text = "<color=#7CFF9A>Acertou!</color>  Dano: " + r.DamageDealt;
                if (r.SkillUsed) text += "  (Depurar!)";
                if (r.Streak >= 2) text += "\n<size=65%>" + bm.Combo.Label() + "</size>";
            }
            else
            {
                enemyActor.PlayRage();
                heroActor.PlayPain();
                AudioManager.Sfx("sfx_wrong");
                AudioManager.Sfx("sfx_rage", 0.9f);
                int total = 0;
                foreach (var h in r.Hits) total += h;
                string head = r.TimedOut ? "Tempo esgotado!" : "Errou!";
                text = "<color=#FF7A7A>" + head + "</color>  " + enemy.Name + " usou " + r.EnemyAttack.Value.Name + "\n<size=65%>Você perdeu " + total + " de vida</size>";
            }
            RefreshHud();
            ui.ShowBanner(text);
            yield return new WaitForSeconds(1.6f);

            if (r.PhaseChanged && !r.EnemyDefeated)
            {
                ui.ShowBanner("<color=#FF9A4A>" + enemy.Name + " ficou furioso!</color>\n<size=65%>Fase " + enemy.Phase + " — menos tempo e ataques mais fortes</size>");
                yield return new WaitForSeconds(1.6f);
            }

            if (!string.IsNullOrEmpty(r.Question.explanation))
            {
                ui.ShowBanner("<size=60%>" + r.Question.explanation + "</size>");
                yield return new WaitForSeconds(2.4f);
            }
            ui.HideBanner();
            if (!r.EnemyDefeated && !r.PlayerDefeated)
            {
                heroActor.PlayIdle();
                enemyActor.PlayIdle();
            }
        }

        string SummaryText()
        {
            string s = "Acertos: " + bm.CorrectTotal + " de " + bm.TotalAsked + "  •  Melhor combo: x" + bm.Combo.Best + "\n";
            foreach (var kv in bm.Stats)
                s += Settings.TopicName(kv.Key) + ": " + kv.Value[0] + "/" + kv.Value[1] + "   ";
            return s;
        }

        IEnumerator WaitContinue()
        {
            continuePressed = false;
            while (!continuePressed) yield return null;
        }

        void Restart()
        {
            SceneManager.LoadScene(Settings.BattleScene);
        }
    }
}
