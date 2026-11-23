using Misterio.Puzzles.Floor1;
using Misterio.Puzzles.Floor2;
using Misterio.Puzzles.Floor3;
using Misterio.Puzzles.Floor4;
using NUnit.Framework;
using UnityEngine;

namespace Misterio.Puzzles.Tests
{
    /// <summary>
    /// The route rules are pure data, so they live in the testable assembly: a broken chain or a
    /// dead end is caught here instead of stranding the player mid-night.
    /// </summary>
    public sealed class RouteRulesTests
    {
        [Test]
        public void SuccessChainAlternatesCutscenesAndFloors()
        {
            Assert.AreEqual("CC01_A_Madrugada_Comeca", RouteRules.NextAfter("Exterior_Chegada"));
            Assert.AreEqual("Floor1_Estatuas", RouteRules.NextAfter("CC01_A_Madrugada_Comeca"));
            Assert.AreEqual("CC02_Acesso_a_Galeria", RouteRules.NextAfter("Floor1_Estatuas"));
            Assert.AreEqual("Floor2_Galeria", RouteRules.NextAfter("CC02_Acesso_a_Galeria"));
            Assert.AreEqual("CC03_A_Galeria_Reorganizada", RouteRules.NextAfter("Floor2_Galeria"));
            Assert.AreEqual("Floor3_Acervo", RouteRules.NextAfter("CC03_A_Galeria_Reorganizada"));
            Assert.AreEqual("CC04_Passagem_Silenciosa", RouteRules.NextAfter("Floor3_Acervo"));
            Assert.AreEqual("CC05_A_Obra_no_Escuro", RouteRules.NextAfter("CC04_Passagem_Silenciosa"));
            Assert.AreEqual("Floor4_Salao", RouteRules.NextAfter("CC05_A_Obra_no_Escuro"));
            Assert.AreEqual("CC06_A_Energia_Retorna", RouteRules.NextAfter("Floor4_Salao"));
            Assert.AreEqual("CC07_A_Trilha_de_Moema", RouteRules.NextAfter("CC06_A_Energia_Retorna"));
            Assert.AreEqual("CC08_A_Abertura_do_Dia", RouteRules.NextAfter("CC07_A_Trilha_de_Moema"));
            Assert.AreEqual("Credits", RouteRules.NextAfter("CC08_A_Abertura_do_Dia"));
        }

        [Test]
        public void TheNightStartsOutsideTheBuilding()
        {
            // The player crosses the campus first, which is what gives the night room to breathe.
            Assert.AreEqual("Exterior_Chegada", RouteRules.SuccessChain[0]);
            Assert.AreEqual("CC01_A_Madrugada_Comeca", RouteRules.NextAfter("Exterior_Chegada"));
        }

        [Test]
        public void EverySceneOnTheChainExists()
        {
            foreach (string scene in RouteRules.SuccessChain)
                Assert.IsTrue(System.IO.File.Exists("Assets/Scenes/" + scene + ".unity"),
                              "Missing scene on the route: " + scene);
        }

        [Test]
        public void CreditsEndTheRoute()
        {
            Assert.IsNull(RouteRules.NextAfter("Credits"));
        }

        [Test]
        public void RouteHasNoDeadEndBeforeTheCredits()
        {
            string current = "CC01_A_Madrugada_Comeca";
            int steps = 0;
            while (steps < 40)
            {
                string next = RouteRules.NextAfter(current);
                if (string.IsNullOrEmpty(next)) break;
                Assert.AreNotEqual(current, next, "The route must never point at itself: " + current);
                current = next;
                steps++;
            }
            Assert.AreEqual("Credits", current, "The success route must end at the credits.");
        }

        [Test]
        public void EveryFloorAppearsExactlyOnce()
        {
            int floors = 0;
            foreach (var scene in RouteRules.SuccessChain)
                if (scene.StartsWith("Floor")) floors++;
            Assert.AreEqual(4, floors, "The route must visit all four floors.");
        }

        [Test]
        public void EveryCutsceneOnTheRouteIsPresent()
        {
            // CC01, CC02, CC03, CC04, CC05, CC06, CC07 and CC08 are on the success route.
            int cutscenes = 0;
            foreach (var scene in RouteRules.SuccessChain)
                if (scene.StartsWith("CC")) cutscenes++;
            Assert.AreEqual(8, cutscenes);
        }

        [Test]
        public void TheRouteHasOneExteriorAndThirteenSteps()
        {
            int exteriors = 0;
            foreach (var scene in RouteRules.SuccessChain)
                if (scene.StartsWith("Exterior")) exteriors++;
            Assert.AreEqual(1, exteriors, "The approach appears once, before the opening cutscene.");
            Assert.AreEqual(14, RouteRules.SuccessChain.Length);
        }



        [Test]
        public void FailureInTheArchiveReturnsToTheArchive()
        {
            Assert.AreEqual("Floor3_Acervo", RouteRules.ResumeTarget(3));
            Assert.AreEqual("Floor1_Estatuas", RouteRules.ResumeTarget(1));
            Assert.AreEqual("Floor2_Galeria", RouteRules.ResumeTarget(2));
            Assert.AreEqual("Floor4_Salao", RouteRules.ResumeTarget(4));
            Assert.IsNull(RouteRules.ResumeTarget(0));
            Assert.IsNull(RouteRules.ResumeTarget(9));
        }
    }
}
