using System.Collections;
using System.Collections.Generic;
using EspacoCultural.Audio;
using EspacoCultural.Jogo;
using UnityEngine;
using UnityEngine.UI;

namespace EspacoCultural.Gameplay
{
    /// <summary>
    /// Andar 3 — Acervo: furtividade.
    /// - A Moema patrulha a fileira do meio; o chão que ela enxerga aparece destacado (produto escalar
    ///   para o cone + linha de visão por Bresenham bloqueada pelas vitrines).
    /// - Um holofote varre a frente do salão (colunas 4–8, fileiras 3–4): ser iluminado também assusta a Moema.
    /// - Tábuas soltas fazem barulho.
    /// - 4 peças estão no chão; a 5ª, a carranca, está no bico da Moema. Ela só a larga quando corre atrás
    ///   da bola de borracha (ginásio): E arremessa a bola na direção em que o guarda olha.
    /// Layout das 4 peças do chão verificado por BFS no espaço (célula, tick da patrulha, tick do holofote, peças):
    /// solução mínima ≈ 12,5 s. A carranca soma a parte da bola.
    ///   ..........TE.     T tábua  V vitrine  P peça  E elevador  S início
    ///   .........T...
    ///   .....P....P..
    ///   ....T..P..VVP
    ///   S.VV....TVV..
    /// </summary>
    public class Andar3Acervo : FloorBase
    {
        private static readonly Vector2Int[] VitrineLeft = { new Vector2Int(10, 3), new Vector2Int(2, 4), new Vector2Int(9, 4) };
        private static readonly Vector2Int[] Planks = { new Vector2Int(10, 0), new Vector2Int(9, 1), new Vector2Int(4, 3), new Vector2Int(8, 4) };
        private static readonly Vector2Int[] PieceCells = { new Vector2Int(5, 2), new Vector2Int(10, 2), new Vector2Int(7, 3), new Vector2Int(12, 3) };
        private static readonly string[] PieceTex = { "peca_vaso", "peca_vaso", "peca_vaso", "peca_vaso" };
        private static readonly Vector2Int[] Waypoints = { new Vector2Int(1, 2), new Vector2Int(11, 2) };
        private static readonly Vector2Int ExitCell = new Vector2Int(11, 0);
        private static readonly Vector2Int ServiceCell = new Vector2Int(0, 0);

        // Holofote: varre as colunas HoloMin..HoloMax nas fileiras 3–4, uma coluna a cada HoloTicks.
        private const int HoloMin = 4, HoloMax = 8, HoloTicks = 4;
        private const int DistractTicks = 24;   // 6 s bicando a bola
        private const int Total = 5;

        private readonly HashSet<Vector2Int> vitrineCells = new HashSet<Vector2Int>();
        private readonly Dictionary<Vector2Int, RawImage> plankImages = new Dictionary<Vector2Int, RawImage>();
        private readonly List<GridActor> pieces = new List<GridActor>();
        private readonly bool[] collected = new bool[4];
        private Image[,] visionTiles;
        private Image[] holoTiles;
        private RawImage holoLamp;

        private MoemaPatrol patrol;
        private GridActor moema;
        private int tick, holoTick;
        private float tickTimer;
        private MoemaPatrol.State current;
        private List<MoemaPatrol.State> distraction;
        private int distractIdx;
        private bool failing;

        // Carranca
        private bool moemaCarries;
        private bool carrancaCollected;
        private GridActor carrancaOnFloor;
        private RawImage carrancaHeld;
        private Vector2Int carrancaCell;

        // Bola
        private bool ballOut;
        private RawImage ballImg;

        private static readonly Color VisionColor = new Color(1f, 0.86f, 0.35f, 0.28f);
        private static readonly Color HoloColor = new Color(1f, 0.97f, 0.8f, 0.38f);

        protected override int FloorNumber => 3;
        protected override string BackgroundName => "fundo_andar3";
        protected override FloorGrid CreateGrid() => new FloorGrid(190, 5);
        protected override Vector2Int DefaultStart => new Vector2Int(0, 4);
        protected override Dictionary<string, (Vector2Int, Vector2Int)> SpawnPoints => new Dictionary<string, (Vector2Int, Vector2Int)>
        {
            { "andar3_entrada", (new Vector2Int(0, 4), Up) },
            { "andar3_elevador", (new Vector2Int(11, 1), Down) },
        };
        protected override string IntroTitle => "ANDAR 3 — ACERVO DE ARTEFATOS";
        protected override string IntroText =>
            "A Moema está patrulhando o acervo! O chão iluminado é o que ela enxerga; as vitrines bloqueiam a visão dela.\n\n" +
            "Recolha as 4 peças do chão sem ser visto. Evite as tábuas soltas e a luz do holofote.\n" +
            "A 5ª peça, a carranca, está no bico dela! Só uma distração faria a Moema largar... uma bola, talvez?\n\n" +
            "Com a bola: E arremessa na direção em que o guarda olha.";
        protected override string Controls => GameState.Has("bola")
            ? "SETAS/WASD andar (agachado)    E arremessar a bola    I itens    ESC pausa"
            : "SETAS/WASD andar (agachado)    pise nas peças para pegar    I itens    ESC pausa";

        private int CollectedCount
        {
            get { int n = carrancaCollected ? 1 : 0; foreach (var c in collected) if (c) n++; return n; }
        }

        protected override string ObjectiveText()
        {
            if (Solved) return "Peças recuperadas!\nO elevador de serviço liberou.";
            string t = $"Recolha as peças sem ser visto.\nPeças: {CollectedCount}/{Total}";
            if (!carrancaCollected) t += GameState.Has("bola") ? "\nCarranca: distraia a Moema (E)" : "\nCarranca: no bico da Moema";
            return t;
        }

        protected override void BuildLevel()
        {
            Player.walkFront = Px.Tex("guarda_furtivo_frente_sheet");
            Player.stepTime = 0.25f;
            Player.walkFps = 14f;

            AddStepExit(ServiceCell, null, GoDown);
            ExitSign(ServiceCell, "TÉRREO");
            AddStepExit(ExitCell, () => Solved, () =>
            {
                if (!GameState.Flag("visto_cc04")) { GameState.Set("visto_cc04"); GoTo(Misterio.Puzzles.RouteRules.Cutscene04); }
                else GoTo("Andar4", "andar4_elevador");
            });

            foreach (var v in VitrineLeft)
            {
                vitrineCells.Add(v);
                vitrineCells.Add(v + Vector2Int.right);
                var a = Spawn("Vitrine", "vitrine", v, 113);
                a.offset = new Vector2(Grid.cellW * 0.5f, 0);
                a.SetFeet(a.FeetPos);
            }

            if (AlreadySolved) return;   // acervo arrumado e a Moema já foi embora

            visionTiles = new Image[Grid.cols, Grid.rows];
            for (int x = 0; x < Grid.cols; x++)
                for (int y = 0; y < Grid.rows; y++)
                {
                    var p = Grid.TopLeft(new Vector2Int(x, y));
                    var img = Px.Box($"Visao_{x}_{y}", floorLayer, VisionColor, p.x + 1, p.y + 1, Grid.cellW - 2, Grid.cellH - 2);
                    img.enabled = false;
                    visionTiles[x, y] = img;
                }
            holoTiles = new Image[2];
            for (int i = 0; i < 2; i++) holoTiles[i] = Px.Box("Holofote_" + i, floorLayer, HoloColor, 0, 0, Grid.cellW - 2, Grid.cellH - 2);
            holoLamp = Px.Raw("LuzHolofote", fx, null, 0, 0, 1, 1);
            holoLamp.gameObject.SetActive(false);

            foreach (var c in Planks)
            {
                var p = Grid.TopLeft(c);
                Px.Box("TabuaDestaque", floorLayer, new Color(1f, 0.92f, 0.7f, 0.16f), p.x + 4, p.y + 4, Grid.cellW - 8, Grid.cellH - 8);
                plankImages[c] = Px.Raw("Tabua", floorLayer, Px.Tex("tabua_solta"), p.x + 8, p.y + 7, 32, 16);
            }

            for (int i = 0; i < PieceCells.Length; i++)
                pieces.Add(Spawn("Peca_" + i, PieceTex[i], PieceCells[i], 59));

            patrol = new MoemaPatrol(Waypoints, new[] { 0, 1 });
            moema = Spawn("Moema", "moema_idle_frente_sheet", Waypoints[0], 122, true, 128, 128);
            carrancaHeld = Px.Raw("CarrancaNoBico", world, Px.Tex("carranca"), 0, 0, 30, 30);
            ballImg = Px.Raw("Bola", fx, Jogo.Items.Icon("bola"), 0, 0, 12, 12);
            ballImg.gameObject.SetActive(false);
            ResetPuzzle(false);
        }

        private void ResetPuzzle(bool afterFail)
        {
            tick = 0;
            holoTick = 0;
            tickTimer = 0f;
            distraction = null;
            current = patrol.At(0);
            moema.Snap(current.cell);
            ApplyMoemaAnim(true);
            for (int i = 0; i < pieces.Count; i++)
            {
                collected[i] = false;
                pieces[i].gameObject.SetActive(true);
            }
            moemaCarries = true;
            carrancaCollected = false;
            if (carrancaOnFloor != null) { sortables.Remove(carrancaOnFloor); Destroy(carrancaOnFloor.gameObject); carrancaOnFloor = null; }
            ballOut = false;
            ballImg.gameObject.SetActive(false);
            foreach (var kv in plankImages) kv.Value.texture = Px.Tex("tabua_solta");
            if (afterFail) Player.Teleport(PlayerStart, Up);
            failing = false;
            Hud.SetObjective(ObjectiveText());
            UpdateVision();
        }

        public override bool IsBlocked(Vector2Int c) =>
            vitrineCells.Contains(c) || (moema != null && c == current.cell && !Solved) || (!Solved && c == ExitCell);

        public override void OnPlayerArrived(PlayerController p, Vector2Int c)
        {
            if (failing || Solved) { base.OnPlayerArrived(p, c); return; }
            if (plankImages.TryGetValue(c, out var plank))
            {
                plank.texture = Px.Tex("tabua_solta_pisada");
                GameAudio.Sfx("sfx_tabua");
                Fail("*CREC!* A tábua rangeu e a Moema ouviu!");
                return;
            }
            int idx = System.Array.IndexOf(PieceCells, c);
            bool got = false;
            if (idx >= 0 && !collected[idx]) { collected[idx] = true; pieces[idx].gameObject.SetActive(false); got = true; }
            if (carrancaOnFloor != null && c == carrancaCell)
            {
                carrancaCollected = true;
                sortables.Remove(carrancaOnFloor); Destroy(carrancaOnFloor.gameObject); carrancaOnFloor = null;
                got = true;
            }
            if (got)
            {
                GameAudio.Sfx("sfx_pegar");
                p.PlayOnce(Px.Tex("guarda_pegar_frente_sheet"), 20f);
                Hud.SetObjective(ObjectiveText());
                if (CollectedCount == Total)
                    MarkSolved("Peças recuperadas sem assustar a Moema!\nO elevador de serviço liberou.");
                else Hud.Toast($"Peça recolhida ({CollectedCount}/{Total}).", 1.5f);
            }
            base.OnPlayerArrived(p, c);
        }

        public override void Interact(PlayerController p, Vector2Int cell, Vector2Int facing)
        {
            if (Solved || failing || moema == null) return;
            if (!GameState.Has("bola")) { Hud.Toast("Se tivesse algo para distrair a Moema...", 2f); return; }
            if (ballOut) return;
            // A bola rola até 4 células na direção do olhar, parando antes de vitrine ou parede.
            var land = cell;
            for (int i = 0; i < 4; i++)
            {
                var n = land + facing;
                if (!Grid.Inside(n) || vitrineCells.Contains(n)) break;
                land = n;
            }
            if (land == cell) { Hud.Toast("Não dá para arremessar para esse lado.", 1.5f); return; }
            StartCoroutine(ThrowBall(cell, land));
        }

        private IEnumerator ThrowBall(Vector2Int from, Vector2Int land)
        {
            ballOut = true;
            GameAudio.Sfx("sfx_empurrar", 0.5f);
            ballImg.gameObject.SetActive(true);
            Vector2 a = Grid.Feet(from) + new Vector2(0, -20), b = Grid.Feet(land) + new Vector2(0, -6);
            for (float t = 0; t < 0.35f; t += Time.deltaTime)
            {
                float k = t / 0.35f;
                var pos = Vector2.Lerp(a, b, k) + new Vector2(0, -Mathf.Sin(k * Mathf.PI) * 18f);
                Px.Place(ballImg.rectTransform, Mathf.Round(pos.x - 6), Mathf.Round(pos.y - 6), 12, 12);
                yield return null;
            }
            Px.Place(ballImg.rectTransform, b.x - 6, b.y - 6, 12, 12);
            if (failing) yield break;
            StartDistraction(land);
        }

        /// <summary>A Moema larga a carranca, vai até a bola, bica por 6 s e volta para a patrulha.</summary>
        private void StartDistraction(Vector2Int ball)
        {
            var list = new List<MoemaPatrol.State>();
            var c = current.cell;
            int tx = Mathf.Clamp(ball.x, Waypoints[0].x, Waypoints[1].x);
            var dir = new Vector2Int(System.Math.Sign(tx - c.x), 0);
            var p = c;
            while (p.x != tx) { p += dir; for (int t = 0; t < MoemaPatrol.MoveTicks; t++) list.Add(new MoemaPatrol.State { cell = p, facing = dir, pecking = false }); }
            var face = new Vector2Int(0, ball.y >= 2 ? 1 : -1);
            for (int t = 0; t < DistractTicks; t++) list.Add(new MoemaPatrol.State { cell = p, facing = face, pecking = true });
            var back = -dir;
            while (p != c) { p += back; for (int t = 0; t < MoemaPatrol.MoveTicks; t++) list.Add(new MoemaPatrol.State { cell = p, facing = back, pecking = false }); }
            distraction = list;
            distractIdx = 0;

            if (moemaCarries)
            {
                moemaCarries = false;
                carrancaCell = c;
                carrancaOnFloor = Spawn("Carranca", "carranca", c, 59);
                Hud.Toast("A Moema largou a carranca e foi atrás da bola!", 2.5f);
            }
            else Hud.Toast("A Moema foi atrás da bola!", 2f);
            if (list.Count == DistractTicks) { /* já estava em cima: só bica */ }
            current = list[0];
            ApplyMoemaAnim(true);
        }

        protected override void Update()
        {
            base.Update();
            if (moema == null || failing || Solved || Hud.PopupOpen) return;
            if (InputLocked || Time.timeScale == 0f) return;

            tickTimer += Time.deltaTime;
            while (tickTimer >= MoemaPatrol.Tick)
            {
                tickTimer -= MoemaPatrol.Tick;
                holoTick++;
                MoemaPatrol.State next;
                if (distraction != null)
                {
                    distractIdx++;
                    if (distractIdx >= distraction.Count)
                    {
                        distraction = null;
                        next = patrol.At(tick);
                        OnDistractionEnded();
                    }
                    else next = distraction[distractIdx];
                }
                else { tick++; next = patrol.At(tick); }

                bool newCell = next.cell != current.cell;
                bool stateChanged = newCell || next.pecking != current.pecking || next.facing != current.facing;
                current = next;
                if (newCell) moema.MoveTo(current.cell, MoemaPatrol.Tick * MoemaPatrol.MoveTicks * 0.95f);
                if (stateChanged) ApplyMoemaAnim(false);
                UpdateVision();
            }

            var pc = Player.actor.cell;
            if (MoemaPatrol.Sees(current, pc, vitrineCells))
                Fail("A Moema te viu! Ela se assustou e espalhou tudo de novo.");
            else if (InHolo(pc))
                Fail("O holofote iluminou o guarda! A Moema se assustou com a sombra.");
        }

        private void OnDistractionEnded()
        {
            ballOut = false;
            ballImg.gameObject.SetActive(false);
            if (carrancaOnFloor != null && current.cell == carrancaCell)
            {
                // Voltou para onde largou e a carranca ainda estava lá: pega de novo.
                sortables.Remove(carrancaOnFloor); Destroy(carrancaOnFloor.gameObject); carrancaOnFloor = null;
                moemaCarries = true;
                Hud.Toast("A Moema voltou e pegou a carranca de novo!\nA bola rolou de volta: dá para tentar outra vez.", 3f);
            }
        }

        private int HoloColumn(int t)
        {
            int n = HoloMax - HoloMin, len = 2 * n;
            int k = (t / HoloTicks) % len;
            return k <= n ? HoloMin + k : HoloMin + (len - k);
        }

        private bool InHolo(Vector2Int c) => c.y >= 3 && c.x == HoloColumn(holoTick);

        protected override void LateUpdate()
        {
            base.LateUpdate();
            if (moema == null || Solved) return;
            if (carrancaHeld != null)
            {
                carrancaHeld.gameObject.SetActive(moemaCarries);
                if (moemaCarries)
                {
                    var f = moema.FeetPos;
                    bool left = moema.anim != null && moema.anim.FlipX;
                    Px.Place(carrancaHeld.rectTransform, f.x + (left ? -30 : 2), f.y - 36, 30, 30);
                    carrancaHeld.transform.SetSiblingIndex(moema.transform.GetSiblingIndex() + 1);
                }
            }
            if (holoTiles != null)
            {
                int col = HoloColumn(holoTick);
                for (int i = 0; i < 2; i++)
                {
                    var p = Grid.TopLeft(new Vector2Int(col, 3 + i));
                    Px.Place(holoTiles[i].rectTransform, p.x + 1, p.y + 1, Grid.cellW - 2, Grid.cellH - 2);
                }
            }
        }

        private void ApplyMoemaAnim(bool force)
        {
            if (current.pecking)
                moema.anim.Play(Px.Tex("moema_bicada_frente_sheet"), 8f, true, null, 0, force);
            else if (current.facing.y < 0)
                moema.anim.Play(Px.Tex("moema_andar_costas_sheet"), 12f, true, null, 0, force);
            else
                moema.anim.Play(Px.Tex("moema_andar_frente_sheet"), 12f, true, null, 0, force);
            if (current.facing.x != 0) moema.anim.FlipX = current.facing.x < 0;
        }

        private void UpdateVision()
        {
            if (visionTiles == null) return;
            for (int x = 0; x < Grid.cols; x++)
                for (int y = 0; y < Grid.rows; y++)
                {
                    var c = new Vector2Int(x, y);
                    bool v = c != current.cell && !vitrineCells.Contains(c) && MoemaPatrol.Sees(current, c, vitrineCells);
                    visionTiles[x, y].enabled = v;
                }
        }

        private void Fail(string message)
        {
            if (failing || Solved) return;
            failing = true;
            StartCoroutine(FailRoutine(message));
        }

        private IEnumerator FailRoutine(string message)
        {
            extraLock = true;
            moema.anim.FlipX = Player.actor.FeetPos.x < moema.FeetPos.x;
            moema.anim.Play(Px.Tex("moema_alerta_frente_sheet"), 8f, false, null, 0, true);
            GameAudio.Sfx("sfx_alerta");
            Hud.Toast(message, 1.6f);
            yield return new WaitForSeconds(1.4f);
            // CC04A (alerta da Moema) e volta ao acervo do começo, sem repetir a introdução.
            SkipNextIntro = true;
            GoTo(Misterio.Puzzles.RouteRules.Cutscene04A, "andar3_entrada");
        }
    }
}
