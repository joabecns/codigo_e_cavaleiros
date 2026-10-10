"""Herói controlado pelo jogador."""
from src import settings as S
from src.entities.character import Character


class Player(Character):
    def __init__(self, key="hero_leo", name="Leo"):
        super().__init__(name, key, S.PLAYER_HP, S.PLAYER_ATK, S.PLAYER_DEF)
        self.level = 1
        self.xp = 0
        self.max_fc = S.PLAYER_FC
        self._fc = S.PLAYER_FC

    @property
    def fc(self):
        return self._fc

    @property
    def xp_needed(self):
        return 40 + 20 * (self.level - 1)

    def gain_xp(self, amount):
        """Soma XP e sobe de nível quando possível. Retorna quantos níveis subiu."""
        self.xp += amount
        gained = 0
        while self.level < S.MAX_LEVEL and self.xp >= self.xp_needed:
            self.xp -= self.xp_needed
            self._level_up()
            gained += 1
        return gained

    def _level_up(self):
        self.level += 1
        self._max_hp += 15
        self.atk += 2
        self.max_fc += 5
        self.heal(int(self._max_hp * 0.3))

    def spend_fc(self, amount):
        if self._fc < amount:
            return False
        self._fc -= amount
        return True

    def gain_fc(self, amount):
        self._fc = min(self.max_fc, self._fc + amount)

    def recover_after_fight(self):
        self.heal(int(self._max_hp * S.HEAL_BETWEEN_FIGHTS))
        self._fc = self.max_fc

    def full_restore(self):
        self.restore()
        self._fc = self.max_fc

    def idle_clip(self):
        return "idle"
