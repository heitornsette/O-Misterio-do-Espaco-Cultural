using System;
using System.Collections.Generic;

namespace EspacoCultural.Jogo
{
    /// <summary>
    /// Estado da partida que atravessa as cenas (memória estática, zerada no INICIAR do menu):
    /// inventário, marcas de progresso (flags), ponto de chegada da próxima cena, cansaço e o
    /// progresso parcial de cada andar (para o jogador poder sair do museu no meio do puzzle e voltar).
    /// </summary>
    public static class GameState
    {
        public static readonly HashSet<string> Items = new HashSet<string>();
        public static readonly HashSet<string> Flags = new HashSet<string>();
        /// <summary>Ordem em que os itens foram pegos (para o HUD).</summary>
        public static readonly List<string> ItemOrder = new List<string>();

        /// <summary>Onde o guarda aparece na próxima cena (ex.: "terreo_porta"). Lido uma vez.</summary>
        public static string Spawn;

        /// <summary>Cansaço de 0 a 1. Sobe com o tempo dentro do museu; o café zera.</summary>
        public static float Fatigue;

        // ---- Progresso parcial dos andares (null = ainda não visitado) ----
        public static int[] A1Statues;   // célula (x + y*13) de cada estátua, -1 = fora do térreo
        public static int[] A2Slot;      // placa -> quadro pendurado (-1 vazio)
        public static int[] A2Floor;     // quadro -> célula no chão (-1 = na parede ou na mão)
        public static int A2Carry = -1;  // quadro na mão
        public static int[] A4Rot;       // rotação de cada peça do painel
        public static bool A4Fuse;       // fusível certo instalado

        public static event Action Changed;

        public static void NewGame()
        {
            Items.Clear(); Flags.Clear(); ItemOrder.Clear();
            Spawn = null; Fatigue = 0f;
            A1Statues = null; A2Slot = null; A2Floor = null; A2Carry = -1; A4Rot = null; A4Fuse = false;
            Changed?.Invoke();
        }

        public static bool Has(string id) => Items.Contains(id);

        public static void Give(string id)
        {
            if (!Items.Add(id)) return;
            ItemOrder.Add(id);
            Changed?.Invoke();
        }

        public static void Take(string id)
        {
            if (!Items.Remove(id)) return;
            ItemOrder.Remove(id);
            Changed?.Invoke();
        }

        public static bool Flag(string f) => Flags.Contains(f);

        public static void Set(string f)
        {
            if (Flags.Add(f)) Changed?.Invoke();
        }

        public static string ConsumeSpawn()
        {
            var s = Spawn;
            Spawn = null;
            return s;
        }

        /// <summary>Fusível que o guarda está carregando (0 = nenhum).</summary>
        public static int FuseCarried()
        {
            foreach (var a in new[] { 10, 16, 25, 32 }) if (Has("fusivel_" + a)) return a;
            return 0;
        }

        /// <summary>Pistas opcionais da Moema encontradas (conquista).</summary>
        public static int CluesFound
        {
            get { int n = 0; foreach (var f in Flags) if (f.StartsWith("pista_")) n++; return n; }
        }
        public const int CluesTotal = 4;

        /// <summary>Fator de lentidão pelo cansaço (1 = normal).</summary>
        public static float SlowFactor => Fatigue >= 0.7f ? 1.25f : 1f;
        public static bool Tired => Fatigue >= 0.7f;

        public static void AddFatigue(float amount)
        {
            float before = Fatigue;
            Fatigue = Math.Max(0f, Math.Min(1f, Fatigue + amount));
            if ((before < 0.7f) != (Fatigue < 0.7f)) Changed?.Invoke();
        }
    }
}
