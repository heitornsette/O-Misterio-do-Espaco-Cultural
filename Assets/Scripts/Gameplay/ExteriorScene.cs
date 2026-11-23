using System.Collections;
using Misterio.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Misterio.Gameplay
{
    /// <summary>
    /// The outdoor approach: the guard crosses the campus lawn and the plaza to reach the cultural
    /// centre. There is no puzzle here; the scene exists to give the night room to breathe, to show
    /// the building before the player enters it, and to carry the clock.
    /// </summary>
    public sealed class ExteriorScene : MonoBehaviour
    {
        [Header("World")]
        [SerializeField] GuardActor guard;
        [SerializeField] Transform cameraTransform;
        [SerializeField] float worldLeft = -2.0f;
        [SerializeField] float worldRight = 14.0f;
        [SerializeField] float cameraOffset = 0.9f;
        [SerializeField] float entranceX = 13.2f;

        [Header("Presentation")]
        [SerializeField] string objectiveText = "Atravesse a praça e entre no Espaço Cultural.";
        [SerializeField] Font font;

        GameSession session;
        HudView hud;
        bool entering;

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
            InteractionFeedback.Create(LoadMarker(), hud, guard);
            session.SetClockRunning(true);
            hud.ShowHint("Siga para a direita. A inauguração é às seis.", 7f);
        }

        Font LoadFont()
        {
#if UNITY_EDITOR
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/RobotoMono-Bold.ttf");
            if (asset != null) return asset;
#endif
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        Sprite LoadMarker()
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Gameplay/marcador_interagir.png");
#else
            return null;
#endif
        }

        void Update()
        {
            if (entering) return;

            session.Tick(Time.deltaTime);

            if (guard != null)
            {
                // Keep the guard inside the strip and the camera following him.
                Vector2 position = guard.Position;
                float clampedX = Mathf.Clamp(position.x, worldLeft, worldRight);
                if (!Mathf.Approximately(position.x, clampedX))
                    guard.Teleport(new Vector2(clampedX, position.y));

                if (cameraTransform != null)
                {
                    float targetX = Mathf.Clamp(clampedX + cameraOffset, worldLeft + 2f, worldRight - 1.2f);
                    var cameraPosition = cameraTransform.position;
                    cameraTransform.position = new Vector3(
                        Mathf.Lerp(cameraPosition.x, targetX, 1f - Mathf.Exp(-Time.deltaTime * 3.4f)),
                        cameraPosition.y, cameraPosition.z);
                }

                if (clampedX >= entranceX - 0.4f) StartCoroutine(EnterBuilding());
            }

            // A quick skip for players who just want to get inside.
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.enterKey.wasPressedThisFrame) StartCoroutine(EnterBuilding());
        }

        /// <summary>
        /// Development only: walks the guard to the entrance so an automated run can cross the
        /// approach without a player. Never called in a normal session.
        /// </summary>
        public void SkipToEntrance()
        {
            if (guard != null) guard.Teleport(new Vector2(entranceX, guard.Position.y));
        }

        IEnumerator EnterBuilding()
        {
            if (entering) yield break;
            entering = true;            if (guard != null) guard.CanMove = false;
            hud.SetPrompt(string.Empty);
            hud.ShowHint("Entrando no Espaço Cultural...", 2f);
            session.SetClockRunning(false);
            yield return new WaitForSeconds(0.9f);
            GameFlow.EnsureExists().Load(GameFlow.Cutscene01);
        }
    }
}
