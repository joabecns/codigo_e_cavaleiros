"""Funções utilitárias de desenho (texto, barras, painéis, gradientes)."""
import pygame

_FONTS = {}


def get_font(size, bold=False):
    if not pygame.font.get_init():
        pygame.font.init()
    key = (size, bold)
    if key not in _FONTS:
        font = pygame.font.Font(None, size)
        font.set_bold(bold)
        _FONTS[key] = font
    return _FONTS[key]


def draw_text(surf, text, size, color, pos, anchor="topleft", bold=False, shadow=False):
    font = get_font(size, bold)
    img = font.render(str(text), True, color)
    rect = img.get_rect()
    setattr(rect, anchor, (int(pos[0]), int(pos[1])))
    if shadow:
        surf.blit(font.render(str(text), True, (0, 0, 0)), rect.move(2, 2))
    surf.blit(img, rect)
    return rect


def wrap_text(text, font, max_width):
    lines, cur = [], ""
    for word in text.split():
        test = (cur + " " + word).strip()
        if font.size(test)[0] <= max_width:
            cur = test
        else:
            if cur:
                lines.append(cur)
            cur = word
    if cur:
        lines.append(cur)
    return lines


def gradient(size, top, bottom):
    w, h = size
    surf = pygame.Surface(size)
    for y in range(h):
        k = y / max(1, h - 1)
        color = tuple(int(top[i] + (bottom[i] - top[i]) * k) for i in range(3))
        pygame.draw.line(surf, color, (0, y), (w, y))
    return surf


def draw_bar(surf, rect, ratio, fill, back=(30, 30, 45), border=(235, 235, 245)):
    rect = pygame.Rect(rect)
    ratio = max(0.0, min(1.0, ratio))
    pygame.draw.rect(surf, back, rect, border_radius=6)
    if ratio > 0:
        inner = pygame.Rect(rect.x, rect.y, max(4, int(rect.w * ratio)), rect.h)
        pygame.draw.rect(surf, fill, inner, border_radius=6)
    pygame.draw.rect(surf, border, rect, 2, border_radius=6)


def draw_panel(surf, rect, fill=(15, 18, 38, 220), border=(120, 140, 220), radius=14):
    rect = pygame.Rect(rect)
    panel = pygame.Surface(rect.size, pygame.SRCALPHA)
    pygame.draw.rect(panel, fill, panel.get_rect(), border_radius=radius)
    pygame.draw.rect(panel, border, panel.get_rect(), 2, border_radius=radius)
    surf.blit(panel, rect.topleft)


def draw_eyes(surf, cx, cy, gap, r, pupil=(20, 20, 30), dx=0):
    for sgn in (-1, 1):
        x = int(cx + sgn * gap)
        pygame.draw.circle(surf, (255, 255, 255), (x, int(cy)), int(r))
        pygame.draw.circle(surf, pupil, (x + int(dx), int(cy)), max(2, int(r * 0.55)))
