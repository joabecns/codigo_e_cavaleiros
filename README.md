# Caçadores de Bugs

RPG educativo por turnos feito na Unity 6 (6000.6.4f1). Responda perguntas de programação, derrote 4 vilões e o Compilador Sombrio.

- 5 fases, 5 perguntas por rodada, 15 s por pergunta
- Erro ou tempo esgotado: o vilão fica furioso e escolhe um ataque ao acaso
- Escolha de herói em 3D (arraste para girar 360°, dois cliques para escolher)
- Áudio gerado por código (sem arquivos externos)

## Como abrir
Abra a pasta na Unity 6000.6.4f1 (ou superior), carregue `Assets/Scenes/Menu.unity` e dê Play. Requer Git LFS (`git lfs install`) para baixar os modelos.

## Controles
1 a 4 responder • S depurar (gasta Foco) • M silenciar • setas/A/D e Enter/duplo clique na seleção

## Estrutura
`Assets/Scripts` (lógica, UI, áudio), `Assets/Editor` (construtores de cena/prefab/áudio), `Assets/Tests` (12 testes NUnit), `Referencia_Python` (versão Pygame original).

## Créditos e licença
Os modelos 3D e animações são de terceiros; confirme a licença do pacote antes de tornar o repositório público (veja `Referencia_Python/CREDITS.md`).
