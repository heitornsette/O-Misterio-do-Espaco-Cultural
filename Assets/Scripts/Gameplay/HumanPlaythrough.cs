using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Misterio.Core;
using Misterio.Puzzles.Floor1;
using Misterio.Puzzles.Floor2;
using Misterio.Puzzles.Floor4;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Misterio.Gameplay
{
    /// <summary>
    /// Plays the whole game as a person would: it walks the guard with real movement input, presses
    /// the interact key on real interactables, steps the conduit cursor and turns tiles with the
    /// same keys a player uses. It never calls a puzzle API to solve something, so any fault in the
    /// input path shows up here.
    /// Every frame it records a state line and periodically saves a screenshot, which together form
    /// the evidence a reviewer can check without being present.
    /// Activated with -playtest &lt;folder&gt;; completely inert otherwise.
    /// </summary>
    public sealed class HumanPlaythrough : MonoBehaviour
    {
        public static HumanPlaythrough Instance { get; private set; }

        [SerializeField] float captureInterval = 0.5f;
        [SerializeField] int maxShots = 400;

        StreamWriter log;
        string outputDirectory;
        int shot;
        float nextCapture;
        float sceneEnteredAt;
        string sceneName;

        // Findings collected during the run.
        readonly List<string> findings = new List<string>();

        // Steering state.
        Vector2 walkTarget;
        bool hasWalkTarget;

        // Per-scene progress guards so a stuck step is reported instead of hanging forever.
        float stepStartedAt;
        Vector2 lastPosition;
        float stillSince;
        int sameSceneFrames;

        static bool started;

        void Awake()
        {
            string folder = ReadArgument("-playtest");
            if (folder == null)
            {
                enabled = false;
                return;
            }

            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // The playthrough must outlive the scenes it walks through. Without this every scene
            // change created a fresh instance that truncated the frame log and restarted the run,
            // which made the evidence unreadable.
            DontDestroyOnLoad(gameObject);
            if (started) return;
            started = true;
            outputDirectory = Path.IsPathRooted(folder) ? folder : Path.Combine(Application.persistentDataPath, folder);
            Directory.CreateDirectory(outputDirectory);
            log = new StreamWriter(Path.Combine(outputDirectory, "frames.csv"), false, new UTF8Encoding(false));
            log.WriteLine("time,scene,frame,px,py,nearest,walking,hud,extra");
            Application.runInBackground = true;
            string dawnArgument = ReadArgument("-forcedawn");
            if (!string.IsNullOrEmpty(dawnArgument) && int.TryParse(dawnArgument, out int dawnFloor))
                ForceDawnOn(dawnFloor);

            PlaytestInput.Enable();
            SceneManager.sceneLoaded += OnSceneLoaded;
            StartCoroutine(Run());
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            PlaytestInput.Disable();
            log?.Flush();
            log?.Dispose();
            if (Instance == this) Instance = null;
        }

        static string ReadArgument(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return null;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            sceneName = scene.name;
            sceneEnteredAt = Time.time;
            hasWalkTarget = false;
            stepStartedAt = Time.time;
        }

        void Update()
        {
            if (log == null) return;

            RecordFrame();
            CaptureIfDue();
        }

        /// <summary>One-shot presses are cleared here, after every Update had a chance to read them.</summary>
        void LateUpdate()
        {
            PlaytestInput.ClearOneShots();
        }

        void RecordFrame()
        {
            var guard = FindAnyObjectByType<GuardActor>();
            var hud = FindAnyObjectByType<HudView>();
            string hudText = hud != null ? hud.VisibleHint : string.Empty;

            string nearest = guard != null && guard.Nearest != null ? guard.Nearest.name : string.Empty;
            bool walking = guard != null && PlaytestInput.Move.sqrMagnitude > 0.01f;
            Vector2 p = guard != null ? guard.Position : Vector2.zero;

            log.WriteLine(string.Join(",",
                Time.time.ToString("F3", CultureInfo.InvariantCulture),
                sceneName ?? SceneManager.GetActiveScene().name,
                Time.frameCount.ToString(CultureInfo.InvariantCulture),
                p.x.ToString("F3", CultureInfo.InvariantCulture),
                p.y.ToString("F3", CultureInfo.InvariantCulture),
                Escape(nearest),
                walking ? "1" : "0",
                Escape(hudText),
                Extra()));

            if (Time.frameCount % 60 == 0) log.Flush();
        }

        /// <summary>Short puzzle-specific status so the log shows progress, not just position.</summary>
        string Extra()
        {
            var floor1 = FindAnyObjectByType<Floor1Controller>();
            if (floor1 != null && floor1.Board != null)
                return "f1_pending=" + floor1.Board.PendingStatues();

            var floor2 = FindAnyObjectByType<Floor2Controller>();
            if (floor2 != null && floor2.Puzzle != null)
                return "f2_placed=" + floor2.Puzzle.PlacedCount() + "|carried=" + floor2.Carried;

            var floor3 = FindAnyObjectByType<Floor3Controller>();
            if (floor3 != null)
                return "f3_pieces=" + floor3.CollectedPieces + "/" + floor3.TotalPieces +
                       "|clock=" + (GameSession.Instance != null ? GameSession.Instance.ClockText() : "?");

            var floor4 = FindAnyObjectByType<Floor4Controller>();
            if (floor4 != null && floor4.Circuit != null)
                return "f4_solved=" + (floor4.Circuit.IsSolved() ? 1 : 0);

            var session = Misterio.Core.GameSession.Instance;
            return session != null ? "clock=" + session.ClockText() : string.Empty;
        }

        static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Replace(',', ';').Replace('\n', ' ').Replace('\r', ' ');
        }

        void CaptureIfDue()
        {
            if (shot >= maxShots || Time.time < nextCapture) return;
            nextCapture = Time.time + captureInterval;
            string name = "shot_" + shot.ToString("0000") + "_" + (sceneName ?? "scene") + ".png";
            shot++;
            ScreenCapture.CaptureScreenshot(Path.Combine(outputDirectory, name));
        }

        // ------------------------------------------------------------------ driver

        IEnumerator Run()
        {
            yield return new WaitForSeconds(1.5f);

            while (true)
            {
                string current = SceneManager.GetActiveScene().name;
                if (current == "CC09_A_Abertura_Precisa_Esperar" || current == "CC10_A_Ultima_Sala_Ainda_no_Escuro")
                {
                    yield return TestResultButtons();
                    continue;
                }

                if (current == "Credits")
                {
                    // The credits are the end of the night: record and stop.
                    yield return new WaitForSeconds(3f);
                    WriteFindings();
                    Debug.Log("[Playtest] finished. Findings: " + (findings.Count == 0 ? "none" : findings.Count.ToString()));
                    yield return new WaitForSeconds(1f);
                    Application.Quit();
                    yield break;
                }

                if (current.StartsWith("CC"))
                {
                    yield return WaitCutscene();
                    continue;
                }

                // The forced ending must run before the floor's own handler, otherwise the puzzle
                // simply completes and the time-out path is never exercised.
                if (current.StartsWith("Floor") && forcedawnFloor > 0)
                {
                    int number = current.Contains("1") ? 1 : current.Contains("2") ? 2 : current.Contains("3") ? 3 : 4;
                    if (number == forcedawnFloor)
                    {
                        yield return new WaitForSeconds(2f);
                        Misterio.Core.GameSession.Instance?.JumpToDawn();
                        forcedawnFloor = -1;
                        yield return new WaitForSeconds(8f);
                        continue;
                    }
                }

                if (current == "Exterior_Chegada") { yield return PlayExterior(); continue; }
                if (current == "Epilogue_Gramado") { yield return PlayEpilogue(); continue; }
                if (current == "Floor1_Estatuas") { yield return PlayFloor1(); continue; }
                if (current == "Floor2_Galeria") { yield return PlayFloor2(); continue; }
                if (current == "Floor3_Acervo") { yield return PlayFloor3(); continue; }
                if (current == "Floor4_Salao") { yield return PlayFloor4(); continue; }

                findings.Add("Unknown scene reached: " + current);
                yield return new WaitForSeconds(1f);
            }
        }

        int forcedawnFloor = -1;
        static bool testedExit;

        /// <summary>Runs the clock to 06:00 on the given floor so the time-out ending is reached.</summary>
        public void ForceDawnOn(int floor) => forcedawnFloor = floor;

        IEnumerator WaitCutscene()
        {
            float started = Time.time;
            while (Time.time - started < 90f)
            {
                string current = SceneManager.GetActiveScene().name;
                if (!current.StartsWith("CC") && current != "Credits") yield break;
                yield return null;
            }
            findings.Add("Cutscene did not finish within 90s: " + SceneManager.GetActiveScene().name);
        }

        /// <summary>
        /// Presses the retry button of a time-out ending through the real UnityEvent, which only
        /// executes persistent listeners at runtime. This is the only place those buttons are
        /// exercised, since the normal route never times out.
        /// </summary>
        IEnumerator TestResultButtons()
        {
            string scene = SceneManager.GetActiveScene().name;
            yield return new WaitForSeconds(1.5f);

            var buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Button retry = null;
            Button exit = null;
            foreach (var button in buttons)
            {
                if (button == null || button.onClick.GetPersistentEventCount() == 0) continue;
                string method = button.onClick.GetPersistentMethodName(0);
                if (method == "RetrySameFloor" || method == "RetrySameAttempt") retry = button;
                if (method == "ReturnToMenu") exit = button;
            }

            if (retry == null) findings.Add(scene + ": no retry button wired");
            if (exit == null) findings.Add(scene + ": no exit button wired");

            // Test the exit path once, on the first ending reached, because it restarts the night.
            if (exit != null && !testedExit)
            {
                testedExit = true;
                exit.onClick.Invoke();
                float exitWait = 0f;
                while (exitWait < 8f && SceneManager.GetActiveScene().name == scene)
                {
                    exitWait += Time.deltaTime;
                    yield return null;
                }
                string exited = SceneManager.GetActiveScene().name;
                if (exited == scene) findings.Add(scene + ": exit button did not leave the ending");
                else Debug.Log("[Playtest] " + scene + " exit -> " + exited);
                yield break;
            }

            if (retry == null) yield break;
            retry.onClick.Invoke();
            float waited = 0f;
            while (waited < 8f && SceneManager.GetActiveScene().name == scene)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            if (SceneManager.GetActiveScene().name == scene)
                findings.Add(scene + ": retry button did not leave the ending");
            else
                Debug.Log("[Playtest] " + scene + " retry -> " + SceneManager.GetActiveScene().name);
        }

        IEnumerator PlayExterior()
        {
            // Walk right until the scene changes; the entrance ends it.
            yield return WalkRight(80f, "Exterior_Chegada");
        }

        IEnumerator PlayEpilogue()
        {
            yield return WalkRight(80f, "Epilogue_Gramado");
        }

        IEnumerator WalkRight(float timeout, string expectedScene)
        {
            float started = Time.time;
            while (Time.time - started < timeout)
            {
                if (SceneManager.GetActiveScene().name != expectedScene) yield break;
                PlaytestInput.Move = Vector2.right;
                yield return null;
            }
            findings.Add("Could not leave " + expectedScene + " by walking right");
        }

        /// <summary>Walks to a world point using real movement input. Returns false on timeout.</summary>
        IEnumerator WalkTo(Vector2 target, float tolerance = 0.06f, float timeout = 12f)
        {
            float started = Time.time;
            var guard = FindAnyObjectByType<GuardActor>();
            if (guard == null) yield break;

            while (Time.time - started < timeout)
            {
                Vector2 delta = target - guard.Position;
                if (delta.magnitude <= tolerance) break;

                // Move one axis at a time when the error is clearly axis-aligned, which is what a
                // player does with a keyboard and what the collision model expects.
                Vector2 direction = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y)
                    ? new Vector2(Mathf.Sign(delta.x), 0f)
                    : new Vector2(0f, Mathf.Sign(delta.y));
                PlaytestInput.Move = direction;
                yield return null;
            }

            PlaytestInput.Move = Vector2.zero;
            if ((target - guard.Position).magnitude > tolerance)
                findings.Add("Could not reach " + target.ToString("F2") + " in " + SceneManager.GetActiveScene().name);
        }

        /// <summary>Presses the interact key once and lets a frame pass.</summary>
        IEnumerator PressInteract()
        {
            PlaytestInput.InteractPressed = true;
            PlaytestInput.MarkPress();
            yield return null;
            yield return null;
        }

        IEnumerator PlayFloor1()
        {
            var controller = FindAnyObjectByType<Floor1Controller>();
            var guard = FindAnyObjectByType<GuardActor>();
            if (controller == null || guard == null) yield break;

            var board = controller.Board;
            var solution = SokobanSolver.Solve(board);
            if (solution == null)
            {
                findings.Add("Floor1: solver found no solution for the shipped layout");
                yield break;
            }

            // Replay the solver's steps as real walking. Walking into a statue pushes it.
            // The target is derived from where the guard actually is: the board only tracks the
            // player on a push, so using board.Player here would steer him back and forth.
            foreach (var step in solution)
            {
                if (SceneManager.GetActiveScene().name != "Floor1_Estatuas") yield break;
                Vector2Int from = controller.CellOf(guard.Position);
                Vector2 target = controller.WorldOf(from + step);
                yield return WalkTo(target, 0.05f, 3f);
                yield return null;

                // Nudge into the statue if the cell was not reached, so the push still happens.
                if (controller.CellOf(guard.Position) != from + step)
                {
                    PlaytestInput.Move = new Vector2(step.x, step.y);
                    for (int i = 0; i < 20; i++) yield return null;
                    PlaytestInput.Move = Vector2.zero;
                    yield return null;
                }
            }

            // The floor completes on its own once solved; give it the beat it needs.
            float started = Time.time;
            while (Time.time - started < 6f && SceneManager.GetActiveScene().name == "Floor1_Estatuas")
                yield return null;

            if (SceneManager.GetActiveScene().name == "Floor1_Estatuas")
                findings.Add("Floor1: puzzle not solved after replaying the solution");
        }

        IEnumerator PlayFloor2()
        {
            var controller = FindAnyObjectByType<Floor2Controller>();
            if (controller == null) yield break;

            var paintings = FindObjectsByType<PaintingInteractable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var hooks = FindObjectsByType<HookInteractable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (paintings.Length == 0 || hooks.Length == 0)
            {
                findings.Add("Floor2: no paintings or hooks found");
                yield break;
            }

            // The solution is known; hang each painting on its own hook in order.
            // Objects are matched by their own index, not by the order the scene query returned
            // them, which is arbitrary.
            int[] solution = { 1, 0, 2 };
            for (int slot = 0; slot < solution.Length; slot++)
            {
                if (SceneManager.GetActiveScene().name != "Floor2_Galeria") yield break;

                PaintingInteractable painting = null;
                HookInteractable hook = null;
                foreach (var candidate in paintings)
                    if (candidate != null && candidate.PaintingIndex == solution[slot]) painting = candidate;
                foreach (var candidate in hooks)
                    if (candidate != null && candidate.Slot == slot) hook = candidate;

                if (painting == null || hook == null)
                {
                    findings.Add("Floor2: could not find painting " + solution[slot] + " or hook " + slot);
                    continue;
                }

                yield return WalkTo(painting.InteractPoint, 0.10f, 12f);
                yield return PressInteract();

                yield return WalkTo(hook.InteractPoint, 0.10f, 12f);
                yield return PressInteract();
            }

            float started = Time.time;
            while (Time.time - started < 8f && SceneManager.GetActiveScene().name == "Floor2_Galeria")
                yield return null;

            if (SceneManager.GetActiveScene().name == "Floor2_Galeria")
            {
                var puzzle = controller.Puzzle;
                findings.Add("Floor2: puzzle not solved after hanging every painting (placed=" +
                             (puzzle != null ? puzzle.PlacedCount() : -1) +
                             ", correct=" + (puzzle != null ? puzzle.CorrectCount() : -1) + ")");
            }
        }

        IEnumerator PlayFloor3()
        {
            var controller = FindAnyObjectByType<Floor3Controller>();
            if (controller == null) yield break;

            var guard = FindAnyObjectByType<GuardActor>();
            if (guard == null) yield break;

            // Moema patrols a rectangle. Pieces outside it can be taken at leisure; the ones inside
            // need a gap in the patrol, so they are left for last. That is the order a person takes.
            var inside = new List<Vector2>
            {
                new Vector2(-1.10f, -0.30f)   // the only piece inside the patrol rectangle
            };

            Vector2 lastPos = guard.Position;
            float lastMoveAt = Time.time;
            int skipIndex = 0;
            float deadline = Time.time + 240f;

            while (Time.time < deadline && SceneManager.GetActiveScene().name == "Floor3_Acervo")
            {
                var pieces = FindObjectsByType<PieceInteractable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                if (pieces.Length == 0) break;

                if ((guard.Position - lastPos).sqrMagnitude > 0.0002f)
                {
                    lastPos = guard.Position;
                    lastMoveAt = Time.time;
                }
                else if (Time.time - lastMoveAt > 2.0f)
                {
                    skipIndex++;
                    lastMoveAt = Time.time;
                }

                var patrol = controller.Patrol;

                // Outside pieces first, nearest first.
                var outsidePieces = new List<PieceInteractable>();
                var insidePieces = new List<PieceInteractable>();
                foreach (var piece in pieces)
                {
                    if (piece == null) continue;
                    bool risky = false;
                    foreach (var point in inside)
                        if (Vector2.Distance(piece.InteractPoint, point) < 0.35f) risky = true;
                    (risky ? insidePieces : outsidePieces).Add(piece);
                }
                outsidePieces.Sort((a, b) =>
                    Vector2.Distance(a.InteractPoint, guard.Position)
                        .CompareTo(Vector2.Distance(b.InteractPoint, guard.Position)));
                insidePieces.Sort((a, b) =>
                    Vector2.Distance(a.InteractPoint, guard.Position)
                        .CompareTo(Vector2.Distance(b.InteractPoint, guard.Position)));

                var ordered = new List<PieceInteractable>(outsidePieces);
                ordered.AddRange(insidePieces);
                if (ordered.Count == 0) break;
                PieceInteractable targetPiece = ordered[skipIndex % ordered.Count];
                if (targetPiece == null) break;

                Vector2 delta = targetPiece.InteractPoint - guard.Position;
                if (delta.magnitude <= targetPiece.InteractRadius * 0.8f)
                {
                    PlaytestInput.Move = Vector2.zero;
                    yield return PressInteract();
                    continue;
                }

                Vector2 direction = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y)
                    ? new Vector2(Mathf.Sign(delta.x), 0f)
                    : new Vector2(0f, Mathf.Sign(delta.y));

                if (patrol != null)
                {
                    // Do not step into the cone; step away instead of standing still, because
                    // standing still keeps the guard inside the sweep.
                    Vector2 next = guard.Position + direction * 0.10f;
                    if (patrol.CanSee(next))
                    {
                        Vector2 away = guard.Position - patrol.Position;
                        away.y = 0f;
                        if (Mathf.Abs(away.x) < 0.01f) away = Vector2.left;
                        Vector2 retreat = new Vector2(Mathf.Sign(away.x), 0f);
                        PlaytestInput.Move = patrol.CanSee(guard.Position + retreat * 0.10f) ? Vector2.zero : retreat;
                        yield return null;
                        continue;
                    }
                }

                PlaytestInput.Move = direction;
                yield return null;
            }

            PlaytestInput.Move = Vector2.zero;

            float started = Time.time;
            while (Time.time - started < 8f && SceneManager.GetActiveScene().name == "Floor3_Acervo")
                yield return null;

            if (SceneManager.GetActiveScene().name == "Floor3_Acervo")
                findings.Add("Floor3: puzzle not solved after navigating the patrol (" +
                             controller.CollectedPieces + "/" + controller.TotalPieces + ")");
        }

        IEnumerator PlayFloor4()
        {
            var controller = FindAnyObjectByType<Floor4Controller>();
            if (controller == null) yield break;
            var circuit = controller.Circuit;
            if (circuit == null) yield break;

            // Walk to the panel, then drive the cursor with real key presses.
            var guard = FindAnyObjectByType<GuardActor>();
            yield return WalkTo(new Vector2(controller.WorldOf(circuit.Source).x - 1.0f,
                                            controller.WorldOf(circuit.Source).y), 0.15f, 12f);

            var solvedMasks = controller.SolvedMasksForPlaytest;
            for (int y = 0; y < circuit.Height; y++)
            {
                for (int x = 0; x < circuit.Width; x++)
                {
                    if (SceneManager.GetActiveScene().name != "Floor4_Salao") yield break;
                    byte wanted = solvedMasks[y * circuit.Width + x];
                    if (wanted == 0) continue;

                    // Move the cursor onto this tile, one step per frame.
                    yield return MoveCursorTo(new Vector2Int(x, y));
                    // Turn until the mask matches, at most four times.
                    for (int turn = 0; turn < 4; turn++)
                    {
                        if (circuit.Get(new Vector2Int(x, y)).Mask == wanted) break;
                        PlaytestInput.TurnPressed = true;
                        PlaytestInput.MarkPress();
                        yield return null;
                        yield return null;
                    }
                }
            }

            float started = Time.time;
            while (Time.time - started < 6f && SceneManager.GetActiveScene().name == "Floor4_Salao")
                yield return null;

            if (SceneManager.GetActiveScene().name == "Floor4_Salao")
                findings.Add("Floor4: puzzle not solved after aligning every conduit");
        }

        IEnumerator MoveCursorTo(Vector2Int target)
        {
            var controller = FindAnyObjectByType<Floor4Controller>();
            if (controller == null) yield break;

            float deadline = Time.time + 6f;
            while (Time.time < deadline)
            {
                Vector2Int cursor = controller.CursorForPlaytest;
                if (cursor == target) break;

                Vector2Int delta = target - cursor;
                PlaytestInput.CursorStep = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y)
                    ? new Vector2Int(System.Math.Sign(delta.x), 0)
                    : new Vector2Int(0, System.Math.Sign(delta.y));
                PlaytestInput.MarkPress();
                yield return null;
                yield return null;
            }
        }

        void OnApplicationQuit()
        {
            WriteFindings();
        }

        public void WriteFindings()
        {
            if (outputDirectory == null) return;
            log?.Flush();
            File.WriteAllLines(Path.Combine(outputDirectory, "findings.txt"),
                findings.Count > 0 ? findings.ToArray() : new[] { "NONE" });
        }

        /// <summary>Called by the build entry point after the run finishes.</summary>
        public IEnumerator FinishAndQuit()
        {
            yield return new WaitForSeconds(1f);
            WriteFindings();
            log?.Flush();
            Application.Quit();
        }
    }
}
