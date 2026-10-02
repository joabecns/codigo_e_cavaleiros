"""Classe principal: janela, loop e troca de cenas."""
import os

import pygame

from src import settings as S
from src.core.audio import AudioManager
from src.core.session import Session
from src.questions.question_bank import QuestionBank
from src.scenes.battle import BattleScene
from src.scenes.end import EndScene
from src.scenes.menu import MenuScene, SettingsScene
from src.scenes.select import SelectScene
from src.ui.sprites import SpriteBank


class Game:
    def __init__(self):
        pygame.init()
        self.screen = pygame.display.set_mode((S.WIDTH, S.HEIGHT))
        pygame.display.set_caption(S.TITLE)
        self.clock = pygame.time.Clock()
        self.audio = AudioManager()
        self.bank = QuestionBank(os.path.join(S.DATA_DIR, "questions.json"))
        self.sprites = SpriteBank()
        self.running = True
        self.session = None
        self.scene = MenuScene(self)
        self.audio.start_music()

    # ----- navegação entre cenas -----
    def change_scene(self, scene):
        self.scene = scene

    def go_menu(self):
        self.session = None
        self.change_scene(MenuScene(self))

    def go_settings(self):
        self.change_scene(SettingsScene(self))

    def go_select(self):
        self.change_scene(SelectScene(self))

    def start_new_game(self, hero_key):
        self.session = Session(hero_key, self.sprites.info(hero_key)["name"])
        self.change_scene(BattleScene(self, self.session))

    def next_stage(self):
        s = self.session
        s.player.recover_after_fight()
        s.stage += 1
        if s.stage >= S.TOTAL_STAGES:
            self.change_scene(EndScene(self, s))
        else:
            self.change_scene(BattleScene(self, s))

    def retry_stage(self):
        self.session.player.full_restore()
        self.change_scene(BattleScene(self, self.session))

    def quit(self):
        self.running = False

    # ----- loop -----
    def frame(self, dt):
        events = pygame.event.get()
        for e in events:
            if e.type == pygame.QUIT:
                self.running = False
        self.scene.handle_events(events)
        self.scene.update(dt)
        if self.session and self.session.clock_running:
            self.session.elapsed += dt
        self.scene.draw(self.screen)
        pygame.display.flip()

    def run(self):
        while self.running:
            self.frame(min(self.clock.tick(S.FPS) / 1000.0, 0.05))
        pygame.quit()
