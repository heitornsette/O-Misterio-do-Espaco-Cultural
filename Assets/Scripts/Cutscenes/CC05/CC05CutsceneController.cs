using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Misterio.Cutscenes
{
    [Serializable]
    public sealed class CC05SubtitleCue
    {
        public float start;
        public float end;
        public string speaker;
        [TextArea] public string text;
        public AudioClip voice;
    }

    /// <summary>Ten-second fourth-floor reveal. The global clock is supplied by the game.</summary>
    public sealed class CC05CutsceneController : MonoBehaviour
    {
        public const float Duration = 10f;
        const float RunStart = 3.28f;
        const float RunEnd = 4.66f;
        const float ClimbStart = 4.66f;
        const float WindowArrival = 4.98f;
        const float MoemaExitStart = 5.24f;
        const float MoemaGone = 5.56f;
        const float ClockReveal = 5.96f;

        static readonly float[] GuardSteps = { 0.84f, 1.13f, 1.46f, 3.14f };
        static readonly float[] MoemaSteps = { 3.40f, 3.86f, 4.27f, 4.60f };
        static readonly Vector3[] MoemaRunPath =
        {
            new Vector3(-1.42f, -0.58f, -0.06f),
            new Vector3(-0.74f, -0.52f, -0.06f),
            new Vector3(0.36f, -0.27f, -0.06f),
            new Vector3(1.24f, -0.08f, -0.06f)
        };

        public bool ControlsLocked { get; private set; }
        public float Elapsed => elapsed;
        public string GlobalClockValue => globalClockValue;

        [Header("Câmera e salão")]
        [SerializeField] Camera sceneCamera;
        [SerializeField] SpriteRenderer nightSky;
        [SerializeField] SpriteRenderer dawnSky;
        [SerializeField] SpriteRenderer flashlightBeam;
        [SerializeField] Transform flashlightBeamTransform;
        [SerializeField] SpriteRenderer panelGlow;
        [SerializeField] SpriteRenderer damagedWireSpark;

        [Header("Elevador")]
        [SerializeField] SpriteRenderer elevatorCab;
        [SerializeField] SpriteRenderer elevatorDoorLeft;
        [SerializeField] SpriteRenderer elevatorDoorRight;
        [SerializeField] Sprite[] elevatorDoorDetails;

        [Header("Segurança")]
        [SerializeField] Transform guard;
        [SerializeField] SpriteRenderer guardRenderer;
        [SerializeField] Sprite[] guardIdleFront;
        [SerializeField] Sprite[] guardIdleBack;
        [SerializeField] Sprite[] guardWalkFront;
        [SerializeField] SpriteRenderer guardShadow;

        [Header("Moema")]
        [SerializeField] Transform moema;
        [SerializeField] SpriteRenderer moemaRenderer;
        [SerializeField] Sprite[] moemaIdleFront;
        [SerializeField] Sprite[] moemaWalkBack;
        [SerializeField] SpriteRenderer moemaShadow;

        [Header("Painel e obra")]
        [SerializeField] SpriteRenderer panelRenderer;
        [SerializeField] Sprite panelBase;
        [SerializeField] Sprite[] panelSparkFrames;
        [SerializeField] SpriteRenderer sculptureRenderer;
        [SerializeField] SpriteRenderer[] beamDust;

        [Header("Legendas e relógio global")]
        [SerializeField] Text subtitleText;
        [SerializeField] Image subtitleBackground;
        [SerializeField] Image clockPlate;
        [SerializeField] Text clockLabel;
        [SerializeField] Text clockValueText;
        [SerializeField] CC05SubtitleCue[] subtitles =
        {
            new CC05SubtitleCue
            {
                start = 0.08f,
                end = 2.88f,
                speaker = "NARRADOR",
                text = "O amanhecer se aproximava, e a obra principal ainda estava no escuro."
            },
            new CC05SubtitleCue
            {
                start = 3.17f,
                end = 4.57f,
                speaker = "NARRADOR",
                text = "Mesmo com a confusão, o circuito ainda podia ser reparado."
            },
            new CC05SubtitleCue
            {
                start = 4.62f,
                end = 5.26f,
                speaker = "SEGURANÇA",
                text = "Moema..."
            },
            new CC05SubtitleCue
            {
                start = 8.42f,
                end = 9.92f,
                speaker = "SEGURANÇA",
                text = "Vamos fazer essa luz voltar."
            }
        };

        [Header("Som")]
        [SerializeField] AudioSource voiceSource;
        [SerializeField] AudioSource ambienceSource;
        [SerializeField] AudioSource effectsSource;
        [SerializeField] AudioSource musicSource;
        [SerializeField] AudioClip elevatorMotor;
        [SerializeField] AudioClip electricalHum;
        [SerializeField] AudioClip electricalSnap;
        [SerializeField] AudioClip windowCreak;
        [SerializeField] AudioClip featherRustle;
        [SerializeField] AudioClip guardStep;
        [SerializeField] AudioClip moemaStep;
        [SerializeField] AudioClip restrainedMusic;

        [Header("Integração com o jogo")]
        [SerializeField] bool playOnAwake = true;
        [SerializeField] UnityEvent onCutsceneStarted = new UnityEvent();
        [SerializeField] UnityEvent onCutsceneFinished = new UnityEvent();
        [SerializeField, HideInInspector] bool startPausedForEditorReview;

        float elapsed;
        double lastRealtime;
        Vector3 guardStart;
        Vector3 guardScale;
        Vector3 moemaStart;
        Vector3 moemaScale;
        Vector3 doorLeftStart;
        Vector3 doorRightStart;
        Vector3 beamStartScale;
        Vector3 guardShadowStart;
        Vector3 moemaShadowStart;
        string globalClockValue = "--:--";
        int activeVoiceCueIndex = -1;
        int nextGuardStep;
        int nextMoemaStep;
        bool playing;
        bool finished;
        bool playedElevator;
        bool playedSnapOne;
        bool playedSnapTwo;
        bool playedWindow;
        bool playedFeathers;

        void Awake()
        {
            guardStart = guard != null ? guard.position : new Vector3(-2.42f, -1.28f, -0.08f);
            guardScale = guard != null ? guard.localScale : Vector3.one;
            moemaStart = moema != null ? moema.position : new Vector3(-1.30f, -0.58f, -0.06f);
            moemaScale = moema != null ? moema.localScale : Vector3.one;
            doorLeftStart = elevatorDoorLeft != null ? elevatorDoorLeft.transform.position : new Vector3(-2.66f, 0.42f, -0.04f);
            doorRightStart = elevatorDoorRight != null ? elevatorDoorRight.transform.position : new Vector3(-2.14f, 0.42f, -0.04f);
            beamStartScale = flashlightBeamTransform != null ? flashlightBeamTransform.localScale : Vector3.one;
            guardShadowStart = guardShadow != null ? guardShadow.transform.position : Vector3.zero;
            moemaShadowStart = moemaShadow != null ? moemaShadow.transform.position : Vector3.zero;

            ResetVisuals();
            ControlsLocked = playOnAwake || startPausedForEditorReview;
            lastRealtime = Time.realtimeSinceStartupAsDouble;
            if (!startPausedForEditorReview && playOnAwake) Begin();
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

        /// <summary>Starts the reveal after the fourth-floor stealth sequence succeeds.</summary>
        public void PlayCutscene()
        {
            if (playing && !finished) return;
            startPausedForEditorReview = false;
            Begin();
        }

        /// <summary>Receives the current game clock. This cutscene never advances or replaces it.</summary>
        public void SetGlobalClock(string value)
        {
            globalClockValue = string.IsNullOrWhiteSpace(value) ? "--:--" : value.Trim();
            if (clockValueText != null) clockValueText.text = globalClockValue;
        }

#if UNITY_EDITOR
        /// <summary>Stops playback before exact-time visual review in the Editor.</summary>
        public void PrepareEditorFrameReviewCapture()
        {
            playing = false;
            finished = false;
            elapsed = 0f;
            ControlsLocked = true;
            activeVoiceCueIndex = -1;
            StopAudio();
            ResetVisuals();
            EvaluateVisuals(0f, false);
        }

        /// <summary>Evaluates a single exact timeline position without advancing clocks or audio.</summary>
        public void SetEditorReviewTime(float time)
        {
            elapsed = Mathf.Clamp(time, 0f, Duration);
            EvaluateVisuals(elapsed, false);
        }
#endif

        void Begin()
        {
            elapsed = 0f;
            activeVoiceCueIndex = -1;
            nextGuardStep = 0;
            nextMoemaStep = 0;
            playedElevator = false;
            playedSnapOne = false;
            playedSnapTwo = false;
            playedWindow = false;
            playedFeathers = false;
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

        void StartAudio()
        {
            if (ambienceSource != null && electricalHum != null)
            {
                ambienceSource.clip = electricalHum;
                ambienceSource.loop = true;
                ambienceSource.volume = 0.075f;
                ambienceSource.Play();
            }
            if (musicSource != null && restrainedMusic != null)
            {
                musicSource.clip = restrainedMusic;
                musicSource.loop = true;
                musicSource.volume = 0.018f;
                musicSource.Play();
            }
        }

        void StopAudio()
        {
            if (voiceSource != null) voiceSource.Stop();
            if (ambienceSource != null) ambienceSource.Stop();
            if (effectsSource != null) effectsSource.Stop();
            if (musicSource != null) musicSource.Stop();
        }

        void ResetVisuals()
        {
            if (guard != null)
            {
                guard.position = guardStart;
                guard.localScale = guardScale;
                guard.rotation = Quaternion.identity;
            }
            if (guardRenderer != null)
            {
                guardRenderer.enabled = true;
                guardRenderer.color = Color.white;
                SetFrame(guardRenderer, guardIdleFront, 0);
            }
            if (guardShadow != null)
            {
                guardShadow.enabled = true;
                guardShadow.transform.position = guardShadowStart;
                guardShadow.color = new Color(0.015f, 0.020f, 0.035f, 0.54f);
            }
            if (moema != null)
            {
                moema.position = moemaStart;
                moema.localScale = moemaScale;
                moema.rotation = Quaternion.identity;
                moema.gameObject.SetActive(true);
            }
            if (moemaRenderer != null)
            {
                moemaRenderer.enabled = true;
                moemaRenderer.color = Color.white;
                moemaRenderer.flipX = false;
                moemaRenderer.sortingOrder = 4;
                SetFrame(moemaRenderer, moemaIdleFront, 0);
            }
            if (moemaShadow != null)
            {
                moemaShadow.enabled = true;
                moemaShadow.transform.position = moemaShadowStart;
                moemaShadow.color = new Color(0.015f, 0.020f, 0.035f, 0.40f);
            }
            if (elevatorDoorLeft != null) elevatorDoorLeft.transform.position = doorLeftStart;
            if (elevatorDoorRight != null) elevatorDoorRight.transform.position = doorRightStart;
            if (elevatorCab != null) elevatorCab.enabled = true;
            if (elevatorDoorLeft != null) elevatorDoorLeft.enabled = true;
            if (elevatorDoorRight != null) elevatorDoorRight.enabled = true;
            if (nightSky != null) nightSky.color = new Color(1f, 1f, 1f, 0.78f);
            if (dawnSky != null) dawnSky.color = new Color(1f, 1f, 1f, 0f);
            if (flashlightBeam != null) flashlightBeam.color = new Color(0.90f, 0.91f, 0.83f, 0f);
            if (flashlightBeamTransform != null) flashlightBeamTransform.localScale = beamStartScale;
            if (panelGlow != null) panelGlow.color = new Color(0.74f, 0.82f, 1f, 0f);
            if (panelRenderer != null) panelRenderer.sprite = panelBase;
            if (damagedWireSpark != null) damagedWireSpark.enabled = false;
            if (sculptureRenderer != null) sculptureRenderer.color = new Color(0.72f, 0.77f, 0.90f, 1f);
            if (beamDust != null)
                foreach (SpriteRenderer mote in beamDust)
                    if (mote != null) mote.enabled = false;
            if (subtitleText != null)
            {
                subtitleText.text = string.Empty;
                subtitleText.enabled = false;
                subtitleText.color = Color.white;
            }
            if (subtitleBackground != null)
            {
                subtitleBackground.enabled = false;
                subtitleBackground.color = new Color(0.012f, 0.020f, 0.045f, 0.80f);
            }
            if (clockValueText != null) clockValueText.text = globalClockValue;
            if (clockLabel != null) clockLabel.text = "HORA GLOBAL";
            SetClockAlpha(0f);
        }

        void EvaluateVisuals(float time, bool playVoice)
        {
            UpdateCamera(time);
            UpdateElevator(time);
            UpdateGuard(time);
            UpdateMoema(time);
            UpdateSky(time);
            UpdatePanelAndSparks(time);
            UpdateFlashlight(time);
            UpdateDust(time);
            UpdateClock(time);
            UpdateSubtitleAndVoice(time, playVoice);
        }

        void UpdateCamera(float time)
        {
            if (sceneCamera == null) return;
            Vector3 position;
            float size;
            if (time < 2.72f)
            {
                float u = Smooth(time / 2.72f);
                position = Vector3.Lerp(new Vector3(-2.48f, 0.10f, -10f), new Vector3(0f, 0f, -10f), u);
                size = Mathf.Lerp(1.92f, 2.8125f, u);
            }
            else if (time < 3.12f)
            {
                float u = Smooth(Mathf.InverseLerp(2.72f, 3.12f, time));
                position = Vector3.Lerp(new Vector3(0f, 0f, -10f), new Vector3(-2.36f, 0.24f, -10f), u);
                size = Mathf.Lerp(2.8125f, 1.86f, u);
            }
            else if (time < 5.68f)
            {
                float u = Smooth(Mathf.InverseLerp(3.12f, 5.68f, time));
                position = Vector3.Lerp(new Vector3(-2.36f, 0.24f, -10f), new Vector3(1.92f, 0.50f, -10f), u);
                size = Mathf.Lerp(1.86f, 1.74f, u);
            }
            else if (time < 8f)
            {
                position = new Vector3(1.92f, 0.50f, -10f);
                size = 1.74f;
            }
            else
            {
                // The eighth-second change is a deliberate storyboard cut to the over-shoulder plan.
                position = new Vector3(-1.84f, 0.15f, -10f);
                size = 1.84f;
            }
            sceneCamera.transform.position = position;
            sceneCamera.orthographicSize = size;
        }

        void UpdateElevator(float time)
        {
            float opening = Smooth(Mathf.InverseLerp(0.12f, 0.88f, time));
            if (elevatorDoorLeft != null)
                elevatorDoorLeft.transform.position = doorLeftStart + Vector3.left * (0.47f * opening);
            if (elevatorDoorRight != null)
                elevatorDoorRight.transform.position = doorRightStart + Vector3.right * (0.47f * opening);
            if (elevatorDoorDetails != null && elevatorDoorDetails.Length > 0)
            {
                int frame = Mathf.FloorToInt(time * 5f) % elevatorDoorDetails.Length;
                if (elevatorDoorLeft != null) elevatorDoorLeft.sprite = elevatorDoorDetails[frame];
                if (elevatorDoorRight != null) elevatorDoorRight.sprite = elevatorDoorDetails[frame];
            }
        }

        void UpdateGuard(float time)
        {
            if (guard == null || guardRenderer == null) return;
            guard.localScale = guardScale;
            guard.rotation = Quaternion.identity;

            if (time < 0.70f)
            {
                guardRenderer.enabled = time >= 0.45f;
                guard.position = guardStart;
                SetFrame(guardRenderer, guardIdleFront, Mathf.FloorToInt(time * 3.5f));
            }
            else if (time < 1.64f)
            {
                guardRenderer.enabled = true;
                float u = Smooth(Mathf.InverseLerp(0.70f, 1.64f, time));
                guard.position = Vector3.Lerp(guardStart, new Vector3(-1.82f, -1.30f, guardStart.z), u);
                SetFrame(guardRenderer, guardWalkFront, Mathf.FloorToInt((time - 0.70f) * 10f));
            }
            else if (time < 3.10f)
            {
                float settle = 1f - Smooth(Mathf.InverseLerp(1.64f, 1.91f, time));
                guard.position = new Vector3(-1.82f, -1.30f + 0.012f * settle, guardStart.z);
                guard.localScale = guardScale * (1f - 0.003f * settle);
                SetFrame(guardRenderer, guardIdleFront, Mathf.FloorToInt(time * 3.6f));
            }
            else if (time < 3.50f)
            {
                float u = Smooth(Mathf.InverseLerp(3.10f, 3.50f, time));
                guard.position = Vector3.Lerp(new Vector3(-1.82f, -1.30f, guardStart.z),
                    new Vector3(-2.05f, -1.30f, guardStart.z), u);
                SetFrame(guardRenderer, guardWalkFront, Mathf.FloorToInt((time - 3.10f) * 12f));
            }
            else if (time < 4.20f)
            {
                guard.position = new Vector3(-2.05f, -1.30f, guardStart.z);
                SetFrame(guardRenderer, guardIdleFront, Mathf.FloorToInt(time * 3.2f));
            }
            else
            {
                float lean = Smooth(Mathf.InverseLerp(8.08f, 8.42f, time));
                float approach = Smooth(Mathf.InverseLerp(7.82f, 8.40f, time));
                float finalX = Mathf.Lerp(-2.05f, -3.28f, approach);
                guard.position = new Vector3(finalX - 0.025f * lean, -1.30f, guardStart.z);
                guard.localScale = new Vector3(guardScale.x * (1f + 0.006f * lean),
                    guardScale.y * (1f - 0.004f * lean), guardScale.z);
                guard.rotation = Quaternion.Euler(0f, 0f, -0.8f * lean);
                SetFrame(guardRenderer, guardIdleBack, Mathf.FloorToInt(time * 3f));
            }

            if (guardShadow != null)
                guardShadow.transform.position = new Vector3(guard.position.x + 0.02f,
                    guardShadowStart.y + (guard.position.y - guardStart.y) * 0.12f, guardShadowStart.z);
        }

        void UpdateMoema(float time)
        {
            if (moema == null || moemaRenderer == null) return;
            if (time < RunStart)
            {
                moema.gameObject.SetActive(true);
                moemaRenderer.enabled = true;
                moemaRenderer.flipX = false;
                moemaRenderer.sortingOrder = 4;
                moemaRenderer.color = Color.white;
                moema.position = moemaStart + new Vector3(0f, 0.008f * Mathf.Sin(time * 3.8f), 0f);
                SetFrame(moemaRenderer, moemaIdleFront, Mathf.FloorToInt(time * 3f));
                UpdateMoemaShadow(moema.position, 1f);
                return;
            }

            if (time < RunEnd)
            {
                float u = Smooth(Mathf.InverseLerp(RunStart, RunEnd, time));
                float gait = Mathf.Repeat((time - RunStart) * 16f / FrameCount(moemaWalkBack), 1f);
                float bob = (1f - Mathf.Cos(gait * Mathf.PI * 2f)) * 0.012f;
                moema.position = SamplePath(MoemaRunPath, u) + Vector3.up * bob;
                moema.localScale = moemaScale * Mathf.Lerp(0.93f, 1f, u);
                moema.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-1f, 0.5f, u));
                moemaRenderer.flipX = false;
                moemaRenderer.color = Color.white;
                moemaRenderer.sortingOrder = u < 0.72f ? 4 : 6;
                SetFrame(moemaRenderer, moemaWalkBack, Mathf.FloorToInt((time - RunStart) * 16f));
                UpdateMoemaShadow(moema.position, 1f);
                return;
            }

            if (time < WindowArrival)
            {
                float u = Mathf.InverseLerp(ClimbStart, WindowArrival, time);
                float climb = u * u * (3f - 2f * u);
                float smallScramble = Mathf.Sin(u * Mathf.PI) * 0.075f;
                moema.position = Vector3.Lerp(new Vector3(1.24f, -0.08f, -0.06f),
                    new Vector3(1.92f, 0.94f, -0.06f), climb) + Vector3.up * smallScramble;
                moema.localScale = moemaScale * Mathf.Lerp(1f, 0.82f, climb);
                moema.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0.5f, -2.2f, climb));
                moemaRenderer.sortingOrder = 7;
                moemaRenderer.color = Color.white;
                SetFrame(moemaRenderer, moemaWalkBack, Mathf.FloorToInt((time - ClimbStart) * 18f));
                UpdateMoemaShadow(moema.position + Vector3.down * 0.15f, 1f - climb * 0.5f);
                return;
            }

            if (time < MoemaExitStart)
            {
                moema.position = new Vector3(1.92f, 0.94f, -0.06f);
                moema.localScale = moemaScale * 0.82f;
                moema.rotation = Quaternion.identity;
                moemaRenderer.sortingOrder = 7;
                moemaRenderer.color = Color.white;
                SetFrame(moemaRenderer, moemaWalkBack, 0);
                UpdateMoemaShadow(moema.position + Vector3.down * 0.15f, 0.45f);
                return;
            }

            if (time < MoemaGone)
            {
                float exit = Smooth(Mathf.InverseLerp(MoemaExitStart, MoemaGone, time));
                moema.position = new Vector3(1.92f, 0.94f + 0.20f * exit, -0.06f);
                moema.localScale = moemaScale * Mathf.Lerp(0.82f, 0.66f, exit);
                Color tint = Color.white;
                tint.a = 1f - exit;
                moemaRenderer.color = tint;
                SetFrame(moemaRenderer, moemaWalkBack, Mathf.FloorToInt(time * 18f));
                UpdateMoemaShadow(moema.position + Vector3.down * 0.15f, 0.30f * (1f - exit));
                return;
            }

            moema.gameObject.SetActive(false);
            if (moemaShadow != null) moemaShadow.enabled = false;
        }

        void UpdateMoemaShadow(Vector3 position, float scale)
        {
            if (moemaShadow == null) return;
            moemaShadow.enabled = scale > 0.03f;
            moemaShadow.transform.position = new Vector3(position.x, position.y - 0.06f * scale, moemaShadowStart.z);
            moemaShadow.transform.localScale = new Vector3(0.72f * scale, 0.18f * scale, 1f);
            moemaShadow.color = new Color(0.012f, 0.016f, 0.028f, 0.42f * scale);
        }

        void UpdateSky(float time)
        {
            float dawn = Smooth(Mathf.InverseLerp(3.55f, 7.0f, time));
            dawn *= 0.72f;
            if (nightSky != null) nightSky.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.78f, 0.02f, dawn));
            if (dawnSky != null) dawnSky.color = new Color(1f, 1f, 1f, dawn);
        }

        void UpdatePanelAndSparks(float time)
        {
            bool panelArc = InRange(time, 0.43f, 0.61f) || InRange(time, 1.94f, 2.12f) ||
                            InRange(time, 3.18f, 3.35f) || InRange(time, 5.05f, 5.20f);
            if (panelRenderer != null)
            {
                if (!panelArc || panelSparkFrames == null || panelSparkFrames.Length == 0)
                {
                    panelRenderer.sprite = panelBase;
                }
                else
                {
                    int frame = Mathf.FloorToInt((time - Mathf.Floor(time * 8f) / 8f) * 32f) % panelSparkFrames.Length;
                    panelRenderer.sprite = panelSparkFrames[frame];
                }
            }

            float light = 0.16f + 0.07f * (0.5f + 0.5f * Mathf.Sin(time * 5.4f));
            light = Mathf.Lerp(light, 0.40f, Smooth(Mathf.InverseLerp(7.95f, 8.45f, time)));
            if (time > 5.8f && time < 7.9f) light *= 0.55f;
            if (panelGlow != null) panelGlow.color = new Color(0.78f, 0.83f, 1f, light);

            bool wireArc = InRange(time, 3.78f, 3.94f) || InRange(time, 4.45f, 4.61f);
            if (damagedWireSpark != null)
            {
                damagedWireSpark.enabled = wireArc;
                if (wireArc)
                {
                    float flicker = 0.64f + 0.36f * Mathf.Sin((time - 3.78f) * 85f);
                    damagedWireSpark.color = new Color(1f, 0.72f, 0.38f, flicker);
                }
            }
        }

        void UpdateFlashlight(float time)
        {
            if (flashlightBeam == null || flashlightBeamTransform == null || guard == null) return;
            Vector3 source = guard.position + new Vector3(-0.03f, 0.53f, -0.20f);
            Vector3 target = new Vector3(-4.25f, 0.83f, -0.20f);
            float intensity = 0.56f;
            if (time >= 3.15f && time < 5.55f)
            {
                float fade = 1f - Smooth(Mathf.InverseLerp(3.18f, 3.94f, time));
                target = new Vector3(-4.20f, 0.92f, -0.20f);
                intensity = 0.56f * fade;
            }
            else if (time >= 5.55f && time < 7.95f)
            {
                target = new Vector3(2.20f, 0.60f, -0.20f);
                intensity = 0f;
            }
            else if (time >= 7.95f)
            {
                target = new Vector3(-4.25f, 0.83f, -0.20f);
                intensity = 0.66f;
            }
            float on = Smooth(Mathf.InverseLerp(0.48f, 1.02f, time));
            float off = 1f - Smooth(Mathf.InverseLerp(5.45f, 5.85f, time));
            if (time >= 7.95f) off = 1f;
            Vector3 delta = target - source;
            float length = Mathf.Clamp(delta.magnitude, 0.7f, 4.6f);
            flashlightBeamTransform.position = (source + target) * 0.5f;
            flashlightBeamTransform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            flashlightBeamTransform.localScale = new Vector3(beamStartScale.x * length / 2.90f,
                beamStartScale.y * Mathf.Lerp(0.90f, 1.08f, Mathf.Clamp01(length / 3.4f)), beamStartScale.z);
            flashlightBeam.color = new Color(0.91f, 0.91f, 0.79f, intensity * on * off);
        }

        void UpdateDust(float time)
        {
            if (beamDust == null) return;
            float visibility = time < 2.95f ? 1f : (time < 5.5f ? 0.72f : 0.26f);
            float fade = flashlightBeam != null ? flashlightBeam.color.a : 0f;
            for (int i = 0; i < beamDust.Length; i++)
            {
                SpriteRenderer mote = beamDust[i];
                if (mote == null) continue;
                bool visible = fade > 0.10f && (Mathf.Sin(time * (1.45f + i * 0.13f) + i * 1.7f) > 0.08f);
                mote.enabled = visible;
                if (!visible) continue;
                float x = -3.85f + Mathf.Repeat(i * 0.77f + time * (0.09f + i * 0.008f), 2.5f);
                float y = 0.44f + 0.28f * Mathf.Sin(time * 0.7f + i * 2.2f);
                mote.transform.position = new Vector3(x, y, -0.24f);
                float alpha = Mathf.Clamp01(fade * visibility) * (0.16f + 0.06f * (i % 3));
                mote.color = new Color(0.93f, 0.90f, 0.70f, alpha);
            }
        }

        void UpdateClock(float time)
        {
            float alpha = Smooth(Mathf.InverseLerp(ClockReveal, 6.20f, time));
            if (time > 7.8f) alpha = Mathf.Lerp(alpha, 1f, Smooth(Mathf.InverseLerp(7.8f, 8.12f, time)));
            SetClockAlpha(alpha);
            if (clockValueText != null) clockValueText.text = globalClockValue;
        }

        void SetClockAlpha(float alpha)
        {
            if (clockPlate != null)
            {
                clockPlate.enabled = alpha > 0.01f;
                Color c = clockPlate.color;
                c.a = 0.84f * alpha;
                clockPlate.color = c;
            }
            SetTextAlpha(clockLabel, alpha);
            SetTextAlpha(clockValueText, alpha);
        }

        void UpdateSubtitleAndVoice(float time, bool playVoice)
        {
            CC05SubtitleCue active = null;
            int activeIndex = -1;
            if (subtitles != null)
            {
                for (int i = 0; i < subtitles.Length; i++)
                {
                    CC05SubtitleCue cue = subtitles[i];
                    if (cue == null || time < cue.start || time >= cue.end) continue;
                    active = cue;
                    activeIndex = i;
                    break;
                }
            }

            bool visible = active != null;
            float cueAlpha = 1f;
            if (visible)
            {
                float fadeIn = Smooth(Mathf.InverseLerp(active.start, active.start + 0.10f, time));
                float fadeOut = 1f - Smooth(Mathf.InverseLerp(active.end - 0.12f, active.end, time));
                cueAlpha = fadeIn * fadeOut;
            }
            if (subtitleText != null)
            {
                subtitleText.enabled = visible;
                if (activeIndex != activeVoiceCueIndex)
                    subtitleText.text = visible ? active.speaker + "\n“" + active.text + "”" : string.Empty;
                Color c = subtitleText.color;
                c.a = cueAlpha;
                subtitleText.color = c;
            }
            if (subtitleBackground != null)
            {
                subtitleBackground.enabled = visible;
                Color c = subtitleBackground.color;
                c.a = 0.80f * cueAlpha;
                subtitleBackground.color = c;
            }

            if (activeIndex != activeVoiceCueIndex)
            {
                activeVoiceCueIndex = activeIndex;
                if (playVoice && voiceSource != null)
                {
                    voiceSource.Stop();
                    if (active != null && active.voice != null)
                    {
                        voiceSource.clip = active.voice;
                        voiceSource.Play();
                    }
                }
            }
        }

        void UpdateAudio()
        {
            if (!playedElevator && elapsed >= 0.08f)
            {
                playedElevator = true;
                PlayEffect(elevatorMotor, 0.42f);
            }
            if (!playedSnapOne && elapsed >= 3.18f)
            {
                playedSnapOne = true;
                PlayEffect(electricalSnap, 0.28f);
            }
            if (!playedSnapTwo && elapsed >= 4.44f)
            {
                playedSnapTwo = true;
                PlayEffect(electricalSnap, 0.20f);
            }
            if (!playedWindow && elapsed >= 4.72f)
            {
                playedWindow = true;
                PlayEffect(windowCreak, 0.32f);
            }
            if (!playedFeathers && elapsed >= 4.90f)
            {
                playedFeathers = true;
                PlayEffect(featherRustle, 0.28f);
            }

            while (nextGuardStep < GuardSteps.Length && elapsed >= GuardSteps[nextGuardStep])
            {
                PlayEffect(guardStep, 0.30f);
                nextGuardStep++;
            }
            while (nextMoemaStep < MoemaSteps.Length && elapsed >= MoemaSteps[nextMoemaStep])
            {
                PlayEffect(moemaStep, 0.22f);
                nextMoemaStep++;
            }

            if (musicSource != null && musicSource.isPlaying)
            {
                float pulse = Smooth(Mathf.InverseLerp(5.8f, 8.12f, elapsed));
                float lineDuck = 1f - 0.32f * Smooth(Mathf.InverseLerp(8.24f, 8.46f, elapsed)) *
                                       (1f - Smooth(Mathf.InverseLerp(9.85f, 10f, elapsed)));
                musicSource.volume = Mathf.Lerp(0.018f, 0.047f, pulse) * lineDuck;
            }
        }

        void Finish()
        {
            if (finished) return;
            elapsed = Duration;
            finished = true;
            playing = false;
            ControlsLocked = false;
            if (voiceSource != null) voiceSource.Stop();
            if (musicSource != null) musicSource.Stop();
            onCutsceneFinished?.Invoke();
        }

        void PlayEffect(AudioClip clip, float volume)
        {
            if (effectsSource != null && clip != null) effectsSource.PlayOneShot(clip, volume);
        }

        static void SetFrame(SpriteRenderer renderer, Sprite[] frames, int index)
        {
            if (renderer == null || frames == null || frames.Length == 0) return;
            renderer.sprite = frames[Mathf.Abs(index) % frames.Length];
        }

        static int FrameCount(Sprite[] frames) => frames != null && frames.Length > 0 ? frames.Length : 1;
        static bool InRange(float time, float start, float end) => time >= start && time < end;
        static float Smooth(float value) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(value));

        static void SetTextAlpha(Text text, float alpha)
        {
            if (text == null) return;
            text.enabled = alpha > 0.01f;
            Color c = text.color;
            c.a = alpha;
            text.color = c;
        }

        static Vector3 SamplePath(Vector3[] path, float progress)
        {
            if (path == null || path.Length == 0) return Vector3.zero;
            if (path.Length == 1) return path[0];
            float position = Mathf.Clamp01(progress) * (path.Length - 1);
            int index = Mathf.Min(Mathf.FloorToInt(position), path.Length - 2);
            float t = position - index;
            Vector3 p0 = path[Mathf.Max(index - 1, 0)];
            Vector3 p1 = path[index];
            Vector3 p2 = path[index + 1];
            Vector3 p3 = path[Mathf.Min(index + 2, path.Length - 1)];
            return 0.5f * ((2f * p1) + (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t * t * t);
        }
    }
}
