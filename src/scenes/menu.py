"""Tela inicial e tela de configurações."""
import random

import pygame

from src import settings as S
from src.scenes.scene import Scene
from src.ui.drawing import draw_panel, draw_text, gradient
from src.ui.widgets import Button, Slider


class _ButtonScene(Scene):
    """Comportamento comum: navegar em botões com teclado ou mouse."""
    buttons = []

    def __init__(self, game):
        super().__init__(game)
        self.selected = 0

    def _move(self, step):
        self.selected = (self.selected + step) % len(self.buttons)
        self.game.audio.play("click")

    def handle_events(self, events):
        for e in events:
            if e.type == pygame.KEYDOWN:
                if e.key in (pygame.K_UP, pygame.K_w):
                    self._move(-1)
                elif e.key in (pygame.K_DOWN, pygame.K_s):
                    self._move(1)
                elif e.key in (pygame.K_RETURN, pygame.K_KP_ENTER, pygame.K_SPACE):
                    self.game.audio.play("click")
                    self.buttons[self.selected].callback()
                    return
                elif e.key == pygame.K_ESCAPE:
                    self.on_escape()
            elif e.type == pygame.MOUSEMOTION:
                for i, b in enumerate(self.buttons):
                    if b.contains(e.pos):
                        self.selected = i
            elif e.type == pygame.MOUSEBUTTONDOWN and e.button == 1:
                for i, b in enumerate(self.buttons):
                    if b.contains(e.pos):
                        self.selected = i
                        self.game.audio.play("click")
                        b.callback()
                        return

    def on_escape(self):
        pass


class MenuScene(_ButtonScene):
    def __init__(self, game):
        super().__init__(game)
        self.buttons = [
            Button((490, 330, 300, 64), "Jogar", game.go_select),
            Button((490, 412, 300, 64), "Configurações", game.go_settings),
            Button((490, 494, 300, 64), "Sair", game.quit),
        ]
        self.t = 0.0
        self.bg = gradient((S.WIDTH, S.HEIGHT), (12, 14, 40), (45, 22, 75))
        self.symbols = [[random.randint(0, S.WIDTH), random.uniform(0, S.HEIGHT), random.uniform(30, 90),
                         random.choice("01{}<>;()=")] for _ in range(70)]

    def update(self, dt):
        self.t += dt
        for sym in self.symbols:
            sym[1] += sym[2] * dt
            if sym[1] > S.HEIGHT:
                sym[0], sym[1] = random.randint(0, S.WIDTH), -20

    def draw(self, screen):
        screen.blit(self.bg, (0, 0))
        for x, y, _, ch in self.symbols:
            draw_text(screen, ch, 30, (60, 110, 90), (x, y))
        draw_text(screen, "Código & Cavaleiros", 108, S.GOLD, (S.WIDTH // 2, 120), "center", bold=True, shadow=True)
        draw_text(screen, "RPG educativo por turnos  |  Python + Pygame", 34, (200, 210, 255), (S.WIDTH // 2, 195), "center")
        for i, b in enumerate(self.buttons):
            b.draw(screen, i == self.selected)
        draw_text(screen, "Setas + ENTER ou mouse", 26, (170, 180, 220), (S.WIDTH // 2, 660), "center")


class SettingsScene(_ButtonScene):
    def __init__(self, game):
        super().__init__(game)
        self.bg = gradient((S.WIDTH, S.HEIGHT), (12, 14, 40), (45, 22, 75))
        a = game.audio
        self.music_slider = Slider((340, 140, 600, 70), "Música", lambda: a.music_volume, a.set_music_volume)
        self.sfx_slider = Slider((340, 222, 600, 70), "Efeitos", lambda: a.sfx_volume, self._set_sfx)
        self.buttons = [
            self.music_slider,
            self.sfx_slider,
            Button((440, 304, 400, 64), "Voltar", game.go_menu),
        ]
        self.dragging = None

    def _set_sfx(self, value):
        changed = abs(value - self.game.audio.sfx_volume) > 1e-3
        self.game.audio.set_sfx_volume(value)
        if changed:
            self.game.audio.play("click")  # prévia do novo volume dos efeitos

    def handle_events(self, events):
        rest = []
        for e in events:
            current = self.buttons[self.selected]
            if e.type == pygame.KEYDOWN and e.key in (pygame.K_LEFT, pygame.K_a, pygame.K_RIGHT, pygame.K_d):
                if isinstance(current, Slider):
                    current.nudge(-1 if e.key in (pygame.K_LEFT, pygame.K_a) else 1)
                continue
            if e.type == pygame.MOUSEBUTTONDOWN and e.button == 1:
                slider = next((s for s in (self.music_slider, self.sfx_slider) if s.hit_bar(e.pos)), None)
                if slider:
                    self.dragging = slider
                    self.selected = self.buttons.index(slider)
                    slider.set_from_x(e.pos[0])
                    continue
            elif e.type == pygame.MOUSEMOTION and self.dragging:
                self.dragging.set_from_x(e.pos[0])
                continue
            elif e.type == pygame.MOUSEBUTTONUP and e.button == 1:
                self.dragging = None
                continue
            rest.append(e)
        super().handle_events(rest)

    def on_escape(self):
        self.game.go_menu()

    def update(self, dt):
        pass

    def draw(self, screen):
        screen.blit(self.bg, (0, 0))
        draw_text(screen, "Configurações", 72, S.GOLD, (S.WIDTH // 2, 70), "center", bold=True, shadow=True)
        for i, b in enumerate(self.buttons):
            b.draw(screen, i == self.selected)
        draw_text(screen, "Setas esquerda/direita ou arraste: volume   |   ENTER: mudo   |   ESC: voltar", 22,
                  (190, 200, 240), (S.WIDTH // 2, 388), "center")
        draw_panel(screen, (240, 410, 800, 250))
        draw_text(screen, "Como jogar", 40, S.GOLD, (S.WIDTH // 2, 440), "center", bold=True)
        tips = ["Responda com as teclas 1-4 ou clicando na alternativa.",
                "Acertou: você ataca. Errou ou o tempo acabou: o inimigo contra-ataca.",
                "Acertos seguidos formam combo e aumentam o dano.",
                "Tecla S: ativa o Depurar (dano x1.5, gasta 8 de Foco).",
                "Vença 4 inimigos e o Compilador Sombrio. ESC volta ao menu."]
        for i, line in enumerate(tips):
            draw_text(screen, line, 27, (225, 230, 255), (S.WIDTH // 2, 485 + i * 32), "center")
