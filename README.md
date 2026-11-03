# O Mistério do Espaço Cultural

Projeto Unity 2D criado do zero para o jogo.

## Cutscene CC01 — A madrugada começa

Cena: `Assets/Scenes/CC01_A_Madrugada_Comeca.unity`  
Duração: 35,7 segundos, incluindo o cartão de título após a ação.  
Construção: no Unity, use **Misterio → Build CC01 - A madrugada começa**.

A cena abre no saguão às 02:00 e segue os seis beats do roteiro: apresentação do turno, ronda e café, estrondo na escadaria, lanterna e cassetete, pegadas no facho de luz, e caminhada em direção à porta entreaberta. Ela usa o fundo pixel art do jogo e os sprites originais do segurança, com animações de idle e caminhada. A sequência inclui legendas, iluminação e poeira em pixel art, sons de ambiente, música, estrondo, clique, passos, mariposa, pena e título. O relógio permanece em 02:00 e `ControlsLocked` só é liberado ao final.

No encerramento, a imagem corta para preto após a última fala, o som ambiente baixa e o título aparece depois de uma pausa curta com impacto sonoro. Referências de ritmo e apresentação: trailers oficiais de [Alan Wake 2](https://www.youtube.com/watch?v=MEWgOlTIW4Y), [Layers of Fear](https://www.youtube.com/watch?v=JJMeiW0Gz_Y) e [Silent Hill 2](https://www.youtube.com/watch?v=xRShSWXFDFA).

Os espaços dos clipes de voz estão vazios para receber as gravações do narrador e do segurança. Os campos estão na lista `subtitles` de `CC01CutsceneController`.

O projeto contém somente esta cutscene por enquanto. `onCutsceneFinished` é o ponto de integração com a cena jogável, quando ela for criada.

### Estudo 3D opcional

Uma exploração low-poly opcional fica em `Assets/Scenes/CC01_A_Madrugada_3D.unity`, com fontes em `Documentation/Blender/CC01_Cinematic.blend` e `Documentation/Blender/create_cc01.py`. Ela não está nas Build Settings. A abertura padrão continua usando o fundo e os sprites pixel art do próprio jogo.

## Arte e fontes

- Sprites do segurança, Moema e paleta: `Assets/Art/Characters` e `Assets/Art/Palette`.
- Fundo original da cena: `Documentation/ArtSource/foyer_night_master.png`; versão de jogo: `Assets/Art/Backgrounds/foyer_night_384x216.png`.
- Narrativa, storyboard e documentação: `Documentation/Source`.
- Projeto criado com Unity `6000.6.0f1`; pixel art usa filtro Point e compressão desligada.
