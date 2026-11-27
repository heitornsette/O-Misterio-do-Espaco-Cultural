using System.Collections.Generic;
using EspacoCultural.Audio;
using EspacoCultural.Jogo;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EspacoCultural.Gameplay
{
    /// <summary>
    /// Andar 1 — Térreo: Sokoban das estátuas identificadas.
    /// Cada estátua tem a SUA marcação (o nome está na fita). O andar é uma matriz 2D (statueAt[x,y]);
    /// cada passo vai para uma pilha (Z desfaz) e R recomeça. Pedestais fixos atrapalham o caminho.
    /// A estátua do Vaqueiro não está aqui: a Moema a arrastou até a fonte do CC (campus).
    /// Quando o guarda volta com ela, a estátua reaparece no lugar de início.
    /// Saídas: porta de serviço (0,0) → campus; escada (11,0)/(12,0) → galeria, depois de resolvido.
    /// Mapa (13×5): # pedestal, A–D estátuas, a–d marcações. Solução mínima verificada por BFS: 21 empurrões.
    ///   .............
    ///   ....a.....#..
    ///   .....BA..#d..
    ///   .b.D#..Cc....
    ///   .............
    /// </summary>
    public class Andar1Sokoban : FloorBase
    {
        private static readonly Vector2Int[] Pedestals = { new Vector2Int(10, 1), new Vector2Int(9, 2), new Vector2Int(4, 3) };
        private static readonly Vector2Int[] StatueStart = { new Vector2Int(6, 2), new Vector2Int(5, 2), new Vector2Int(7, 3), new Vector2Int(3, 3) };
        private static readonly Vector2Int[] Targets = { new Vector2Int(4, 1), new Vector2Int(1, 3), new Vector2Int(8, 3), new Vector2Int(10, 2) };
        private static readonly string[] StatueTex = { "estatua_pescador", "estatua_rendeira", "estatua_sanfoneiro", "estatua_vaqueiro" };
        private static readonly string[] StatueName = { "PESCADOR", "RENDEIRA", "SANFONEIRO", "VAQUEIRO" };
        private const int Vaqueiro = 3;

        private static readonly Vector2Int Door = new Vector2Int(0, 0);
        private static readonly Vector2Int Stairs = new Vector2Int(11, 0);

        private struct Move
        {
            public Vector2Int playerFrom, playerFacing;
            public int statue;          // -1 se não empurrou nada
            public Vector2Int statueFrom;
        }

        private readonly GridActor[] statues = new GridActor[4];
        private readonly bool[] present = new bool[4];
        private readonly RawImage[] markers = new RawImage[4];
        private readonly HashSet<Vector2Int> pedestalCells = new HashSet<Vector2Int>();
        private readonly Stack<Move> history = new Stack<Move>();
        private int[,] statueAt;
        private GridActor barrier;
        private bool warnedDeadlock;

        protected override int FloorNumber => 1;
        protected override string BackgroundName => "fundo_andar1";
        protected override FloorGrid CreateGrid() => new FloorGrid(190, 5);
        protected override Vector2Int DefaultStart => new Vector2Int(0, 1);
        protected override Dictionary<string, (Vector2Int, Vector2Int)> SpawnPoints => new Dictionary<string, (Vector2Int, Vector2Int)>
        {
            { "terreo_porta", (new Vector2Int(0, 1), Down) },
            { "terreo_escada", (new Vector2Int(11, 1), Down) },
        };
        protected override string IntroTitle => "ANDAR 1 — TÉRREO";
        protected override string IntroText =>
            "As estátuas de madeira foram arrastadas para fora das marcações. Cada fita tem o nome da sua estátua.\n\n" +
            "Empurre cada uma até a SUA marcação. Os pedestais não saem do lugar.\n" +
            "Falta uma: as pegadas de três dedos saem pela porta de serviço, à esquerda...\n\n" +
            "SETAS/WASD empurrar   Z desfazer   R recomeçar   I itens";
        protected override string Controls => "SETAS/WASD andar/empurrar    Z desfazer    R recomeçar    I itens    ESC pausa";

        protected override string ObjectiveText()
        {
            if (Solved) return "Escada liberada!\nSuba pela escada (canto direito).";
            string s = $"Leve cada estátua à sua marcação.\nNo lugar: {OnTargetCount()}/4";
            if (!present[Vaqueiro]) s += "\nFalta o Vaqueiro (porta de serviço)";
            return s;
        }

        // ------------------------------------------------------------

        protected override void BuildLevel()
        {
            statueAt = new int[Grid.cols, Grid.rows];
            for (int x = 0; x < Grid.cols; x++) for (int y = 0; y < Grid.rows; y++) statueAt[x, y] = -1;

            foreach (var c in Pedestals)
            {
                pedestalCells.Add(c);
                Spawn("Pedestal", "pedestal", c, 52);
            }

            for (int i = 0; i < 4; i++)
            {
                markers[i] = FloorTile("Marcacao_" + StatueName[i], "marcacao", Targets[i]);
                var tl = Grid.TopLeft(Targets[i]);
                var lbl = Px.Label("Nome", markers[i].rectTransform, fontBody, 8, new Color(0.08f, 0.1f, 0.2f, 0.9f), 0, 9, Grid.cellW, 12, TextAnchor.UpperCenter);
                lbl.text = StatueName[i].Length > 7 ? StatueName[i].Substring(0, 5) + "." : StatueName[i];
            }

            // Vaqueiro: só aparece depois de trazido da fonte do CC.
            if (!GameState.Flag("vaqueiro_devolvido") && GameState.Has("vaqueiro"))
            {
                GameState.Take("vaqueiro");
                GameState.Set("vaqueiro_devolvido");
                Hud.Toast("Você trouxe a estátua do Vaqueiro de volta ao térreo!", 3f);
            }
            bool vaqueiroHere = GameState.Flag("vaqueiro_devolvido");

            int[] saved = GameState.A1Statues;
            for (int i = 0; i < 4; i++)
            {
                present[i] = i != Vaqueiro || vaqueiroHere;
                Vector2Int cell = StatueStart[i];
                if (AlreadySolved) cell = Targets[i];
                else if (saved != null && saved[i] >= 0) cell = Cell(saved[i]);
                if (present[i] && statueAt[cell.x, cell.y] >= 0) cell = FreeCellNear(StatueStart[i]);
                statues[i] = Spawn("Estatua_" + StatueName[i], StatueTex[i], cell, 113);
                statues[i].gameObject.SetActive(present[i]);
                if (present[i]) statueAt[cell.x, cell.y] = i;
            }

            barrier = Spawn("Corda_Escada", "bloqueio_escada", Stairs, 113);
            if (Solved) { sortables.Remove(barrier); Destroy(barrier.gameObject); }

            AddStepExit(Door, null, () => GoTo(CampusScene, "ec_porta"));
            System.Action up = () =>
            {
                if (!GameState.Flag("visto_cc02")) { GameState.Set("visto_cc02"); GoTo(Misterio.Puzzles.RouteRules.Cutscene02); }
                else GoTo("Andar2", "andar2_entrada");
            };
            AddStepExit(Stairs, () => Solved, up);
            AddStepExit(Stairs + Vector2Int.right, () => Solved, up);

            // Pistas da Moema (decoração): pegadas indo para a porta de serviço
            Px.Raw("Pegada", floorLayer, Px.Tex("pegada"), 120, 300);
            Px.Raw("Pegada", floorLayer, Px.Tex("pegada"), 92, 284);
            Px.Raw("Pegada", floorLayer, Px.Tex("pegada"), 64, 266);
            Px.Raw("Pegada", floorLayer, Px.Tex("pegada"), 40, 240);
            Px.Raw("Pena", floorLayer, Px.Tex("pena"), 46, 196);

            RefreshMarkers();
        }

        private Vector2Int FreeCellNear(Vector2Int c)
        {
            for (int r = 0; r < 13; r++)
                for (int dx = -r; dx <= r; dx++)
                    for (int dy = -r; dy <= r; dy++)
                    {
                        var n = new Vector2Int(c.x + dx, c.y + dy);
                        if (Grid.Inside(n) && !IsBlocked(n) && statueAt[n.x, n.y] < 0 && n != Door && n.y > 0) return n;
                    }
            return c;
        }

        protected override void OnEntered()
        {
            if (!present[Vaqueiro] && !GameState.Flag("dica_vaqueiro"))
            {
                GameState.Set("dica_vaqueiro");
                Hud.Toast("A marcação do VAQUEIRO está vazia...\nAs pegadas saem pela porta de serviço.", 4f);
            }
        }

        protected override void SaveProgress()
        {
            var s = new int[4];
            for (int i = 0; i < 4; i++) s[i] = present[i] ? Idx(statues[i].cell) : -1;
            GameState.A1Statues = s;
        }

        public override bool IsBlocked(Vector2Int c) => pedestalCells.Contains(c) || (!Solved && c == Stairs);

        protected override string ContextHint(Vector2Int cell, Vector2Int facing)
        {
            if (cell == new Vector2Int(0, 1) && facing == Up) return "CIMA: sair pela porta de serviço (campus)";
            if (!Solved && cell.y <= 1 && cell.x >= 10) return "A escada está bloqueada até as estátuas voltarem ao lugar.";
            return null;
        }

        public override StepResult TryStep(PlayerController p, Vector2Int d)
        {
            var from = p.actor.cell;
            var t = from + d;
            if (!Grid.Inside(t) || IsBlocked(t)) return StepResult.Blocked;

            int s = statueAt[t.x, t.y];
            if (s < 0)
            {
                history.Push(new Move { playerFrom = from, playerFacing = p.Facing, statue = -1 });
                p.Step(d);
                return StepResult.Moved;
            }

            var beyond = t + d;
            if (!Grid.Inside(beyond) || IsBlocked(beyond) || statueAt[beyond.x, beyond.y] >= 0 || beyond == Door)
                return StepResult.Blocked;

            history.Push(new Move { playerFrom = from, playerFacing = p.Facing, statue = s, statueFrom = t });
            statueAt[t.x, t.y] = -1;
            statueAt[beyond.x, beyond.y] = s;
            statues[s].MoveTo(beyond, 0.32f * GameState.SlowFactor);
            GameAudio.Sfx("sfx_empurrar", 0.8f);
            p.walkFps = 18f;
            p.Step(d, Px.Tex("guarda_empurrar_frente_sheet"), 0.32f, () => p.walkFps = 12f);
            AfterPush();
            return StepResult.Moved;
        }

        private void AfterPush()
        {
            RefreshMarkers();
            Hud.SetObjective(ObjectiveText());
            if (OnTargetCount() == 4)
            {
                MarkSolved("Estátuas no lugar. Agora, as pinturas.");
                if (barrier != null) { sortables.Remove(barrier); Destroy(barrier.gameObject); }
                Hud.SetObjective(ObjectiveText());
                return;
            }
            if (AnyDeadlock())
            {
                if (!warnedDeadlock)
                {
                    GameAudio.Sfx("sfx_erro");
                    Hud.Toast("Essa estátua travou!\nZ desfaz o último passo, R recomeça.", 4f);
                }
                warnedDeadlock = true;
            }
            else warnedDeadlock = false;
        }

        protected override void Update()
        {
            base.Update();
            if (Solved || InputLocked || Player.Busy || Player.actor.Moving) return;
            var k = Keyboard.current;
            if (k == null) return;
            if (k.zKey.wasPressedThisFrame || k.backspaceKey.wasPressedThisFrame) Undo();
            else if (k.rKey.wasPressedThisFrame) Restart();
        }

        private void Undo()
        {
            if (history.Count == 0) return;
            var m = history.Pop();
            if (m.statue >= 0)
            {
                var now = statues[m.statue].cell;
                statueAt[now.x, now.y] = -1;
                statueAt[m.statueFrom.x, m.statueFrom.y] = m.statue;
                statues[m.statue].Snap(m.statueFrom);
            }
            Player.Teleport(m.playerFrom, m.playerFacing);
            RefreshMarkers();
            warnedDeadlock = AnyDeadlock();
            Hud.SetObjective(ObjectiveText());
        }

        private void Restart()
        {
            history.Clear();
            for (int x = 0; x < Grid.cols; x++) for (int y = 0; y < Grid.rows; y++) statueAt[x, y] = -1;
            for (int i = 0; i < 4; i++)
            {
                if (!present[i]) continue;
                statues[i].Snap(StatueStart[i]);
                statueAt[StatueStart[i].x, StatueStart[i].y] = i;
            }
            Player.Teleport(new Vector2Int(0, 1), Down);
            warnedDeadlock = false;
            RefreshMarkers();
            Hud.SetObjective(ObjectiveText());
            Hud.Toast("Recomeçando o térreo.", 1.5f);
        }

        private int OnTargetCount()
        {
            int n = 0;
            for (int i = 0; i < 4; i++) if (present[i] && statues[i] != null && statues[i].cell == Targets[i]) n++;
            return n;
        }

        private void RefreshMarkers()
        {
            for (int i = 0; i < 4; i++)
                markers[i].texture = Px.Tex(present[i] && statues[i].cell == Targets[i] ? "marcacao_ok" : "marcacao");
        }

        // ---- Detecção de travamento ----

        private bool Wall(Vector2Int c) => !Grid.Inside(c) || IsBlocked(c);

        private bool AnyDeadlock()
        {
            for (int i = 0; i < 4; i++)
                if (present[i] && IsDeadlocked(i, statues[i].cell)) return true;
            return false;
        }

        private bool IsDeadlocked(int i, Vector2Int c)
        {
            if (c == Targets[i]) return false;
            bool h = Wall(c + Vector2Int.left) || Wall(c + Vector2Int.right);
            bool v = Wall(c + Vector2Int.up) || Wall(c + Vector2Int.down);
            if (h && v) return true; // canto
            // Encostada na parede de cima/baixo: só anda na horizontal — a marcação dela precisa estar na mesma linha.
            if ((c.y == 0 || c.y == Grid.rows - 1) && Targets[i].y != c.y) return true;
            if ((c.x == 0 || c.x == Grid.cols - 1) && Targets[i].x != c.x) return true;
            return false;
        }
    }
}
