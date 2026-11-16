# CC09 — A abertura precisa esperar

A cena usa composições específicas para cada andar, com o trabalho interrompido às 06:00 e o segurança comunicando o adiamento à curadoria.

## Materiais reutilizados

| Uso | Fonte no projeto |
| --- | --- |
| Hall do térreo | `Assets/Art/CC02/andar1.jpg` |
| Esculturas e marcas de piso | `Assets/Art/CC02/estatua_vaqueiro.png`, `estatua_pescador.png`, `estatua_rendeira.png`, `placement_mark.asset` |
| Galeria e pinturas | `Assets/Art/CC03/galeria_moonlit.png`, `obra_mare_mucuripe.png`, `obra_nevoa_baturite.png`, `obra_luz_sertao.png` |
| Moldura para a pintura deslocada | `Assets/Art/CC07/cc07_mare_mucuripe_matte.asset` |
| Sala do acervo e peças espalhadas | `Assets/Art/CC04/archive_quiet.png`, `cc04_archive_piece.asset`, `cc04_bird_object.asset` |
| Segurança, Moema e sombras | Animações em `Assets/Art/CC01` e `Assets/Art/CC04A` |
| Rádio e visor | `Assets/Art/CC08/cc08_security_radio.asset`, `cc08_radio_screen.asset` |
| Brilho suave | `Assets/Art/CC06/cc06_soft_pixel_glow.asset` |
| Interface e vinheta | `Assets/Art/CC01/cinematic_vignette.asset`, `Assets/Art/CC07/cc07_ui_white_pixel.asset` |

## Som e fala

As ambiências, o toque do relógio, o assentamento da escultura, o ruído da moldura, os passos de Moema, o clique do rádio e a música final reaproveitam sons já existentes em CC02, CC03, CC04A e CC08. As falas da narração e da curadoria têm slots no componente `CC09CutsceneController`; não havia gravações específicas da CC09 entre os assets locais usados nesta cena.

## Construção e revisão

- Cena: `Assets/Scenes/CC09_A_Abertura_Precisa_Esperar.unity`.
- Gerador: `Assets/Scripts/Editor/CC09SceneBuilder.cs`.
- Captura de revisão: `Assets/Scripts/Editor/CC09FrameCapture.cs`.
- A captura completa produz 841 quadros por andar (14 segundos a 60 quadros por segundo) em `Library/CC09ReviewFrames`; esses quadros são material de revisão local, não arquivos da build.
