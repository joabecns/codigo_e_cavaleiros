# Pipeline que gerou os sprites (referência)

Os personagens são **modelos 3D low-poly** (peças GLB) animados por **retargeting** das animações
KayKit (Rig_Medium) para o esqueleto dos personagens, mais duas animações criadas por código
(`clips.py`): **lamento** (cai de joelhos e soluça) e **raiva** (pisa forte, punhos tremendo).
O Panda3D renderiza cada quadro em modo offscreen e o resultado vira PNG com fundo transparente.

| Arquivo | Função |
|---|---|
| `rigtools.py` | FK, retargeting KayKit para o esqueleto do personagem e escrita de GLB animado |
| `clips.py` | Monta os clipes: idle, dor (Hit), comemoração (Cheering + pulos), raiva e lamento |
| `recolor.py` | Recolore peças escolhendo a cor no atlas de texturas |
| `sprites.py` / `make_sprites.py` | Câmera diagonal, renderização offscreen e exportação dos PNGs + manifest.json |
| `specs.json` | Quais peças compõem cada herói e vilão |

Não é necessário para jogar. Para rodar de novo: `pip install panda3d panda3d-gltf pygltflib scipy numpy pillow`,
ter os pacotes originais e ajustar os caminhos `/tmp/w/...` no topo dos scripts.
