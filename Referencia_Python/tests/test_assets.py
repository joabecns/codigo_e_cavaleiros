"""Confere que os personagens 3D pré-renderizados existem e trazem as animações do jogo."""
import pygame
import pytest

from src import settings as S
from src.entities.enemy import ENEMY_ORDER
from src.ui.sprites import SpriteActor, SpriteBank

HERO_CLIPS = {"idle", "pain", "cheer", "lament"}
ENEMY_CLIPS = HERO_CLIPS | {"rage"}


@pytest.fixture(scope="module")
def bank():
    pygame.display.init()
    pygame.display.set_mode((16, 16))
    return SpriteBank()


def test_all_heroes_have_clips_and_portraits(bank):
    for key in S.HEROES:
        info = bank.info(key)
        assert HERO_CLIPS <= set(info["clips"])
        assert {"front", "front_cheer"} <= set(info["front"])
        assert len(bank.frames(key, "lament")) > 14   # ajoelha (0-13) e depois soluça em loop


def test_all_villains_have_clips(bank):
    for cls in ENEMY_ORDER:
        info = bank.info(cls().key)
        assert ENEMY_CLIPS <= set(info["clips"])


def test_one_shot_clips_return_to_idle_and_loops_keep_playing(bank):
    actor = SpriteActor(bank, S.HEROES[0])
    actor.play("pain")
    for _ in range(200):
        actor.update(0.05)
        actor.current()
    assert actor.clip == "idle"
    actor.play("lament", then="lament")
    for _ in range(400):
        actor.update(0.05)
        actor.current()
    assert actor.clip == "lament"
