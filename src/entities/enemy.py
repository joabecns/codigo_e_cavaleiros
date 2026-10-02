"""Vilões e chefão (personagens 3D low-poly).

Regra de IA do projeto: quando o jogador erra a questão, o inimigo escolhe
ALEATORIAMENTE (biblioteca random) qual ataque vai usar. Como o jogo é educativo,
o 'ataque' é só um efeito (provocação): o vilão faz cara de raiva e o herói faz cara de dor.
"""
import random
from dataclasses import dataclass

from src import settings as S
from src.entities.character import Character


@dataclass(frozen=True)
class EnemyAttack:
    name: str
    power: float
    hits: int = 1


class Enemy(Character):
    topics = []
    topic_label = ""

    def __init__(self, name, key, hp, atk, defense, attacks, xp_reward):
        super().__init__(name, key, hp, atk, defense)
        self._attacks = list(attacks)
        self.xp_reward = xp_reward

    @property
    def attacks(self):
        return self._attacks

    @property
    def question_time(self):
        return S.QUESTION_TIME

    def idle_clip(self):
        return "idle"

    def choose_attack(self):
        """IA: sorteio aleatório entre os ataques disponíveis."""
        return random.choice(self.attacks)

    def damage_against(self, attack, target):
        return max(1, round(self.atk * attack.power - target.defense * 0.5))


class PalhacoBug(Enemy):
    topics = ["variaveis"]
    topic_label = "Variáveis e tipos"

    def __init__(self):
        super().__init__("Palhaço Bug", "vil_palhaco", 46, 7, 0,
                         [EnemyAttack("Risada Maligna", 1.0), EnemyAttack("Buzina Estridente", 1.3),
                          EnemyAttack("Confete de Erro", 0.8)], 45)


class RatoLoop(Enemy):
    topics = ["repeticao"]
    topic_label = "Estruturas de repetição"

    def __init__(self):
        super().__init__("Rato Loop", "vil_rato", 56, 9, 1,
                         [EnemyAttack("Guincho Agudo", 1.0), EnemyAttack("Roda Infinita", 0.7, hits=2),
                          EnemyAttack("Laço Apertado", 1.2)], 60)


class CoelhoNulo(Enemy):
    topics = ["funcoes"]
    topic_label = "Funções e listas"

    def __init__(self):
        super().__init__("Coelho Nulo", "vil_coelho", 66, 12, 2,
                         [EnemyAttack("Olhar Vazio", 1.0), EnemyAttack("Exceção!", 1.4),
                          EnemyAttack("Vazamento", 0.9)], 75)


class SargentoHeranca(Enemy):
    topics = ["poo"]
    topic_label = "Classes e POO"

    def __init__(self):
        super().__init__("Sargento Herança", "vil_sargento", 76, 14, 3,
                         [EnemyAttack("Grito de Ordem", 1.0), EnemyAttack("Sobrescrita", 1.3),
                          EnemyAttack("Super()", 1.1)], 90)


class Boss(Enemy):
    """Chefão com fases: quanto menos HP, mais ataques no sorteio e menos tempo por questão."""

    def __init__(self, name, key, hp, atk, defense, attacks, phase2, phase3, xp_reward=0):
        super().__init__(name, key, hp, atk, defense, attacks, xp_reward)
        self._phase2, self._phase3 = list(phase2), list(phase3)

    @property
    def phase(self):
        r = self.ratio
        return 1 if r > 0.6 else (2 if r > 0.3 else 3)

    @property
    def attacks(self):
        pool = list(self._attacks)
        if self.phase >= 2:
            pool += self._phase2
        if self.phase >= 3:
            pool += self._phase3
        return pool

    @property
    def question_time(self):
        return S.BOSS_FINAL_PHASE_TIME if self.phase == 3 else S.QUESTION_TIME


class CompiladorSombrio(Boss):
    topics = ["variaveis", "repeticao", "funcoes", "poo", "ia"]
    topic_label = "Todos os tópicos"

    def __init__(self):
        super().__init__("Compilador Sombrio", "boss_capitao", 125, 17, 4,
                         [EnemyAttack("Erro de Sintaxe", 1.0), EnemyAttack("Warning", 0.8),
                          EnemyAttack("Stack Overflow", 1.3)],
                         [EnemyAttack("Segfault", 1.6)],
                         [EnemyAttack("Kernel Panic", 1.9), EnemyAttack("Loop de Bugs", 0.8, hits=2)])


ENEMY_ORDER = [PalhacoBug, RatoLoop, CoelhoNulo, SargentoHeranca, CompiladorSombrio]


def create_enemy(stage):
    return ENEMY_ORDER[stage]()
