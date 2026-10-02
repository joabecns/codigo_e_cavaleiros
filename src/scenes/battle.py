"""Batalha estilo Pokémon com personagens 3D low-poly.

Câmera diagonal na altura do ombro do herói. Não há golpes diretos: o dano é só efeito
(flash, faísca, número). Quem erra/acerta faz cara de dor ou raiva; o derrotado se ajoelha
lamentando e o vencedor pula de alegria.
"""
import math
import random

import pygame

from src import settings as S
from src.combat.battle_manager import BattleManager
from src.entities.enemy import create_enemy
from src.scenes.scene import Scene
from src.ui.backgrounds import THEMES, make_background
from src.ui.drawing import draw_bar, draw_panel, draw_text, get_font, wrap_text
from src.ui.sprites import SpriteActor

PANEL = pygame.Rect(30, 452, 1220, 256)
OPTION_RECTS = [pygame.Rect(50 + (i % 2) * 600, 548 + (i // 2) * 76, 580, 64) for i in range(4)]
ANSWER_KEYS = {pygame.K_1: 0, pygame.K_2: 1, pygame.K_3: 2, pygame.K_4: 3,
               pygame.K_KP1: 0, pygame.K_KP2: 1, pygame.K_KP3: 2, pygame.K_KP4: 3}
CONFIRM_KEYS = (pygame.K_RETURN, pygame.K_KP_ENTER, pygame.K_SPACE)
ENEMY_BOX = pygame.Rect(880, 16, 376, 112)
PLAYER_BOX = pygame.Rect(850, 318, 406, 120)


def _option_lines(text, max_w):
    for size in (32, 28, 25):
        if get_font(size).size(text)[0] <= max_w:
            return size, [text]
    return 24, wrap_text(text, get_font(24), max_w)[:2]


def _star(surf, cx, cy, r, color):
    pts = []
    for i in range(10):
        ang = -math.pi / 2 + i * math.pi / 5
        rad = r if i % 2 == 0 else r * 0.45
        pts.append((cx + math.cos(ang) * rad, cy + math.sin(ang) * rad))
    pygame.draw.polygon(surf, color, pts)


class BattleScene(Scene):
    def __init__(self, game, session):
        super().__init__(game)
        self.session = session
        self.player = session.player
        self.enemy = create_enemy(session.stage)
        self.bm = BattleManager(self.player, self.enemy, game.bank)
        self.theme = THEMES[session.stage]
        bank = game.sprites
        self.hero = SpriteActor(bank, self.player.key)
        self.foe = SpriteActor(bank, self.enemy.key)
        self.bg = make_background(session.stage, bank.info(self.player.key)["feet"], bank.info(self.enemy.key)["feet"])
        self.t = 0.0
        self.state, self.state_time = "intro", 0.0
        self.remaining = self.total_time = S.QUESTION_TIME
        self.q_label = ""
        self.result = None
        self.pending = []
        self.floaters = []
        self.sparks = []
        self.shake = 0.0
        self.disp_hero_hp = self.player.hp
        self.disp_enemy_hp = self.enemy.hp
        self.levels_gained = 0
        self.resolve_duration = 1.7

    # ---------- estados ----------
    def set_state(self, name):
        self.state, self.state_time = name, 0.0

    def _next_question(self):
        self.bm.next_question()
        self.remaining = self.total_time = self.enemy.question_time
        self.q_label = f"Rodada {self.bm.round}  |  Questão {self.bm.asked_in_round + 1}/{S.QUESTIONS_PER_ROUND}"
        self.result = None
        self.set_state("question")

    def _answer(self, idx):
        if self.state != "question":
            return
        res = self.bm.answer(idx)
        self.result = res
        self.set_state("resolve")
        audio = self.game.audio
        self.pending = []
        if res.correct:
            audio.play("correct")
            self.pending.append((0.35, "foe_hurt", 0))
            self.resolve_duration = 1.9
        else:
            audio.play("wrong")
            self.pending.append((0.25, "foe_rage", 0))
            for i, dmg in enumerate(res.hits):
                self.pending.append((0.6 + 0.5 * i, "hero_hurt", dmg))
            self.resolve_duration = 3.4
        if res.phase_changed:
            self.pending.append((0.9, "phase", 0))

    def _burst(self, pos, color, n=9):
        for _ in range(n):
            ang = random.uniform(0, 2 * math.pi)
            spd = random.uniform(80, 220)
            self.sparks.append([pos[0], pos[1], math.cos(ang) * spd, math.sin(ang) * spd - 60, 0.7, color])

    def _fire(self, kind, value):
        res = self.result
        if kind == "foe_hurt":
            self.disp_enemy_hp = max(0, self.disp_enemy_hp - res.damage_dealt)
            self.foe.play("pain")
            self.foe.flash, self.shake = 0.3, 0.25
            head = self.foe.head()
            self._burst(head, (255, 230, 90))
            if res.skill_used:
                self._float("DEPURAR!", head[0], head[1] - 50, S.BLUE)
            self._float(f"-{res.damage_dealt}", head[0], head[1] - 10, S.GOLD)
            self.game.audio.play("hit")
        elif kind == "foe_rage":
            self.foe.play("rage")
            head = self.foe.head()
            self._float(res.enemy_attack.name + "!", head[0] - 150, head[1] + 30, (255, 160, 90))
        elif kind == "hero_hurt":
            self.disp_hero_hp = max(0, self.disp_hero_hp - value)
            self.hero.play("pain")
            self.hero.flash, self.shake = 0.3, 0.25
            head = self.hero.head()
            self._burst(head, (255, 110, 110))
            self._float(f"-{value}", head[0], head[1] - 10, S.RED)
            self.game.audio.play("hit")
        elif kind == "phase":
            self._float(f"FASE {self.enemy.phase}!", S.WIDTH // 2, 200, S.RED)

    def _float(self, text, x, y, color):
        self.floaters.append([text, x, y, color, 1.4])

    def _finish_resolve(self):
        res = self.result
        self.disp_hero_hp, self.disp_enemy_hp = self.player.hp, self.enemy.hp
        if res.enemy_defeated:
            self.levels_gained = self.player.gain_xp(self.enemy.xp_reward)
            self.session.absorb(self.bm)
            self.foe.play("lament", then="lament")
            self.hero.play("cheer", then="cheer")
            self.game.audio.play("win")
            self.set_state("victory")
        elif res.player_defeated:
            self.session.absorb(self.bm)
            self.hero.play("lament", then="lament")
            self.foe.play("cheer", then="cheer")
            self.game.audio.play("lose")
            self.set_state("defeat")
        elif res.round_finished:
            self.set_state("banner")
        else:
            self._next_question()

    # ---------- eventos ----------
    def handle_events(self, events):
        for e in events:
            if e.type == pygame.KEYDOWN:
                if e.key == pygame.K_ESCAPE:
                    self.game.go_menu()
                    return
                if self.state == "question":
                    if e.key in ANSWER_KEYS:
                        self._answer(ANSWER_KEYS[e.key])
                    elif e.key == pygame.K_s:
                        if self.bm.toggle_skill():
                            self.game.audio.play("skill")
                elif e.key in CONFIRM_KEYS:
                    self._confirm()
                elif e.key == pygame.K_m and self.state == "defeat":
                    self.game.go_menu()
                    return
            elif e.type == pygame.MOUSEBUTTONDOWN and e.button == 1:
                if self.state == "question":
                    for i, rect in enumerate(OPTION_RECTS):
                        if rect.collidepoint(e.pos):
                            self._answer(i)
                else:
                    self._confirm()

    def _confirm(self):
        if self.state == "intro":
            self._next_question()
        elif self.state == "resolve" and self.state_time > 0.7:
            self._finish_resolve()
        elif self.state == "banner":
            self._next_question()
        elif self.state == "victory" and self.state_time > 1.5:
            self.game.next_stage()
        elif self.state == "defeat" and self.state_time > 1.5:
            self.game.retry_stage()

    # ---------- atualização ----------
    def update(self, dt):
        self.t += dt
        self.state_time += dt
        self.shake = max(0.0, self.shake - dt)
        self.hero.update(dt)
        self.foe.update(dt)
        for f in self.floaters:
            f[2] -= 45 * dt
            f[4] -= dt
        self.floaters = [f for f in self.floaters if f[4] > 0]
        for s in self.sparks:
            s[0] += s[2] * dt
            s[1] += s[3] * dt
            s[3] += 300 * dt
            s[4] -= dt
        self.sparks = [s for s in self.sparks if s[4] > 0]
        if self.state == "intro" and self.state_time > 4.0:
            self._next_question()
        elif self.state == "question":
            self.remaining -= dt
            if self.remaining <= 0:
                self._answer(None)
        elif self.state == "resolve":
            for ev in [p for p in self.pending if p[0] <= self.state_time]:
                self.pending.remove(ev)
                self._fire(ev[1], ev[2])
            if self.state_time >= self.resolve_duration:
                self._finish_resolve()
        elif self.state == "banner" and self.state_time > 1.8:
            self._next_question()

    # ---------- desenho ----------
    def draw(self, screen):
        ox = random.randint(-5, 5) if self.shake > 0 else 0
        oy = random.randint(-3, 3) if self.shake > 0 else 0
        screen.blit(self.bg, (ox, oy))
        self.foe.draw(screen, ox, oy)
        self.hero.draw(screen, ox, oy)
        for x, y, _, _, life, color in self.sparks:
            _star(screen, x, y, 5 + 9 * life, color)
        self._draw_hud(screen)
        if self.state in ("question", "resolve"):
            self._draw_question_panel(screen)
        else:
            self._draw_dialog(screen)
        for text, x, y, color, life in self.floaters:
            draw_text(screen, text, 50, color, (x, y), "center", bold=True, shadow=True)

    def _draw_hud(self, screen):
        p, e = self.player, self.enemy
        # inimigo (canto superior direito)
        draw_panel(screen, ENEMY_BOX)
        draw_text(screen, e.name, 36, S.WHITE, (ENEMY_BOX.x + 16, ENEMY_BOX.y + 10), bold=True)
        draw_text(screen, "VILÃO" if not hasattr(e, "phase") else f"CHEFE  Fase {e.phase}", 24, (255, 130, 130),
                  (ENEMY_BOX.right - 16, ENEMY_BOX.y + 14), "topright", bold=True)
        draw_text(screen, "HP", 26, S.GOLD, (ENEMY_BOX.x + 16, ENEMY_BOX.y + 52), bold=True)
        draw_bar(screen, (ENEMY_BOX.x + 52, ENEMY_BOX.y + 50, 306, 22), self.disp_enemy_hp / e.max_hp, (90, 210, 110))
        draw_text(screen, f"{self.disp_enemy_hp}/{e.max_hp}", 22, S.WHITE, (ENEMY_BOX.x + 205, ENEMY_BOX.y + 61), "center")
        draw_text(screen, f"Tópico: {e.topic_label}", 22, (200, 210, 240), (ENEMY_BOX.x + 16, ENEMY_BOX.y + 84))
        # herói / jogador (à direita, acima do painel de perguntas)
        draw_panel(screen, PLAYER_BOX)
        draw_text(screen, p.name, 36, S.WHITE, (PLAYER_BOX.x + 16, PLAYER_BOX.y + 8), bold=True)
        draw_text(screen, f"Nv {p.level}", 28, S.GOLD, (PLAYER_BOX.right - 16, PLAYER_BOX.y + 12), "topright", bold=True)
        draw_text(screen, "HP", 24, S.GOLD, (PLAYER_BOX.x + 16, PLAYER_BOX.y + 46), bold=True)
        draw_bar(screen, (PLAYER_BOX.x + 52, PLAYER_BOX.y + 44, 340, 20), self.disp_hero_hp / p.max_hp, (90, 210, 110))
        draw_text(screen, f"{self.disp_hero_hp}/{p.max_hp}", 20, S.WHITE, (PLAYER_BOX.x + 222, PLAYER_BOX.y + 54), "center")
        draw_bar(screen, (PLAYER_BOX.x + 52, PLAYER_BOX.y + 70, 340, 12), p.fc / p.max_fc, (70, 150, 255))
        draw_text(screen, f"FC {p.fc}/{p.max_fc}", 16, S.WHITE, (PLAYER_BOX.x + 222, PLAYER_BOX.y + 76), "center")
        draw_bar(screen, (PLAYER_BOX.x + 52, PLAYER_BOX.y + 88, 340, 6), p.xp / p.xp_needed, (250, 200, 70))
        armed = self.bm.skill_armed
        draw_text(screen, "DEPURAR ATIVO! (x1.5)" if armed else f"[S] Depurar (custa {S.SKILL_COST} FC)", 20,
                  S.GOLD if armed else (190, 200, 230), (PLAYER_BOX.x + 52, PLAYER_BOX.y + 98))
        # fase / rodada / combo (canto superior esquerdo)
        draw_text(screen, f"Fase {self.session.stage + 1}/{S.TOTAL_STAGES} - {self.theme['name']}", 26,
                  S.WHITE, (24, 20), shadow=True)
        if self.q_label:
            draw_text(screen, self.q_label, 30, S.WHITE, (24, 52), bold=True, shadow=True)
        combo = self.bm.combo.label()
        if combo:
            draw_text(screen, combo, 34, S.GOLD, (24, 86), bold=True, shadow=True)

    def _draw_question_panel(self, screen):
        draw_panel(screen, PANEL)
        q = self.result.question if (self.state == "resolve" and self.result) else self.bm.current
        if q is None:
            return
        if self.state == "question":
            ratio = self.remaining / self.total_time
            color = S.GREEN if ratio > 0.5 else (S.GOLD if ratio > 0.25 else S.RED)
            draw_bar(screen, (PANEL.x, PANEL.y - 12, PANEL.w, 10), ratio, color)
            size = 34
            lines = wrap_text(q.text, get_font(size), 1150)
            if len(lines) > 2:
                size = 28
                lines = wrap_text(q.text, get_font(size), 1150)
            for i, line in enumerate(lines[:2]):
                draw_text(screen, line, size, S.WHITE, (52, 470 + i * 34))
        else:
            self._draw_feedback(screen, q)
        mouse = pygame.mouse.get_pos()
        for i, (rect, opt) in enumerate(zip(OPTION_RECTS, q.options)):
            fill, edge = (38, 46, 98), (110, 125, 200)
            if self.state == "question" and rect.collidepoint(mouse):
                fill = (62, 84, 175)
            if self.state == "resolve":
                if i == q.answer:
                    fill, edge = (28, 128, 70), (130, 255, 170)
                elif i == self.result.chosen:
                    fill, edge = (150, 40, 50), (255, 130, 130)
            pygame.draw.rect(screen, fill, rect, border_radius=12)
            pygame.draw.rect(screen, edge, rect, 3, border_radius=12)
            draw_text(screen, str(i + 1), 36, S.GOLD, (rect.x + 20, rect.centery), "midleft", bold=True)
            size, lines = _option_lines(opt, rect.w - 76)
            top = rect.centery - len(lines) * size // 2 + 2
            for j, line in enumerate(lines):
                draw_text(screen, line, size, S.WHITE, (rect.x + 58, top + j * size))

    def _draw_feedback(self, screen, q):
        r = self.result
        if r.correct:
            head, color = f"Correto!  {self.enemy.name} sentiu o golpe do conhecimento!", S.GREEN
        elif r.timed_out:
            head, color = f"Tempo esgotado!  {self.enemy.name} usa {r.enemy_attack.name}!", S.RED
        else:
            head, color = f"Errado!  {self.enemy.name} usa {r.enemy_attack.name}!", S.RED
        draw_text(screen, head, 38, color, (52, 468), bold=True)
        text = f"Resposta: {q.options[q.answer]}. {q.explanation}"
        for i, line in enumerate(wrap_text(text, get_font(27), 1150)[:2]):
            draw_text(screen, line, 27, (225, 230, 250), (52, 506 + i * 28))

    def _draw_dialog(self, screen):
        """Caixa de texto no rodapé (como no Pokémon), deixando os personagens visíveis."""
        b = self.bm
        draw_panel(screen, PANEL, fill=(10, 12, 30, 240))
        prompt = "ENTER para continuar"
        if self.state == "intro":
            title, color = f"Fase {self.session.stage + 1}/{S.TOTAL_STAGES}", S.GOLD
            lines = [f"{self.player.name} enfrenta {self.enemy.name}!", f"Tópico: {self.enemy.topic_label}  |  "
                     f"{S.QUESTIONS_PER_ROUND} questões por rodada"]
            prompt = "ENTER para começar"
        elif self.state == "banner":
            title, color = f"Rodada {b.round - 1} concluída!", S.GOLD
            lines = [f"{self.enemy.name} ainda resiste!", f"Nova rodada de {S.QUESTIONS_PER_ROUND} questões..."]
        elif self.state == "victory":
            title, color = "VITÓRIA!", S.GREEN
            lines = [f"{self.enemy.name} se ajoelha, derrotado.  Acertos: {b.correct_total}/{b.total_asked}  |  "
                     f"Melhor combo: x{b.combo.best}", f"+{self.enemy.xp_reward} XP"]
            if self.levels_gained:
                lines[1] += f"   |   {self.player.name} subiu para o nível {self.player.level}!"
            last = self.session.stage == S.TOTAL_STAGES - 1
            prompt = "ENTER para " + ("ver o resultado" if last else "a próxima fase")
        else:
            title, color = "DERROTA", S.RED
            lines = [f"{self.player.name} caiu de joelhos...", "Estude as explicações e tente de novo!"]
            prompt = "ENTER: tentar de novo   |   M: menu"
        draw_text(screen, title, 64, color, (S.WIDTH // 2, 492), "center", bold=True, shadow=True)
        for i, line in enumerate(lines):
            draw_text(screen, line, 30, S.WHITE, (S.WIDTH // 2, 556 + i * 38), "center")
        if int(self.t * 2) % 2 == 0:
            draw_text(screen, prompt, 30, S.GOLD, (S.WIDTH // 2, 670), "center", bold=True)
