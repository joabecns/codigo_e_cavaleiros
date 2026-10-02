"""Sprites dos personagens 3D (pré-renderizados) e o 'ator' que toca as animações."""
import json
import os

import pygame

from src import settings as S

ANIM_FPS = 15
# frame onde o loop recomeça; clipes fora da tabela (pain, rage) tocam uma vez e voltam ao 'then'
LOOP_FROM = {"idle": 0, "cheer": 0, "lament": 14}


class SpriteBank:
    """Carrega (sob demanda) os quadros de cada personagem a partir do manifest.json."""

    def __init__(self, base=None):
        self.dir = base or os.path.join(S.ASSETS_DIR, "characters")
        with open(os.path.join(self.dir, "manifest.json"), encoding="utf-8") as f:
            self.manifest = json.load(f)
        self._cache = {}

    def info(self, key):
        return self.manifest[key]

    def frames(self, key, clip, group="clips"):
        """Lista de (Surface, x, y); x,y = posição do recorte no quadro 1280x720."""
        cache_key = (key, group, clip)
        if cache_key not in self._cache:
            out = []
            for e in self.manifest[key][group][clip]:
                if e:
                    img = pygame.image.load(os.path.join(self.dir, key, e["file"])).convert_alpha()
                    out.append((img, e["x"], e["y"]))
            self._cache[cache_key] = out
        return self._cache[cache_key]


class SpriteActor:
    def __init__(self, bank, key):
        self.bank, self.key = bank, key
        self.clip, self.then, self.t = "idle", "idle", 0.0
        self.flash = 0.0

    def play(self, clip, then="idle"):
        self.clip, self.then, self.t = clip, then, 0.0

    def update(self, dt):
        self.t += dt
        self.flash = max(0.0, self.flash - dt)

    def current(self):
        frames = self.bank.frames(self.key, self.clip)
        i = int(self.t * ANIM_FPS)
        if i >= len(frames):
            loop = LOOP_FROM.get(self.clip)
            if loop is None:
                self.play(self.then)
                return self.current()
            i = loop + (i - loop) % (len(frames) - loop)
        return frames[i]

    def head(self):
        img, x, y = self.current()
        return x + img.get_width() // 2, y + 30

    def draw(self, screen, ox=0, oy=0):
        img, x, y = self.current()
        if self.flash > 0:
            img = img.copy()
            img.fill((95, 95, 95, 0), special_flags=pygame.BLEND_RGB_ADD)
        screen.blit(img, (x + ox, y + oy))
