"""Tela de escolha do herói (vale até o fim da jornada). Os vilões não são escolhidos pelo jogador."""
import pygame

from src import settings as S
from src.scenes.scene import Scene
from src.ui.drawing import draw_panel, draw_text, gradient
from src.ui.sprites import ANIM_FPS

THUMB_H = 118


class SelectScene(Scene):
    def __init__(self, game):
        super().__init__(game)
        self.keys = list(S.HEROES)
        self.index = 0
        self.t = 0.0
        self.confirm_left = 0.0
        self.bg = gradient((S.WIDTH, S.HEIGHT), (24, 20, 70), (74, 60, 140))
        self.thumb_rects = [pygame.Rect(0, 0, 150, 150) for _ in self.keys]
        total = len(self.keys) * 170 - 20
        for i, r in enumerate(self.thumb_rects):
            r.topleft = ((S.WIDTH - total) // 2 + i * 170, 516)
        self.thumbs = {}
        self.big = {}

    # ---- utilidades ----
    def _front(self, key, clip):
        return self.game.sprites.frames(key, clip, group="front")

    def _thumb(self, key):
        if key not in self.thumbs:
            img, _, _ = self._front(key, "front")[0]
            k = THUMB_H / img.get_height()
            self.thumbs[key] = pygame.transform.smoothscale(img, (int(img.get_width() * k), THUMB_H))
        return self.thumbs[key]

    def _big_frame(self, key, clip, i):
        cache_key = (key, clip, i)
        if cache_key not in self.big:
            frames = self._front(key, clip)
            img, x, y = frames[i]
            img0, x0, y0 = frames[0]
            k = 290 / max(1, img0.get_height())
            scaled = pygame.transform.smoothscale(img, (int(img.get_width() * k), int(img.get_height() * k)))
            self.big[cache_key] = (scaled, int((x - x0) * k), int((y - y0) * k),
                                   int(img0.get_width() * k), int(img0.get_height() * k))
        return self.big[cache_key]

    def _move(self, step):
        if self.confirm_left > 0:
            return
        self.index = (self.index + step) % len(self.keys)
        self.t = 0.0
        self.game.audio.play("select")

    def _confirm(self):
        if self.confirm_left <= 0:
            self.confirm_left = 1.8
            self.t = 0.0
            self.game.audio.play("win")

    # ---- ciclo ----
    def handle_events(self, events):
        for e in events:
            if e.type == pygame.KEYDOWN:
                if e.key in (pygame.K_LEFT, pygame.K_a):
                    self._move(-1)
                elif e.key in (pygame.K_RIGHT, pygame.K_d):
                    self._move(1)
                elif e.key in (pygame.K_RETURN, pygame.K_KP_ENTER, pygame.K_SPACE):
                    self._confirm()
                elif e.key == pygame.K_ESCAPE:
                    self.game.go_menu()
                    return
            elif e.type == pygame.MOUSEBUTTONDOWN and e.button == 1:
                for i, r in enumerate(self.thumb_rects):
                    if r.collidepoint(e.pos):
                        if i == self.index:
                            self._confirm()
                        else:
                            self._move(i - self.index)
                if pygame.Rect(540, 170, 200, 300).collidepoint(e.pos):
                    self._confirm()

    def update(self, dt):
        self.t += dt
        if self.confirm_left > 0:
            self.confirm_left -= dt
            if self.confirm_left <= 0:
                self.game.start_new_game(self.keys[self.index])

    def draw(self, screen):
        screen.blit(self.bg, (0, 0))
        key = self.keys[self.index]
        name = self.game.sprites.info(key)["name"]
        draw_text(screen, "Escolha seu herói", 76, S.GOLD, (S.WIDTH // 2, 54), "center", bold=True, shadow=True)
        draw_text(screen, "Ele acompanha você até o final da jornada", 30, (215, 220, 255), (S.WIDTH // 2, 104), "center")
        # palco
        pygame.draw.ellipse(screen, (16, 12, 44), (470, 444, 340, 44))
        pygame.draw.ellipse(screen, (120, 110, 210), (470, 438, 340, 44), 4)
        clip = "front_cheer" if self.confirm_left > 0 else "front"
        frames = self._front(key, clip)
        i = int(self.t * ANIM_FPS) % len(frames)
        scaled, dx, dy, w0, h0 = self._big_frame(key, clip, i)
        screen.blit(scaled, (S.WIDTH // 2 - w0 // 2 + dx, 464 - h0 + dy))
        draw_text(screen, name, 54, S.WHITE, (S.WIDTH // 2, 150), "center", bold=True, shadow=True)
        # miniaturas
        for i, (k, r) in enumerate(zip(self.keys, self.thumb_rects)):
            sel = i == self.index
            draw_panel(screen, r, fill=(70, 80, 190, 235) if sel else (20, 20, 60, 200),
                       border=(255, 230, 130) if sel else (120, 130, 200))
            th = self._thumb(k)
            screen.blit(th, (r.centerx - th.get_width() // 2, r.y + 8))
            draw_text(screen, self.game.sprites.info(k)["name"], 26, S.WHITE, (r.centerx, r.bottom - 12), "center", bold=True)
        if self.confirm_left > 0:
            draw_text(screen, f"{name} aceitou o desafio!", 40, S.GREEN, (S.WIDTH // 2, 694), "center", bold=True, shadow=True)
        else:
            draw_text(screen, "Setas: trocar   |   ENTER: escolher   |   ESC: voltar", 26, (200, 205, 240), (S.WIDTH // 2, 696), "center")
