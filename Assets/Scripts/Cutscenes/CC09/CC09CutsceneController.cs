using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Misterio.Cutscenes
{
    public enum CC09FloorVariant
    {
        GroundFloor = 1,
        Gallery = 2,
        Archive = 3
    }

    /// <summary>Fourteen-second, three-floor timeout ending. Game time is frozen for its full duration.</summary>
    public sealed class CC09CutsceneController : MonoBehaviour
    {
        public const float Duration = 14f;
        const float RadioShot = 7f;
        const float ResultShot = 11f;

        public bool ControlsLocked { get; private set; } = true;
        public float Elapsed => elapsed;
        public string GlobalClockValue => globalClockValue;
        public CC09FloorVariant FloorVariant => floorVariant;

        [Header("Andar e câmera")]
        [SerializeField] Camera sceneCamera;
        [SerializeField] GameObject[] floorRoots = new GameObject[3];
        [SerializeField] CC09FloorVariant floorVariant = CC09FloorVariant.GroundFloor;
        [SerializeField] Vector3[] floorFocusPositions = new Vector3[3];
        [SerializeField] Vector3[] radioCameraPositions = new Vector3[3];
        [SerializeField] float[] radioCameraSizes = { 0.78f, 0.78f, 0.82f };

        [Header("Segurança e rádio")]
        [SerializeField] Transform[] guards = new Transform[3];
        [SerializeField] SpriteRenderer[] guardRenderers = new SpriteRenderer[3];
        [SerializeField] SpriteRenderer[] guardShadows = new SpriteRenderer[3];
        [SerializeField] Transform[] radios = new Transform[3];
        [SerializeField] SpriteRenderer[] radioScreens = new SpriteRenderer[3];
        [SerializeField] Sprite[] guardIdleBack;
        [SerializeField] Sprite[] guardIdleFront;

        [Header("Trabalho ainda incompleto")]
        [SerializeField] Transform[] displacedStatues;
        [SerializeField] Transform displacedPainting;
        [SerializeField] Transform[] scatteredArchivePieces;
        [SerializeField] Transform moema;
        [SerializeField] SpriteRenderer moemaRenderer;
        [SerializeField] SpriteRenderer moemaShadow;
        [SerializeField] Sprite[] moemaIdleFront;
        [SerializeField] Sprite[] moemaWalkBack;

        [Header("Placas da galeria")]
        [SerializeField] Canvas sceneCanvas;
        [SerializeField] Transform[] galleryPlaqueAnchors;
        [SerializeField] Text[] galleryPlaqueLabels;

        [Header("Imagem e legendas")]
        [SerializeField] Image subtitleBackground;
        [SerializeField] Text subtitleText;
        [SerializeField] Image clockHud;
        [SerializeField] Text clockHudLabel;
        [SerializeField] Text clockHudValue;
        [SerializeField] Image morningWash;
        [SerializeField] Image fadeImage;
        [SerializeField] Image resultBackdrop;
        [SerializeField] CanvasGroup resultCanvasGroup;
        [SerializeField] Text resultFloorLabel;
        [SerializeField] Text resultTitle;
        [SerializeField] Text resultDescription;
        [SerializeField] Button retryButton;
        [SerializeField] Button menuButton;

        [Header("Vozes — slots para gravação")]
        [SerializeField] AudioClip narratorOpening;
        [SerializeField] AudioClip[] narratorFloorLines = new AudioClip[3];
        [SerializeField] AudioClip narratorRadioSummary;
        [SerializeField] AudioClip curatorRadioReply;
        [SerializeField] AudioClip narratorEnding;

        [Header("Som")]
        [SerializeField] AudioSource voiceSource;
        [SerializeField] AudioSource ambienceSource;
        [SerializeField] AudioSource morningAirSource;
        [SerializeField] AudioSource effectsSource;
        [SerializeField] AudioSource musicSource;
        [SerializeField] AudioClip[] floorAmbiences = new AudioClip[3];
        [SerializeField] AudioClip morningAir;
        [SerializeField] AudioClip clockChime;
        [SerializeField] AudioClip groundFloorSettle;
        [SerializeField] AudioClip galleryWoodShift;
        [SerializeField] AudioClip moemaTrot;
        [SerializeField] AudioClip radioClick;
        [SerializeField] AudioClip calmEndingMusic;
        [SerializeField] AudioClip xylophoneNote;

        [Header("Integração com o jogo")]
        [SerializeField] bool playOnAwake = true;
        [SerializeField] UnityEvent onCutsceneStarted = new UnityEvent();
        [SerializeField] UnityEvent onCutsceneFinished = new UnityEvent();
        [SerializeField] UnityEvent onReturnToMenu = new UnityEvent();

        float elapsed;
        double lastRealtime;
        string globalClockValue = "06:00";
        bool playing;
        bool finished;
        bool clockCuePlayed;
        bool workCuePlayed;
        bool radioCuePlayed;
        bool finalCuePlayed;
        int activeSubtitle = -1;
        float nextMoemaStep = 4.34f;
        Vector3 cameraStartPosition;
        float cameraStartSize;
        Vector3[] guardStarts;
        Vector3[] guardScales;
        Vector3[] guardShadowStarts;
        Vector3[] radioLocalStarts;
        Vector3[] moemaWorkStarts;
        Vector3[] statueStarts;
        Vector3 displacedPaintingStart;
        Quaternion displacedPaintingRotation;
        Vector3[] archivePieceStarts;
        Vector3 moemaStart;
        Vector3 moemaScale;
        Vector3 moemaShadowStart;
        Vector3 moemaShadowScale;

        void Awake()
        {
            cameraStartPosition = sceneCamera != null ? sceneCamera.transform.position : new Vector3(0f, 0f, -10f);
            cameraStartSize = sceneCamera != null ? sceneCamera.orthographicSize : 1f;
            guardStarts = CapturePositions(guards);
            guardScales = CaptureScales(guards);
            guardShadowStarts = CapturePositions(guardShadows != null ? Array.ConvertAll(guardShadows, item => item != null ? item.transform : null) : null);
            radioLocalStarts = CaptureLocalPositions(radios);
            statueStarts = CapturePositions(displacedStatues);
            archivePieceStarts = CapturePositions(scatteredArchivePieces);
            displacedPaintingStart = displacedPainting != null ? displacedPainting.position : Vector3.zero;
            displacedPaintingRotation = displacedPainting != null ? displacedPainting.rotation : Quaternion.identity;
            moemaStart = moema != null ? moema.position : new Vector3(0.60f, -0.56f, -0.12f);
            moemaScale = moema != null ? moema.localScale : Vector3.one;
            moemaShadowStart = moemaShadow != null ? moemaShadow.transform.position : moemaStart + Vector3.down * 0.035f;
            moemaShadowScale = moemaShadow != null ? moemaShadow.transform.localScale : Vector3.one;
            if (moema != null) moemaWorkStarts = new[]
            {
                moemaStart + new Vector3(-0.44f, 0f, 0f),
                moemaStart + new Vector3(0.04f, -0.015f, 0f)
            };
            else moemaWorkStarts = Array.Empty<Vector3>();

            if (clockHudValue != null) clockHudValue.text = globalClockValue;
            ResetVisuals();
            ControlsLocked = true;
            lastRealtime = Time.realtimeSinceStartupAsDouble;
            if (playOnAwake) Begin();
        }

        void Update()
        {
            if (!playing || finished) return;
            double now = Time.realtimeSinceStartupAsDouble;
            elapsed = Mathf.Min(Duration, elapsed + (float)Math.Max(0.0, now - lastRealtime));
            lastRealtime = now;
            EvaluateVisuals(elapsed, true);
            UpdateAudio();
            if (elapsed >= Duration) Finish();
        }

        /// <summary>Sets the global time shown during the interruption; this scene never advances it.</summary>
        public void SetGlobalClock(string value)
        {
            globalClockValue = string.IsNullOrWhiteSpace(value) ? "06:00" : value.Trim();
            if (clockHudValue != null) clockHudValue.text = globalClockValue;
        }

        /// <summary>Choose the unfinished floor before calling PlayCutscene.</summary>
        public void SetFloorVariant(CC09FloorVariant value)
        {
            floorVariant = (CC09FloorVariant)Mathf.Clamp((int)value, 1, 3);
            if (clockHudValue != null) clockHudValue.text = globalClockValue;
            if (playing && !finished)
            {
                Begin();
                return;
            }
            ResetVisuals();
            EvaluateVisuals(elapsed, false);
        }

        /// <summary>Integer-friendly hook for the gameplay floor selector (1, 2 or 3).</summary>
        public void SetIncompleteFloor(int floor) => SetFloorVariant((CC09FloorVariant)Mathf.Clamp(floor, 1, 3));

        public void PlayCutscene()
        {
            if (playing && !finished) return;
            Begin();
        }

        /// <summary>
        /// Returns to the floor that ran out of time so the player can try that floor again, instead
        /// of replaying this cutscene.
        /// </summary>
        public void RetrySameFloor()
        {
            var session = Misterio.Core.GameSession.Instance;
            string floor = Misterio.Puzzles.RouteRules.ResumeTarget(session != null ? session.TimedOutFloor : 0);
            if (!string.IsNullOrEmpty(floor) && Misterio.Core.GameFlow.Instance != null)
            {
                Misterio.Core.GameFlow.Instance.Load(floor);
                return;
            }
            // Fallback for a scene opened on its own: replay the cutscene.
            Begin();
        }

        /// <summary>Starts the whole night again. There is no menu scene, so this is the exit path.</summary>
        public void ReturnToMenu()
        {
            onReturnToMenu?.Invoke();
            if (Misterio.Core.GameFlow.Instance == null) return;
            Misterio.Core.GameSession.Instance?.ResetRun();
            Misterio.Core.GameFlow.Instance.Load(Misterio.Core.GameFlow.Exterior);
        }

#if UNITY_EDITOR
        public void PrepareEditorFrameReviewCapture()
        {
            playing = false;
            finished = false;
            elapsed = 0f;
            ControlsLocked = true;
            activeSubtitle = -1;
            clockCuePlayed = workCuePlayed = radioCuePlayed = finalCuePlayed = false;
            nextMoemaStep = 4.34f;
            StopAudio();
            ResetVisuals();
            EvaluateVisuals(0f, false);
        }

        public void SetEditorReviewFloor(CC09FloorVariant value)
        {
            floorVariant = (CC09FloorVariant)Mathf.Clamp((int)value, 1, 3);
            PrepareEditorFrameReviewCapture();
        }

        public void SetEditorReviewTime(float time)
        {
            elapsed = Mathf.Clamp(time, 0f, Duration);
            EvaluateVisuals(elapsed, false);
        }
#endif

        void Begin()
        {
            elapsed = 0f;
            activeSubtitle = -1;
            clockCuePlayed = workCuePlayed = radioCuePlayed = finalCuePlayed = false;
            nextMoemaStep = 4.34f;
            playing = true;
            finished = false;
            ControlsLocked = true;
            StopAudio();
            ResetVisuals();
            StartAudio();
            EvaluateVisuals(0f, true);
            lastRealtime = Time.realtimeSinceStartupAsDouble;
            onCutsceneStarted?.Invoke();
        }

        void ResetVisuals()
        {
            SetFloorRoot();
            if (sceneCamera != null)
            {
                sceneCamera.transform.position = cameraStartPosition;
                sceneCamera.orthographicSize = cameraStartSize;
            }
            RestoreTransforms(guards, guardStarts, guardScales);
            RestoreLocalPositions(radios, radioLocalStarts);
            RestorePositions(displacedStatues, statueStarts);
            RestorePositions(scatteredArchivePieces, archivePieceStarts);
            if (displacedPainting != null)
            {
                displacedPainting.position = displacedPaintingStart;
                displacedPainting.rotation = displacedPaintingRotation;
            }
            if (moema != null)
            {
                moema.position = moemaStart;
                moema.localScale = moemaScale;
                moema.rotation = Quaternion.identity;
            }
            if (moemaRenderer != null) { moemaRenderer.color = Color.white; moemaRenderer.enabled = true; }
            if (moemaShadow != null)
            {
                moemaShadow.enabled = true;
                moemaShadow.transform.position = moemaShadowStart;
                moemaShadow.transform.localScale = moemaShadowScale;
            }
            SetAllRadioScreens(0f);
            if (subtitleText != null)
            {
                subtitleText.text = string.Empty;
                subtitleText.enabled = false;
                subtitleText.color = Color.white;
            }
            if (subtitleBackground != null) subtitleBackground.color = new Color(0.012f, 0.018f, 0.038f, 0f);
            if (clockHud != null) clockHud.color = new Color(1f, 1f, 1f, 0f);
            if (clockHudLabel != null) clockHudLabel.color = new Color(0.62f, 0.73f, 0.90f, 0f);
            if (clockHudValue != null) clockHudValue.color = new Color(0.93f, 0.96f, 1f, 0f);
            if (morningWash != null) morningWash.color = new Color(1f, 0.76f, 0.48f, 0f);
            if (fadeImage != null) fadeImage.color = new Color(0f, 0f, 0f, 0f);
            if (resultBackdrop != null) resultBackdrop.color = new Color(0.015f, 0.030f, 0.078f, 0f);
            if (resultCanvasGroup != null) { resultCanvasGroup.alpha = 0f; resultCanvasGroup.interactable = false; resultCanvasGroup.blocksRaycasts = false; }
            if (retryButton != null) retryButton.interactable = false;
            if (menuButton != null) menuButton.interactable = false;
            UpdateResultFloorLabel();
        }

        void EvaluateVisuals(float time, bool playVoice)
        {
            SetFloorRoot();
            UpdateCamera(time);
            UpdateGuard(time);
            UpdateWork(time);
            UpdateMoema(time);
            UpdatePlaqueLabels();
            UpdateClock(time);
            UpdateMorningWash(time);
            UpdateSubtitle(time, playVoice);
            UpdateResult(time);
            UpdateFade(time);
        }

        void SetFloorRoot()
        {
            int active = FloorIndex;
            if (floorRoots == null) return;
            for (int i = 0; i < floorRoots.Length; i++)
                if (floorRoots[i] != null && floorRoots[i].activeSelf != (i == active)) floorRoots[i].SetActive(i == active);
        }

        void UpdateCamera(float time)
        {
            if (sceneCamera == null) return;
            Vector3 widePosition = new Vector3(cameraStartPosition.x, cameraStartPosition.y, cameraStartPosition.z);
            Vector3 focus = ValueAt(floorFocusPositions, FloorIndex, widePosition);
            Vector3 radioPosition = ValueAt(radioCameraPositions, FloorIndex, focus);
            float radioSize = ValueAt(radioCameraSizes, FloorIndex, cameraStartSize);
            Vector3 position;
            float size;

            if (time < 2.72f)
            {
                float u = Smooth(Mathf.InverseLerp(0.18f, 2.72f, time));
                position = Vector3.Lerp(widePosition + new Vector3(0f, 0.025f, 0f), widePosition, u);
                size = Mathf.Lerp(1.04f, 1.02f, u);
            }
            else if (time < 3.18f)
            {
                float u = Smooth(Mathf.InverseLerp(2.72f, 3.18f, time));
                position = Vector3.Lerp(widePosition, focus, u);
                size = Mathf.Lerp(1.02f, 0.98f, u);
            }
            else if (time < 6.82f)
            {
                float settle = Mathf.Sin((time - 3.18f) * 1.15f) * 0.008f;
                position = focus + Vector3.up * settle;
                size = 0.98f;
            }
            else if (time < 7.12f)
            {
                float u = Smooth(Mathf.InverseLerp(6.82f, 7.12f, time));
                position = Vector3.Lerp(focus, radioPosition, u);
                size = Mathf.Lerp(0.98f, radioSize, u);
            }
            else if (time < 10.86f)
            {
                float push = Smooth(Mathf.InverseLerp(7.10f, 8.2f, time));
                position = Vector3.Lerp(radioPosition, radioPosition + new Vector3(0f, 0.015f, 0f), push);
                size = Mathf.Lerp(radioSize, radioSize * 0.965f, push);
            }
            else
            {
                float u = Smooth(Mathf.InverseLerp(10.86f, 11.42f, time));
                position = Vector3.Lerp(radioPosition, widePosition, u);
                size = Mathf.Lerp(radioSize * 0.965f, 1f, u);
            }
            sceneCamera.transform.position = position;
            sceneCamera.orthographicSize = size;
        }

        void UpdateGuard(float time)
        {
            int index = FloorIndex;
            if (guards == null || index >= guards.Length || guards[index] == null) return;
            Transform guard = guards[index];
            SpriteRenderer renderer = ValueAt(guardRenderers, index, null);
            if (renderer != null)
            {
                Sprite[] frames = time >= 7.04f && time < ResultShot ? guardIdleFront : guardIdleBack;
                SetFrame(renderer, frames, Mathf.FloorToInt(Mathf.Max(0f, time - 2.9f) * 3.2f));
            }
            float breathe = Mathf.Sin(time * 2.3f) * 0.008f;
            Vector3 position = guardStarts[index] + Vector3.up * breathe;
            if (index == 0)
            {
                float release = Smooth(Mathf.InverseLerp(3.45f, 4.08f, time));
                position += Vector3.left * (0.075f * release);
            }
            if (time >= RadioShot && time < ResultShot)
            {
                float speak = Mathf.Sin((time - RadioShot) * 7.5f) * 0.0025f;
                position += Vector3.up * speak;
            }
            guard.position = position;
            SpriteRenderer shadow = ValueAt(guardShadows, index, null);
            if (shadow != null && guardShadowStarts != null && index < guardShadowStarts.Length)
            {
                shadow.transform.position = guardShadowStarts[index] + Vector3.up * breathe * 0.16f;
                if (index == 0) shadow.transform.position += Vector3.left * (0.075f * Smooth(Mathf.InverseLerp(3.45f, 4.08f, time)));
            }
            UpdateRadio(time, index);
        }

        void UpdateRadio(float time, int index)
        {
            Transform radio = ValueAt(radios, index, null);
            if (radio == null) return;
            float reach = Smooth(Mathf.InverseLerp(7.14f, 7.52f, time)) * (1f - Smooth(Mathf.InverseLerp(10.70f, 10.96f, time)));
            radio.localPosition = radioLocalStarts[index] + Vector3.up * (0.40f * reach);
            SpriteRenderer screen = ValueAt(radioScreens, index, null);
            if (screen != null)
            {
                float active = Smooth(Mathf.InverseLerp(7.20f, 7.30f, time)) * (1f - Smooth(Mathf.InverseLerp(10.72f, 10.94f, time)));
                float pulse = 0.78f + Mathf.Sin((time - 7.20f) * 5.2f) * 0.10f;
                screen.color = new Color(0.52f, 0.82f, 1f, 0.15f + active * pulse * 0.68f);
            }
        }

        void UpdateWork(float time)
        {
            if (displacedStatues != null)
                for (int i = 0; i < displacedStatues.Length; i++)
                {
                    if (displacedStatues[i] == null || statueStarts == null || i >= statueStarts.Length) continue;
                    float release = i == 1 ? Mathf.Sin(Smooth(Mathf.InverseLerp(3.55f, 4.06f, time)) * Mathf.PI) * 0.012f : 0f;
                    displacedStatues[i].position = statueStarts[i] + Vector3.up * release;
                }

            if (displacedPainting != null)
            {
                float settle = Mathf.Sin(Mathf.Clamp01((time - 3.25f) / 0.82f) * Mathf.PI) * 1.15f;
                displacedPainting.rotation = displacedPaintingRotation * Quaternion.Euler(0f, 0f, settle);
                displacedPainting.position = displacedPaintingStart + Vector3.up * (Mathf.Abs(settle) * 0.0025f);
            }

            if (scatteredArchivePieces != null)
                for (int i = 0; i < scatteredArchivePieces.Length; i++)
                    if (scatteredArchivePieces[i] != null && archivePieceStarts != null && i < archivePieceStarts.Length)
                    {
                        float settle = Mathf.Sin(time * 1.1f + i * 1.6f) * 0.002f;
                        scatteredArchivePieces[i].position = archivePieceStarts[i] + Vector3.up * settle;
                    }
        }

        void UpdateMoema(float time)
        {
            if (moema == null || moemaRenderer == null || FloorIndex != 2) return;
            const float listenStart = 3.70f;
            const float trotStart = 4.30f;
            const float trotEnd = 6.62f;
            Vector3 beforeListen = moemaWorkStarts != null && moemaWorkStarts.Length > 0 ? moemaWorkStarts[0] : moemaStart;
            Vector3 afterListen = moemaWorkStarts != null && moemaWorkStarts.Length > 1 ? moemaWorkStarts[1] : moemaStart;
            if (time < listenStart)
            {
                float patrol = Smooth(Mathf.InverseLerp(0.55f, listenStart, time));
                moema.position = Vector3.Lerp(beforeListen, afterListen, patrol);
                SetFrame(moemaRenderer, moemaWalkBack, Mathf.FloorToInt(time * 10f));
            }
            else if (time < trotStart)
            {
                float stop = Smooth(Mathf.InverseLerp(listenStart, trotStart, time));
                moema.position = Vector3.Lerp(afterListen, afterListen + Vector3.up * 0.025f, stop);
                SetFrame(moemaRenderer, moemaIdleFront, Mathf.FloorToInt((time - listenStart) * 2.5f));
            }
            else if (time < trotEnd)
            {
                float run = Smooth(Mathf.InverseLerp(trotStart, trotEnd, time));
                Vector3 target = new Vector3(1.48f, -0.79f, moemaStart.z);
                moema.position = Vector3.Lerp(afterListen + Vector3.up * 0.025f, target, run);
                SetFrame(moemaRenderer, moemaWalkBack, Mathf.FloorToInt((time - trotStart) * 12f));
                if (moemaShadow != null)
                {
                    moemaShadow.transform.position = Vector3.Lerp(moemaShadowStart, target + Vector3.down * 0.025f, run);
                    moemaShadow.transform.localScale = Vector3.Lerp(Vector3.one * 0.75f, Vector3.one * 0.55f, run);
                }
            }
            else
            {
                moema.position = new Vector3(1.48f, -0.79f, moemaStart.z);
                moemaRenderer.enabled = false;
                if (moemaShadow != null) moemaShadow.enabled = false;
            }
        }

        void UpdatePlaqueLabels()
        {
            if (sceneCanvas == null || galleryPlaqueAnchors == null || galleryPlaqueLabels == null || sceneCamera == null) return;
            RectTransform canvasRect = sceneCanvas.transform as RectTransform;
            if (canvasRect == null) return;
            for (int i = 0; i < galleryPlaqueLabels.Length && i < galleryPlaqueAnchors.Length; i++)
            {
                Text label = galleryPlaqueLabels[i];
                Transform anchor = galleryPlaqueAnchors[i];
                if (label == null || anchor == null) continue;
                bool visible = FloorIndex == 1 && elapsed >= 2.8f && elapsed < ResultShot;
                label.enabled = visible;
                if (!visible) continue;
                Vector2 screen = sceneCamera.WorldToScreenPoint(anchor.position);
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out Vector2 local))
                    (label.transform as RectTransform).anchoredPosition = local;
            }
        }

        void UpdateClock(float time)
        {
            if (clockHudValue != null) clockHudValue.text = time < 0.45f ? "05:59" : globalClockValue;
            float hudAlpha = Smooth(Mathf.InverseLerp(0.22f, 0.62f, time));
            float tickAccent = 1f - Smooth(Mathf.InverseLerp(0.48f, 0.96f, time));
            if (clockHud != null) clockHud.color = new Color(0.012f, 0.022f, 0.052f, 0.88f * hudAlpha);
            if (clockHudLabel != null) SetTextAlpha(clockHudLabel, hudAlpha);
            if (clockHudValue != null)
            {
                Color valueColor = Color.Lerp(new Color(0.93f, 0.96f, 1f), new Color(1f, 0.89f, 0.68f), tickAccent * 0.48f);
                valueColor.a = hudAlpha;
                clockHudValue.color = valueColor;
                float pulse = 1f + 0.035f * tickAccent;
                clockHudValue.transform.localScale = Vector3.one * pulse;
            }
        }

        void UpdateMorningWash(float time)
        {
            if (morningWash == null) return;
            float morning = Smooth(Mathf.InverseLerp(0.55f, 2.80f, time));
            float end = 1f - Smooth(Mathf.InverseLerp(11.00f, 11.45f, time));
            Color c = morningWash.color;
            c.a = morning * 0.075f * end;
            morningWash.color = c;
        }

        void UpdateSubtitle(float time, bool playVoice)
        {
            int cue = -1;
            string speaker = string.Empty;
            string text = string.Empty;
            AudioClip voice = null;
            if (time >= 0.60f && time < 2.82f)
            {
                cue = 0; speaker = "NARRADOR"; text = "Às seis, o prazo terminou."; voice = narratorOpening;
            }
            else if (time >= 3.16f && time < 6.82f)
            {
                cue = 1; speaker = "NARRADOR"; text = FloorNarration; voice = ValueAt(narratorFloorLines, FloorIndex, null);
            }
            else if (time >= 7.16f && time < 9.23f)
            {
                cue = 2; speaker = "NARRADOR"; text = "O segurança protegeu o espaço e avisou a equipe."; voice = narratorRadioSummary;
            }
            else if (time >= 9.30f && time < 10.96f)
            {
                cue = 3; speaker = "CURADORIA • RÁDIO"; text = "Entendido. Vamos cuidar disso juntos."; voice = curatorRadioReply;
            }
            else if (time >= 11.18f)
            {
                cue = 4; speaker = "NARRADOR"; text = "A exposição ainda não estava pronta. A noite podia ser tentada outra vez."; voice = narratorEnding;
            }

            float alpha = cue < 0 ? 0f : Smooth(Mathf.InverseLerp(cue == 4 ? 11.18f : CueStart(cue), (cue == 4 ? 11.18f : CueStart(cue)) + 0.12f, time)) *
                (cue == 4 ? 1f : 1f - Smooth(Mathf.InverseLerp(CueEnd(cue) - 0.12f, CueEnd(cue), time)));
            if (subtitleText != null)
            {
                subtitleText.enabled = cue >= 0 && cue != 4;
                if (cue != activeSubtitle) subtitleText.text = cue < 0 || cue == 4 ? string.Empty : speaker + "\n“" + text + "”";
                Color c = subtitleText.color; c.a = alpha; subtitleText.color = c;
            }
            if (subtitleBackground != null)
            {
                subtitleBackground.enabled = cue >= 0 && cue != 4;
                Color c = subtitleBackground.color; c.a = 0.82f * alpha; subtitleBackground.color = c;
            }
            if (cue != activeSubtitle)
            {
                activeSubtitle = cue;
                if (playVoice && voiceSource != null)
                {
                    voiceSource.Stop();
                    if (voice != null) { voiceSource.clip = voice; voiceSource.Play(); }
                }
            }
        }

        void UpdateResult(float time)
        {
            float alpha = Smooth(Mathf.InverseLerp(ResultShot + 0.12f, ResultShot + 0.55f, time));
            if (resultBackdrop != null)
            {
                Color c = resultBackdrop.color; c.a = 0.88f * alpha; resultBackdrop.color = c;
            }
            if (resultCanvasGroup != null)
            {
                resultCanvasGroup.alpha = alpha;
                bool ready = time >= ResultShot + 0.48f;
                resultCanvasGroup.interactable = ready;
                resultCanvasGroup.blocksRaycasts = ready;
            }
            if (retryButton != null) retryButton.interactable = time >= ResultShot + 0.48f;
            if (menuButton != null) menuButton.interactable = time >= ResultShot + 0.48f;
        }

        void UpdateFade(float time)
        {
            if (fadeImage == null) return;
            float opening = 1f - Smooth(Mathf.InverseLerp(0f, 0.34f, time));
            float intoResult = Smooth(Mathf.InverseLerp(10.88f, 11.04f, time)) * (1f - Smooth(Mathf.InverseLerp(11.04f, 11.24f, time)));
            fadeImage.color = new Color(0f, 0f, 0f, Mathf.Max(opening, intoResult));
        }

        void UpdateAudio()
        {
            if (!clockCuePlayed && elapsed >= 0.45f)
            {
                clockCuePlayed = true;
                PlayEffect(clockChime, 0.10f);
            }
            if (!workCuePlayed && elapsed >= 3.68f)
            {
                workCuePlayed = true;
                if (FloorIndex == 0) PlayEffect(groundFloorSettle, 0.12f);
                else if (FloorIndex == 1) PlayEffect(galleryWoodShift, 0.10f);
            }
            if (!radioCuePlayed && elapsed >= 7.12f)
            {
                radioCuePlayed = true;
                PlayEffect(radioClick, 0.14f);
            }
            if (!finalCuePlayed && elapsed >= 11.22f)
            {
                finalCuePlayed = true;
                PlayEffect(xylophoneNote, 0.11f);
            }
            if (FloorIndex == 2 && elapsed >= nextMoemaStep && elapsed < 6.62f)
            {
                PlayEffect(moemaTrot, 0.075f);
                nextMoemaStep = elapsed + 0.42f;
            }
            if (musicSource != null && musicSource.isPlaying)
            {
                float duck = activeSubtitle >= 0 ? 0.64f : 1f;
                musicSource.volume = Mathf.Lerp(0.035f, 0.055f, Smooth(Mathf.InverseLerp(3.2f, 10.8f, elapsed))) * duck;
            }
        }

        void StartAudio()
        {
            AudioClip ambience = ValueAt(floorAmbiences, FloorIndex, null);
            if (ambienceSource != null && ambience != null)
            {
                ambienceSource.clip = ambience;
                ambienceSource.loop = true;
                ambienceSource.volume = 0.10f;
                ambienceSource.Play();
            }
            if (morningAirSource != null && morningAir != null)
            {
                morningAirSource.clip = morningAir;
                morningAirSource.loop = true;
                morningAirSource.volume = 0.045f;
                morningAirSource.PlayDelayed(0.62f);
            }
            if (musicSource != null && calmEndingMusic != null)
            {
                musicSource.clip = calmEndingMusic;
                musicSource.loop = true;
                musicSource.volume = 0.035f;
                musicSource.PlayDelayed(0.82f);
            }
        }

        void StopAudio()
        {
            if (voiceSource != null) voiceSource.Stop();
            if (ambienceSource != null) ambienceSource.Stop();
            if (morningAirSource != null) morningAirSource.Stop();
            if (effectsSource != null) effectsSource.Stop();
            if (musicSource != null) musicSource.Stop();
        }

        void PlayEffect(AudioClip clip, float volume)
        {
            if (effectsSource != null && clip != null) effectsSource.PlayOneShot(clip, volume);
        }

        void SetAllRadioScreens(float brightness)
        {
            if (radioScreens == null) return;
            for (int i = 0; i < radioScreens.Length; i++)
                if (radioScreens[i] != null) radioScreens[i].color = new Color(0.52f, 0.82f, 1f, brightness);
        }

        void UpdateResultFloorLabel()
        {
            if (resultFloorLabel == null) return;
            string[] labels = { "ANDAR 1  •  TÉRREO", "ANDAR 2  •  GALERIA", "ANDAR 3  •  ACERVO" };
            resultFloorLabel.text = labels[FloorIndex];
        }

        void Finish()
        {
            if (finished) return;
            elapsed = Duration;
            finished = true;
            playing = false;
            ControlsLocked = false;
            StopAudio();
            if (onCutsceneFinished != null) onCutsceneFinished.Invoke();
        }

        int FloorIndex => Mathf.Clamp((int)floorVariant - 1, 0, 2);
        string FloorNarration => FloorIndex == 0
            ? "As estátuas ainda precisavam voltar ao lugar."
            : FloorIndex == 1
                ? "Uma pintura ainda esperava sua parede."
                : "Algumas peças ainda esperavam ser recolhidas.";

        static float CueStart(int cue) => cue == 0 ? 0.60f : cue == 1 ? 3.16f : cue == 2 ? 7.16f : 9.30f;
        static float CueEnd(int cue) => cue == 0 ? 2.82f : cue == 1 ? 6.82f : cue == 2 ? 9.23f : 10.96f;
        static float Smooth(float value) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(value));

        static void SetFrame(SpriteRenderer renderer, Sprite[] frames, int frame)
        {
            if (renderer == null || frames == null || frames.Length == 0) return;
            renderer.sprite = frames[Mathf.Abs(frame) % frames.Length];
        }

        static void SetTextAlpha(Text text, float alpha)
        {
            if (text == null) return;
            Color c = text.color; c.a = alpha; text.color = c;
        }

        static T ValueAt<T>(T[] values, int index, T fallback) where T : class
            => values != null && index >= 0 && index < values.Length && values[index] != null ? values[index] : fallback;

        static float ValueAt(float[] values, int index, float fallback)
            => values != null && index >= 0 && index < values.Length ? values[index] : fallback;

        static Vector3 ValueAt(Vector3[] values, int index, Vector3 fallback)
            => values != null && index >= 0 && index < values.Length ? values[index] : fallback;

        static Vector3[] CapturePositions(Transform[] transforms)
        {
            if (transforms == null) return Array.Empty<Vector3>();
            Vector3[] result = new Vector3[transforms.Length];
            for (int i = 0; i < result.Length; i++) result[i] = transforms[i] != null ? transforms[i].position : Vector3.zero;
            return result;
        }

        static Vector3[] CaptureLocalPositions(Transform[] transforms)
        {
            if (transforms == null) return Array.Empty<Vector3>();
            Vector3[] result = new Vector3[transforms.Length];
            for (int i = 0; i < result.Length; i++) result[i] = transforms[i] != null ? transforms[i].localPosition : Vector3.zero;
            return result;
        }

        static Vector3[] CaptureScales(Transform[] transforms)
        {
            if (transforms == null) return Array.Empty<Vector3>();
            Vector3[] result = new Vector3[transforms.Length];
            for (int i = 0; i < result.Length; i++) result[i] = transforms[i] != null ? transforms[i].localScale : Vector3.one;
            return result;
        }

        static void RestoreTransforms(Transform[] transforms, Vector3[] positions, Vector3[] scales)
        {
            if (transforms == null) return;
            for (int i = 0; i < transforms.Length; i++)
                if (transforms[i] != null)
                {
                    if (positions != null && i < positions.Length) transforms[i].position = positions[i];
                    if (scales != null && i < scales.Length) transforms[i].localScale = scales[i];
                }
        }

        static void RestorePositions(Transform[] transforms, Vector3[] positions)
        {
            if (transforms == null || positions == null) return;
            for (int i = 0; i < transforms.Length && i < positions.Length; i++)
                if (transforms[i] != null) transforms[i].position = positions[i];
        }

        static void RestoreLocalPositions(Transform[] transforms, Vector3[] positions)
        {
            if (transforms == null || positions == null) return;
            for (int i = 0; i < transforms.Length && i < positions.Length; i++)
                if (transforms[i] != null) transforms[i].localPosition = positions[i];
        }
    }
}
