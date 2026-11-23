using UnityEngine;

namespace Misterio.Puzzles
{
    /// <summary>
    /// The play order of the whole game as pure data. Keeping it here (instead of only in the
    /// MonoBehaviour) means the chain can be unit tested and cannot silently break.
    /// </summary>
    public static class RouteRules
    {
        public const string Exterior = "Exterior_Chegada";
        public const string Cutscene01 = "CC01_A_Madrugada_Comeca";
        public const string Floor1 = "Floor1_Estatuas";
        public const string Cutscene02 = "CC02_Acesso_a_Galeria";
        public const string Floor2 = "Floor2_Galeria";
        public const string Cutscene03 = "CC03_A_Galeria_Reorganizada";
        public const string Floor3 = "Floor3_Acervo";
        public const string Cutscene04 = "CC04_Passagem_Silenciosa";
        public const string Cutscene04A = "CC04A_Alerta_de_Moema";
        public const string Cutscene05 = "CC05_A_Obra_no_Escuro";
        public const string Floor4 = "Floor4_Salao";
        public const string Cutscene06 = "CC06_A_Energia_Retorna";
        public const string Cutscene07 = "CC07_A_Trilha_de_Moema";
        public const string Cutscene08 = "CC08_A_Abertura_do_Dia";
        public const string Cutscene09 = "CC09_A_Abertura_Precisa_Esperar";
        public const string Cutscene10 = "CC10_A_Ultima_Sala_Ainda_no_Escuro";
        public const string Credits = "Credits";

        /// <summary>The success route, in play order.</summary>
        public static readonly string[] SuccessChain =
        {
            Exterior, Cutscene01, Floor1, Cutscene02, Floor2, Cutscene03, Floor3,
            Cutscene04, Cutscene05, Floor4, Cutscene06, Cutscene07, Cutscene08, Credits
        };

        /// <summary>Next scene on the success route, or null at the end.</summary>
        public static string NextAfter(string sceneName)
        {
            for (int i = 0; i < SuccessChain.Length - 1; i++)
                if (SuccessChain[i] == sceneName) return SuccessChain[i + 1];
            return null;
        }

        /// <summary>Floor to resume after a failure cutscene (0 or unknown returns null).</summary>
        public static string ResumeTarget(int floor)
        {
            switch (floor)
            {
                case 1: return Floor1;
                case 2: return Floor2;
                case 3: return Floor3;
                case 4: return Floor4;
                default: return null;
            }
        }
    }
}
