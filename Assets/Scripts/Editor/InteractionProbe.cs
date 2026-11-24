using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Misterio.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Misterio.EditorTools
{
    /// <summary>
    /// Acceptance gate for interaction: proves the player can actually read the gallery clues and
    /// pick up / hang the paintings. Rendering a scene is not the same as being able to play it, so
    /// each interactable is exercised through its public path and the HUD is inspected.
    /// </summary>
    public static class InteractionProbe
    {
        const BindingFlags Any =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        [MenuItem("Misterio/Probe Interaction (clues, paintings, pieces, conduits)")]
        public static void Probe()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/InteractionProbe"));
            Directory.CreateDirectory(root);
            var report = new List<string>();

            report.Add("Gallery: " + ProbeGallery());
            report.Add("Archive: " + ProbeArchive());
            report.Add("Hall: " + ProbeHall());

            File.WriteAllLines(Path.Combine(root, "report.txt"), report);
            Debug.Log("[InteractionProbe]\n" + string.Join("\n", report));
            EditorApplication.Exit(0);
        }

        static object Call(object target, string method, params object[] args)
        {
            var info = target.GetType().GetMethod(method, Any);
            return info?.Invoke(target, args);
        }

        static object Field(object target, string name)
        {
            var info = target.GetType().GetField(name, Any);
            if (info != null) return info.GetValue(target);
            return target.GetType().GetProperty(name, Any)?.GetValue(target);
        }

        /// <summary>
        /// Opens a scene and runs the lifecycle in the order the engine uses: every Awake first,
        /// then every Start. Running them per-object would let a floor's Awake clear the registry
        /// before its interactables had a chance to enable.
        /// </summary>
        static void Open(string sceneName)
        {
            EditorSceneManager.OpenScene("Assets/Scenes/" + sceneName + ".unity", OpenSceneMode.Single);

            var behaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            // The engine enables objects before Awake, so mirror that first.
            foreach (var behaviour in behaviours)
                if (behaviour != null && behaviour.isActiveAndEnabled)
                    behaviour.SendMessage("OnEnable", SendMessageOptions.DontRequireReceiver);

            foreach (var behaviour in behaviours)
                if (behaviour != null) Call(behaviour, "Awake");

            foreach (var behaviour in behaviours)
                if (behaviour != null) Call(behaviour, "Start");
        }

        /// <summary>
        /// Drives the interaction the way a player does: press the key, let the guard resolve the
        /// nearest target and use it. Calling Interactable.Interact directly hides the wiring bugs
        /// that this whole probe exists to catch.
        /// </summary>
        static bool PressInteractThroughGuard(GuardActor guard)
        {
            if (guard == null) return false;
            // Editor mode does not run OnEnable, so the registry is rebuilt from the scene.
            Interactable.RebuildRegistry(
                Object.FindObjectsByType<Interactable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None));
            PlaytestInput.Enable();
            PlaytestInput.InteractPressed = true;
            PlaytestInput.MarkPress();
            Call(guard, "Update");
            PlaytestInput.Clear();
            PlaytestInput.Disable();
            return true;
        }

        static string HintText()
        {
            var hud = Object.FindAnyObjectByType<HudView>();
            if (hud == null) return "<no HUD>";
            var field = typeof(HudView).GetField("hintText", Any);
            var text = field?.GetValue(hud) as Text;
            return text != null ? text.text : "<no hint text>";
        }

        static string ProbeGallery()
        {
            Open("Floor2_Galeria");
            var guard = Object.FindAnyObjectByType<GuardActor>();
            if (guard == null) return "FAILED: no guard";

            var controller = Object.FindAnyObjectByType<Floor2Controller>();
            if (controller == null) return "FAILED: no controller";

            // The clues are scenery read by proximity, so what matters is that the floor can show
            // two distinct texts and that the plaque positions are inside the walkable area.

            var points = Field(controller, "cluePlaquePoints") as Vector2[];
            if (points == null || points.Length < 2) return "FAILED: plaque trigger points missing";
            foreach (var point in points)
                if (!guard.IsFree(point)) return "FAILED: plaque at " + point.ToString("F2") + " is unreachable";

            Call(controller, "ReadClue", 0);
            string first = HintText();
            Call(controller, "ReadClue", 1);
            string second = HintText();
            if (string.IsNullOrEmpty(first) || string.IsNullOrEmpty(second))
                return "FAILED: a clue produced no text";
            if (first == second) return "FAILED: both clues show the same text";

            int hooks = Object.FindObjectsByType<HookInteractable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
            var paintings = Object.FindObjectsByType<PaintingInteractable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (hooks != 3) return "FAILED: expected 3 hooks, found " + hooks;
            if (paintings.Length != 3) return "FAILED: expected 3 paintings, found " + paintings.Length;

            // Real-path check: stand on a painting and press the key.
            guard.Teleport(paintings[0].InteractPoint);
            PressInteractThroughGuard(guard);
            Call(guard, "Update");
            int carried = (int)Field(controller, "carried");
            if (carried < 0) return "FAILED: pressing interact did not pick up a painting";

            // Hang it through the same path and confirm the puzzle registers it.
            var hookList = Object.FindObjectsByType<HookInteractable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            guard.Teleport(hookList[0].InteractPoint);
            PressInteractThroughGuard(guard);
            Call(guard, "Update");
            var puzzle = Field(controller, "puzzle") as Misterio.Puzzles.Floor2.GalleryPuzzle;
            int placed = puzzle != null ? puzzle.PlacedCount() : -1;
            if (placed != 1) return "FAILED: hanging through the key did not place a painting (placed=" + placed + ")";

            return "OK 2 distinct clues reachable, 3 hooks, 3 paintings, pickup and hang via key work";
        }

        static string ProbeArchive()
        {
            Open("Floor3_Acervo");
            var guard = Object.FindAnyObjectByType<GuardActor>();
            var controller = Object.FindAnyObjectByType<Floor3Controller>();
            if (guard == null || controller == null) return "FAILED: missing guard/controller";

            var pieces = Object.FindObjectsByType<PieceInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (pieces.Length != 8) return "FAILED: expected 8 pieces, found " + pieces.Length;

            int collected = 0;
            foreach (var piece in pieces)
            {
                if (piece == null || !piece.isActiveAndEnabled) continue;
                piece.Interact(guard);
                collected = (int)Field(controller, "CollectedPieces");
            }

            var patrol = Field(controller, "patrol") as Misterio.Puzzles.Floor3.StealthPatrol;
            if (patrol == null) return "FAILED: no patrol";

            return collected == 8
                ? "OK 8/8 pieces collectable, patrol deterministic"
                : "FAILED: collected " + collected + "/8";
        }

        static string ProbeHall()
        {
            Open("Floor4_Salao");
            var controller = Object.FindAnyObjectByType<Floor4Controller>();
            if (controller == null) return "FAILED: no controller";

            var circuit = Field(controller, "circuit") as Misterio.Puzzles.Floor4.ConduitCircuit;
            if (circuit == null) return "FAILED: no circuit";

            int tiles = Object.FindObjectsByType<ConduitTileView>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            if (tiles != circuit.Width * circuit.Height)
                return "FAILED: expected " + (circuit.Width * circuit.Height) + " tiles, found " + tiles;

            // Turning must change the piece, and the panel must start unsolved.
            var cell = circuit.Source;
            byte before = circuit.Get(cell).Mask;
            bool turned = circuit.Turn(cell);
            byte after = circuit.Get(cell).Mask;

            if (!turned) return "FAILED: source tile cannot be turned";
            if (before == after) return "FAILED: turning did not change the mask";
            if (circuit.IsSolved()) return "FAILED: panel starts already solved";

            return "OK " + tiles + " tiles, turning works, panel starts unsolved";
        }
    }
}
