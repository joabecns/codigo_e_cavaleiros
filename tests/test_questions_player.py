import os
import random

import pytest

from src import settings as S
from src.entities.player import Player
from src.questions.question_bank import QuestionBank


@pytest.fixture
def bank():
    return QuestionBank(os.path.join(S.DATA_DIR, "questions.json"), random.Random(3))


def test_bank_is_valid_and_covers_all_topics(bank):
    assert len(bank.questions) >= 40
    assert {q.topic for q in bank.questions} == set(S.TOPIC_NAMES)
    assert all(len(q.options) == 4 and 0 <= q.answer < 4 for q in bank.questions)


def test_draw_does_not_repeat_until_exhausted(bank):
    ids = [bank.draw(["poo"]).id for _ in range(8)]
    assert len(set(ids)) == 8
    assert bank.draw(["poo"]).id  # depois de esgotar, recomeça sem erro


def test_shuffle_keeps_correct_answer(bank):
    original = {q.id: q for q in bank.questions}
    for _ in range(50):
        q = bank.draw(["variaveis", "ia"])
        o = original[q.id]
        assert q.options[q.answer] == o.options[o.answer]


def test_player_level_up_and_hp_bounds():
    p = Player()
    assert p.gain_xp(45) == 1 and p.level == 2 and p.atk == S.PLAYER_ATK + 2
    p.receive_damage(10_000)
    assert p.hp == 0 and not p.alive
    p.restore()
    assert p.heal(50) == 0


def test_max_level_cap():
    p = Player()
    p.gain_xp(100_000)
    assert p.level == S.MAX_LEVEL
