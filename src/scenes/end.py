"""Tela final com o relatório de desempenho."""
import pygame

from src import settings as S
from src.scenes.menu import _ButtonScene
from src.ui.drawing import draw_bar, draw_panel, draw_text, gradient
from src.ui.widgets import Button


class EndScene(_ButtonScene):
    def __init__(self, game, session):
        super().__init__(game)
        session.clock_running = False
        self.session = session
        self.bg = gradient((S.WIDTH, S.HEIGHT), (20, 10, 50), (90, 50, 110))
        self.buttons = [Button((330, 610, 300, 60), "Menu principal", game.go_menu),
                        Button((650, 610, 300, 60), "Sair", game.quit)]
        self.t = 0.0

    def update(self, dt):
        self.t += dt

    def _move(self, step):
        super()._move(step)

    def handle_events(self, events):
        for e in events:  # aqui as setas esquerda/direita também navegam
            if e.type == pygame.KEYDOWN and e.key in (pygame.K_LEFT, pygame.K_RIGHT):
                self._move(1)
        super().handle_events(events)

    def draw(self, screen):
        s = self.session
        screen.blit(self.bg, (0, 0))
        draw_text(screen, "VITÓRIA!", 100, S.GOLD, (S.WIDTH // 2, 80), "center", bold=True, shadow=True)
        draw_text(screen, "O Compilador Sombrio foi derrotado. O código compila!", 36, S.WHITE,
                  (S.WIDTH // 2, 150), "center")
        draw_panel(screen, (200, 195, 880, 395))
        mins, secs = divmod(int(s.elapsed), 60)
        grade = round(s.accuracy * 10, 1)
        draw_text(screen, f"Nota: {grade}/10", 56, S.GREEN if grade >= 6 else S.GOLD, (S.WIDTH // 2, 235), "center", bold=True)
        draw_text(screen, f"Tempo: {mins:02d}:{secs:02d}    Acertos: {s.total_correct}/{s.total_asked} "
                          f"({int(s.accuracy * 100)}%)    Melhor combo: x{s.best_combo}    Nível: {s.player.level}",
                  27, S.WHITE, (S.WIDTH // 2, 282), "center")
        draw_text(screen, "Desempenho por tópico", 32, S.GOLD, (S.WIDTH // 2, 325), "center", bold=True)
        for i, (key, name) in enumerate(S.TOPIC_NAMES.items()):
            ok, total = s.stats.get(key, [0, 0])
            y = 365 + i * 42
            draw_text(screen, name, 27, S.WHITE, (240, y + 4))
            ratio = ok / total if total else 0
            draw_bar(screen, (520, y, 380, 26), ratio, S.GREEN if ratio >= 0.6 else (230, 150, 70))
            draw_text(screen, f"{ok}/{total}", 26, S.WHITE, (920, y + 4))
        for i, b in enumerate(self.buttons):
            b.draw(screen, i == self.selected)
