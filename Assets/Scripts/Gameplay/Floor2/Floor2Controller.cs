using System.Collections.Generic;
using Misterio.Puzzles.Floor2;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Misterio.Gameplay
{
    /// <summary>
    /// Gallery floor: the guard reads the plaques, picks a painting from the floor and hangs it on
    /// a hook. Carrying slows him down, which is what makes the deduction feel physical instead of
    /// like a menu.
    /// </summary>
    public sealed class Floor2Controller : PuzzleFloor
    {
        [Header("Slots")]
        [SerializeField] Transform[] hookViews;
        [SerializeField] Transform[] floorPaintingViews;
        [SerializeField] SpriteRenderer carriedView;

        [Header("Movement")]
        [SerializeField, Min(0.2f)] float carryingSpeedMultiplier = 0.62f;

        GalleryPuzzle puzzle;
        int carried = -1;
        int focusedSlot = -1;
        readonly List<Rect> blockers = new List<Rect>();
        float baseSpeed;
        float hintCooldown;

        public GalleryPuzzle Puzzle => puzzle;
        public int Carried => carried;

        static readonly GalleryPainting[] Paintings =
        {
            new GalleryPainting
            {
                Id = "mare", Title = "Maré de Mucuripe", Author = "Acervo fictício", Year = 1998,
                Palette = PaletteFamily.Cool, Theme = "litoral", Symbol = "onda"
            },
            new GalleryPainting
            {
                Id = "nevoa", Title = "Névoa de Baturité", Author = "Acervo fictício", Year = 2004,
                Palette = PaletteFamily.Cool, Theme = "serra", Symbol = "montanha"
            },
            new GalleryPainting
            {
                Id = "sertao", Title = "Luz do Sertão", Author = "Acervo fictício", Year = 1972,
                Palette = PaletteFamily.Warm, Theme = "sertão", Symbol = "sol"
            }
        };

        /// <summary>
        /// Clue 1: the warm painting hangs furthest right.
        /// Clue 2: the coastal canvas hangs beside the oldest work.
        /// Together they allow exactly one arrangement: [névoa, maré, sertão].
        /// </summary>
        static readonly string[] Clues =
        {
            "A obra de paleta quente fica à direita de todas.",
            "A tela do litoral fica ao lado da obra mais antiga."
        };

        static readonly int[] Solution = { 1, 0, 2 };

        protected override void Awake()
        {
            base.Awake();
            floorNumber = 2;
            objectiveText = "Leia as placas e pendure cada quadro no gancho certo.";
            solvedPause = 1.6f;
        }

        protected override void OnFloorReady()
        {
            puzzle = new GalleryPuzzle(Paintings, Solution, Clues);
            baseSpeed = 1.15f;

            for (int i = 0; i < hookViews.Length; i++)
            {
                if (hookViews[i] == null) continue;
                var hook = hookViews[i].gameObject.AddComponent<HookInteractable>();
                hook.Bind(this, i);
            }
            for (int i = 0; i < floorPaintingViews.Length; i++)
            {
                if (floorPaintingViews[i] == null) continue;
                var painting = floorPaintingViews[i].gameObject.AddComponent<PaintingInteractable>();
                painting.Bind(this, i);
            }

            if (carriedView != null) carriedView.enabled = false;
            Guard.Configure(WalkableRect(), blockers);
            SyncViews();
            UpdateCounter();
            Hud.ShowHint(Clues[0], 8f);
        }

        Rect WalkableRect() => new Rect(-1.72f, -0.92f, 3.44f, 1.42f);

        protected override void OnFloorTick(float deltaTime)
        {
            hintCooldown = Mathf.Max(0f, hintCooldown - deltaTime);
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.qKey.wasPressedThisFrame && carried >= 0) DropCarried();
            if (puzzle != null && puzzle.IsSolved() && !Solved) CompleteFloor();
        }

        public void PickUp(int paintingIndex)
        {
            if (carried == paintingIndex) return;
            carried = paintingIndex;
            if (carriedView != null) carriedView.enabled = true;
            SetGuardSpeed(baseSpeed * carryingSpeedMultiplier);
            FloorSfx.Instance?.Pickup();
            SyncViews();
            UpdateCounter();
            NoteProgress();
        }

        public void DropCarried()
        {
            carried = -1;
            if (carriedView != null) carriedView.enabled = false;
            SetGuardSpeed(baseSpeed);
            SyncViews();
            UpdateCounter();
        }

        public void HangOn(int slot)
        {
            if (carried < 0) return;
            int painting = carried;
            puzzle.Place(painting, slot);
            carried = -1;
            if (carriedView != null) carriedView.enabled = false;
            SetGuardSpeed(baseSpeed);
            SyncViews();
            UpdateCounter();
            NoteProgress();

            if (puzzle.IsSolved())
            {
                FloorSfx.Instance?.Confirm();
                Hud.ShowHint("Clique magnético: o último quadro se encaixou.", 4f);
            }
            else if (puzzle.IsCorrectAt(slot))
            {
                FloorSfx.Instance?.Confirm();
                if (hintCooldown <= 0f)
                {
                    hintCooldown = 2.5f;
                    Hud.ShowHint("Encaixe firme — esse quadro está no lugar certo.", 3f);
                }
            }
            else
            {
                FloorSfx.Instance?.Error();
            }
        }

        public void TakeFromSlot(int slot)
        {
            int painting = puzzle.PaintingAt(slot);
            if (painting < 0) return;
            puzzle.Remove(slot);
            carried = painting;
            if (carriedView != null) carriedView.enabled = true;
            SetGuardSpeed(baseSpeed * carryingSpeedMultiplier);
            SyncViews();
            UpdateCounter();
            NoteProgress();
        }

        public bool IsSlotOccupied(int slot) => !puzzle.IsSlotEmpty(slot);

        void SetGuardSpeed(float speed)
        {
            Guard.SetSpeed(speed);
        }

        void SyncViews()
        {
            if (floorPaintingViews != null)
            {
                for (int i = 0; i < floorPaintingViews.Length && i < Paintings.Length; i++)
                {
                    if (floorPaintingViews[i] == null) continue;
                    bool onFloor = !puzzle.IsPlaced(i) && i != carried;
                    floorPaintingViews[i].gameObject.SetActive(onFloor);
                }
            }

            if (hookViews != null)
            {
                for (int i = 0; i < hookViews.Length && i < Paintings.Length; i++)
                {
                    if (hookViews[i] == null) continue;
                    var renderer = hookViews[i].GetComponent<SpriteRenderer>();
                    if (renderer == null) continue;
                    int painting = puzzle.PaintingAt(i);
                    renderer.enabled = painting >= 0;
                    if (painting >= 0) renderer.color = painting == Solution[i]
                        ? new Color(0.85f, 1f, 0.88f, 1f)
                        : new Color(1f, 0.85f, 0.72f, 1f);
                }
            }
        }

        void UpdateCounter()
        {
            Hud.SetCounter("Quadros pendurados: " + puzzle.PlacedCount() + "/" + puzzle.SlotCount);
            if (carried >= 0)
                Hud.SetObjective("Leve o quadro até um gancho e pressione E. Q larga o quadro.");
            else
                Hud.SetObjective("Leia as placas e pendure cada quadro no gancho certo.");
        }

        /// <summary>Development only: hangs every painting in the right place.</summary>
        protected override void AutoCompleteForCapture()
        {
            for (int slot = 0; slot < Solution.Length; slot++) puzzle.Place(Solution[slot], slot);
            carried = -1;
            if (carriedView != null) carriedView.enabled = false;
            SyncViews();
            UpdateCounter();
            CompleteFloor();
        }

        protected override string HintForFloor() =>
            "Leia as placas: duas pistas bastam para descobrir a ordem certa.";

        public string PaintingLabel(int index) =>
            Paintings[index].Title + " (" + Paintings[index].YearText + ", " + Paintings[index].Symbol + ")";
    }
}
