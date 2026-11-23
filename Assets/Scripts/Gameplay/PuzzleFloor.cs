using System.Collections;
using Misterio.Core;
using UnityEngine;

namespace Misterio.Gameplay
{
    /// <summary>
    /// Shared behaviour for the four playable floors: keeps the night clock running while the
    /// player is in control, handles the time-out ending, and plays the short beat that leads into
    /// the next cutscene once the puzzle is solved.
    /// </summary>
    public abstract class PuzzleFloor : MonoBehaviour
    {
        [Header("Floor")]
        [SerializeField, Min(1)] protected int floorNumber = 1;
        [SerializeField] protected string objectiveText = "Reorganizar o acervo.";
        [SerializeField, Min(0f)] protected float solvedPause = 1.4f;

        protected GameSession Session { get; private set; }
        protected HudView Hud { get; private set; }
        protected GuardActor Guard { get; private set; }

        protected bool Solved { get; private set; }
        protected bool TimedOut { get; private set; }
        protected float FloorElapsed { get; private set; }
        float idleSeconds;
        Vector2 lastFootstepPosition;
        float footstepTimer;

        protected virtual void Awake()
        {
            Session = GameSession.EnsureExists();
            Guard = FindAnyObjectByType<GuardActor>();
            // Only stale entries are dropped: clearing the list here would unregister objects whose
            // OnEnable already ran before this Awake, which silently hid interactables.
            Interactable.PruneRegistry();
        }

        protected virtual void Start()
        {
            // Awake may not have run yet when the floor is started from a tool or a test harness,
            // so make sure the session and the guard are resolved before building anything.
            if (Session == null) Session = GameSession.EnsureExists();
            if (Guard == null) Guard = FindAnyObjectByType<GuardActor>();

            var font = LoadFont();
            Hud = HudView.Create(font, objectiveText);
            PauseMenu.Create(font);
            InteractionFeedback.Create(LoadMarkerSprite(), Hud, Guard);

            Session.SetClockRunning(true);
            Session.DawnReached += OnDawnReached;
            if (Guard != null) lastFootstepPosition = Guard.Position;
            OnFloorReady();
        }

        Sprite LoadMarkerSprite()
        {
#if UNITY_EDITOR
            var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Gameplay/marcador_interagir.png");
            if (sprite != null) return sprite;
#endif
            return null;
        }

        protected virtual void OnDestroy()
        {
            if (Session != null) Session.DawnReached -= OnDawnReached;
        }

        /// <summary>
        /// Plays a soft footstep while the guard walks, which makes the floors feel alive without
        /// any new audio asset.
        /// </summary>
        void UpdateFootsteps(float delta)
        {
            if (Guard == null) return;
            Vector2 position = Guard.Position;
            bool walking = (position - lastFootstepPosition).sqrMagnitude > 0.0000004f;
            lastFootstepPosition = position;
            if (!walking)
            {
                footstepTimer = 0f;
                return;
            }
            footstepTimer -= delta;
            if (footstepTimer > 0f) return;
            footstepTimer = 0.34f;
            FloorSfx.Instance?.Step();
        }

        /// <summary>Called once the HUD and the session are ready; build the puzzle here.</summary>
        protected virtual void OnFloorReady() { }

        /// <summary>Called every frame while the floor is playable.</summary>
        protected virtual void OnFloorTick(float deltaTime) { }

        /// <summary>Seconds without progress, used to offer a gentle hint.</summary>
        protected void NoteProgress() => idleSeconds = 0f;

        protected void Update()
        {
            if (Solved || TimedOut) return;

            float delta = Time.deltaTime;
            FloorElapsed += delta;
            Session.Tick(delta);
            OnFloorTick(delta);
            UpdateFootsteps(delta);

            idleSeconds += delta;
            if (idleSeconds > 90f)
            {
                idleSeconds = 0f;
                Hud?.ShowHint(HintForFloor());
            }
        }

        protected virtual string HintForFloor() => "Observe o ambiente: as pistas estão no cenário.";

        protected virtual void OnDawnReached()
        {
            if (Solved || TimedOut) return;
            TimedOut = true;
            StartCoroutine(DawnRoutine());
        }

        IEnumerator DawnRoutine()
        {
            if (Guard != null) Guard.CanMove = false;
            Hud?.SetPrompt(string.Empty);
            Session.MarkTimedOut(floorNumber);
            yield return new WaitForSeconds(0.6f);
            // The main hall has its own ending; the other floors share the postponement cutscene.
            string scene = floorNumber >= 4 ? GameFlow.Cutscene10 : GameFlow.Cutscene09;
            GameFlow.EnsureExists().Load(scene);
        }

        /// <summary>Marks the floor solved and moves on after a short beat.</summary>
        protected void CompleteFloor()
        {
            if (Solved || TimedOut) return;
            Solved = true;
            Session.SetClockRunning(false);
            Session.MarkFloorSolved(floorNumber, FloorElapsed);
            if (Guard != null) Guard.CanMove = false;
            Hud?.SetPrompt(string.Empty);
            StartCoroutine(SolvedRoutine());
        }

        IEnumerator SolvedRoutine()
        {
            yield return new WaitForSeconds(solvedPause);
            GameFlow.EnsureExists().GoToNext();
        }

        /// <summary>
        /// Development only: solves the floor immediately so the automated route capture can walk
        /// the whole game in the built player. Never called in a normal run.
        /// </summary>
        protected virtual void AutoCompleteForCapture() => CompleteFloor();

        protected virtual Font LoadFont()        {
            var font = Resources.Load<Font>("RobotoMono-Bold");
            if (font != null) return font;
#if UNITY_EDITOR
            font = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/RobotoMono-Bold.ttf");
            if (font != null) return font;
#endif
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }
}
