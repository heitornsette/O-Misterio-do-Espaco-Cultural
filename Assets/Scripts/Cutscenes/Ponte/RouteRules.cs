using System.Collections.Generic;

namespace Misterio.Puzzles
{
    /// <summary>
    /// PONTE: a rota do NOSSO jogo, com as cutscenes da equipe entre os andares.
    /// MenuInicial → CC01 → Andar1 → CC02 → Andar2 → CC03 → Andar3 (visto/barulho → CC04A → Andar3)
    /// → CC04 → CC05 → Andar4 → CC06 → CC07 → CC08 → Créditos → MenuInicial.
    /// Fim do tempo: CC09 (andares 1–3) ou CC10 (andar 4), com "tentar de novo" e "voltar ao menu".
    /// </summary>
    public static class RouteRules
    {
        public const string Menu = "MenuInicial";
        public const string Cutscene01 = "CC01_A_Madrugada_Comeca";
        public const string Cutscene02 = "CC02_Acesso_a_Galeria";
        public const string Cutscene03 = "CC03_A_Galeria_Reorganizada";
        public const string Cutscene04 = "CC04_Passagem_Silenciosa";
        public const string Cutscene04A = "CC04A_Alerta_de_Moema";
        public const string Cutscene05 = "CC05_A_Obra_no_Escuro";
        public const string Cutscene06 = "CC06_A_Energia_Retorna";
        public const string Cutscene07 = "CC07_A_Trilha_de_Moema";
        public const string Cutscene08 = "CC08_A_Abertura_do_Dia";
        public const string Cutscene09 = "CC09_A_Abertura_Precisa_Esperar";
        public const string Cutscene10 = "CC10_A_Ultima_Sala_Ainda_no_Escuro";
        public const string Credits = "Credits";

        private static readonly Dictionary<string, string> next = new Dictionary<string, string>
        {
            { Cutscene01, "Andar1" },
            { Cutscene02, "Andar2" },
            { Cutscene03, "Andar3" },
            { Cutscene04A, "Andar3" },
            { Cutscene04, Cutscene05 },
            { Cutscene05, "Andar4" },
            { Cutscene06, Cutscene07 },
            { Cutscene07, Cutscene08 },
            { Cutscene08, Credits },
            { Credits, Menu },
        };

        /// <summary>Próxima cena depois desta (null = a cena decide sozinha, como CC09/CC10).</summary>
        public static string NextAfter(string scene) => next.TryGetValue(scene, out var n) ? n : null;

        /// <summary>Andar para "tentar de novo" depois do fim do tempo.</summary>
        public static string ResumeTarget(int floor) => floor >= 1 && floor <= 4 ? "Andar" + floor : null;
    }
}
