"""Widgets simples de interface."""
import pygame

from src.ui.drawing import draw_text


class Button:
    def __init__(self, rect, text, callback, size=44):
        self.rect = pygame.Rect(rect)
        self.text = text
        self.callback = callback
        self.size = size

    def draw(self, surf, selected=False):
        fill = (70, 100, 225) if selected else (35, 42, 92)
        edge = (210, 225, 255) if selected else (110, 125, 190)
        pygame.draw.rect(surf, fill, self.rect, border_radius=12)
        pygame.draw.rect(surf, edge, self.rect, 3, border_radius=12)
        draw_text(surf, self.text, self.size, (255, 255, 255), self.rect.center, "center", shadow=True)

    def contains(self, pos):
        return self.rect.collidepoint(pos)


class Slider:
    """Controle deslizante 0-100% com a mesma interface do Button (draw/contains/callback).

    Setas esquerda/direita ajustam, clicar/arrastar na barra define o valor e ENTER alterna mudo.
    """

    def __init__(self, rect, label, get_value, set_value, step=0.1, size=38):
        self.rect = pygame.Rect(rect)
        self.label = label
        self.get_value = get_value
        self.set_value = set_value
        self.step = step
        self.size = size
        self.bar = pygame.Rect(self.rect.x + 200, self.rect.centery - 9, self.rect.w - 330, 18)
        self._last = get_value() or 1.0

    def callback(self):  # ENTER / clique fora da barra: mudo <-> volume anterior
        value = self.get_value()
        if value > 0:
            self._last = value
            self.set_value(0.0)
        else:
            self.set_value(self._last or 1.0)

    def nudge(self, direction):
        self.set_value(round(max(0.0, min(1.0, self.get_value() + direction * self.step)), 2))

    def hit_bar(self, pos):
        return self.bar.inflate(30, 34).collidepoint(pos)

    def set_from_x(self, x):
        ratio = max(0.0, min(1.0, (x - self.bar.x) / self.bar.w))
        self.set_value(round(ratio * 20) / 20)  # passos de 5%

    def contains(self, pos):
        return self.rect.collidepoint(pos)

    def draw(self, surf, selected=False):
        value = self.get_value()
        fill = (70, 100, 225) if selected else (35, 42, 92)
        edge = (210, 225, 255) if selected else (110, 125, 190)
        pygame.draw.rect(surf, fill, self.rect, border_radius=12)
        pygame.draw.rect(surf, edge, self.rect, 3, border_radius=12)
        draw_text(surf, self.label, self.size, (255, 255, 255), (self.rect.x + 24, self.rect.centery), "midleft",
                  shadow=True)
        pygame.draw.rect(surf, (18, 20, 50), self.bar, border_radius=9)
        if value > 0:
            pygame.draw.rect(surf, (250, 200, 70), (self.bar.x, self.bar.y, max(18, int(self.bar.w * value)),
                                                    self.bar.h), border_radius=9)
        pygame.draw.rect(surf, edge, self.bar, 2, border_radius=9)
        knob = (self.bar.x + int(self.bar.w * value), self.bar.centery)
        pygame.draw.circle(surf, (255, 255, 255), knob, 14 if selected else 12)
        pygame.draw.circle(surf, (40, 50, 110), knob, 14 if selected else 12, 3)
        text = f"{int(round(value * 100))}%" if value > 0 else "MUDO"
        draw_text(surf, text, self.size - 6, (255, 230, 130) if value > 0 else (255, 140, 140),
                  (self.rect.right - 24, self.rect.centery), "midright", bold=True, shadow=True)
