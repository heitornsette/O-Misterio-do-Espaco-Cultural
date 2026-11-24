using System.Collections.Generic;
using Misterio.Core;
using Misterio.Puzzles.Floor3;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Misterio.Gameplay
{
    /// <summary>
    /// Archive floor: Moema patrols a fixed loop while the guard collects the scattered pieces
    /// without stepping on loose boards or entering her sight cone.
    /// Being seen is not a fail state: it plays the short alert beat, costs the time spent and
    /// restarts the positions, so the night clock is the real pressure.
    /// </summary>
    public sealed class Floor3Controller : PuzzleFloor
    {
        [Header("Patrol")]
        [SerializeField] Transform moemaView;
        [SerializeField, Min(0.05f)] float patrolSpeed = 0.42f;
        [SerializeField, Min(0.2f)] float viewDistance = 1.25f;
        [SerializeField, Range(5f, 80f)] float halfAngleDegrees = 38f;
        [SerializeField] Vector2[] patrolPoints =
        {
            new Vector2(-1.35f, 0.30f), new Vector2(0.05f, 0.30f),
            new Vector2(0.05f, -0.62f), new Vector2(-1.35f, -0.62f)
        };

        [Header("Hazards and pieces")]
        [SerializeField] Rect[] noisyBoards =
        {
            new Rect(-0.55f, -0.20f, 0.30f, 0.16f),
            new Rect(0.65f, 0.05f, 0.30f, 0.16f),
            new Rect(1.20f, -0.55f, 0.30f, 0.16f)
        };
        [SerializeField] Transform[] pieceViews;
        [SerializeField] Vector2[] piecePoints =
        {
            new Vector2(-1.10f, -0.30f), new Vector2(0.40f, 0.28f),
            new Vector2(1.15f, -0.10f), new Vector2(1.45f, 0.35f),
            new Vector2(-1.25f, 0.62f), new Vector2(0.75f, -0.72f),
            new Vector2(1.35f, 0.30f), new Vector2(-0.85f, -0.70f)
        };
        [SerializeField] Transform[] boardViews;
        [SerializeField] Rect[] caseBlockers =
        {
            new Rect(-0.30f, 0.55f, 0.55f, 0.22f),
            new Rect(0.85f, 0.55f, 0.55f, 0.22f)
        };

        [Header("Detection")]
        [SerializeField] LineRenderer sightCone;
        [SerializeField, Min(0.2f)] float graceAfterAlert = 1.6f;

        StealthPatrol patrol;
        readonly HashSet<int> collected = new HashSet<int>();
        readonly List<Rect> blockers = new List<Rect>();
        float graceTimer;
        bool alertActive;
        int alerts;
        float noiseCooldown;
        Vector2 lastGuardPosition;

        public int CollectedPieces => collected.Count;
        public int TotalPieces => piecePoints != null ? piecePoints.Length : 0;
        public StealthPatrol Patrol => patrol;

        protected override void Awake()
        {
            base.Awake();
            floorNumber = 3;
            objectiveText = "Recolha as peças do acervo sem ser visto por Moema.";
            solvedPause = 1.6f;
        }

        protected override void OnFloorReady()
        {
            patrol = new StealthPatrol(patrolPoints, patrolSpeed, viewDistance, halfAngleDegrees, 0.35f);

            blockers.AddRange(caseBlockers);
            Guard.Configure(new Rect(-1.75f, -0.95f, 3.5f, 1.80f), blockers);

            // Pieces recovered before the CC04A alert stay recovered when the floor resumes.
            for (int i = 0; i < TotalPieces; i++)
                if (Session.HasArchivePiece(i)) collected.Add(i);

            for (int i = 0; i < pieceViews.Length && i < piecePoints.Length; i++)
            {
                if (pieceViews[i] == null) continue;
                var piece = pieceViews[i].gameObject.AddComponent<PieceInteractable>();
                piece.Bind(this, i);
                if (collected.Contains(i)) pieceViews[i].gameObject.SetActive(false);
            }

            SyncViews();
            UpdateCounter();
            lastGuardPosition = Guard.Position;
            // A short grace on entry (and on every return from the alert cutscene) prevents an
            // instant re-detection while the player is still reading the HUD.
            graceTimer = graceAfterAlert;
            if (collected.Count > 0)
                Hud.ShowHint("Você voltou ao corredor. As peças que já recolheu continuam com você.", 6f);
            else
                Hud.ShowHint("Moema patrulha em círculo. Espere ela virar de costas.", 7f);
        }

        protected override void OnFloorTick(float deltaTime)
        {
            if (alertActive) return;

            graceTimer = Mathf.Max(0f, graceTimer - deltaTime);
            noiseCooldown = Mathf.Max(0f, noiseCooldown - deltaTime);
            patrol.Tick(deltaTime);
            if (moemaView != null)
                moemaView.position = new Vector3(patrol.Position.x, patrol.Position.y, moemaView.position.z);

            UpdateSightCone();
            CheckLooseBoard(deltaTime);

            if (graceTimer <= 0f && patrol.CanSee(Guard.Position, BuildOccluders()))
                TriggerAlert();

            if (collected.Count >= TotalPieces && !Solved) CompleteFloor();
        }

        Vector2[] BuildOccluders()
        {
            var flat = new Vector2[caseBlockers.Length * 4];
            for (int i = 0; i < caseBlockers.Length; i++)
            {
                Rect rect = caseBlockers[i];
                flat[i * 4 + 0] = new Vector2(rect.xMin, rect.yMin);
                flat[i * 4 + 1] = new Vector2(rect.xMax, rect.yMin);
                flat[i * 4 + 2] = new Vector2(rect.xMax, rect.yMax);
                flat[i * 4 + 3] = new Vector2(rect.xMin, rect.yMax);
            }
            return flat;
        }

        /// <summary>
        /// A loose board creaks under the guard's feet and pulls Moema's attention towards him.
        /// </summary>
        void CheckLooseBoard(float deltaTime)
        {
            Vector2 position = Guard.Position;
            bool moved = (position - lastGuardPosition).sqrMagnitude > 0.0000004f;
            lastGuardPosition = position;
            if (!moved || noiseCooldown > 0f) return;
            if (!OnNoisyBoard()) return;

            noiseCooldown = 2.2f;
            patrol.Distract(position, 1.5f);
            FloorSfx.Instance?.Turn();
            Hud.ShowHint("A tábua rangeu. Moema virou para o barulho.", 3.5f);
        }

        void UpdateSightCone()
        {
            if (sightCone == null) return;
            float angle = halfAngleDegrees * Mathf.Deg2Rad;
            Vector2 forward = patrol.Facing.normalized;
            sightCone.positionCount = 3;
            sightCone.SetPosition(0, patrol.Position);
            sightCone.SetPosition(1, patrol.Position + Rotate(forward, angle) * viewDistance);
            sightCone.SetPosition(2, patrol.Position + Rotate(forward, -angle) * viewDistance);
        }

        static Vector2 Rotate(Vector2 vector, float radians)
        {
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            return new Vector2(vector.x * cos - vector.y * sin, vector.x * sin + vector.y * cos);
        }

        void TriggerAlert()
        {
            if (alertActive || Solved) return;
            alerts++;
            StartCoroutine(AlertRoutine());
        }

        System.Collections.IEnumerator AlertRoutine()
        {
            alertActive = true;
            Guard.CanMove = false;
            Guard.CanInteract = false;
            Hud.SetPrompt(string.Empty);
            FloorSfx.Instance?.Error();

            // The short alert beat is the CC04A cutscene: it shows Moema reacting and then returns
            // to this floor with the pieces already collected still in hand.
            Session.RequestResumeFloor(3);
            Session.SetClockRunning(false);
            if (moemaView != null)
            {
                var renderer = moemaView.GetComponent<SpriteRenderer>();
                if (renderer != null) renderer.color = new Color(1f, 0.82f, 0.72f, 1f);
            }

            yield return new WaitForSeconds(0.35f);
            GameFlow.EnsureExists().Load(GameFlow.Cutscene04A);
        }

        public void Collect(int pieceIndex)
        {
            if (alertActive || pieceIndex < 0 || pieceIndex >= TotalPieces) return;
            if (!collected.Add(pieceIndex)) return;

            if (pieceViews != null && pieceIndex < pieceViews.Length && pieceViews[pieceIndex] != null)
                pieceViews[pieceIndex].gameObject.SetActive(false);

            Session.SetArchivePiece(pieceIndex, true);
            Session.CollectPiece();
            FloorSfx.Instance?.Pickup();
            UpdateCounter();
            NoteProgress();
            if (collected.Count >= TotalPieces) CompleteFloor();
        }

        public bool IsCollected(int pieceIndex) => collected.Contains(pieceIndex);

        /// <summary>True while the guard stands on a loose board; used for the creak warning.</summary>
        public bool OnNoisyBoard()
        {
            if (noisyBoards == null) return false;
            Vector2 position = Guard.Position;
            foreach (var board in noisyBoards)
                if (board.Contains(position)) return true;
            return false;
        }

        void SyncViews()
        {
            if (boardViews != null)
                for (int i = 0; i < boardViews.Length; i++)
                    if (boardViews[i] != null) boardViews[i].gameObject.SetActive(true);
        }

        void UpdateCounter()
        {
            Hud.SetCounter("Peças recolhidas: " + collected.Count + "/" + TotalPieces);
            Hud.SetObjective(collected.Count >= TotalPieces
                ? "Elevador de serviço liberado."
                : "Recolha as peças do acervo sem ser visto por Moema.");
        }

        /// <summary>Development only: collects every piece so the route capture can move on.</summary>
        protected override void AutoCompleteForCapture()
        {
            for (int i = 0; i < TotalPieces; i++) Collect(i);
            UpdateCounter();
            CompleteFloor();
        }

        protected override string HintForFloor() =>
            "Observe o giro de Moema: o cone mostra exatamente até onde ela vê.";
    }
}
