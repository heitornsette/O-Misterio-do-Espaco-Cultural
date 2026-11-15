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

## Cutscene CC03 — A galeria reorganizada

- Cena: `Assets/Scenes/CC03_A_Galeria_Reorganizada.unity`
- Duração: 9 segundos.
- Construção: no Unity, use **Misterio → Build CC03 - A galeria reorganizada**.

Ao concluir o puzzle das pinturas, a última obra se encaixa no nicho e a câmera recua para revelar a galeria completa. O segurança confere as telas, caminha até a porta do acervo e a abre; a transição mostra o corredor escuro e uma sombra distante sem revelar Moema. A cena inclui placas azuis, luar, legendas, música e efeitos de encaixe, passos, porta e ruído distante. `ControlsLocked` só é liberado ao final, e `onCutsceneFinished` está pronto para conectar o próximo andar, cuja fase jogável ainda não foi construída.

As três pinturas — litoral de Mucuripe, serra de Baturité e sertão — foram criadas como obras fictícias originais. Os fundos e as pinturas ficam em `Assets/Art/CC03`; seis efeitos e trechos musicais em WAV ficam em `Assets/Audio/CC03`. A cena reutiliza as animações do segurança, a fonte e a vinheta de CC01. Os espaços de voz permanecem prontos para gravação em `CC03CutsceneController`.

## Cutscene CC04 — Passagem silenciosa

- Cena: `Assets/Scenes/CC04_Passagem_Silenciosa.unity`
- Duração: 9 segundos.
- Construção: no Unity, use **Misterio → Build CC04 - Passagem silenciosa**.

Depois de recolher as peças do acervo sem assustar Moema, o segurança recua sem tocar nos cacos, passa por trás dela e segue para o elevador de serviço. Moema continua entretida com um objeto próximo; o segurança faz um gesto discreto de silêncio antes de entrar no elevador. As portas se fecham com Moema visível pela fresta e o indicador refletido no metal. A cena inclui legendas, luz e sombras do holofote, brilho sutil nos cacos, passos abafados, rangido leve e motor do elevador. `ControlsLocked` é liberado ao final e `onCutsceneFinished` permite ligar a sequência à próxima etapa.

O fundo do acervo e os efeitos em pixel art ficam em `Assets/Art/CC04`; os seis sons WAV ficam em `Assets/Audio/CC04`. A cena reutiliza as animações do segurança, o sprite de Moema, a fonte e a vinheta já presentes no projeto. Os campos de voz ficam prontos para gravação em `CC04CutsceneController`.

## Cutscene CC04A — Alerta de Moema

- Cena: `Assets/Scenes/CC04A_Alerta_de_Moema.unity`
- Duração: 4 segundos.
- Construção: no Unity, use **Misterio → Build CC04A - Alerta de Moema**.

No acervo do terceiro andar, uma tábua cede e chama a atenção de Moema. A cena mantém o segurança e Moema no mesmo enquadramento, deixa o holofote atravessar os dois e reserva uma pausa musical para a reação. Moema ergue o pescoço, recua e faz um pivô curto antes de trotar pela rota livre; a câmera abre o quadro e permanece parada, e o segurança não a persegue. O fade cobre o retorno ao estado inicial do puzzle. `ControlsLocked` permanece ativo durante a cutscene; `onAttemptRestartRequested` é o ponto de integração para reiniciar o puzzle quando essa etapa jogável existir.

O cenário do terceiro andar, a vitrine, as tábuas e os quadros de Moema vieram da pasta de assets compartilhada no Drive. Os arquivos de origem e a composição de referência estão arquivados em `Documentation/ArtSource/CC04A`; os sprites e efeitos usados pela cena ficam em `Assets/Art/CC04A`, e os quadros originais de Moema em `Assets/Art/Characters`. Os sete sons WAV — ambiente, pausa musical, tábua, penas, trote, xilofone e reinício — ficam em `Assets/Audio/CC04A`. As falas estão legendadas, com campos de voz prontos para gravação em `CC04ACutsceneController`.

Para revisar a animação quadro a quadro no Editor, use **Misterio → Review → Arm CC04A frame capture** e entre em Play Mode. A captura gera 241 imagens em `Library/CC04AReviewFrames/sequence`, de 0 a 4 segundos em passos de 1/60 s.

## Cutscene CC05 — A obra no escuro

- Cena: `Assets/Scenes/CC05_A_Obra_no_Escuro.unity`
- Duração: 10 segundos.
- Construção: no Unity, use **Misterio → Build CC05 - A obra no escuro**.

As portas do elevador se abrem para o salão principal do quarto andar. A lanterna encontra o quadro elétrico aberto; a obra central permanece apagada. Os conduítes interrompidos conduzem o olhar do painel até a basculante. Moema atravessa o fundo, sobe pela estrutura e desaparece pela janela sem animação ou som de voo. O céu clareia apenas nas vidraças. No último plano, o segurança se volta para o painel, pronto para iniciar a etapa do circuito.

O visor usa **Misterio.Cutscenes.CC05CutsceneController.SetGlobalClock(string)** para receber o horário atual da partida. A cena começa com `--:--` em pré-visualização e não cria, fixa nem avança um relógio próprio. `onCutsceneStarted` e `onCutsceneFinished` são os ganchos para pausar e retomar o tempo global e os temporizadores locais durante a transição. `PlayCutscene()` inicia a cena após a furtividade; o evento `onCutsceneFinished` pode ser ligado ao puzzle de energia quando ele estiver implementado.

Os fundos, painel, quadro de faíscas, obra apagada e faixas de céu foram selecionados do pacote do quarto andar compartilhado no Drive e ficam em `Assets/Art/CC05`. A cena mantém o ponto, o recorte e a transparência em pixel art. As animações existentes do segurança e de Moema são reutilizadas. O zumbido, os estalos e a trilha baixa são específicos desta cena; motor do elevador, passos, rangido e penas reutilizam os efeitos já presentes no projeto.

Para revisar o movimento quadro a quadro, use **Misterio → Review → Arm CC05 frame capture** e entre em Play Mode. A captura produz 601 imagens em `Library/CC05ReviewFrames/sequence`, de 0 a 10 segundos em passos de 1/60 s.

## Cutscene CC06 — A energia retorna

- Cena: `Assets/Scenes/CC06_A_Energia_Retorna.unity`
- Duração: 12 segundos.
- Construção: no Unity, use **Misterio → Build CC06 - A energia retorna**.

O close inicial mostra os dois terminais e o último trecho do conduíte se encaixando. A câmera então recua para manter o circuito e o salão legíveis enquanto um pulso azul e branco percorre a fiação. Os focos se acendem em sequência, a obra intacta ganha uma borda quente e a mariposa cruza o salão até a basculante. A animação do inseto usa escala e curva de voo contínuas, sem brilho mágico. A pausa final mantém a obra, o segurança e a fala de encerramento no mesmo enquadramento.

A cena reutiliza o quarto andar, o painel e a obra da CC05. O sprite da mariposa veio da pasta compartilhada no Drive e fica em `Assets/Art/Characters/Mariposa_Base.png`; focos, reflexo de piso, terminais e pulso são sprites pixelados próprios da cena em `Assets/Art/CC06`. `CC06CutsceneController.SetGlobalClock(string)` recebe a hora real da partida e a cena não a avança. Os eventos `onCutsceneStarted` e `onCutsceneFinished` permitem congelar e retomar o relógio e os temporizadores globais.

Para revisar a cena quadro a quadro, use **Misterio → Review → Arm CC06 frame capture** e entre em Play Mode. A captura produz 721 imagens em `Library/CC06ReviewFrames/sequence`, de 0 a 12 segundos em passos de 1/60 s. As falas estão legendadas e os campos de voz permanecem prontos para gravação.

## Arte e fontes

- Sprites do segurança, Moema e paleta: `Assets/Art/Characters` e `Assets/Art/Palette`.
- Fundo original da cena: `Documentation/ArtSource/foyer_night_master.png`; versão de jogo: `Assets/Art/Backgrounds/foyer_night_384x216.png`.
- Narrativa, storyboard e documentação: `Documentation/Source`.
- Projeto criado com Unity `6000.6.0f1`; pixel art usa filtro Point e compressão desligada.
