using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Misterio.Gameplay
{
    /// <summary>
    /// The guard in the playable floors: free 8-direction movement with axis-separated collision
    /// (so he slides along walls instead of sticking), a locking flag for cutscenes, and the
    /// nearest interactable target.
    /// Collision uses plain rectangles instead of physics: the floors are fixed dioramas, so this
    /// stays deterministic and needs no layer configuration.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class GuardActor : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] InputActionAsset inputActions;
        [SerializeField] string moveActionPath = "Player/Move";
        [SerializeField] string interactActionPath = "Player/Interact";

        [Header("Movement")]
        [SerializeField, Min(0.05f)] float moveSpeed = 1.15f;
        [SerializeField] Vector2 halfExtents = new Vector2(0.075f, 0.06f);
        [SerializeField] Rect walkable = new Rect(-1.7f, -0.95f, 3.4f, 1.45f);

        [Header("Visuals")]
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] Sprite[] idleFront;
        [SerializeField] Sprite[] idleBack;
        [SerializeField] Sprite[] walkFront;
        [SerializeField] Sprite[] walkBack;
        [SerializeField, Min(0.1f)] float idleFramesPerSecond = 2f;
        [SerializeField, Min(0.1f)] float walkFramesPerSecond = 10f;

        readonly List<Rect> blockers = new List<Rect>();

        InputAction moveAction;
        InputAction interactAction;
        bool ownsActions;
        bool facingBack;
        bool facingLeft;
        float animationTime;
        Vector2 inputValue;

        public bool CanMove { get; set; } = true;
        public bool CanInteract { get; set; } = true;
        public Vector2 Position => transform.position;
        public Vector2 Facing => facingLeft ? Vector2.left : Vector2.right;

        /// <summary>Raised when the interact key is pressed while an interactable is in range.</summary>
        public event System.Action<Interactable> Interacted;

        /// <summary>Raised when a movement step is refused, with the attempted direction.</summary>
        public event System.Action<Vector2> Blocked;

        public Interactable Nearest { get; private set; }

        void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

            if (inputActions != null)
            {
                moveAction = inputActions.FindAction(moveActionPath, throwIfNotFound: false);
                interactAction = inputActions.FindAction(interactActionPath, throwIfNotFound: false);
            }

            if (moveAction == null)
            {
                moveAction = new InputAction("Move", InputActionType.Value);
                moveAction.AddCompositeBinding("2DVector")
                    .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                    .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
                moveAction.AddCompositeBinding("2DVector")
                    .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                    .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
                ownsActions = true;
            }

            if (interactAction == null)
            {
                interactAction = new InputAction("Interact", InputActionType.Button);
                interactAction.AddBinding("<Keyboard>/e");
                interactAction.AddBinding("<Keyboard>/enter");
                interactAction.AddBinding("<Keyboard>/space");
                ownsActions = true;
            }
        }

        void OnEnable()
        {
            moveAction?.Enable();
            interactAction?.Enable();
        }

        void OnDisable()
        {
            moveAction?.Disable();
            interactAction?.Disable();
        }

        void OnDestroy()
        {
            if (ownsActions)
            {
                moveAction?.Dispose();
                interactAction?.Dispose();
            }
        }

        public void Configure(Rect walkableArea, IEnumerable<Rect> obstacles)
        {
            walkable = walkableArea;
            blockers.Clear();
            if (obstacles != null) blockers.AddRange(obstacles);
        }

        /// <summary>Used by the gallery, where carrying a painting slows the guard down.</summary>
        public void SetSpeed(float speed) => moveSpeed = Mathf.Max(0.05f, speed);

        public void AddBlocker(Rect rect) => blockers.Add(rect);
        public void ClearBlockers() => blockers.Clear();

        /// <summary>
        /// Logs an error when the guard cannot stand where he is. A guard outside his own walkable
        /// area cannot move at all, which freezes the whole scene, so this must be loud.
        /// </summary>
        public bool ValidatePlacement(string context)
        {
            if (IsFree(transform.position)) return true;
            Debug.LogError("[Guard] cannot stand at " + Position.ToString("F2") + " in " + context +
                           " (walkable " + walkable + "). The scene is unplayable.");
            return false;
        }

        public void Teleport(Vector2 position) => transform.position = new Vector3(position.x, position.y, transform.position.z);

        void Update()
        {
            inputValue = PlaytestInput.Active
                ? Vector2.ClampMagnitude(PlaytestInput.Move, 1f)
                : (moveAction != null ? Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f) : Vector2.zero);
            bool walking = CanMove && inputValue.sqrMagnitude > 0.01f;

            if (walking)
            {
                animationTime += Time.deltaTime;
                if (inputValue.y > 0.15f) facingBack = true;
                else if (inputValue.y < -0.15f) facingBack = false;
                if (inputValue.x < -0.1f) facingLeft = true;
                else if (inputValue.x > 0.1f) facingLeft = false;

                Vector2 step = inputValue * moveSpeed * Time.deltaTime;
                MoveAxis(step.x, 0f);
                MoveAxis(0f, step.y);
            }
            else
            {
                animationTime += Time.deltaTime * 0.35f;
            }

            UpdateSprite(walking);
            UpdateNearest();
        }

        void MoveAxis(float dx, float dy)
        {
            if (Mathf.Abs(dx) < 0.000001f && Mathf.Abs(dy) < 0.000001f) return;
            Vector2 current = transform.position;
            Vector2 target = current + new Vector2(dx, dy);

            if (!IsFree(target))
            {
                Blocked?.Invoke(new Vector2(dx, dy).normalized);
                return;
            }
            transform.position = new Vector3(target.x, target.y, transform.position.z);
        }

        /// <summary>True when the guard's footprint fits at the position without touching a blocker.</summary>
        public bool IsFree(Vector2 position)
        {
            var footprint = new Rect(position.x - halfExtents.x, position.y - halfExtents.y,
                                     halfExtents.x * 2f, halfExtents.y * 2f);

            if (!walkable.Contains(footprint.min) || !walkable.Contains(footprint.max)) return false;

            foreach (var blocker in blockers)
                if (blocker.Overlaps(footprint)) return false;

            return true;
        }

        void UpdateNearest()
        {
            Interactable best = null;
            float bestDistance = float.MaxValue;
            foreach (var candidate in Interactable.All)
            {
                if (candidate == null || !candidate.IsAvailable) continue;
                float distance = Vector2.Distance(candidate.InteractPoint, transform.position);
                if (distance > candidate.InteractRadius || distance >= bestDistance) continue;
                best = candidate;
                bestDistance = distance;
            }

            if (best != Nearest)
            {
                Nearest?.SetHighlighted(false);
                Nearest = best;
                Nearest?.SetHighlighted(true);
            }

            bool pressed = PlaytestInput.Active
                ? PlaytestInput.InteractPressed
                : interactAction != null && interactAction.WasPressedThisFrame();

            if (Nearest == null || !CanInteract || !pressed) return;

            // Actually use the object. The event alone did nothing: nothing in the project ever
            // subscribed to it, so pressing the interact key was silently ignored in the real game.
            Nearest.Interact(this);
            Interacted?.Invoke(Nearest);
        }

        void UpdateSprite(bool walking)
        {
            if (spriteRenderer == null) return;
            Sprite[] frames = walking
                ? (facingBack ? walkBack : walkFront)
                : (facingBack ? idleBack : idleFront);
            if (frames == null || frames.Length == 0) return;
            float rate = walking ? walkFramesPerSecond : idleFramesPerSecond;
            spriteRenderer.sprite = frames[Mathf.FloorToInt(animationTime * rate) % frames.Length];
            spriteRenderer.flipX = facingLeft;
        }
    }
}
