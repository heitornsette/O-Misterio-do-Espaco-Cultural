using System.Collections.Generic;
using EspacoCultural.Jogo;

namespace EspacoCultural.Campus
{
    /// <summary>Objetivo mostrado no campus: o que ainda falta buscar para os andares do museu.</summary>
    public static class CampusGoals
    {
        public static string Text()
        {
            var l = new List<string>();
            if (!GameState.Flag("vaqueiro_devolvido"))
                l.Add(GameState.Has("vaqueiro") ? "Leve o Vaqueiro ao térreo (porta de serviço)." : "Siga as pegadas: cadê o Vaqueiro?");
            if (GameState.Flag("intro2") && !GameState.Has("catalogo") && !GameState.Flag("resolvido2"))
                l.Add("Catálogo da exposição: Biblioteca.");
            if (GameState.Flag("intro3") && !GameState.Has("bola") && !GameState.Flag("resolvido3"))
                l.Add("Algo para distrair a Moema...");
            if (!GameState.A4Fuse && GameState.FuseCarried() != 25 && !GameState.Flag("resolvido4") && GameState.Flag("radio"))
                l.Add("Fusível EC-4: quadro do ginásio.");
            if (l.Count == 0) return "Volte ao Espaço Cultural\n(porta de serviço, à esquerda).";
            if (l.Count > 3) l.RemoveRange(3, l.Count - 3);
            return string.Join("\n", l);
        }
    }
}
