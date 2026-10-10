"""Lógica pura de batalha (sem desenhar nada): fácil de testar."""
from dataclasses import dataclass, field
from typing import Optional

from src import settings as S
from src.combat.combo import ComboSystem
from src.entities.enemy import EnemyAttack
from src.questions.question_bank import Question


@dataclass
class TurnResult:
    question: Question
    chosen: Optional[int]
    correct: bool
    timed_out: bool
    streak: int = 0
    damage_dealt: int = 0
    skill_used: bool = False
    enemy_attack: Optional[EnemyAttack] = None
    hits: list = field(default_factory=list)
    round_finished: bool = False
    enemy_defeated: bool = False
    player_defeated: bool = False
    phase_changed: bool = False


class BattleManager:
    def __init__(self, player, enemy, bank, per_round=S.QUESTIONS_PER_ROUND):
        self.player, self.enemy, self.bank = player, enemy, bank
        self.per_round = per_round
        self.combo = ComboSystem()
        self.round = 1
        self.asked_in_round = 0
        self.total_asked = 0
        self.correct_total = 0
        self.skill_armed = False
        self.current = None
        self.stats = {}   # tópico -> [acertos, total]

    def next_question(self):
        self.current = self.bank.draw(self.enemy.topics)
        return self.current

    def toggle_skill(self):
        """Arma/desarma o 'Depurar' (só arma se houver Foco suficiente)."""
        if self.skill_armed:
            self.skill_armed = False
        elif self.player.fc >= S.SKILL_COST:
            self.skill_armed = True
        return self.skill_armed

    def calc_damage(self, mult):
        return max(1, round(self.player.atk * mult - self.enemy.defense * 0.5))

    def answer(self, chosen):
        """Processa a resposta (None = tempo esgotado). Acertou: o herói ataca.
        Errou: o inimigo sorteia (random) um ataque e contra-ataca."""
        q = self.current
        if q is None:
            raise RuntimeError("Nenhuma questão ativa")
        correct = chosen is not None and chosen == q.answer
        phase_before = getattr(self.enemy, "phase", None)
        self.combo.register(correct)
        res = TurnResult(q, chosen, correct, chosen is None, streak=self.combo.streak)
        stat = self.stats.setdefault(q.topic, [0, 0])
        stat[1] += 1
        if correct:
            stat[0] += 1
            self.correct_total += 1
            mult = self.combo.multiplier()
            if self.skill_armed and self.player.spend_fc(S.SKILL_COST):
                res.skill_used = True
                mult *= S.SKILL_MULT
            res.damage_dealt = self.enemy.receive_damage(self.calc_damage(mult))
            self.player.gain_fc(S.FC_PER_HIT)
        else:
            attack = self.enemy.choose_attack()
            res.enemy_attack = attack
            for _ in range(attack.hits):
                res.hits.append(self.player.receive_damage(self.enemy.damage_against(attack, self.player)))
                if not self.player.alive:
                    break
        self.skill_armed = False
        self.current = None
        self.asked_in_round += 1
        self.total_asked += 1
        res.enemy_defeated = not self.enemy.alive
        res.player_defeated = not self.player.alive
        res.phase_changed = phase_before is not None and self.enemy.phase != phase_before
        if not (res.enemy_defeated or res.player_defeated) and self.asked_in_round >= self.per_round:
            res.round_finished = True
            self.round += 1
            self.asked_in_round = 0
        return res
