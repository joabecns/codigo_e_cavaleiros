"""Estado da partida em andamento (herói, fase atual e estatísticas)."""
from src.entities.player import Player


class Session:
    def __init__(self, hero_key="hero_leo", hero_name="Leo"):
        self.hero_key = hero_key
        self.player = Player(hero_key, hero_name)
        self.stage = 0
        self.stats = {}
        self.best_combo = 0
        self.elapsed = 0.0
        self.clock_running = True

    def absorb(self, battle):
        """Acumula as estatísticas de uma batalha encerrada."""
        for topic, (ok, total) in battle.stats.items():
            cur = self.stats.setdefault(topic, [0, 0])
            cur[0] += ok
            cur[1] += total
        self.best_combo = max(self.best_combo, battle.combo.best)

    @property
    def total_asked(self):
        return sum(t for _, t in self.stats.values())

    @property
    def total_correct(self):
        return sum(c for c, _ in self.stats.values())

    @property
    def accuracy(self):
        return self.total_correct / self.total_asked if self.total_asked else 0.0
