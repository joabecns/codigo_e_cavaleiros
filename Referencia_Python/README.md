# Código & Cavaleiros — RPG educativo por turnos (versão 3D low-poly)

RPG por turnos em **Python + Pygame**, com batalha no estilo **Pokémon** e personagens **3D low-poly**.
Você escolhe um herói e enfrenta 4 vilões e o chefão **Compilador Sombrio** respondendo questões de programação, POO e IA.

## Como rodar
```bash
python -m venv .venv && source .venv/bin/activate   # Windows: .venv\Scripts\activate
pip install -r requirements.txt
python main.py
```
Python 3.10+ e pygame 2.5+. Testes: `python -m pytest -q` (17 testes).

## Como jogar
| Ação | Controle |
|---|---|
| Escolher herói | setas + ENTER (ou clique) |
| Responder | teclas **1-4** ou clique na alternativa |
| Depurar (dano x1.5, custa 8 de Foco) | tecla **S** antes de responder |
| Avançar telas | **ENTER** / clique |
| Voltar ao menu | **ESC** |

- **Escolha do herói:** 5 opções (Leo, Theo, Bruno, Max, Caio); vale até o fim. Os vilões e o chefe são fixos.
- **Batalha estilo Pokémon:** câmera diagonal na altura do ombro do herói, com o vilão à frente. Caixa do vilão (nome e vida)
  no alto, caixa do herói (nome, vida, Foco, XP) à direita e as perguntas no rodapé.
- **Sem golpes diretos:** o dano é só efeito. Ao **acertar**, o vilão faz cara de dor (flash, faíscas, número de dano).
  Ao **errar** (ou estourar os 15 s), o vilão **sorteia com `random.choice`** uma provocação, fica com raiva e o herói faz cara de dor.
- **Fim da luta:** o derrotado **fica de joelhos lamentando** e o vencedor **pula de alegria**.
- **Rodada = 5 questões.** Se o vilão resistir, começa outra rodada. Partida completa: 5 a 7 minutos.
- **Combos** (+25% por acerto seguido, até +75%), **níveis** (mais HP, Foco e ataque) e chefão com 3 fases.

## Vilões
| Fase | Vilão | Tópico |
|---|---|---|
| 1 | Palhaço Bug | Variáveis e tipos |
| 2 | Rato Loop | Estruturas de repetição |
| 3 | Coelho Nulo | Funções e listas |
| 4 | Sargento Herança | Classes e POO |
| 5 | Compilador Sombrio (chefe) | Todos os tópicos |

## Estrutura
```
main.py
data/questions.json    40 questões (5 tópicos x 8)
assets/characters/     sprites PNG das animações 3D + manifest.json (10 personagens)
src/core/              game.py, session.py, audio.py (sons gerados por código)
src/scenes/            scene.py (abstrata), menu.py, select.py (escolha do herói), battle.py, end.py
src/entities/          character.py (abstrata), player.py, enemy.py (4 vilões + Boss)
src/combat/            battle_manager.py (regras, sem pygame), combo.py
src/questions/         question_bank.py
src/ui/                sprites.py (SpriteBank/SpriteActor), backgrounds.py, drawing.py, widgets.py
tools/render_pipeline/ scripts que geraram os sprites (referência)
tests/                 combate, combo, questões, jogador, assets, simulação e bot completo
```

## Como os personagens 3D funcionam no Pygame
Cada personagem foi montado em 3D (corpo + rosto + cabelo + roupas + acessórios), animado com as animações KayKit
retargetadas mais lamento e raiva feitos por código, e **renderizado em quadros PNG** com a mesma câmera diagonal.
O Pygame só toca essas sequências (`SpriteActor`). A câmera é fixa, como no Pokémon, e o jogo continua leve e 100% Pygame.

## Conceitos de POO
- **Herança:** `Character → Player / Enemy → PalhacoBug, RatoLoop, CoelhoNulo, SargentoHeranca, Boss → CompiladorSombrio`; `Scene → Menu, Select, Battle, End`.
- **Polimorfismo:** `Boss` sobrescreve `attacks` e `question_time`; cada cena implementa `handle_events/update/draw`.
- **Encapsulamento:** HP protegido (`receive_damage`, `heal`); Foco por `spend_fc`/`gain_fc`.
- **Abstração:** `Character` e `Scene` são classes abstratas (`abc`).
- **Composição:** `BattleManager` usa `ComboSystem`, `QuestionBank`, `Player` e `Enemy`; a cena usa dois `SpriteActor`.
