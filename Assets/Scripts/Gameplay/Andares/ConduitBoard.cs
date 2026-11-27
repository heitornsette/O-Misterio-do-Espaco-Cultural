using System;
using System.Collections.Generic;
using EspacoCultural.Audio;
using EspacoCultural.Jogo;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EspacoCultural.Gameplay
{
    /// <summary>
    /// Painel de conduítes do Andar 4 (regra do jogo "Net").
    /// Cada peça guarda suas conexões numa máscara de 4 bits (N=1, L=2, S=4, O=8); girar 90° no sentido
    /// horário = rotacionar os bits (N→L→S→O). As 40 peças formam uma árvore que cobre o painel inteiro
    /// (gerada por busca em profundidade aleatória). O circuito fecha quando:
    ///  - uma BFS a partir da fonte (entrada pela esquerda de (0,2)) alcança TODAS as peças;
    ///  - não sobra nenhuma ponta solta (todo lado aberto encosta num lado aberto do vizinho, na fonte ou numa lâmpada);
    ///  - as duas lâmpadas (direita de (7,1) e de (7,3)) recebem energia.
    /// A peça (2,2) é o fusível: o encaixe fica vazio até o guarda instalar o fusível de 25 A (circuito EC-4).
    /// Peças com parafusos estão fixas na posição certa.
    /// Controles: setas movem o cursor, E/Espaço giram, clique do mouse gira a peça clicada, ESC fecha.
    /// </summary>
    public class ConduitBoard
    {
        public const int N = 1, E = 2, S = 4, W = 8;
        public const int Cols = 8, Rows = 5, TileSize = 32;

        // Tipos: 0 ponta, 1 reta, 2 curva, 3 T, 4 cruz (linha por linha, de cima para baixo)
        private static readonly int[] Types =
        {
            2, 1, 2, 2, 1, 0, 2, 2,
            1, 0, 1, 3, 1, 2, 1, 3,
            2, 1, 1, 3, 0, 2, 2, 1,
            0, 3, 2, 0, 2, 1, 2, 3,
            2, 3, 1, 1, 2, 0, 3, 2,
        };
        private static readonly int[] SolRot =
        {
            1, 1, 2, 1, 1, 3, 1, 2,
            0, 2, 0, 0, 1, 2, 0, 0,
            3, 0, 0, 0, 3, 0, 3, 0,
            2, 0, 3, 0, 1, 1, 2, 0,
            0, 3, 1, 1, 3, 1, 3, 3,
        };
        private static readonly int[] StartRot =
        {
            2, 0, 2, 1, 2, 2, 2, 3,
            3, 3, 1, 3, 0, 2, 1, 3,
            2, 3, 1, 3, 2, 3, 0, 1,
            3, 1, 2, 2, 3, 0, 0, 2,
            0, 1, 0, 2, 0, 2, 1, 0,
        };
        private static readonly bool[] Fixed =
        {
            false, false, true, true, false, false, false, false,
            false, false, false, false, false, true, false, false,
            false, false, false, false, false, false, false, false,
            false, false, false, false, false, false, false, false,
            true, false, false, false, false, false, false, false,
        };
        private static readonly string[] TexName = { "conduite_ponta", "conduite_reta", "conduite_curva", "conduite_t", "conduite_cruz" };
        private static readonly int[] BaseMask = { N, N | S, N | E, N | E | S, N | E | S | W };

        public static readonly Vector2Int Source = new Vector2Int(0, 2);   // energia entra pelo O
        public static readonly Vector2Int[] Sinks = { new Vector2Int(7, 1), new Vector2Int(7, 3) };  // saem pelo L
        public static readonly Vector2Int FuseCell = new Vector2Int(2, 2);
        public const int FuseAmps = 25;

        private readonly int[] rot = new int[Cols * Rows];
        private readonly RawImage[] tiles = new RawImage[Cols * Rows];
        private readonly RawImage[] bolts = new RawImage[Cols * Rows];
        private readonly bool[] powered = new bool[Cols * Rows];
        private RectTransform root;
        private RectTransform boardRect;
        private RawImage cursor;
        private readonly RawImage[] sinkIcons = new RawImage[2];
        private Text status;
        private Vector2Int cur = new Vector2Int(0, 2);
        private float openedAt;

        public bool Open => root != null && root.gameObject.activeSelf;
        public bool Solved { get; private set; }
        public bool FuseInstalled { get; private set; }
        public event Action Completed;
        public event Action Closed;

        public static int Rotate(int mask, int times)
        {
            for (int i = 0; i < ((times % 4) + 4) % 4; i++) mask = ((mask << 1) | (mask >> 3)) & 15;
            return mask;
        }

        private bool IsFuse(int i) => i == FuseCell.y * Cols + FuseCell.x;
        private bool Empty(int i) => IsFuse(i) && !FuseInstalled;

        public int MaskAt(int x, int y)
        {
            int i = y * Cols + x;
            if (Empty(i)) return 0;
            return Rotate(BaseMask[Types[i]], rot[i]);
        }

        public void Build(RectTransform parent, Font title, Font body)
        {
            const int bw = Cols * TileSize, bh = Rows * TileSize;
            const int pw = bw + 112, ph = bh + 80;
            float px0 = (Px.W - pw) / 2f, py0 = 52;
            root = Px.Panel("PainelConduites", parent, px0, py0, pw, ph);
            Px.Label("Titulo", root, title, 8, Px.Warm, 10, 10, pw - 20, 12, TextAnchor.UpperCenter).text = "PAINEL ELÉTRICO — CIRCUITO EC-4";

            boardRect = Px.Rect("Grade", root);
            Px.Place(boardRect, 56, 28, bw, bh);
            var saved = GameState.A4Rot;
            for (int y = 0; y < Rows; y++)
                for (int x = 0; x < Cols; x++)
                {
                    int i = y * Cols + x;
                    rot[i] = Fixed[i] ? SolRot[i] : (saved != null ? saved[i] : StartRot[i]);
                    var rt = Px.Rect($"Peca_{x}_{y}", boardRect);
                    Px.PlaceCenter(rt, x * TileSize + TileSize / 2f, y * TileSize + TileSize / 2f, TileSize, TileSize);
                    var img = rt.gameObject.AddComponent<RawImage>();
                    img.raycastTarget = false;
                    tiles[i] = img;
                    if (Fixed[i]) bolts[i] = Px.Raw("Parafusos", boardRect, Px.Tex("conduite_fixa"), x * TileSize, y * TileSize, TileSize, TileSize);
                }
            FuseInstalled = GameState.A4Fuse;

            Px.Raw("Fonte", root, Px.Tex("conduite_fonte"), 56 - 34, 28 + Source.y * TileSize, 32, 32);
            for (int s = 0; s < 2; s++)
                sinkIcons[s] = Px.Raw("Lampada" + s, root, Px.Tex("conduite_destino"), 56 + bw + 2, 28 + Sinks[s].y * TileSize, 32, 32);
            cursor = Px.Raw("Cursor", boardRect, Px.Tex("conduite_cursor"), 0, 0, 36, 36);
            status = Px.Label("Estado", root, body, 8, Px.Warm, 10, ph - 46, pw - 20, 12, TextAnchor.UpperCenter);
            Px.Label("Ajuda", root, body, 8, Px.LightBlue, 10, ph - 30, pw - 20, 24, TextAnchor.UpperCenter).text =
                "SETAS escolher   E/ESPAÇO ou clique girar   ESC sair\nLigue TODAS as peças, sem pontas soltas, às duas lâmpadas.";

            root.gameObject.SetActive(false);
            Refresh();
        }

        public void InstallFuse()
        {
            FuseInstalled = true;
            GameState.A4Fuse = true;
            int i = FuseCell.y * Cols + FuseCell.x;
            rot[i] = 1;   // entra deitado: precisa girar
            Refresh();
        }

        public int[] Rotations => (int[])rot.Clone();

        public void Show()
        {
            root.gameObject.SetActive(true);
            root.SetAsLastSibling();
            openedAt = Time.unscaledTime;
            Refresh();
        }

        public void Hide()
        {
            if (!Open) return;
            root.gameObject.SetActive(false);
            GameState.A4Rot = Rotations;
            Closed?.Invoke();
        }

        /// <summary>Chamar todo quadro enquanto o painel estiver aberto.</summary>
        public void Tick()
        {
            if (!Open || Solved) return;
            var k = Keyboard.current;
            if (k == null) return;
            bool justOpened = Time.unscaledTime - openedAt < 0.15f;

            if (k.escapeKey.wasPressedThisFrame && !justOpened) { Hide(); return; }
            if (k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame) MoveCursor(-1, 0);
            if (k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame) MoveCursor(1, 0);
            if (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame) MoveCursor(0, -1);
            if (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame) MoveCursor(0, 1);
            if (!justOpened && (k.eKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame))
                RotateAt(cur);

            var m = Mouse.current;
            if (m != null && m.leftButton.wasPressedThisFrame &&
                RectTransformUtility.ScreenPointToLocalPointInRectangle(boardRect, m.position.ReadValue(), null, out var local))
            {
                int x = Mathf.FloorToInt(local.x / TileSize);
                int y = Mathf.FloorToInt(-local.y / TileSize);
                if (x >= 0 && y >= 0 && x < Cols && y < Rows)
                {
                    cur = new Vector2Int(x, y);
                    RotateAt(cur);
                }
            }
        }

        private void MoveCursor(int dx, int dy)
        {
            cur = new Vector2Int(Mathf.Clamp(cur.x + dx, 0, Cols - 1), Mathf.Clamp(cur.y + dy, 0, Rows - 1));
            Refresh();
        }

        private void RotateAt(Vector2Int c)
        {
            int i = c.y * Cols + c.x;
            if (Fixed[i]) { GameAudio.Sfx("sfx_erro", 0.4f); return; }
            if (Empty(i)) { GameAudio.Sfx("sfx_erro", 0.4f); return; }
            if (Types[i] == 4) return;   // a cruz é igual em qualquer posição
            rot[i] = (rot[i] + 1) % 4;
            GameAudio.Sfx("sfx_girar", 0.7f);
            Refresh();
            if (Solved) { GameState.A4Rot = Rotations; Completed?.Invoke(); }
        }

        private static readonly int[] Bits = { N, E, S, W };
        private static readonly int[] Opps = { S, W, N, E };
        private static readonly Vector2Int[] Steps = { new Vector2Int(0, -1), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(-1, 0) };

        /// <summary>Lado aberto que sai do painel na fonte ou numa lâmpada (não conta como ponta solta).</summary>
        private static bool Terminal(Vector2Int c, int bit) =>
            (c == Source && bit == W) || ((c == Sinks[0] || c == Sinks[1]) && bit == E);

        /// <summary>BFS a partir da fonte; devolve se o circuito está completo.</summary>
        private bool Flow(out int loose, out bool[] sinkOn)
        {
            Array.Clear(powered, 0, powered.Length);
            sinkOn = new bool[2];
            loose = 0;
            if ((MaskAt(Source.x, Source.y) & W) != 0)
            {
                var q = new Queue<Vector2Int>();
                q.Enqueue(Source);
                powered[Source.y * Cols + Source.x] = true;
                while (q.Count > 0)
                {
                    var c = q.Dequeue();
                    int m = MaskAt(c.x, c.y);
                    for (int k = 0; k < 4; k++)
                    {
                        if ((m & Bits[k]) == 0) continue;
                        var n = c + Steps[k];
                        if (n.x < 0 || n.y < 0 || n.x >= Cols || n.y >= Rows) continue;
                        int ni = n.y * Cols + n.x;
                        if (powered[ni] || (MaskAt(n.x, n.y) & Opps[k]) == 0) continue;
                        powered[ni] = true;
                        q.Enqueue(n);
                    }
                }
            }
            for (int s = 0; s < 2; s++)
                sinkOn[s] = powered[Sinks[s].y * Cols + Sinks[s].x] && (MaskAt(Sinks[s].x, Sinks[s].y) & E) != 0;

            bool all = true;
            for (int y = 0; y < Rows; y++)
                for (int x = 0; x < Cols; x++)
                {
                    int i = y * Cols + x;
                    if (!powered[i]) all = false;
                    var c = new Vector2Int(x, y);
                    int m = MaskAt(x, y);
                    for (int k = 0; k < 4; k++)
                    {
                        if ((m & Bits[k]) == 0 || Terminal(c, Bits[k])) continue;
                        var n = c + Steps[k];
                        bool ok = n.x >= 0 && n.y >= 0 && n.x < Cols && n.y < Rows && (MaskAt(n.x, n.y) & Opps[k]) != 0;
                        if (!ok) loose++;
                    }
                }
            return FuseInstalled && all && loose == 0 && sinkOn[0] && sinkOn[1];
        }

        private void Refresh()
        {
            Solved = Flow(out int loose, out var sinkOn);
            int poweredCount = 0;
            for (int y = 0; y < Rows; y++)
                for (int x = 0; x < Cols; x++)
                {
                    int i = y * Cols + x;
                    if (powered[i]) poweredCount++;
                    string tex = Empty(i) ? "conduite_vazio" : IsFuse(i) ? "conduite_fusivel" : TexName[Types[i]];
                    if (powered[i] && !Empty(i)) tex += "_on";
                    tiles[i].texture = Px.Tex(tex);
                    tiles[i].rectTransform.localEulerAngles = new Vector3(0, 0, Empty(i) ? 0 : -90f * rot[i]);
                }
            for (int s = 0; s < 2; s++)
                if (sinkIcons[s] != null) sinkIcons[s].texture = Px.Tex(sinkOn[s] ? "conduite_destino_on" : "conduite_destino");
            if (status != null)
                status.text = !FuseInstalled ? "Encaixe vazio: falta o fusível de 25 A."
                    : Solved ? "CIRCUITO FECHADO!"
                    : $"Energizadas: {poweredCount}/{Cols * Rows}   Pontas soltas: {loose}";
            if (cursor != null)
            {
                Px.Place(cursor.rectTransform, cur.x * TileSize - 2, cur.y * TileSize - 2, 36, 36);
                cursor.transform.SetAsLastSibling();
                cursor.enabled = !Solved;
            }
        }
    }
}
