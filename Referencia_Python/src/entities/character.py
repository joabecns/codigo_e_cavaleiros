"""Classe base abstrata dos personagens (herói e inimigos)."""
from abc import ABC, abstractmethod


class Character(ABC):
    def __init__(self, name, key, max_hp, atk, defense):
        self.name = name
        self.key = key            # chave do personagem 3D no manifest de sprites
        self._max_hp = int(max_hp)
        self._hp = int(max_hp)
        self.atk = atk
        self.defense = defense

    @property
    def hp(self):
        return self._hp

    @property
    def max_hp(self):
        return self._max_hp

    @property
    def alive(self):
        return self._hp > 0

    @property
    def ratio(self):
        return self._hp / self._max_hp

    def receive_damage(self, amount):
        """Encapsulamento: o HP nunca fica negativo. Retorna o dano aplicado."""
        amount = min(self._hp, max(0, int(amount)))
        self._hp -= amount
        return amount

    def heal(self, amount):
        before = self._hp
        self._hp = min(self._max_hp, self._hp + max(0, int(amount)))
        return self._hp - before

    def restore(self):
        self._hp = self._max_hp

    @abstractmethod
    def idle_clip(self):
        """Nome da animação de descanso deste personagem."""
