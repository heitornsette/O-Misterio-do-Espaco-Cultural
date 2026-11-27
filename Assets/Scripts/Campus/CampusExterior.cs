using System.Collections;
using System.Collections.Generic;
using EspacoCultural.Audio;
using EspacoCultural.Jogo;
using UnityEngine;

namespace EspacoCultural.Campus
{
    /// <summary>
    /// Cena "Campus": a faixa externa da Unifor, com rolagem lateral (3.200 px = 5 telas):
    ///   [Espaço Cultural] [Centro de Convivência] [Praça da fonte] [Biblioteca Central] [Ginásio]
    /// Cada tela é um fundo pintado (PixelLab, 640×360). Palmeiras em primeiro plano escondem as emendas
    /// e passam com parallax. Postes, portas e janelas têm Light 2D; o guarda leva a lanterna.
    /// Interações: porta de serviço do museu, entrada do CC, fonte + registro (estátua do Vaqueiro),
    /// vigia da biblioteca + porta, entrada do ginásio e as pistas opcionais da Moema.
    /// </summary>
    public class CampusExterior : CampusScene
    {
        private const float EC = 0, CC = 640, PR = 1280, BI = 1920, GI = 2560;

        protected override float WorldWidth => 3200;
        protected override Rect WalkArea => new Rect(24, 8, 3200 - 48, 52);
        protected override string DefaultSpawn => "ec_porta";
        protected override Dictionary<string, Vector2> SpawnPoints => new Dictionary<string, Vector2>
        {
            { "ec_porta", new Vector2(EC + 175, 52) },
            { "cc_porta", new Vector2(CC + 320, 50) },
            { "bib_porta", new Vector2(BI + 318, 50) },
            { "gin_porta", new Vector2(GI + GinDoorX, 50) },
        };

        // Posições que dependem da arte da praça e do ginásio (px dentro da tela de 640).
        private const float FountainX = 322, FountainY = 60, FountainW = 300;
        private const float ValveX = 455;

        // Fonte e bancos da praça: o guarda passa pela frente.
        protected override IEnumerable<Rect> Blockers => new[]
        {
            new Rect(PR + 145, 36, 356, 60),
            new Rect(PR + 78, 40, 64, 40), new Rect(PR + 498, 40, 64, 40),
        };
        private const float GinDoorX = 320;

        private readonly List<SpriteRenderer> jets = new List<SpriteRenderer>();
        private Sprite[] jetFrames;
        private SpriteRenderer vaqueiroInFountain;
        private SpriteRenderer vigia;

        protected override void BuildWorld()
        {
            Background("fundo_ext_ec", EC);
            Background("fundo_ext_cc", CC);
            Background("fundo_ext_praca", PR);
            Background("fundo_ext_bib", BI);
            Background("fundo_ext_gin", GI);

            // Palmeiras em primeiro plano nas emendas (parallax 1.25: passam mais rápido que o cenário)
            foreach (float x in new[] { CC, PR, BI, GI })
            {
                var palm = Prop("Palmeira", "palmeira_frente", new Vector2(x, -6), -1, false);
                if (palm == null) continue;
                palm.sortingOrder = 2500;
                palm.color = new Color(0.55f, 0.6f, 0.75f);
                Parallax(palm.transform, 1.25f);
            }

            // ---- Luzes ----
            PointLight("Poste_EC", new Vector2(EC + 625, 122), 130, LampWarm, 1.2f);
            PointLight("PortaServico", new Vector2(EC + 175, 92), 80, WindowWarm, 1.0f);
            PointLight("Marquise_CC", new Vector2(CC + 320, 115), 170, WindowWarm, 0.9f);
            PointLight("Poste_CC", new Vector2(CC + 603, 127), 130, LampWarm, 1.2f);
            PointLight("Poste_Praca1", new Vector2(PR + 28, 140), 120, LampWarm, 1.1f);
            PointLight("Poste_Praca2", new Vector2(PR + 623, 140), 120, LampWarm, 1.1f);
            PointLight("Registro", new Vector2(PR + 455, 110), 50, CoolWhite, 0.5f);
            PointLight("Poste_Bib1", new Vector2(BI + 14, 150), 130, LampWarm, 1.2f);
            PointLight("Poste_Bib2", new Vector2(BI + 625, 150), 130, LampWarm, 1.2f);
            PointLight("Janelas_Bib", new Vector2(BI + 318, 200), 170, WindowWarm, 0.6f);
            PointLight("Entrada_Gin", new Vector2(GI + GinDoorX, 110), 150, WindowWarm, 0.9f);

            // ---- Espaço Cultural: porta de serviço ----
            AddSpot("PortaServico", new Vector2(EC + 175, 60), () => "entrar no Espaço Cultural (porta de serviço)",
                () => GoTo("Andar1", "terreo_porta"));

            // ---- Centro de Convivência ----
            AddSpot("EntradaCC", new Vector2(CC + 320, 60), () => "entrar no Centro de Convivência",
                () => GoTo("CC_Interior", "cc_entrada"));

            // ---- Praça: fonte, estátua e registro ----
            BuildFountain();
            AddSpot("Fonte", new Vector2(PR + FountainX, 28), () =>
                    vaqueiroInFountain != null && vaqueiroInFountain.enabled ? "olhar a estátua na fonte" : "olhar a fonte",
                UseFountain, null, 90);
            AddSpot("Registro", new Vector2(PR + ValveX, 34), () => "registro da fonte (no muro)", UseValve, null, 40);
            Prop("Pegada", "pegada", new Vector2(PR + 132, 22), -1, false);
            Prop("Pegada", "pegada", new Vector2(PR + 108, 30), -1, false);
            Prop("Pegada", "pegada", new Vector2(PR + 84, 24), -1, false);
            AddSpot("Pegadas", new Vector2(PR + 108, 26), () => "pegadas molhadas no chão",
                () => Clue("pegadas", "Pegadas molhadas de três dedos saem da fonte em direção ao Espaço Cultural. " +
                                      "Seja lá quem for, passou por aqui depois de mexer na estátua."), null, 28);

            // ---- Biblioteca: vigia e porta ----
            vigia = Prop("Vigia_Raimundo", "vigia_idle", new Vector2(BI + 236, 52), 126);
            if (vigia == null)
            {
                var frames = CampusArt.Sheet("guarda_idle_frente_sheet", 113);
                vigia = PropSprite("Vigia_Raimundo", frames.Length > 0 ? frames[0] : null, new Vector2(BI + 236, 52));
                vigia.flipX = true;
                vigia.color = new Color(0.85f, 0.85f, 0.95f);
            }
            AddSpot("Vigia", new Vector2(BI + 236, 52), () => "conversar com o Seu Raimundo (vigia)", TalkVigia);
            AddSpot("PortaBib", new Vector2(BI + 318, 60), () => "entrar na Biblioteca Central", () =>
            {
                if (GameState.Flag("biblioteca_aberta")) GoTo("Biblioteca", "bib_entrada");
                else hud.Toast("Trancada. O Seu Raimundo, o vigia, deve ter a chave.", 2.5f);
            });
            Prop("Pena", "pena", new Vector2(BI + 470, 20), -1, false);
            AddSpot("Pena", new Vector2(BI + 478, 24), () => "uma pena cinza no chão",
                () => Clue("pena", "Uma pena cinza, longa e macia, perto dos vasos da biblioteca. " +
                                   "A mesma cor da que estava junto à porta de serviço do museu."), null, 26);

            // ---- Ginásio ----
            AddSpot("PortaGin", new Vector2(GI + GinDoorX, 60), () => "entrar no Ginásio Poliesportivo",
                () => GoTo("Ginasio", "gin_entrada"));

            // ---- Bordas ----
            AddSpot("BordaEsq", new Vector2(30, 40), () => "o campus continua... mas a ronda é por aqui", () => { }, null, 30);
            AddSpot("BordaDir", new Vector2(3170, 40), () => "o campus continua... mas a ronda é por aqui", () => { }, null, 30);
        }

        protected override void OnEntered()
        {
            if (GameState.Flag("radio")) return;
            GameState.Set("radio");
            SayThen("RÁDIO", () => hud.SetObjective(Objective()),
                "<color=#8EC1DA>Central:</color> Vigia do Espaço Cultural, na escuta? Câmbio.",
                "<color=#8EC1DA>Central:</color> O salão principal caiu de novo, circuito EC-4. Os fusíveis reserva " +
                "ficam no quadro de luz do ginásio.",
                "<color=#8EC1DA>Central:</color> A zeladora está no Centro de Convivência e o Seu Raimundo cuida da " +
                "biblioteca. Qualquer coisa, fala com eles. Câmbio, desligo.",
                "<color=#F4C98A>Guarda:</color> Entendido. ...E que pegadas são essas na praça?");
        }

        // ------------------------------------------------------------
        // Fonte

        private void BuildFountain()
        {
            jetFrames = new Sprite[3];
            for (int f = 0; f < 3; f++) jetFrames[f] = CampusArt.FromTexture(JetTexture(f), new Vector2(5, 0));
            bool on = !GameState.Flag("fonte_desligada");
            float left = PR + FountainX - FountainW * 0.5f + 20;
            for (int i = 0; i < 5; i++)
            {
                var j = PropSprite("Jato" + i, jetFrames[i % 3], new Vector2(left + i * (FountainW - 40) / 4f, FountainY + 8));
                j.sortingOrder = Walker.SortOrder(FountainY) - 1;
                j.enabled = on;
                jets.Add(j);
            }
            if (on) StartCoroutine(AnimateJets());

            if (!GameState.Has("vaqueiro") && !GameState.Flag("vaqueiro_devolvido"))
            {
                vaqueiroInFountain = Prop("VaqueiroNaFonte", "estatua_vaqueiro", new Vector2(PR + FountainX + 6, FountainY + 2), 113);
                if (vaqueiroInFountain != null) vaqueiroInFountain.sortingOrder = Walker.SortOrder(FountainY);
            }
        }

        private IEnumerator AnimateJets()
        {
            int f = 0;
            while (jets.Count > 0 && jets[0] != null && jets[0].enabled)
            {
                for (int i = 0; i < jets.Count; i++) jets[i].sprite = jetFrames[(f + i) % 3];
                f++;
                yield return new WaitForSeconds(0.14f);
            }
        }

        /// <summary>Jato d'água em pixel art (10×26), 3 quadros.</summary>
        private static Texture2D JetTexture(int frame)
        {
            int w = 10, h = 26;
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var clear = new Color(0, 0, 0, 0);
            var c1 = new Color32(0xe9, 0xf8, 0xfd, 230); var c2 = new Color32(0x8e, 0xc1, 0xda, 220); var c3 = new Color32(0x52, 0x76, 0x9a, 200);
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) t.SetPixel(x, y, clear);
            int top = h - 3 - frame;
            for (int y = 0; y < top; y++) { t.SetPixel(4, y, c2); t.SetPixel(5, y, c1); }
            for (int x = 2; x <= 7; x++) t.SetPixel(x, top, c1);
            t.SetPixel(1, top - 1, c2); t.SetPixel(8, top - 1, c2);
            t.SetPixel(1 + frame % 2, top - 4, c3); t.SetPixel(8 - frame % 2, top - 6, c3);
            t.SetPixel(3, 1, c3); t.SetPixel(6, 1, c3); t.SetPixel(2, 0, c3); t.SetPixel(7, 0, c3);
            t.Apply();
            return t;
        }

        private void UseFountain()
        {
            if (vaqueiroInFountain == null || !vaqueiroInFountain.enabled)
            {
                hud.Toast("A fonte da praça. A água está parada.", 2f);
                return;
            }
            if (!GameState.Flag("fonte_desligada"))
            {
                Say("FONTE",
                    "A estátua do Vaqueiro está no meio da fonte, cercada pelos jatos d'água!\n" +
                    "Pegadas de três dedos em volta... Como ela veio parar aqui?",
                    "Pegar agora seria encharcar a madeira. Melhor fechar o registro da fonte primeiro.");
                return;
            }
            vaqueiroInFountain.enabled = false;
            GameState.Give("vaqueiro");
            GameAudio.Sfx("sfx_pegar");
            hud.Popup("ESTÁTUA DO VAQUEIRO", "Você pegou a estátua do Vaqueiro. Ela está molhada, mas inteira.\n\n" +
                "O lugar dela é a marcação do térreo do Espaço Cultural.");
        }

        private void UseValve()
        {
            if (GameState.Flag("fonte_desligada")) { hud.Toast("O registro está fechado.", 1.5f); return; }
            if (!GameState.Has("chave_registro"))
            {
                Say("REGISTRO DA FONTE",
                    "Uma caixinha de metal com o registro da fonte. Está trancada com cadeado.\n" +
                    "A zeladora do turno da noite deve ter a chave.");
                if (!GameState.Flag("pista_bico"))
                    Clue("bico", "Marcas de bico no cadeado! Alguém bem curioso tentou abrir isso antes de você.");
                return;
            }
            GameState.Set("fonte_desligada");
            GameState.Take("chave_registro");
            foreach (var j in jets) j.enabled = false;
            GameAudio.Sfx("sfx_encaixe");
            hud.Toast("Você abriu o cadeado e fechou o registro.\nOs jatos da fonte pararam.", 3f);
        }

        // ------------------------------------------------------------
        // Vigia da biblioteca

        private void TalkVigia()
        {
            if (!GameState.Flag("biblioteca_aberta"))
            {
                SayThen("SEU RAIMUNDO (VIGIA)", () => GameState.Set("biblioteca_aberta"),
                    "Opa! Também de ronda? Por aqui tudo calmo... tirando uma ave enorme que passou correndo atrás de uma mariposa.",
                    "Precisa da biblioteca a essa hora? Pode entrar, eu destranco. Só não bagunça o fichário!");
                return;
            }
            if (!GameState.Has("catalogo") && GameState.Flag("intro2"))
                Say("SEU RAIMUNDO (VIGIA)", "Catálogo da exposição? Procura no fichário, por assunto. Ele dá o número da estante.");
            else
                Say("SEU RAIMUNDO (VIGIA)", "Boa ronda! E cuidado com aquela ave, viu?");
        }
    }
}
