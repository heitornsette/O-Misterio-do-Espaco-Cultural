using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Misterio.Cutscenes
{
    [Serializable]
    public sealed class CC04SubtitleCue
    {
        public float start;
        public float end;
        public string speaker;
        [TextArea] public string text;
        public AudioClip voice;
    }

    /// <summary>Nine-second quiet passage through the artifact archive to the service elevator.</summary>
    public sealed class CC04CutsceneController : MonoBehaviour
    {
        public const float Duration = 9f;
        const float PickupEnd = 1.22f;
        const float RetreatStart = 1.25f;
        const float RetreatEnd = 2.95f;
        const float PassageStart = 3.00f;
        const float HushStart = 4.32f;
        const float HushEnd = 4.86f;
        const float ElevatorEntry = 5.90f;
        const float DoorCloseStart = 6.00f;
        const float DoorCloseEnd = 8.72f;
        const float WalkFramesPerSecond = 16f;

        public bool ControlsLocked { get; private set; } = true;
        public float Elapsed => elapsed;
        public bool IsPausedForEditorReview => startPausedForEditorReview;

        [Header("Câmera e cenário")]
        [SerializeField] Camera sceneCamera;
        [SerializeField] Transform spotlightPool;
        [SerializeField] SpriteRenderer spotlightRenderer;
        [SerializeField] SpriteRenderer shardGlint;
        [SerializeField] SpriteRenderer nearbyObject;

        [Header("Segurança")]
        [SerializeField] Transform guard;
        [SerializeField] SpriteRenderer guardRenderer;
        [SerializeField] Sprite[] guardIdleBack;
        [SerializeField] Sprite[] guardIdleFront;
        [SerializeField] Sprite[] guardWalkBack;
        [SerializeField] SpriteRenderer guardShadow;
        [SerializeField] SpriteRenderer hushFinger;
        [SerializeField] SpriteRenderer archivePiece;

        [Header("Moema")]
        [SerializeField] Transform moema;
        [SerializeField] SpriteRenderer moemaRenderer;
        [SerializeField] SpriteRenderer moemaShadow;

        [Header("Elevador")]
        [SerializeField] RectTransform leftDoor;
        [SerializeField] RectTransform rightDoor;
        [SerializeField] Image leftDoorReflection;
        [SerializeField] Image rightDoorReflection;
        [SerializeField] Image elevatorIndicator;
        [SerializeField] Image indicatorGlow;

        [Header("Interface")]
        [SerializeField] Text subtitleText;
        [SerializeField] Image subtitleBackground;
        [SerializeField] Image vignetteImage;
        [SerializeField] Image fadeImage;
        [SerializeField] CC04SubtitleCue[] subtitles =
        {
            new CC04SubtitleCue
            {
                start = 0.30f,
                end = 2.82f,
                speaker = "NARRADOR",
                text = "No acervo, cada passo podia chamar atenção."
            },
            new CC04SubtitleCue
            {
                start = 4.12f,
                end = 5.72f,
                speaker = "SEGURANÇA",
                text = "Devagar... não vamos assustá-la."
            },
            new CC04SubtitleCue
            {
                start = 6.68f,
                end = 8.86f,
                speaker = "NARRADOR",
                text = "Moema continuou entretida. O segurança pôde seguir."
            }
        };

        [Header("Som")]
        [SerializeField] AudioSource voiceSource;
        [SerializeField] AudioSource ambientSource;
        [SerializeField] AudioSource effectsSource;
        [SerializeField] AudioSource musicSource;
        [SerializeField] AudioClip archiveNight;
        [SerializeField] AudioClip quietPhrase;
        [SerializeField] AudioClip clothAndBreath;
        [SerializeField] AudioClip woodCreak;
        [SerializeField] AudioClip softStep;
        [SerializeField] AudioClip elevatorMotor;

        [Header("Integração")]
        [SerializeField] UnityEvent onCutsceneFinished = new UnityEvent();
        [SerializeField, HideInInspector] bool startPausedForEditorReview;

        readonly Vector3[] retreatPath =
        {
            new Vector3(-0.72f, -0.44f, -0.08f),
            new Vector3(-0.86f, -0.44f, -0.08f),
            new Vector3(-0.91f, -0.42f, -0.08f),
            new Vector3(-0.80f, -0.43f, -0.08f)
        };

        readonly Vector3[] passagePath =
        {
            new Vector3(-0.80f, -0.43f, -0.08f),
            new Vector3(-0.58f, -0.44f, -0.08f),
            new Vector3(-0.36f, -0.44f, -0.08f),
            new Vector3(-0.08f, -0.45f, -0.08f),
            new Vector3(0.30f, -0.46f, -0.08f),
            new Vector3(0.62f, -0.45f, -0.08f),
            new Vector3(1.13f, -0.36f, -0.08f)
        };

        static readonly float[] FootstepTimes = { 1.65f, 2.34f, 3.24f, 3.78f, 4.08f, 5.11f, 5.55f };

        float elapsed;
        double lastRealtime;
        Vector3 guardStart;
        Vector3 moemaStart;
        Vector3 moemaScale;
        Vector3 guardScale;
        Vector3 pieceStart;
        Vector3 nearbyStart;
        int nextFootstep;
        int activeVoiceCueIndex = -1;
        bool finished;
        bool skipFirstReviewUpdate;
        bool playedCloth;
        bool playedCreak;
        bool playedMotor;

        void Awake()
        {
            if (sceneCamera == null) sceneCamera = Camera.main;
            guardStart = guard != null ? guard.position : new Vector3(-0.72f, -0.44f, -0.08f);
            guardScale = guard != null ? guard.localScale : Vector3.one;
            moemaStart = moema != null ? moema.position : new Vector3(-0.38f, -0.34f, -0.08f);
            moemaScale = moema != null ? moema.localScale : Vector3.one;
            pieceStart = archivePiece != null ? archivePiece.transform.position : Vector3.zero;
            nearbyStart = nearbyObject != null ? nearbyObject.transform.position : Vector3.zero;

            ControlsLocked = true;
            lastRealtime = Time.realtimeSinceStartupAsDouble;
            if (subtitleText != null)
            {
                subtitleText.text = string.Empty;
                subtitleText.enabled = false;
            }
            if (subtitleBackground != null) subtitleBackground.enabled = false;
            if (hushFinger != null) hushFinger.enabled = false;
            if (shardGlint != null) shardGlint.enabled = true;
            if (archivePiece != null) archivePiece.enabled = true;
            if (leftDoor != null) leftDoor.gameObject.SetActive(false);
            if (rightDoor != null) rightDoor.gameObject.SetActive(false);
            if (leftDoorReflection != null) leftDoorReflection.enabled = false;
            if (rightDoorReflection != null) rightDoorReflection.enabled = false;
            if (elevatorIndicator != null) elevatorIndicator.enabled = false;
            if (indicatorGlow != null) indicatorGlow.enabled = false;
            if (fadeImage != null) fadeImage.color = Color.black;

            if (!startPausedForEditorReview) StartAudio();
        }

        void Update()
        {
            if (finished) return;

            double now = Time.realtimeSinceStartupAsDouble;
            if (startPausedForEditorReview)
            {
                lastRealtime = now;
                return;
            }
            if (skipFirstReviewUpdate)
            {
                skipFirstReviewUpdate = false;
                lastRealtime = now;
                elapsed = 0f;
                UpdateCamera();
                UpdateGuard();
                UpdateMoema();
                UpdateLightAndDetails();
                UpdateElevatorDoors();
                UpdateSubtitleAndVoice();
                return;
            }
            elapsed = Mathf.Min(Duration, elapsed + (float)Math.Max(0.0, now - lastRealtime));
            lastRealtime = now;

            UpdateCamera();
            UpdateGuard();
            UpdateMoema();
            UpdateLightAndDetails();
            UpdateElevatorDoors();
            UpdateSubtitleAndVoice();
            UpdateAudio();
            if (elapsed >= Duration) Finish();
        }

        public void ResumeForEditorFrameReview()
        {
            if (!startPausedForEditorReview) return;
            startPausedForEditorReview = false;
            elapsed = 0f;
            nextFootstep = 0;
            activeVoiceCueIndex = -1;
            finished = false;
            playedCloth = false;
            playedCreak = false;
            playedMotor = false;
            ControlsLocked = true;
            StartAudio();
            lastRealtime = Time.realtimeSinceStartupAsDouble;
            skipFirstReviewUpdate = true;
        }

        void StartAudio()
        {
            if (ambientSource != null && archiveNight != null)
            {
                ambientSource.clip = archiveNight;
                ambientSource.loop = true;
                ambientSource.volume = 0.18f;
                ambientSource.Play();
            }
            if (musicSource != null && quietPhrase != null)
            {
                musicSource.clip = quietPhrase;
                musicSource.loop = false;
                musicSource.volume = 0.10f;
                musicSource.Play();
            }
        }

        void UpdateCamera()
        {
            if (sceneCamera == null) return;

            Vector3 close = new Vector3(-0.56f, 0.02f, -10f);
            Vector3 archiveWide = new Vector3(0.20f, 0.01f, -10f);
            Vector3 exitView = new Vector3(-0.38f, 0.02f, -10f);
            Vector3 target;
            float targetSize;

            if (elapsed < 3.0f)
            {
                target = close;
                targetSize = Mathf.Lerp(0.76f, 0.82f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 2.8f, elapsed)));
            }
            else if (elapsed < 3.72f)
            {
                float pan = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(3.0f, 3.72f, elapsed));
                target = Vector3.Lerp(close, archiveWide, pan);
                targetSize = Mathf.Lerp(0.82f, 1f, pan);
            }
            else if (elapsed < 5.84f)
            {
                target = archiveWide;
                targetSize = 1f;
            }
            else
            {
                float reframe = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(5.84f, 6.16f, elapsed));
                target = Vector3.Lerp(archiveWide, exitView, reframe);
                targetSize = Mathf.Lerp(1f, 0.78f, reframe);
            }

            sceneCamera.transform.position = target;
            sceneCamera.orthographicSize = targetSize;
            if (vignetteImage != null)
            {
                float alpha = elapsed >= 6f ? 0.30f : 0.23f;
                vignetteImage.color = new Color(1f, 1f, 1f, alpha);
            }
            if (fadeImage != null)
            {
                float opening = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.02f, 0.27f, elapsed));
                fadeImage.color = new Color(0f, 0f, 0f, opening);
            }
        }

        void UpdateGuard()
        {
            if (guard == null || guardRenderer == null) return;

            bool enteredElevator = elapsed >= ElevatorEntry;
            guard.gameObject.SetActive(!enteredElevator);
            if (enteredElevator)
            {
                if (hushFinger != null) hushFinger.enabled = false;
                return;
            }

            bool pickingUp = elapsed < PickupEnd;
            bool retreating = elapsed >= RetreatStart && elapsed < RetreatEnd;
            bool crossing = elapsed >= PassageStart && elapsed < ElevatorEntry && !(elapsed >= HushStart && elapsed < HushEnd);
            bool hush = elapsed >= HushStart && elapsed < HushEnd;
            bool walking = retreating || crossing;

            Vector3 pos;
            if (pickingUp)
            {
                float crouch = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.18f, 0.58f, elapsed)) *
                               (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.82f, 1.18f, elapsed)));
                pos = guardStart + new Vector3(0f, -0.032f * crouch, 0f);
                guard.localScale = new Vector3(guardScale.x * (1f - 0.012f * crouch), guardScale.y * (1f - 0.075f * crouch), guardScale.z);
                SetFrame(guardIdleBack, Mathf.FloorToInt(elapsed * 4f) % FrameCount(guardIdleBack));
            }
            else if (retreating)
            {
                float progress = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(RetreatStart, RetreatEnd, elapsed));
                pos = SamplePath(retreatPath, progress);
                AnimateWalking(pos, elapsed - RetreatStart, guardWalkBack);
            }
            else if (hush)
            {
                pos = SamplePath(passagePath, 0.512f);
                guard.localScale = guardScale;
                SetFrame(guardIdleFront, Mathf.Clamp(Mathf.FloorToInt((elapsed - HushStart) * 3f), 0, FrameCount(guardIdleFront) - 1));
            }
            else if (crossing)
            {
                float pathProgress;
                if (elapsed < HushStart)
                    pathProgress = Mathf.Lerp(0f, 0.512f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(PassageStart, HushStart, elapsed)));
                else
                    pathProgress = Mathf.Lerp(0.512f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(HushEnd, ElevatorEntry, elapsed)));
                pos = SamplePath(passagePath, pathProgress);
                AnimateWalking(pos, elapsed - PassageStart, guardWalkBack);
            }
            else
            {
                pos = retreatPath[retreatPath.Length - 1];
                guard.localScale = guardScale;
                SetFrame(guardIdleBack, 0);
            }

            if (!walking) guard.position = pos;
            guard.rotation = Quaternion.identity;
            guardRenderer.flipX = false;
            if (guardShadow != null)
            {
                bool behindMoema = Mathf.Abs(pos.x - moemaStart.x) < 0.28f;
                guardRenderer.sortingOrder = behindMoema ? 7 : 9;
                guardShadow.transform.position = pos + new Vector3(0.035f, -0.006f, 0.015f);
                guardShadow.transform.localScale = new Vector3(0.25f, 0.055f, 1f);
                guardShadow.color = new Color(0.006f, 0.010f, 0.023f, hush ? 0.28f : 0.37f);
                guardShadow.sortingOrder = behindMoema ? 5 : 7;
                guardShadow.enabled = true;
            }
            if (hushFinger != null)
            {
                hushFinger.enabled = hush;
                hushFinger.transform.position = pos + new Vector3(0.035f, 0.30f, -0.12f);
            }
        }

        void AnimateWalking(Vector3 pos, float localTime, Sprite[] frames)
        {
            float gait = Mathf.Repeat(localTime * 2f, 1f);
            float bob = (1f - Mathf.Cos(gait * Mathf.PI * 2f)) * 0.004f;
            guard.localScale = new Vector3(guardScale.x, guardScale.y * (1f - 0.015f * (1f - Mathf.Cos(gait * Mathf.PI * 2f)) * 0.5f), guardScale.z);
            guard.position = pos + Vector3.up * bob;
            SetFrame(frames, Mathf.FloorToInt(localTime * WalkFramesPerSecond) % FrameCount(frames));
        }

        void UpdateMoema()
        {
            if (moema == null || moemaRenderer == null) return;

            float peck = Mathf.Max(
                PeckPulse(elapsed, 0.72f, 0.98f),
                PeckPulse(elapsed, 1.62f, 1.91f),
                PeckPulse(elapsed, 2.36f, 2.66f));
            Vector3 peckOffset = new Vector3(0.018f * peck, -0.005f * peck, 0f);
            moema.position = moemaStart + peckOffset;
            moema.localScale = moemaScale * (1f - 0.012f * peck);
            moema.rotation = Quaternion.Euler(0f, 0f, -2.2f * peck);
            if (moemaShadow != null)
            {
                moemaShadow.transform.position = moemaStart + new Vector3(0.10f, -0.005f, 0.015f);
                moemaShadow.enabled = true;
            }
            if (nearbyObject != null)
            {
                nearbyObject.transform.position = nearbyStart + new Vector3(0.002f * peck, -0.002f * peck, 0f);
                float alpha = Mathf.Lerp(0.88f, 1f, peck);
                nearbyObject.color = new Color(1f, 1f, 1f, alpha);
            }
        }

        void UpdateLightAndDetails()
        {
            float cycle = 0.5f + 0.5f * Mathf.Sin((elapsed + 0.12f) * Mathf.PI * 1.22f);
            float beamAlpha = Mathf.Lerp(0.11f, 0.27f, cycle);
            if (spotlightRenderer != null)
            {
                spotlightRenderer.enabled = true;
                spotlightRenderer.color = new Color(0.62f, 0.76f, 1f, beamAlpha);
            }
            if (spotlightPool != null)
            {
                spotlightPool.position = new Vector3(-0.04f + 0.05f * Mathf.Sin(elapsed * 0.82f), -0.24f, 0.04f);
            }
            if (shardGlint != null)
            {
                float glint = Mathf.Max(0f, Mathf.Sin(elapsed * 5.4f + 0.35f));
                shardGlint.color = new Color(0.74f, 0.88f, 1f, 0.10f + glint * 0.34f);
                shardGlint.transform.localScale = new Vector3(0.065f, 0.065f, 1f) * (0.86f + 0.14f * glint);
            }

            if (archivePiece != null)
            {
                float pickup = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.42f, 1.02f, elapsed));
                Vector3 hand = guardStart + new Vector3(0.055f, 0.18f, -0.01f);
                archivePiece.transform.position = Vector3.Lerp(pieceStart, hand, pickup);
                Color color = archivePiece.color;
                color.a = elapsed >= 1.05f ? 0f : 1f - pickup;
                archivePiece.color = color;
                archivePiece.enabled = color.a > 0.015f;
            }
        }

        void UpdateElevatorDoors()
        {
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(DoorCloseStart, DoorCloseEnd, elapsed));
            if (leftDoor != null && rightDoor != null)
            {
                bool visible = elapsed >= DoorCloseStart;
                if (leftDoor.gameObject.activeSelf != visible) leftDoor.gameObject.SetActive(visible);
                if (rightDoor.gameObject.activeSelf != visible) rightDoor.gameObject.SetActive(visible);
                leftDoor.anchoredPosition = new Vector2(Mathf.Lerp(-310f, -118.5f, progress), 0f);
                rightDoor.anchoredPosition = new Vector2(Mathf.Lerp(310f, 118.5f, progress), 0f);
            }

            float lightAlpha = elapsed >= DoorCloseStart
                ? Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(6.02f, 6.34f, elapsed)) * (0.88f + 0.12f * Mathf.Sin(elapsed * 3.3f))
                : 0f;
            if (elevatorIndicator != null)
            {
                elevatorIndicator.enabled = elapsed >= DoorCloseStart;
                elevatorIndicator.color = new Color(0.76f, 0.91f, 1f, lightAlpha);
            }
            if (indicatorGlow != null)
            {
                indicatorGlow.enabled = elapsed >= DoorCloseStart;
                indicatorGlow.color = new Color(0.34f, 0.68f, 1f, 0.38f * lightAlpha);
            }
            float reflectionAlpha = 0.32f * progress * lightAlpha;
            if (leftDoorReflection != null)
            {
                leftDoorReflection.enabled = elapsed >= DoorCloseStart;
                leftDoorReflection.color = new Color(0.55f, 0.78f, 1f, reflectionAlpha);
                leftDoorReflection.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(-205f, -13.5f, progress), -2f);
            }
            if (rightDoorReflection != null)
            {
                rightDoorReflection.enabled = elapsed >= DoorCloseStart;
                rightDoorReflection.color = new Color(0.55f, 0.78f, 1f, reflectionAlpha);
                rightDoorReflection.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(205f, 13.5f, progress), -2f);
            }
        }

        void UpdateSubtitleAndVoice()
        {
            CC04SubtitleCue active = null;
            int activeIndex = -1;
            if (subtitles != null)
            {
                for (int i = 0; i < subtitles.Length; i++)
                {
                    CC04SubtitleCue cue = subtitles[i];
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
                if (active != null && active.voice != null && voiceSource != null)
                {
                    voiceSource.Stop();
                    voiceSource.clip = active.voice;
                    voiceSource.Play();
                }
            }
        }

        void UpdateAudio()
        {
            if (!playedCloth && elapsed >= 0.24f)
            {
                playedCloth = true;
                PlayEffect(clothAndBreath, 0.24f);
            }
            if (!playedCreak && elapsed >= 2.06f)
            {
                playedCreak = true;
                PlayEffect(woodCreak, 0.18f);
            }
            if (!playedMotor && elapsed >= 5.94f)
            {
                playedMotor = true;
                PlayEffect(elevatorMotor, 0.20f);
            }
            while (nextFootstep < FootstepTimes.Length && elapsed >= FootstepTimes[nextFootstep])
            {
                PlayEffect(softStep, 0.18f);
                nextFootstep++;
            }

            if (musicSource != null && musicSource.isPlaying)
            {
                float muffled = Mathf.Lerp(0.10f, 0.035f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(5.85f, 8.8f, elapsed)));
                musicSource.volume = muffled;
                musicSource.pitch = Mathf.Lerp(1f, 0.90f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(6f, 8.9f, elapsed)));
            }
        }

        void PlayEffect(AudioClip clip, float volume)
        {
            if (effectsSource != null && clip != null) effectsSource.PlayOneShot(clip, volume);
        }

        void Finish()
        {
            if (finished) return;
            finished = true;
            elapsed = Duration;
            ControlsLocked = false;
            if (voiceSource != null) voiceSource.Stop();
            if (ambientSource != null) ambientSource.Stop();
            if (musicSource != null) musicSource.Stop();
            onCutsceneFinished?.Invoke();
        }

        static float PeckPulse(float time, float start, float end)
        {
            if (time < start || time >= end) return 0f;
            float p = Mathf.InverseLerp(start, end, time);
            return Mathf.Sin(p * Mathf.PI);
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
            return 0.5f * ((2f * p1) + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t + (-p0 + 3f * p1 - 3f * p2 + p3) * t * t * t);
        }

        void SetFrame(Sprite[] frames, int index)
        {
            if (guardRenderer != null && frames != null && frames.Length > 0)
                guardRenderer.sprite = frames[Mathf.Clamp(index, 0, frames.Length - 1)];
        }

        static int FrameCount(Sprite[] frames) => frames != null && frames.Length > 0 ? frames.Length : 1;
    }
}
