"""Simula partidas completas com um 'bot' e confere a duração estimada (meta: 5-7 min)."""
import os
import random

from src import settings as S
from src.combat.battle_manager import BattleManager
from src.entities.enemy import create_enemy
from src.entities.player import Player
from src.questions.question_bank import QuestionBank

THINK, RIGHT, WRONG, INTRO, END = 8.0, 1.7, 3.4, 4.0, 2.0  # segundos por evento


def play(accuracy, rng):
    bank = QuestionBank(os.path.join(S.DATA_DIR, "questions.json"), rng)
    player, asked, seconds = Player(), 0, 0.0
    for stage in range(S.TOTAL_STAGES):
        while True:  # repete a fase em caso de derrota
            enemy = create_enemy(stage)
            bm = BattleManager(player, enemy, bank)
            seconds += INTRO
            while enemy.alive and player.alive:
                q = bm.next_question()
                ok = rng.random() < accuracy
                bm.answer(q.answer if ok else (q.answer + 1) % 4)
                seconds += THINK + (RIGHT if ok else WRONG)
            asked += bm.total_asked
            seconds += END
            if player.alive:
                break
            player.full_restore()
        player.gain_xp(enemy.xp_reward)
        player.recover_after_fight()
    return asked, seconds / 60


def test_full_game_duration_is_reasonable():
    rng = random.Random(7)
    runs = [play(0.75, rng) for _ in range(60)]
    avg_q = sum(r[0] for r in runs) / len(runs)
    avg_min = sum(r[1] for r in runs) / len(runs)
    print(f"média: {avg_q:.1f} questões, {avg_min:.1f} min")
    assert 20 <= avg_q <= 45
    assert 4.5 <= avg_min <= 7.5
