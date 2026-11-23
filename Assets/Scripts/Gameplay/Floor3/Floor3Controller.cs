using System.Collections.Generic;
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
            new Vector2(1.15f, -0.10f), new Vector2(1.45f, 0.35f)
        };
        [SerializeField] Transform[] boardViews;
        [SerializeField] Rect[] caseBlockers =
        {
            new Rect(-0.30f, 0.55f, 0.55f, 0.22f),
            new Rect(0.85f, 0.55f, 0.55f, 0.22f)
        };

        [Header("Detection")]
        [SerializeField] LineRenderer sightCone;
        [SerializeField, Min(0.5f)] float alertPause = 1.1f;
        [SerializeField, Min(0.2f)] float graceAfterAlert = 1.6f;

        StealthPatrol patrol;
        readonly HashSet<int> collected = new HashSet<int>();
        readonly List<Rect> blockers = new List<Rect>();
        float graceTimer;
        bool alertActive;
        int alerts;
        Vector2 guardStart;

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
            guardStart = Guard.Position;

            blockers.AddRange(caseBlockers);
            Guard.Configure(new Rect(-1.75f, -0.95f, 3.5f, 1.45f), blockers);

            for (int i = 0; i < pieceViews.Length && i < piecePoints.Length; i++)
            {
                if (pieceViews[i] == null) continue;
                var piece = pieceViews[i].gameObject.AddComponent<PieceInteractable>();
                piece.Bind(this, i);
            }

            SyncViews();
            UpdateCounter();
            Hud.ShowHint("Moema patrulha em círculo. Espere ela virar de costas.", 7f);
        }

        protected override void OnFloorTick(float deltaTime)
        {
            if (alertActive) return;

            graceTimer = Mathf.Max(0f, graceTimer - deltaTime);
            patrol.Tick(deltaTime);
            if (moemaView != null)
                moemaView.position = new Vector3(patrol.Position.x, patrol.Position.y, moemaView.position.z);

            UpdateSightCone();

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
            Hud.ShowHint("Moema percebeu! Você voltou ao início do corredor.", 4f);

            // The short alert beat stands in for the CC04A cutscene inside the floor.
            if (moemaView != null)
            {
                var renderer = moemaView.GetComponent<SpriteRenderer>();
                if (renderer != null) renderer.color = new Color(1f, 0.82f, 0.72f, 1f);
            }

            yield return new WaitForSeconds(alertPause);

            if (moemaView != null)
            {
                var renderer = moemaView.GetComponent<SpriteRenderer>();
                if (renderer != null) renderer.color = Color.white;
            }

            Guard.Teleport(guardStart);
            graceTimer = graceAfterAlert;
            alertActive = false;
            Guard.CanMove = true;
            Guard.CanInteract = true;
            UpdateCounter();
        }

        public void Collect(int pieceIndex)
        {
            if (alertActive || pieceIndex < 0 || pieceIndex >= TotalPieces) return;
            if (!collected.Add(pieceIndex)) return;

            if (pieceViews != null && pieceIndex < pieceViews.Length && pieceViews[pieceIndex] != null)
                pieceViews[pieceIndex].gameObject.SetActive(false);

            Session.CollectPiece();
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

        protected override string HintForFloor() =>
            "Observe o giro de Moema: o cone mostra exatamente até onde ela vê.";
    }
}
