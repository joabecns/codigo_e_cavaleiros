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
