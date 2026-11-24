using System.Collections;
using Misterio.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Misterio.Gameplay
{
    /// <summary>
    /// The epilogue on the campus lawn: after the opening succeeds, the guard crosses the grass at
    /// dawn and finds Moema resting under the tree. It closes the story the design document tells,
    /// and it is the only place where the moth and the emu are finally at peace.
    /// </summary>
    public sealed class EpilogueScene : MonoBehaviour
    {
        [Header("World")]
        [SerializeField] GuardActor guard;
        [SerializeField] Transform cameraTransform;
        [SerializeField] float worldLeft = -1.6f;
        [SerializeField] float worldRight = 17.5f;
        [SerializeField] float cameraOffset = 1.0f;
        [SerializeField] float treeX = 16.4f;

        [Header("Presentation")]
        [SerializeField] string objectiveText = "A inauguração começou. Atravesse o gramado.";
        [SerializeField] Font font;

        GameSession session;
        HudView hud;
        bool ending;

        void Awake()
        {
            session = GameSession.EnsureExists();
            if (guard == null) guard = FindAnyObjectByType<GuardActor>();
            if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
        }

        void Start()
        {
            font = font != null ? font : LoadFont();
            hud = HudView.Create(font, objectiveText);
            PauseMenu.Create(font);
            session.SetClockRunning(false);
            hud.ShowHint("Seis da manhã. A exposição está pronta.", 7f);
        }

        Font LoadFont()
        {
#if UNITY_EDITOR
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/RobotoMono-Bold.ttf");
            if (asset != null) return asset;
#endif
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        void Update()
        {
            if (ending || guard == null) return;

            Vector2 position = guard.Position;
            float clampedX = Mathf.Clamp(position.x, worldLeft, worldRight);
            if (!Mathf.Approximately(position.x, clampedX))
                guard.Teleport(new Vector2(clampedX, position.y));

            if (cameraTransform != null)
            {
                float targetX = Mathf.Clamp(clampedX + cameraOffset, worldLeft + 2f, worldRight - 1.0f);
                var cameraPosition = cameraTransform.position;
                cameraTransform.position = new Vector3(
                    Mathf.Lerp(cameraPosition.x, targetX, 1f - Mathf.Exp(-Time.deltaTime * 3.2f)),
                    cameraPosition.y, cameraPosition.z);
            }

            if (clampedX >= treeX - 0.5f) StartCoroutine(Finish());

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.enterKey.wasPressedThisFrame) StartCoroutine(Finish());
        }

        /// <summary>Development only: moves the guard to the tree for automated runs.</summary>
        public void SkipToTree()
        {
            if (guard != null) guard.Teleport(new Vector2(treeX, guard.Position.y));
        }

        IEnumerator Finish()
        {
            if (ending) yield break;
            ending = true;
            if (guard != null) guard.CanMove = false;
            hud.SetPrompt(string.Empty);
            hud.ShowHint("Moema descansa sob a árvore, como se nada tivesse acontecido.", 3f);
            yield return new WaitForSeconds(1.6f);
            GameFlow.EnsureExists().Load(GameFlow.Credits);
        }
    }
}
