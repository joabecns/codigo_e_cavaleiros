"""Arenas de batalha com perspectiva (mesma câmera diagonal usada para renderizar os personagens)."""
import random

import pygame

from src import settings as S
from src.ui.drawing import gradient

THEMES = [
    dict(name="Planície da Sintaxe", top=(110, 185, 255), bottom=(205, 238, 255), ground=(96, 176, 92), deco="hills"),
    dict(name="Floresta dos Laços", top=(30, 70, 55), bottom=(95, 155, 105), ground=(52, 110, 64), deco="trees"),
    dict(name="Caverna das Funções", top=(28, 28, 44), bottom=(80, 80, 108), ground=(84, 84, 102), deco="cave"),
    dict(name="Castelo das Classes", top=(50, 32, 88), bottom=(135, 92, 155), ground=(96, 82, 122), deco="castle"),
    dict(name="Torre do Compilador", top=(20, 5, 12), bottom=(125, 24, 36), ground=(74, 30, 38), deco="tower"),
]


def _shade(c, k):
    return tuple(max(0, min(255, int(v * k))) for v in c)


def _platform(surf, foot, width, color):
    h = int(width * 0.24)
    rim = pygame.Rect(0, 0, width, h)
    rim.center = (int(foot[0]), int(foot[1]) + 6)
    pygame.draw.ellipse(surf, _shade(color, 0.55), rim.move(0, 12))
    pygame.draw.ellipse(surf, _shade(color, 0.75), rim.move(0, 6))
    pygame.draw.ellipse(surf, _shade(color, 1.15), rim)
    pygame.draw.ellipse(surf, _shade(color, 1.4), rim.inflate(-width * 0.18, -h * 0.3))
    pygame.draw.ellipse(surf, (255, 255, 255), rim, 3)


def make_background(stage, hero_feet=(449, 420), foe_feet=(786, 309)):
    th = THEMES[stage]
    W, Hh, H = S.WIDTH, S.HEIGHT, S.HORIZON_Y
    surf = pygame.Surface((W, Hh))
    surf.blit(gradient((W, H + 1), th["top"], th["bottom"]), (0, 0))
    rnd = random.Random(stage * 13 + 5)
    deco = th["deco"]
    if deco == "hills":
        for cx in range(-100, W + 200, 240):
            pygame.draw.ellipse(surf, (110, 190, 112), (cx, H - 70, 320, 150))
            pygame.draw.ellipse(surf, (135, 210, 130), (cx + 120, H - 45, 260, 120))
    elif deco == "trees":
        for x in range(-20, W, 60):
            h = rnd.randint(70, 130)
            pygame.draw.rect(surf, (70, 45, 30), (x + 24, H - h * 0.3, 14, h * 0.35))
            pygame.draw.polygon(surf, (28, 92, 56), [(x, H - h * 0.25), (x + 62, H - h * 0.25), (x + 31, H - h)])
    elif deco == "cave":
        for x in range(0, W, 60):
            h = rnd.randint(30, 100)
            pygame.draw.polygon(surf, (46, 46, 64), [(x, 0), (x + 52, 0), (x + 26, h)])
            g = rnd.randint(15, 55)
            pygame.draw.polygon(surf, (60, 60, 80), [(x + 8, H + 2), (x + 54, H + 2), (x + 30, H - g)])
    elif deco == "castle":
        for x in range(20, W, 150):
            h = rnd.randint(90, 150)
            pygame.draw.rect(surf, (66, 46, 96), (x, H - h, 86, h + 4))
            for c in range(4):
                pygame.draw.rect(surf, (66, 46, 96), (x + c * 24, H - h - 12, 14, 12))
            pygame.draw.rect(surf, (255, 215, 110), (x + 34, H - h + 24, 18, 26))
    else:  # tower
        pygame.draw.circle(surf, (200, 34, 44), (640, 70), 60)
        for x in range(10, W, 130):
            h = rnd.randint(100, 170)
            pygame.draw.rect(surf, (28, 9, 16), (x, H - h, 74, h + 4))
            for y in range(H - h + 20, H - 20, 42):
                pygame.draw.rect(surf, (255, 74, 62), (x + 28, y, 16, 20))
    # chão: degradê + linhas de perspectiva convergindo para o ponto de fuga
    far, near = _shade(th["ground"], 1.15), _shade(th["ground"], 0.72)
    surf.blit(gradient((W, Hh - H), far, near), (0, H))
    lines = pygame.Surface((W, Hh), pygame.SRCALPHA)
    line_col = (255, 255, 255, 34)
    for xb in range(-2400, 3700, 240):
        pygame.draw.line(lines, line_col, (W // 2, H), (xb, Hh), 2)
    for k in range(1, 16):
        y = H + (Hh - H) * (k / 16) ** 2
        pygame.draw.line(lines, line_col, (0, y), (W, y), 2)
    surf.blit(lines, (0, 0))
    base = 256.0   # (y_pés_herói - horizonte): referência de escala
    for foot, w0 in ((foe_feet, 330), (hero_feet, 330)):
        _platform(surf, foot, int(w0 * (foot[1] - H) / base), _shade(th["ground"], 1.05))
    return surf
