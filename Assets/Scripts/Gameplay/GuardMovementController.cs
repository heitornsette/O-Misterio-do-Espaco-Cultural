using UnityEngine;
using UnityEngine.InputSystem;

namespace Misterio.Gameplay
{
    /// <summary>
    /// Basic 2D movement for the guard using the Player/Move action.
    /// The front/back sprite sets are reused for horizontal movement with a horizontal flip.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class GuardMovementController : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] InputActionAsset inputActions;
        [SerializeField] string moveActionPath = "Player/Move";

        [Header("Movement")]
        [SerializeField, Min(0f)] float moveSpeed = 0.65f;
        [SerializeField] Vector2 movementMin = new Vector2(-1.65f, -0.8f);
        [SerializeField] Vector2 movementMax = new Vector2(1.65f, 0.45f);

        [Header("Visuals")]
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] Sprite[] idleFront;
        [SerializeField] Sprite[] idleBack;
        [SerializeField] Sprite[] walkFront;
        [SerializeField] Sprite[] walkBack;
        [SerializeField, Min(0.1f)] float idleFramesPerSecond = 2f;
        [SerializeField, Min(0.1f)] float walkFramesPerSecond = 10f;

        InputAction moveAction;
        bool ownsMoveAction;
        bool facingBack;
        bool facingLeft;
        float animationTime;

        void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            if (inputActions != null)
                moveAction = inputActions.FindAction(moveActionPath, throwIfNotFound: false);

            if (moveAction == null)
            {
                moveAction = new InputAction("Move", InputActionType.Value);
                moveAction.AddCompositeBinding("2DVector")
                    .With("Up", "<Keyboard>/w")
                    .With("Down", "<Keyboard>/s")
                    .With("Left", "<Keyboard>/a")
                    .With("Right", "<Keyboard>/d");
                moveAction.AddCompositeBinding("2DVector")
                    .With("Up", "<Keyboard>/upArrow")
                    .With("Down", "<Keyboard>/downArrow")
                    .With("Left", "<Keyboard>/leftArrow")
                    .With("Right", "<Keyboard>/rightArrow");
                moveAction.AddBinding("<Gamepad>/leftStick");
                moveAction.AddBinding("<Gamepad>/dpad");
                ownsMoveAction = true;
            }
        }

        void OnEnable()
        {
            moveAction?.Enable();
        }

        void OnDisable()
        {
            moveAction?.Disable();
        }

        void OnDestroy()
        {
            if (ownsMoveAction)
                moveAction?.Dispose();
        }

        void Update()
        {
            if (moveAction == null)
                return;

            Vector2 input = Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f);
            bool walking = input.sqrMagnitude > 0.01f;

            if (walking)
            {
                animationTime += Time.deltaTime;
                if (input.y > 0.15f) facingBack = true;
                else if (input.y < -0.15f) facingBack = false;
                if (input.x < -0.1f) facingLeft = true;
                else if (input.x > 0.1f) facingLeft = false;

                Vector3 next = transform.position +
                    new Vector3(input.x, input.y, 0f) * moveSpeed * Time.deltaTime;
                next.x = Mathf.Clamp(next.x, movementMin.x, movementMax.x);
                next.y = Mathf.Clamp(next.y, movementMin.y, movementMax.y);
                transform.position = next;
            }
            else
            {
                animationTime += Time.deltaTime * 0.35f;
            }

            UpdateSprite(walking);
        }

        void UpdateSprite(bool walking)
        {
            if (spriteRenderer == null)
                return;

            Sprite[] frames = walking
                ? (facingBack ? walkBack : walkFront)
                : (facingBack ? idleBack : idleFront);

            if (frames == null || frames.Length == 0)
                return;

            float rate = walking ? walkFramesPerSecond : idleFramesPerSecond;
            int frame = Mathf.FloorToInt(animationTime * rate) % frames.Length;
            spriteRenderer.sprite = frames[frame];
            spriteRenderer.flipX = facingLeft;
        }
    }
}

