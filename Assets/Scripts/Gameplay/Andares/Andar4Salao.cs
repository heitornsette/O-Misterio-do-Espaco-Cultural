using System.Collections;
using System.Collections.Generic;
using EspacoCultural.Audio;
using EspacoCultural.Jogo;
using UnityEngine;
using UnityEngine.UI;

namespace EspacoCultural.Gameplay
{
    /// <summary>
    /// Andar 4 — Salão Principal: religar a energia no painel de conduítes antes das 06:00.
    /// O salão começa no breu; o painel solta faíscas. Fique embaixo do painel, olhe para cima e aperte E.
    /// O painel tem um encaixe vazio: o fusível de 25 A do circuito EC-4 está no quadro de luz do ginásio.
    /// Ao fechar o circuito: as luzes voltam, a obra principal acende e o céu amanhece na janela → CC06.
    /// O elevador (coluna 3, olhando para cima) leva de volta ao térreo.
    /// </summary>
    public class Andar4Salao : FloorBase
    {
        private const int PanelX = 448, PanelY = 124;
        private const int PanelColumn = 9;
        private static readonly Vector2Int Elevator = new Vector2Int(3, 0);

        private RawImage panelImg;
        private SheetAnimator sparks;
        private RawImage obra;
        private RawImage dawn;
        private Image darkness;
        private ConduitBoard board;
        private bool ending;

        protected override int FloorNumber => 4;
        protected override string BackgroundName => "fundo_andar4";
        protected override FloorGrid CreateGrid() => new FloorGrid(200, 5);
        protected override Vector2Int DefaultStart => new Vector2Int(3, 0);
        protected override Dictionary<string, (Vector2Int, Vector2Int)> SpawnPoints => new Dictionary<string, (Vector2Int, Vector2Int)>
        {
            { "andar4_elevador", (new Vector2Int(3, 0), Down) },
        };
        protected override string IntroTitle => "ANDAR 4 — SALÃO PRINCIPAL";
        protected override string IntroText =>
            "Breu total. A Moema mordeu um cabo solto e o painel elétrico (circuito EC-4) está soltando faíscas.\n\n" +
            "Vá até o painel (parede à direita), olhe para cima e aperte E. " +
            "Gire os conduítes até ligar TODAS as peças, sem pontas soltas, às duas lâmpadas.\n" +
            "O fusível queimou: o reserva fica no quadro de luz do ginásio.";

        protected override string ObjectiveText()
        {
            if (Solved) return "Energia restaurada!";
            if (!GameState.A4Fuse && GameState.FuseCarried() != ConduitBoard.FuseAmps)
                return "Religue a energia antes das 06:00.\nFalta o fusível de 25 A (ginásio).";
            return "Religue a energia no painel elétrico\nantes das 06:00.";
        }

        protected override void BuildLevel()
        {
            obra = Px.Raw("ObraPrincipal", floorLayer, Px.Tex(AlreadySolved ? "obra_acesa" : "obra_apagada"), 256, 68, 128, 128);

            panelImg = Px.Raw("PainelEletrico", floorLayer, Px.Tex(AlreadySolved ? "painel" : "painel_faiscas_sheet"), PanelX, PanelY, 62, 71);
            if (!AlreadySolved)
            {
                sparks = panelImg.gameObject.AddComponent<SheetAnimator>();
                sparks.Play(Px.Tex("painel_faiscas_sheet"), 10f, true, null, 3);
            }

            darkness = Px.Box("Breu", fx, new Color(0.02f, 0.03f, 0.08f, AlreadySolved ? 0f : 0.55f), 0, 0, 640, 360);
            darkness.transform.SetAsFirstSibling();

            board = new ConduitBoard();
            board.Build(fx, fontTitle, fontBody);
            board.Completed += OnCircuitClosed;
            board.Closed += () => { extraLock = false; Hud.SetObjective(ObjectiveText()); };
            if (AlreadySolved) dawn.color = Color.white;
        }

        protected override void BuildBackdrop(RectTransform parent)
        {
            Px.Raw("CeuNoite", parent, Px.Tex("ceu_noite"), 326, 58, 220, 70);
            dawn = Px.Raw("CeuAmanhecer", parent, Px.Tex("ceu_amanhecer"), 326, 58, 220, 70);
            dawn.color = new Color(1, 1, 1, 0);
        }

        protected override string ContextHint(Vector2Int cell, Vector2Int facing)
        {
            if (cell == Elevator) return facing == Up ? "E descer de elevador até o térreo" : "Aperte CIMA para usar o elevador.";
            if (Solved) return null;
            if (cell.y == 0 && cell.x == PanelColumn) return facing == Up ? "E abrir o painel elétrico" : "Aperte CIMA para olhar o painel.";
            return null;
        }

        public override void Interact(PlayerController p, Vector2Int cell, Vector2Int facing)
        {
            if (cell == Elevator && facing == Up) { GoDown(); return; }
            if (Solved || cell.y != 0 || cell.x != PanelColumn || facing != Up) return;
            extraLock = true;
            p.PlayOnce(Px.Tex("guarda_painel_costas_sheet"), 18f, OpenPanel, true);
        }

        private void OpenPanel()
        {
            if (!board.FuseInstalled)
            {
                int amps = GameState.FuseCarried();
                if (amps == ConduitBoard.FuseAmps)
                {
                    GameState.Take("fusivel_" + amps);
                    board.InstallFuse();
                    GameAudio.Sfx("sfx_encaixe");
                    Hud.Toast("Fusível de 25 A instalado no encaixe!", 2.5f);
                }
                else if (amps > 0)
                {
                    GameAudio.Sfx("sfx_erro");
                    Hud.Toast($"Esse fusível é de {amps} A.\nA etiqueta do circuito EC-4 pede 25 A.", 3.5f);
                }
                else if (!GameState.Flag("viu_painel"))
                {
                    Hud.Toast("O fusível queimou. Reservas: quadro de luz do ginásio.", 3f);
                }
                GameState.Set("viu_painel");
            }
            board.Show();
        }

        protected override void Update()
        {
            base.Update();
            if (board != null && board.Open && Time.timeScale > 0f) board.Tick();
        }

        private void OnCircuitClosed()
        {
            if (ending) return;
            ending = true;
            StartCoroutine(EndingRoutine());
        }

        private IEnumerator EndingRoutine()
        {
            extraLock = true;
            yield return new WaitForSeconds(0.8f);
            board.Hide();
            extraLock = true;
            MarkSolved("O circuito fechou! As luzes voltaram.");

            GameAudio.Sfx("sfx_energia");
            if (sparks != null) sparks.enabled = false;
            panelImg.texture = Px.Tex("painel");
            panelImg.uvRect = new Rect(0, 0, 1, 1);
            obra.texture = Px.Tex("obra_acesa");

            float t = 0f;
            Color c0 = darkness.color;
            while (t < 2f)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / 2f);
                darkness.color = new Color(c0.r, c0.g, c0.b, Mathf.Lerp(c0.a, 0f, k));
                dawn.color = new Color(1, 1, 1, k);
                yield return null;
            }
            yield return new WaitForSeconds(1.5f);
            GoTo(Misterio.Puzzles.RouteRules.Cutscene06);   // CC06 → CC07 → CC08 → créditos
        }
    }
}
