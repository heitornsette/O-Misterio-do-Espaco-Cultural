using System.Collections.Generic;
using EspacoCultural.Audio;
using EspacoCultural.Jogo;
using UnityEngine;

namespace EspacoCultural.Campus
{
    /// <summary>
    /// Biblioteca Central por dentro (uma tela): o fichário (procura por assunto → número de chamada) e
    /// quatro estantes por faixa de classificação. O catálogo da exposição "Mãos do Ceará" é o 709.8131 C357m.
    /// Sem consultar o fichário dá para chutar, mas há lombadas parecidas para confundir.
    /// </summary>
    public class BibliotecaInterior : CampusScene
    {
        protected override float WorldWidth => 640;
        protected override Rect WalkArea => new Rect(24, 8, 592, 86);
        protected override string DefaultSpawn => "bib_entrada";
        protected override Dictionary<string, Vector2> SpawnPoints => new Dictionary<string, Vector2>
        {
            { "bib_entrada", new Vector2(60, 40) },
        };
        protected override float GlobalIntensity => 0.62f;

        private const string CallNumber = "709.8131 C357m";
        private static readonly float[] ShelfX = { 222, 312, 490, 600 };
        private static readonly string[] ShelfRange = { "000 – 299", "300 – 599", "600 – 799", "800 – 999" };
        private const float CatalogCabinetX = 68;

        protected override void BuildWorld()
        {
            Background("fundo_int_bib", 0);
            PointLight("Luminaria", new Vector2(390, 180), 120, new Color(0.85f, 1f, 0.7f), 1.1f);
            PointLight("Janela1", new Vector2(250, 300), 200, new Color(0.6f, 0.7f, 1f), 0.6f);
            PointLight("Janela2", new Vector2(450, 300), 200, new Color(0.6f, 0.7f, 1f), 0.6f);

            AddSpot("Fichario", new Vector2(CatalogCabinetX, 90), () => "consultar o fichário", UseCabinet);
            for (int i = 0; i < 4; i++)
            {
                int k = i;
                AddSpot("Estante" + i, new Vector2(ShelfX[i], 90), () => $"estante {ShelfRange[k]}", () => UseShelf(k), null, 30);
            }
            AddSpot("Saida", new Vector2(36, 40), () => "sair da biblioteca", () => GoTo("Campus", "bib_porta"), null, 34);
        }

        private void UseCabinet()
        {
            var topics = new[] { "Arquitetura", "Arte cearense", "Botânica do sertão", "Folclore nordestino", "Música popular", "(fechar)" };
            var cards = new[]
            {
                "720.98131 B276a — Arquitetura de Fortaleza",
                CallNumber + " — Catálogo da exposição \"Mãos do Ceará\"",
                "581.98131 S491b — Plantas da caatinga",
                "398.098131 F668c — Contos e lendas do Ceará",
                "781.630981 L732s — Sanfona, zabumba e triângulo",
            };
            choice.Show("FICHÁRIO", "Escolha a gaveta do assunto:", topics, i =>
            {
                if (i < 0 || i >= cards.Length) return;
                GameAudio.Sfx("sfx_ajuste", 0.7f);
                GameState.Set("ficha_" + i);
                hud.Popup("FICHA — " + topics[i].ToUpper(), cards[i] + "\n\nO número de chamada indica a estante: as estantes vão por faixa (000–299, 300–599...).");
            });
        }

        private void UseShelf(int shelf)
        {
            if (GameState.Has("catalogo") || GameState.Flag("resolvido2")) { hud.Toast("Você já pegou o catálogo.", 1.5f); return; }
            string[] spines;
            int right = -1;
            switch (shelf)
            {
                case 2:
                    spines = new[]
                    {
                        "709.8131 C357a — Cerâmica do Cariri",
                        "709.81 C357m — Arte brasileira moderna",
                        CallNumber + " — Mãos do Ceará (catálogo)",
                        "759.8131 C357m — Pintura cearense",
                        "720.98131 B276a — Arquitetura de Fortaleza",
                        "(voltar)",
                    };
                    right = 2;
                    break;
                case 0: spines = new[] { "001.4 M592 — Metodologia científica", "028.5 L732 — Literatura infantil", "(voltar)" }; break;
                case 1: spines = new[] { "398.098131 F668c — Contos e lendas do Ceará", "581.98131 S491b — Plantas da caatinga", "(voltar)" }; break;
                default: spines = new[] { "869.3 Q4r — O Quinze", "981.31 H673 — História do Ceará", "(voltar)" }; break;
            }
            choice.Show($"ESTANTE {ShelfRange[shelf]}", "Lombadas com o número de chamada. Qual você puxa?", spines, i =>
            {
                if (i < 0 || i >= spines.Length - 1) return;
                if (i == right)
                {
                    GameState.Give("catalogo");
                    GameAudio.Sfx("sfx_pegar");
                    hud.Popup("CATÁLOGO \"MÃOS DO CEARÁ\"",
                        "Achou! O catálogo da exposição, com a ficha de cada obra: título, autoria e ano.\n\n" +
                        "Na galeria, as fichas dos quadros agora vão mostrar autor e ano. (I mostra o catálogo.)");
                }
                else hud.Toast("Não é esse livro. Confira o número de chamada no fichário.", 2.5f);
            });
        }
    }
}
