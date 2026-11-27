using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Misterio.Cutscenes
{
    [Serializable]
    public sealed class CC04ASubtitleCue
    {
        public float start;
        public float end;
        public string speaker;
        [TextArea] public string text;
        public AudioClip voice;
    }

    /// <summary>Four-second alert and retry beat for the third-floor artifact archive.</summary>
    public sealed class CC04ACutsceneController : MonoBehaviour
    {
        public const float Duration = 4f;
        const float RunStart = 1f;
        const float RunEnd = 2.82f;
        const float TurnStart = 0.84f;
        const float ResetAtBlack = 3.43f;
        const float FadeOutStart = 3.06f;
        const float FadeOutEnd = 3.38f;
        const float FadeInStart = 3.58f;
        const float FadeInEnd = 3.98f;
        const float MoemaPatrolStopEnd = 0.16f;
        const float AlertPoseEnd = 0.53f;
        const float SpriteFramesPerSecond = 12f;

        static readonly float[] TrotStepTimes = { 1.23f, 1.52f, 1.84f, 2.16f, 2.48f, 2.72f };

        public bool ControlsLocked { get; private set; }
        public float Elapsed => elapsed;
        public bool IsPausedForEditorReview => startPausedForEditorReview;

        [Header("Câmera e luz")]
        [SerializeField] Camera sceneCamera;
        [SerializeField] SpriteRenderer spotlightRenderer;
        [SerializeField] Transform spotlight;

        [Header("Segurança")]
        [SerializeField] Transform guard;
        [SerializeField] SpriteRenderer guardRenderer;
        [SerializeField] Sprite[] guardIdleFront;
        [SerializeField] SpriteRenderer guardShadow;

        [Header("Moema")]
        [SerializeField] Transform moema;
        [SerializeField] SpriteRenderer moemaRenderer;
        [SerializeField] Sprite[] moemaIdleFront;
        [SerializeField] Sprite[] moemaWalkFront;
        [SerializeField] Sprite[] moemaAlertFront;
        [SerializeField] Sprite[] moemaWalkBack;
        [SerializeField] SpriteRenderer moemaShadow;

        [Header("Acervo")]
        [SerializeField] SpriteRenderer plank;
        [SerializeField] Sprite plankResting;
        [SerializeField] Sprite plankPressed;
        [SerializeField] SpriteRenderer shards;
        [SerializeField] SpriteRenderer[] escapeDust;

        [Header("Interface")]
        [SerializeField] Text subtitleText;
        [SerializeField] Image subtitleBackground;
        [SerializeField] Image fadeImage;
        [SerializeField] CC04ASubtitleCue[] subtitles =
        {
            new CC04ASubtitleCue
            {
                start = 0.04f,
                end = 0.68f,
                speaker = "NARRADOR",
                text = "Um ruído chamou a atenção de Moema."
            },
            new CC04ASubtitleCue
            {
                start = 0.69f,
                end = 1.04f,
                speaker = "SEGURANÇA",
                text = "Ops."
            },
            new CC04ASubtitleCue
            {
                start = 1.12f,
                end = 2.86f,
                speaker = "NARRADOR",
                text = "A tentativa precisaria recomeçar."
            }
        };

        [Header("Som")]
        [SerializeField] AudioSource voiceSource;
        [SerializeField] AudioSource ambienceSource;
        [SerializeField] AudioSource effectsSource;
        [SerializeField] AudioSource musicSource;
        [SerializeField] AudioClip archiveAmbience;
        [SerializeField] AudioClip restrainedMusic;
        [SerializeField] AudioClip plankCreak;
        [SerializeField] AudioClip featherRustle;
        [SerializeField] AudioClip trotStep;
        [SerializeField] AudioClip xylophoneAccent;
        [SerializeField] AudioClip restartNote;

        [Header("Integração com o jogo")]
        [SerializeField] bool playOnAwake = true;
        [SerializeField] UnityEvent onCutsceneStarted = new UnityEvent();
        [SerializeField] UnityEvent onAttemptRestartRequested = new UnityEvent();
        [SerializeField] UnityEvent onCutsceneFinished = new UnityEvent();
        [SerializeField, HideInInspector] bool startPausedForEditorReview;

        readonly Vector3[] escapePath =
        {
            new Vector3(1.97f, -1.77f, -0.08f),
            new Vector3(2.35f, -1.63f, -0.08f),
            new Vector3(3.05f, -1.38f, -0.08f),
            new Vector3(4.00f, -1.12f, -0.08f),
            new Vector3(5.72f, -0.86f, -0.08f)
        };

        float elapsed;
        double lastRealtime;
        Vector3 cameraStart;
        float cameraStartSize;
        Vector3 guardStart;
        Vector3 guardScale;
        Vector3 moemaStart;
        Vector3 moemaScale;
        Vector3 spotlightStart;
        Vector3 plankStart;
        Vector3 plankScale;
        Vector3 guardShadowStart;
        Vector3 moemaShadowStart;
        int activeVoiceCueIndex = -1;
        int nextTrotStep;
        bool playing;
        bool finished;
        bool restartRequested;
        bool playedPlank;
        bool playedFeathers;
        bool playedXylophone;
        bool playedRestart;

        void Awake()
        {
            if (sceneCamera == null) sceneCamera = Camera.main;
            cameraStart = sceneCamera != null ? sceneCamera.transform.position : Vector3.zero;
            cameraStartSize = sceneCamera != null ? sceneCamera.orthographicSize : 2.35f;
            guardStart = guard != null ? guard.position : new Vector3(-2.7f, -0.72f, -0.08f);
            guardScale = guard != null ? guard.localScale : Vector3.one;
            moemaStart = moema != null ? moema.position : new Vector3(1.90f, -1.77f, -0.08f);
            moemaScale = moema != null ? moema.localScale : Vector3.one;
            spotlightStart = spotlight != null ? spotlight.position : new Vector3(-2.1f, -0.92f, -0.12f);
            plankStart = plank != null ? plank.transform.position : Vector3.zero;
            plankScale = plank != null ? plank.transform.localScale : Vector3.one;
            guardShadowStart = guardShadow != null ? guardShadow.transform.position : Vector3.zero;
            moemaShadowStart = moemaShadow != null ? moemaShadow.transform.position : Vector3.zero;

            ResetVisualsToPuzzleStart();
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

            UpdateCamera();
            UpdateGuard();
            UpdateMoema();
            UpdatePlank();
            UpdateSpotlight();
            UpdateShards();
            UpdateEscapeDust();
            UpdateSubtitleAndVoice();
            UpdateAudio();

            if (!restartRequested && elapsed >= ResetAtBlack) ResetAttemptAtBlack();
            if (fadeImage != null) UpdateFade();
            if (elapsed >= Duration) Finish();
        }

        /// <summary>Starts the alert when the archive puzzle detects Moema's reaction.</summary>
        public void PlayAlert()
        {
            if (playing && !finished) return;
            startPausedForEditorReview = false;
            Begin();
        }

        /// <summary>Editor preview entry point used by the frame-by-frame review capture.</summary>
        public void ResumeForEditorFrameReview()
        {
            if (!startPausedForEditorReview) return;
            startPausedForEditorReview = false;
            Begin();
        }

#if UNITY_EDITOR
        /// <summary>Freezes playback and stops sound before the editor scrubs exact review frames.</summary>
        public void PrepareEditorFrameReviewCapture()
        {
            playing = false;
            finished = false;
            elapsed = 0f;
            ControlsLocked = true;
            activeVoiceCueIndex = -1;
            if (voiceSource != null) voiceSource.Stop();
            if (ambienceSource != null) ambienceSource.Stop();
            if (effectsSource != null) effectsSource.Stop();
            if (musicSource != null) musicSource.Stop();
            ResetVisualsToPuzzleStart();
            SetEditorReviewTime(0f);
        }

        /// <summary>Evaluates the complete visual state at an exact timeline position, without advancing it.</summary>
        public void SetEditorReviewTime(float time)
        {
            elapsed = Mathf.Clamp(time, 0f, Duration);
            UpdateCamera();
            UpdateGuard();
            UpdateMoema();
            UpdatePlank();
            UpdateSpotlight();
            UpdateShards();
            UpdateEscapeDust();
            UpdateSubtitleAndVoice(false);
            if (fadeImage != null) UpdateFade();
        }
#endif

        void Begin()
        {
            elapsed = 0f;
            activeVoiceCueIndex = -1;
            playing = true;
            finished = false;
            restartRequested = false;
            playedPlank = false;
            playedFeathers = false;
            playedXylophone = false;
            playedRestart = false;
            nextTrotStep = 0;
            ControlsLocked = true;
            ResetVisualsToPuzzleStart();
            StartAudio();
            lastRealtime = Time.realtimeSinceStartupAsDouble;
            onCutsceneStarted?.Invoke();
        }

        void StartAudio()
        {
            if (ambienceSource != null && archiveAmbience != null)
            {
                ambienceSource.Stop();
                ambienceSource.clip = archiveAmbience;
                ambienceSource.loop = true;
                ambienceSource.volume = 0.075f;
                ambienceSource.Play();
            }
            if (musicSource != null && restrainedMusic != null)
            {
                musicSource.Stop();
                musicSource.clip = restrainedMusic;
                musicSource.loop = false;
                musicSource.volume = 0.075f;
                musicSource.Play();
            }
        }

        void ResetVisualsToPuzzleStart()
        {
            if (sceneCamera != null) sceneCamera.transform.position = cameraStart;
            if (guard != null)
            {
                guard.position = guardStart;
                guard.localScale = guardScale;
                guard.rotation = Quaternion.identity;
            }
            if (guardRenderer != null)
            {
                guardRenderer.enabled = true;
                guardRenderer.flipX = false;
                SetGuardFrame(0);
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
                moemaRenderer.flipX = true;
                SetMoemaFrame(moemaIdleFront, 0);
            }
            if (guardShadow != null)
            {
                guardShadow.transform.position = guardShadowStart;
                guardShadow.enabled = true;
            }
            if (moemaShadow != null)
            {
                moemaShadow.transform.position = moemaShadowStart;
                moemaShadow.enabled = true;
            }
            if (spotlight != null) spotlight.position = spotlightStart;
            if (spotlightRenderer != null)
            {
                spotlightRenderer.enabled = true;
                spotlightRenderer.color = new Color(0.88f, 0.91f, 1f, 0.62f);
            }
            if (plank != null)
            {
                plank.transform.position = plankStart;
                plank.transform.localScale = plankScale;
                plank.sprite = plankResting;
                plank.enabled = true;
            }
            if (shards != null)
            {
                shards.enabled = true;
                shards.color = new Color(0.76f, 0.88f, 1f, 0.72f);
            }
            if (escapeDust != null)
                foreach (SpriteRenderer mote in escapeDust)
                    if (mote != null) mote.enabled = false;
            if (subtitleText != null)
            {
                subtitleText.text = string.Empty;
                subtitleText.enabled = false;
            }
            if (subtitleBackground != null) subtitleBackground.enabled = false;
            if (fadeImage != null) fadeImage.color = Color.clear;
        }

        void UpdateCamera()
        {
            if (sceneCamera == null) return;
            float reframe = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.94f, 1.16f, elapsed));
            float returnToStart = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(3.40f, 3.58f, elapsed));
            float x = Mathf.Lerp(cameraStart.x, cameraStart.x + 0.40f, reframe);
            x = Mathf.Lerp(x, cameraStart.x, returnToStart);
            sceneCamera.transform.position = new Vector3(x, cameraStart.y, cameraStart.z);
            sceneCamera.orthographicSize = Mathf.Lerp(cameraStartSize, cameraStartSize + 0.20f, reframe);
            sceneCamera.orthographicSize = Mathf.Lerp(sceneCamera.orthographicSize, cameraStartSize, returnToStart);
        }

        void UpdateGuard()
        {
            if (guard == null || guardRenderer == null) return;
            float settle = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.02f, 0.13f, elapsed));
            guard.position = guardStart + Vector3.up * (0.012f * settle);
            guard.localScale = guardScale * (1f - 0.003f * settle);
            SetGuardFrame(Mathf.FloorToInt(elapsed * 4f) % FrameCount(guardIdleFront));
            if (guardShadow != null)
                guardShadow.transform.position = guardShadowStart + Vector3.up * (0.006f * settle);
        }

        void UpdateMoema()
        {
            if (moema == null || moemaRenderer == null) return;

            if (elapsed >= ResetAtBlack)
            {
                moema.gameObject.SetActive(true);
                moemaRenderer.enabled = true;
                moema.position = moemaStart;
                moema.localScale = moemaScale;
                moema.rotation = Quaternion.identity;
                moemaRenderer.flipX = true;
                SetMoemaFrame(moemaIdleFront, Mathf.FloorToInt(elapsed * 5f) % FrameCount(moemaIdleFront));
                UpdateMoemaShadow(moema.position, 1f);
                return;
            }

            if (elapsed < RunStart)
            {
                moema.gameObject.SetActive(true);
                moemaRenderer.enabled = true;
                moemaRenderer.flipX = true;
                if (elapsed < MoemaPatrolStopEnd)
                {
                    float stop = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, MoemaPatrolStopEnd, elapsed));
                    moema.position = moemaStart + Vector3.right * (0.035f * (1f - stop));
                    SetMoemaFrame(moemaWalkFront, Mathf.FloorToInt(elapsed * 24f) % FrameCount(moemaWalkFront));
                }
                else
                {
                    int alertFrame = Mathf.Clamp(Mathf.FloorToInt((elapsed - MoemaPatrolStopEnd) * 13f), 0, FrameCount(moemaAlertFront) - 1);
                    SetMoemaFrame(moemaAlertFront, alertFrame);
                    float turn = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(TurnStart, RunStart, elapsed));
                    float pivotSqueeze = Mathf.Sin(Mathf.PI * turn);
                    moema.position = moemaStart + Vector3.right * (0.070f * turn) +
                                     Vector3.up * (0.010f * pivotSqueeze);
                    moema.localScale = new Vector3(
                        moemaScale.x * (1f - 0.58f * pivotSqueeze),
                        moemaScale.y * (1f + 0.008f * pivotSqueeze),
                        moemaScale.z);
                    moema.rotation = Quaternion.Euler(0f, 0f, 0.65f * pivotSqueeze);
                    moemaRenderer.flipX = turn < 0.5f;
                }
                if (elapsed < TurnStart)
                {
                    moema.position = moemaStart;
                    moema.localScale = moemaScale;
                    moema.rotation = Quaternion.identity;
                    moemaRenderer.flipX = true;
                }
                UpdateMoemaShadow(moema.position, 1f);
                return;
            }

            if (elapsed >= RunEnd)
            {
                moema.gameObject.SetActive(false);
                if (moemaShadow != null) moemaShadow.enabled = false;
                return;
            }

            float rawProgress = Mathf.InverseLerp(RunStart, RunEnd, elapsed);
            float progress = Mathf.SmoothStep(0f, 1f, rawProgress);
            Vector3 position = SamplePath(escapePath, progress);
            float depthScale = Mathf.Lerp(1f, 0.76f, progress);
            float gait = Mathf.Repeat((elapsed - RunStart) * SpriteFramesPerSecond / FrameCount(moemaWalkBack), 1f);
            float bob = (1f - Mathf.Cos(gait * Mathf.PI * 2f)) * 0.010f;
            moema.position = position + Vector3.up * bob;
            moema.localScale = moemaScale * depthScale;
            moema.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-1.2f, 0.8f, progress));
            moemaRenderer.flipX = false;
            SetMoemaFrame(moemaWalkBack, Mathf.FloorToInt((elapsed - RunStart) * SpriteFramesPerSecond) % FrameCount(moemaWalkBack));
            moema.gameObject.SetActive(true);
            moemaRenderer.enabled = true;
            UpdateMoemaShadow(position, depthScale);
        }

        void UpdateMoemaShadow(Vector3 actorPosition, float size)
        {
            if (moemaShadow == null) return;
            moemaShadow.enabled = true;
            moemaShadow.transform.position = actorPosition + new Vector3(0f, -0.035f * size, 0.03f);
            moemaShadow.transform.localScale = new Vector3(1.32f * size, 1.08f * size, 1f);
            moemaShadow.color = new Color(0.025f, 0.03f, 0.045f, 0.44f * size);
        }

        void UpdatePlank()
        {
            if (plank == null) return;
            float press = Mathf.Sin(Mathf.PI * Mathf.Clamp01(elapsed / 0.15f));
            plank.sprite = elapsed < 0.15f ? plankPressed : plankResting;
            plank.transform.position = plankStart + Vector3.down * (0.008f * press);
            plank.transform.localScale = new Vector3(plankScale.x, plankScale.y * (1f - 0.03f * press), plankScale.z);
        }

        void UpdateSpotlight()
        {
            if (spotlightRenderer == null || spotlight == null) return;
            float x;
            if (elapsed < 1.05f)
            {
                float sweep = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 1.05f, elapsed));
                x = Mathf.Lerp(-2.15f, 1.95f, sweep);
            }
            else
            {
                x = 0.15f + 1.45f * Mathf.Sin((elapsed - 1.05f) * Mathf.PI * 0.72f);
            }
            spotlight.position = new Vector3(x, spotlightStart.y, spotlightStart.z);
            float cycle = 0.5f + 0.5f * Mathf.Sin(elapsed * Mathf.PI * 1.16f + 0.4f);
            float alpha = Mathf.Lerp(0.48f, 0.68f, cycle);
            if (elapsed < 1f) alpha *= Mathf.Lerp(0.84f, 1f, Mathf.SmoothStep(0f, 1f, elapsed));
            spotlightRenderer.color = new Color(0.90f, 0.93f, 1f, alpha);
        }

        void UpdateShards()
        {
            if (shards == null) return;
            float glint = 0.5f + 0.5f * Mathf.Sin(elapsed * 4.2f + 0.7f);
            shards.color = new Color(0.76f, 0.88f, 1f, Mathf.Lerp(0.52f, 0.76f, glint));
        }

        void UpdateEscapeDust()
        {
            if (escapeDust == null) return;
            for (int step = 0; step < TrotStepTimes.Length; step++)
            {
                float age = elapsed - TrotStepTimes[step];
                Vector3 foot = SampleEscapeAtTime(TrotStepTimes[step]) + Vector3.down * 0.05f;
                for (int part = 0; part < 2; part++)
                {
                    int index = step * 2 + part;
                    if (index >= escapeDust.Length || escapeDust[index] == null) continue;
                    SpriteRenderer mote = escapeDust[index];
                    bool visible = age >= 0f && age < 0.22f && elapsed < RunEnd;
                    mote.enabled = visible;
                    if (!visible) continue;
                    float progress = Mathf.Clamp01(age / 0.22f);
                    float side = part == 0 ? -1f : 1f;
                    mote.transform.position = foot + new Vector3(-0.025f + side * progress * 0.045f,
                        progress * (0.045f + part * 0.020f), -0.04f);
                    float alpha = (1f - progress) * (part == 0 ? 0.46f : 0.32f);
                    mote.color = new Color(0.70f, 0.75f, 0.80f, alpha);
                    float moteScale = Mathf.Lerp(0.54f, 0.9f, progress);
                    mote.transform.localScale = new Vector3(moteScale, moteScale, 1f);
                }
            }
        }

        Vector3 SampleEscapeAtTime(float time)
        {
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(RunStart, RunEnd, time));
            return SamplePath(escapePath, progress);
        }

        void UpdateSubtitleAndVoice(bool playVoice = true)
        {
            CC04ASubtitleCue active = null;
            int activeIndex = -1;
            if (subtitles != null)
            {
                for (int i = 0; i < subtitles.Length; i++)
                {
                    CC04ASubtitleCue cue = subtitles[i];
                    if (cue == null || elapsed < cue.start || elapsed >= cue.end) continue;
                    active = cue;
                    activeIndex = i;
                    break;
                }
            }

            bool visible = active != null;
            if (subtitleText != null)
            {
                subtitleText.enabled = visible;
                if (activeIndex != activeVoiceCueIndex)
                    subtitleText.text = visible ? active.speaker + "\n" + active.text : string.Empty;
            }
            if (subtitleBackground != null) subtitleBackground.enabled = visible;

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
            if (!playedPlank && elapsed >= 0.025f)
            {
                playedPlank = true;
                PlayEffect(plankCreak, 0.68f);
            }
            if (!playedFeathers && elapsed >= 1.06f)
            {
                playedFeathers = true;
                PlayEffect(featherRustle, 0.30f);
            }
            if (!playedXylophone && elapsed >= 1.92f)
            {
                playedXylophone = true;
                PlayEffect(xylophoneAccent, 0.20f);
            }
            if (!playedRestart && elapsed >= 3.58f)
            {
                playedRestart = true;
                PlayEffect(restartNote, 0.24f);
            }
            while (nextTrotStep < TrotStepTimes.Length && elapsed >= TrotStepTimes[nextTrotStep])
            {
                PlayEffect(trotStep, 0.32f);
                nextTrotStep++;
            }

            if (musicSource != null && musicSource.isPlaying)
            {
                float restOut = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.22f, 0.34f, elapsed));
                float restIn = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.62f, 0.82f, elapsed));
                float duck = Mathf.Lerp(1f, 0f, restOut) * Mathf.Lerp(0f, 1f, restIn);
                float fade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(3.02f, 3.34f, elapsed));
                musicSource.volume = 0.075f * duck * fade;
            }
            if (ambienceSource != null && ambienceSource.isPlaying)
            {
                float fadeOut = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(3.05f, 3.38f, elapsed));
                float fadeIn = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(3.58f, 3.92f, elapsed));
                float ambienceLevel = elapsed < 3.05f ? 0.075f : 0.075f * Mathf.Lerp(fadeOut, fadeIn, Mathf.InverseLerp(3.38f, 3.58f, elapsed));
                ambienceSource.volume = ambienceLevel;
            }
        }

        void UpdateFade()
        {
            float alpha = 0f;
            if (elapsed >= FadeOutStart && elapsed < FadeOutEnd)
                alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(FadeOutStart, FadeOutEnd, elapsed));
            else if (elapsed >= FadeOutEnd && elapsed < FadeInStart)
                alpha = 1f;
            else if (elapsed >= FadeInStart)
                alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(FadeInStart, FadeInEnd, elapsed));
            fadeImage.color = new Color(0f, 0f, 0f, alpha);
        }

        void ResetAttemptAtBlack()
        {
            restartRequested = true;
            if (guard != null) guard.position = guardStart;
            if (guardRenderer != null) SetGuardFrame(0);
            if (plank != null)
            {
                plank.sprite = plankResting;
                plank.transform.position = plankStart;
                plank.transform.localScale = plankScale;
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
                moemaRenderer.flipX = true;
                SetMoemaFrame(moemaIdleFront, 0);
            }
            if (escapeDust != null)
                foreach (SpriteRenderer mote in escapeDust)
                    if (mote != null) mote.enabled = false;
            if (sceneCamera != null) sceneCamera.transform.position = cameraStart;
            if (moemaShadow != null) moemaShadow.transform.position = moemaShadowStart;
            onAttemptRestartRequested?.Invoke();
        }

        void Finish()
        {
            if (finished) return;
            finished = true;
            playing = false;
            elapsed = Duration;
            ControlsLocked = false;
            if (voiceSource != null) voiceSource.Stop();
            if (musicSource != null) musicSource.Stop();
            onCutsceneFinished?.Invoke();
        }

        void PlayEffect(AudioClip clip, float volume)
        {
            if (effectsSource != null && clip != null) effectsSource.PlayOneShot(clip, volume);
        }

        void SetGuardFrame(int index)
        {
            if (guardRenderer != null && guardIdleFront != null && guardIdleFront.Length > 0)
                guardRenderer.sprite = guardIdleFront[Mathf.Clamp(index, 0, guardIdleFront.Length - 1)];
        }

        void SetMoemaFrame(Sprite[] frames, int index)
        {
            if (moemaRenderer != null && frames != null && frames.Length > 0)
                moemaRenderer.sprite = frames[Mathf.Clamp(index, 0, frames.Length - 1)];
        }

        static int FrameCount(Sprite[] frames) => frames != null && frames.Length > 0 ? frames.Length : 1;

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
