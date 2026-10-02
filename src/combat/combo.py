"""Sistema de combo: acertos consecutivos aumentam o dano."""


class ComboSystem:
    STEP = 0.25       # +25% por acerto extra
    MAX_STEPS = 3     # bônus máximo: +75%

    def __init__(self):
        self.streak = 0
        self.best = 0

    def register(self, correct):
        if correct:
            self.streak += 1
            self.best = max(self.best, self.streak)
        else:
            self.streak = 0

    def multiplier(self):
        steps = min(max(self.streak - 1, 0), self.MAX_STEPS)
        return 1.0 + self.STEP * steps

    def label(self):
        if self.streak < 2:
            return ""
        return f"COMBO x{self.streak}  (+{int((self.multiplier() - 1) * 100)}%)"
