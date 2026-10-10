"""Constantes globais: interface e balanceamento do jogo."""
import os

BASE_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DATA_DIR = os.path.join(BASE_DIR, "data")
ASSETS_DIR = os.path.join(BASE_DIR, "assets")

WIDTH, HEIGHT, FPS = 1280, 720, 60
TITLE = "Código & Cavaleiros"

# Regras da partida (meta de duração total: 5 a 7 minutos)
QUESTIONS_PER_ROUND = 5      # questões por rodada
QUESTION_TIME = 15.0         # segundos para responder
BOSS_FINAL_PHASE_TIME = 10.0 # tempo na fase final do chefão
TOTAL_STAGES = 5             # 4 inimigos comuns + chefão

# Herói
PLAYER_HP, PLAYER_FC, PLAYER_ATK, PLAYER_DEF = 100, 30, 12, 4
MAX_LEVEL = 5
SKILL_COST = 8               # custo em Foco da habilidade "Depurar"
SKILL_MULT = 1.5             # multiplicador de dano do "Depurar"
FC_PER_HIT = 4               # Foco recuperado a cada acerto
HEAL_BETWEEN_FIGHTS = 0.35   # % de HP recuperado entre lutas

TOPIC_NAMES = {
    "variaveis": "Variáveis e tipos",
    "repeticao": "Estruturas de repetição",
    "funcoes": "Funções e listas",
    "poo": "Classes e POO",
    "ia": "Fundamentos de IA",
}

WHITE = (255, 255, 255)
GOLD = (255, 220, 110)
RED = (255, 90, 90)
GREEN = (110, 235, 140)
BLUE = (90, 160, 255)

# Cena 3D pré-renderizada (câmera diagonal, na altura do ombro do jogador)
HORIZON_Y = 164
HEROES = ["hero_leo", "hero_theo", "hero_bruno", "hero_max", "hero_caio"]
