import os
import random

import pytest

from src import settings as S
from src.combat.battle_manager import BattleManager
from src.combat.combo import ComboSystem
from src.entities.enemy import PalhacoBug, CompiladorSombrio
from src.entities.player import Player
from src.questions.question_bank import QuestionBank


@pytest.fixture
def bank():
    return QuestionBank(os.path.join(S.DATA_DIR, "questions.json"), random.Random(1))


def make(bank):
    p, e = Player(), PalhacoBug()
    return p, e, BattleManager(p, e, bank)


def test_correct_answer_damages_enemy_only(bank):
    p, e, bm = make(bank)
    res = bm.answer(bm.next_question().answer)
    assert res.correct and res.damage_dealt > 0
    assert e.hp < e.max_hp and p.hp == p.max_hp


def test_wrong_answer_uses_random_choice_for_enemy_attack(bank, monkeypatch):
    p, e, bm = make(bank)
    bm.next_question()
    calls = []

    def fake_choice(seq):
        calls.append(list(seq))
        return seq[-1]

    monkeypatch.setattr("src.entities.enemy.random.choice", fake_choice)
    res = bm.answer(None)  # tempo esgotado conta como erro
    assert calls == [e.attacks]
    assert res.enemy_attack == e.attacks[-1]
    assert p.hp < p.max_hp and e.hp == e.max_hp


def test_round_has_five_questions(bank):
    p, e, bm = make(bank)
    finished = []
    for _ in range(5):
        bm.next_question()
        finished.append(bm.answer(None).round_finished)
    assert finished == [False] * 4 + [True]
    assert bm.round == 2 and bm.asked_in_round == 0


def test_skill_costs_focus_and_adds_damage(bank):
    p, e, bm = make(bank)
    assert bm.toggle_skill() is True
    res = bm.answer(bm.next_question().answer)
    assert res.skill_used
    assert p.fc == S.PLAYER_FC - S.SKILL_COST + S.FC_PER_HIT
    assert res.damage_dealt == bm.calc_damage(1.0 * S.SKILL_MULT)


def test_skill_not_armed_without_focus(bank):
    p, e, bm = make(bank)
    p.spend_fc(p.fc)
    assert bm.toggle_skill() is False


def test_boss_phases_and_timer():
    boss = CompiladorSombrio()
    assert boss.phase == 1 and boss.question_time == S.QUESTION_TIME
    boss.receive_damage(int(boss.max_hp * 0.75))
    assert boss.phase == 3 and boss.question_time == S.BOSS_FINAL_PHASE_TIME
    assert len(boss.attacks) > 3


def test_combo_multiplier():
    c = ComboSystem()
    assert c.multiplier() == 1.0
    for _ in range(3):
        c.register(True)
    assert c.multiplier() == 1.5
    c.register(False)
    assert c.streak == 0 and c.best == 3
