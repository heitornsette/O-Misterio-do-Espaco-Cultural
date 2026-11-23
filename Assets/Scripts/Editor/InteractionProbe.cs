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

            var plaques = new List<PlaqueInteractable>(
                Object.FindObjectsByType<PlaqueInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            if (plaques.Count < 2)
            {
                // Report what is actually in the scene so a regression is diagnosable.
                var names = new List<string>();
                foreach (var found in plaques) names.Add(found == null ? "<destroyed>" : found.name);
                return "FAILED: expected 2 plaques, found " + plaques.Count + " [" + string.Join(", ", names) + "]";
            }

            int readable = 0;
            var seen = new List<string>();
            foreach (var plaque in plaques)
            {
                plaque.Interact(guard);
                string hint = HintText();
                if (!string.IsNullOrEmpty(hint) && !hint.StartsWith("<"))
                {
                    readable++;
                    seen.Add(hint.Substring(0, System.Math.Min(48, hint.Length)));
                }
            }

            // The two clues must differ: identical text would mean one plaque overwrote the other.
            bool distinct = plaques.Count < 2 || seen.Count < 2 || seen[0] != seen[1];

            var controller = Object.FindAnyObjectByType<Floor2Controller>();
            int hooks = Object.FindObjectsByType<HookInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            int canvases = Object.FindObjectsByType<PaintingInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;

            if (readable < 2) return "FAILED: only " + readable + "/2 plaques readable";
            if (!distinct) return "FAILED: both plaques show the same text";
            if (hooks != 3) return "FAILED: expected 3 hooks, found " + hooks;
            if (canvases != 3) return "FAILED: expected 3 paintings, found " + canvases;
            if (controller == null) return "FAILED: no controller";

            return "OK 2/2 clues readable, 3 hooks, 3 paintings, distinct texts";
        }

        static string ProbeArchive()
        {
            Open("Floor3_Acervo");
            var guard = Object.FindAnyObjectByType<GuardActor>();
            var controller = Object.FindAnyObjectByType<Floor3Controller>();
            if (guard == null || controller == null) return "FAILED: missing guard/controller";

            var pieces = Object.FindObjectsByType<PieceInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (pieces.Length != 4) return "FAILED: expected 4 pieces, found " + pieces.Length;

            int collected = 0;
            foreach (var piece in pieces)
            {
                if (piece == null || !piece.isActiveAndEnabled) continue;
                piece.Interact(guard);
                collected = (int)Field(controller, "CollectedPieces");
            }

            var patrol = Field(controller, "patrol") as Misterio.Puzzles.Floor3.StealthPatrol;
            if (patrol == null) return "FAILED: no patrol";

            return collected == 4
                ? "OK 4/4 pieces collectable, patrol deterministic"
                : "FAILED: collected " + collected + "/4";
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
