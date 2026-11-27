using System.Collections.Generic;
using UnityEngine;

namespace EspacoCultural.Gameplay
{
    /// <summary>
    /// Agenda da patrulha da Moema, em ticks de 0,25 s (igual à simulação usada para provar que
    /// o andar tem solução). Ela vai e volta entre os pontos; em cada ponto marcado, para e bica o chão.
    /// Também calcula a visão: produto escalar entre a direção do olhar e o vetor até a célula
    /// (cone de 40° para cada lado, alcance 4,5 células) + linha de visão (Bresenham) bloqueada pelas vitrines.
    /// </summary>
    public class MoemaPatrol
    {
        public struct State
        {
            public Vector2Int cell;
            public Vector2Int facing;
            public bool pecking;
        }

        public const float Tick = 0.25f;
        public const int MoveTicks = 2;       // 0,5 s por célula
        public const int PauseTicks = 8;      // 2 s bicando
        public const float Range = 4.5f;
        public const float PeckRange = 2f;
        public static readonly float CosHalfAngle = Mathf.Cos(40f * Mathf.Deg2Rad);

        public readonly List<State> schedule = new List<State>();
        public int Period => schedule.Count;

        public MoemaPatrol(IList<Vector2Int> waypoints, ICollection<int> pauseAt)
        {
            var order = new List<int>();
            for (int i = 0; i < waypoints.Count; i++) order.Add(i);
            for (int i = waypoints.Count - 2; i > 0; i--) order.Add(i);

            for (int k = 0; k < order.Count; k++)
            {
                int wi = order[k];
                var a = waypoints[wi];
                var b = waypoints[order[(k + 1) % order.Count]];
                if (pauseAt.Contains(wi))
                    for (int t = 0; t < PauseTicks; t++) schedule.Add(new State { cell = a, facing = new Vector2Int(0, 1), pecking = true });
                var c = a;
                while (c != b)
                {
                    var st = new Vector2Int(System.Math.Sign(b.x - c.x), System.Math.Sign(b.y - c.y));
                    if (st.x != 0 && st.y != 0) st.y = 0;
                    c += st;
                    for (int t = 0; t < MoveTicks; t++) schedule.Add(new State { cell = c, facing = st, pecking = false });
                }
            }
        }

        public State At(int tick) => schedule[((tick % Period) + Period) % Period];

        /// <summary>A célula p está visível para a Moema no estado s?</summary>
        public static bool Sees(State s, Vector2Int p, HashSet<Vector2Int> blockers)
        {
            var m = s.cell;
            float dx = p.x - m.x, dy = p.y - m.y;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            if (d < 0.01f) return true;
            if (d <= 1.01f) return true;                       // colado nela: percebe sempre
            if (d > (s.pecking ? PeckRange : Range)) return false;
            float dot = (dx * s.facing.x + dy * s.facing.y) / d; // produto escalar com o olhar (normalizado)
            if (dot < CosHalfAngle) return false;
            foreach (var c in Line(m, p)) if (blockers.Contains(c)) return false;   // vitrine no caminho
            return true;
        }

        /// <summary>Células entre a e b (sem as pontas), algoritmo de Bresenham.</summary>
        public static IEnumerable<Vector2Int> Line(Vector2Int a, Vector2Int b)
        {
            int x0 = a.x, y0 = a.y, x1 = b.x, y1 = b.y;
            int dx = Mathf.Abs(x1 - x0), dy = -Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
            int err = dx + dy;
            while (true)
            {
                if (x0 == x1 && y0 == y1) yield break;
                int e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
                if (x0 == x1 && y0 == y1) yield break;
                yield return new Vector2Int(x0, y0);
            }
        }
    }
}
