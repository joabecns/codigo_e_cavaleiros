"""Classe base abstrata das cenas (padrão State)."""
from abc import ABC, abstractmethod


class Scene(ABC):
    def __init__(self, game):
        self.game = game

    @abstractmethod
    def handle_events(self, events):
        ...

    @abstractmethod
    def update(self, dt):
        ...

    @abstractmethod
    def draw(self, screen):
        ...
