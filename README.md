# O Mistério do Espaço Cultural

Jogo puzzle 2D em pixel art (Unity) — trabalho de Computação Gráfica da Unifor.
Equipe: Heitor Sette, Levi de Pontes, Natália Pacheco e Savas Neto.

## Abrir o projeto (Windows ou Linux)

1. Instale o **Unity 6000.6.0f1** (Unity 6.6) pelo Unity Hub, com o módulo de build da sua plataforma.
2. Clone o repositório — não precisa de Git LFS:
   ```
   git clone https://github.com/heitornsette/O-Misterio-do-Espaco-Cultural.git
   ```
3. No Unity Hub: **Add › Add project from disk** e escolha a pasta clonada. A primeira abertura importa tudo (alguns minutos).
4. Rode **Espaço Cultural › Andares › Criar cenas dos 4 andares** uma vez (gera os andares e a ordem das cenas no build).
5. Abra `Assets/Scenes/MenuInicial` e dê **Play**.

## Rota do jogo

MenuInicial → CC01 → Andar1 → CC02 → Andar2 → CC03 → Andar3 (falha → CC04A) → CC04 → CC05 → Andar4 → CC06 → CC07 → CC08 → Créditos.
Fim do tempo (06:00): CC09 (andares 1–3) ou CC10 (andar 4).

## Controles

Setas/WASD andam · E interage · Z desfaz e R recomeça (Andar 1) · Esc pausa (com configurações).

## Pastas

- `Assets/Scripts/` — `Menu`, `Tempo` (relógio), `Gameplay` (andares e puzzles), `Campus`, `Jogo`, `Audio`, `Cutscenes` (CC01–CC10 e ponte com a rota).
- `Assets/Resources/Jogo`, `Assets/Resources/Campus`, `Assets/Resources/Audio` — arte e sons carregados em tempo de execução.
- `Assets/Art`, `Assets/Audio` — arte e som das cutscenes e do menu.
- `ArteRecebida/` — arquivos brutos de arte recebidos (fora do Unity).

## Observações para quem usa Linux

- Todo texto do repositório usa fim de linha LF (`.gitattributes`), e não há Git LFS: um `git clone` comum traz tudo.
- `Library/`, `Temp/`, `Logs/`, `UserSettings/` e os `.csproj/.sln` são gerados pelo Unity e ficam fora do Git.
