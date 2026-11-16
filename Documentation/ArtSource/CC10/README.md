# CC10 — A última sala ainda no escuro

A CC10 é o final alternativo de 15 segundos acionado quando o prazo das 06:00 chega durante o puzzle elétrico. O salão permanece seguro, a obra fica intacta e sem os focos, e a equipe adia somente a abertura do Salão Principal até a revisão elétrica.

## Arte reutilizada e criada para a cena

| Uso | Fonte no projeto |
| --- | --- |
| Salão Principal, painel aberto, conduítes e obra intacta | `Assets/Art/CC05/fundo_andar4.png`, `painel.png`, `cc05_damaged_conduits.asset`, `obra_apagada.png` |
| Céu da madrugada e amanhecer | `Assets/Art/CC05/cc05_window_sky_night.asset`, `cc05_window_sky_dawn.asset` |
| Segurança e sombra | `Assets/Art/CC01/GuardIdleFront_*.asset`, `GuardIdleBack_*.asset`; `Assets/Art/CC05/cc05_character_shadow.asset` |
| Rádio e visor | `Assets/Art/CC08/cc08_security_radio.asset`, `cc08_radio_screen.asset` |
| Entrada e caminho ao amanhecer | `Assets/Art/CC08/jardim_amanhecer_arquitetura.png`, `jardim_amanhecer_piso.png` |
| Reitor e curadoria | `Assets/Art/CC08/reitor_base_frente.png`, `curadora1_base_frente.png`, `curadora2_base_frente.png`; ciclo do reitor em `CC08_ReitorIdle_*.asset` |
| Tampa fechada do painel | Sprites próprios gerados em `Assets/Art/CC10/cc10_panel_closed_base.asset` e `cc10_panel_door.asset` |
| Interface | `Assets/Art/CC08/cc08_ui_white_pixel.asset`; fonte `Assets/Fonts/RobotoMono-Bold.ttf` |

## Som e fala

A cena reutiliza o zumbido elétrico baixo da CC05, a ambiência externa da CC08, o clique de confirmação e a trava da CC02, e o acento de xilofone e a pausa musical serena da CC04A. Não havia gravações de narrador ou curadoria específicas da CC10 entre os assets locais; cada fala tem legenda e um campo de áudio opcional em `CC10CutsceneController`.

## Construção e revisão

- Cena: `Assets/Scenes/CC10_A_Ultima_Sala_Ainda_no_Escuro.unity`.
- Gerador: `Assets/Scripts/Editor/CC10SceneBuilder.cs`.
- Controle da cena: `Assets/Scripts/CC10/CC10CutsceneController.cs`.
- Captura de revisão: `Assets/Scripts/Editor/CC10FrameCapture.cs`.
- A captura produz 901 quadros (15 segundos, incluindo o instante final, a 60 quadros por segundo) em `Library/CC10ReviewFrames/sequence`; são arquivos locais de revisão, fora da build.
