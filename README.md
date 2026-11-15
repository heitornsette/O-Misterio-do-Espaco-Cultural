# O Mistério do Espaço Cultural

Projeto Unity 2D criado do zero para o jogo.

## Cutscene CC01 — A madrugada começa

Cena: `Assets/Scenes/CC01_A_Madrugada_Comeca.unity`  
Duração: 35,7 segundos, incluindo o cartão de título após a ação.  
Construção: no Unity, use **Misterio → Build CC01 - A madrugada começa**.

A cena abre no saguão às 02:00 e segue os seis beats do roteiro: apresentação do turno, ronda e café, estrondo na escadaria, lanterna e cassetete, pegadas no facho de luz, e caminhada em direção à porta entreaberta. A sequência inclui legendas, iluminação e poeira em pixel art, sons de ambiente, música, estrondo, clique, passos, mariposa, pena e título. O relógio permanece em 02:00 e `ControlsLocked` só é liberado ao final.

No encerramento, a imagem corta para preto após a última fala, o som ambiente baixa e o título aparece depois de uma pausa curta com impacto sonoro. Referências de ritmo e apresentação: trailers oficiais de [Alan Wake 2](https://www.youtube.com/watch?v=MEWgOlTIW4Y), [Layers of Fear](https://www.youtube.com/watch?v=JJMeiW0Gz_Y) e [Silent Hill 2](https://www.youtube.com/watch?v=xRShSWXFDFA).

Os espaços dos clipes de voz estão vazios para receber as gravações do narrador e do segurança. Os campos estão na lista `subtitles` de `CC01CutsceneController`.

## Cutscene CC02 — Acesso à galeria

- Cena: `Assets/Scenes/CC02_Acesso_a_Galeria.unity`
- Duração: 8 segundos.
- Construção: no Unity, use **Misterio → Build CC02 - Acesso à galeria**.

Após o puzzle das estátuas, a última escultura desliza até a marca no piso. A lanterna ilumina o encaixe, a luz superior da escadaria acende e o segurança sobe os primeiros degraus. A fala “Estátuas no lugar. Agora, as pinturas.” conduz à Galeria de Pinturas. A cena mantém o relógio global fora da tela durante a transição e libera `ControlsLocked` ao final.

O cenário base do térreo veio dos recursos em `comp_grafica/_trailer_assets` do repositório do semestre e foi copiado para `Assets/Art/CC02/andar1.jpg`. As três esculturas abstratas de madeira foram criadas para esta cena e ficam em `Assets/Art/CC02/escultura_*.png`. Marcações, poeira, facho da lanterna e brilho da escadaria ficam na mesma pasta. Seis sons WAV — ambiente, xilofone, encaixe de madeira, confirmação, trava e passos — ficam em `Assets/Audio/CC02`. CC02 reutiliza as animações do segurança, a fonte e a vinheta de CC01. A fala está legendada; o espaço de voz permanece pronto para gravação. `onCutsceneFinished` é o ponto de ligação com a galeria, cuja cena jogável ainda será construída.

O projeto também contém um protótipo independente de movimentação do segurança em `Assets/Scenes/Gameplay_Prototype.unity`; ele não implementa ainda os puzzles do jogo.

## Arte e fontes

- Sprites do segurança, Moema e paleta: `Assets/Art/Characters` e `Assets/Art/Palette`.
- Fundo original da cena: `Documentation/ArtSource/foyer_night_master.png`; versão de jogo: `Assets/Art/Backgrounds/foyer_night_384x216.png`.
- Narrativa, storyboard e documentação: `Documentation/Source`.
- Projeto criado com Unity `6000.6.0f1`; pixel art usa filtro Point e compressão desligada.
