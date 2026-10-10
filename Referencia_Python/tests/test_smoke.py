"""Roda o jogo de verdade (sem janela) com um bot que joga do menu até a tela final."""
import random

import pygame

from src.core.game import Game


def _post(key):
    pygame.event.post(pygame.event.Event(pygame.KEYDOWN, key=key, mod=0, unicode=""))


def test_bot_finishes_the_game():
    rng = random.Random(5)
    game = Game()
    keys = [pygame.K_1, pygame.K_2, pygame.K_3, pygame.K_4]
    for _ in range(60000):
        sc = game.scene
        name = type(sc).__name__
        if name == "EndScene":
            break
        if name in ("MenuScene", "SelectScene"):
            _post(pygame.K_RETURN)
        elif name == "BattleScene":
            if sc.state == "question":
                right = sc.bm.current.answer
                _post(keys[right if rng.random() < 0.8 else (right + 1) % 4])
            elif sc.state in ("victory", "defeat", "banner") and sc.state_time > 1.0:
                _post(pygame.K_RETURN)
        game.frame(0.05)
    assert type(game.scene).__name__ == "EndScene"
    assert game.session.total_asked >= 15
    pygame.quit()
