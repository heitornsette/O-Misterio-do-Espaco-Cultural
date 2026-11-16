# CC08 — fontes de arte e áudio

## Arte

Os assets do jardim foram selecionados do pacote 2D editável compartilhado no Google Drive, em `EspacoCultural2D/Assets`: `Jardim_Amanhecer_10_Arquitetura.png` e `Jardim_Amanhecer_20_Piso.png` são as camadas usadas na cena; `Jardim_Amanhecer_00_Cenario_Completo.png` fica arquivada como referência de composição. O README do pacote informa que o jardim é uma placa-conceito criada para preencher um cenário que não existia no projeto recebido. A importação mantém a pixel art em Filter Mode Point, sem compressão e a 64 pixels por unidade.

O fundo do saguão foi copiado para `Assets/Art/CC08/foyer_amanhecer_base.png` para ajustar seu PPU para 64 apenas nesta cena, sem alterar a escala que as cenas anteriores usam. Os reflexos e as folhas de vidro que deslizam foram construídos a partir da paleta do fundo. Uma faixa suave de luz âmbar no piso e uma luz ambiente quente, atrás das legendas, conectam o interior à manhã sem apagar as lâmpadas azuis do cenário.

Os sprites do reitor e das curadoras vieram das pastas `Personagem/NPCs/reitor` e `Personagem/NPCs/curadora1` / `curadora2` do Drive compartilhado. A sequência de quatro quadros de idle do reitor e a de voo da mariposa vieram das subpastas `animacoes`. A cena usa ainda os quadros originais de idle do segurança e de Moema já presentes no projeto.

## Áudio

`cc08_ambiencia_exterior.wav` e `cc08_ambiencia_saguão.wav` foram extraídos do `R04_Pacote_Assets_validado_v02.zip`, subpasta `Audio`. O arquivo de cues do pacote descreve a ambiência externa como ar exterior com ruído distante da cidade; na CC08 ela fica baixa, sob as passadas e o tema de xilofone. O saguão usa o hum sintetizado para o teaser. Passos, abertura, clique do rádio, asas e a nota final reutilizam sons já importados pelas cutscenes anteriores.

O espaço dos clipes de narração e diálogo permanece pronto em `CC08CutsceneController`; as legendas reproduzem o roteiro e aceitam as gravações quando forem fornecidas.
