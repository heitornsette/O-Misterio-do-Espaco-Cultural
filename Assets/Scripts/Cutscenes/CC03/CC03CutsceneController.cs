using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Misterio.Cutscenes
{
    [Serializable]
    public sealed class CC03SubtitleCue
    {
        public float start;
        public float end;
        public string speaker;
        [TextArea] public string text;
        public AudioClip voice;
    }

    /// <summary>Nine-second transition from the completed painting gallery to the artifact archive.</summary>
    public sealed class CC03CutsceneController : MonoBehaviour
    {
        public const float Duration = 9f;
        const float PaintingSettleStart = 0.25f;
        const float PaintingSettleEnd = 1.18f;
        const float WalkStart = 4.82f;
        const float WalkEnd = 6.24f;
        const float ArchiveCut = 6.48f;
        const float WalkFramesPerSecond = 16f;

        public bool ControlsLocked { get; private set; } = true;
        public float Elapsed => elapsed;

        [Header("Scene")]
        [SerializeField] Camera sceneCamera;
        [SerializeField] Vector3 galleryWidePosition = new Vector3(0f, 0.02f, -10f);
        [SerializeField] Vector3 archiveCameraPosition = new Vector3(0f, 0f, -10f);
        [SerializeField] GameObject galleryRoot;
        [SerializeField] GameObject archiveRoot;
        [SerializeField] SpriteRenderer finalPainting;
        [SerializeField] Transform finalPaintingTarget;
        [SerializeField] SpriteRenderer guardRenderer;
        [SerializeField] Transform guard;
        [SerializeField] Sprite[] guardIdleBack;
        [SerializeField] Sprite[] guardWalkBack;
        [SerializeField] Text[] plaqueLabels;
        [SerializeField] Transform[] plaqueAnchors;
        [SerializeField] SpriteRenderer moonbeamRenderer;
        [SerializeField] SpriteRenderer archiveShadow;
        [SerializeField] SpriteRenderer archiveSpotlightFlash;

        [Header("UI")]
        [SerializeField] Text subtitleText;
        [SerializeField] Image subtitleBackground;
        [SerializeField] Image galleryDimImage;
        [SerializeField] Image vignetteImage;
        [SerializeField] Image fadeImage;
        [SerializeField] CC03SubtitleCue[] subtitles =
        {
            new CC03SubtitleCue
            {
                start = 2.95f,
                end = 4.90f,
                speaker = "NARRADOR",
                text = "Com os quadros de volta às paredes, o segurança seguiu para o acervo."
            },
            new CC03SubtitleCue
            {
                start = 4.98f,
                end = 6.32f,
                speaker = "SEGURANÇA",
                text = "Tudo certo por aqui. Vamos ao próximo andar."
            },
            new CC03SubtitleCue
            {
                start = 6.72f,
                end = 8.62f,
                speaker = "NARRADOR",
                text = "Desta vez, seria preciso andar em silêncio."
            }
        };

        [Header("Audio")]
        [SerializeField] AudioSource voiceSource;
        [SerializeField] AudioSource ambientSource;
        [SerializeField] AudioSource effectsSource;
        [SerializeField] AudioSource musicSource;
        [SerializeField] AudioClip galleryNight;
        [SerializeField] AudioClip quietGalleryPhrase;
        [SerializeField] AudioClip magneticClick;
        [SerializeField] AudioClip softStep;
        [SerializeField] AudioClip archiveDoor;
        [SerializeField] AudioClip distantWoodTap;

        [Header("Integration")]
        [SerializeField] UnityEvent onCutsceneFinished = new UnityEvent();

        float elapsed;
        double lastRealtime;
        Vector3 cameraStart;
        Vector3 paintingStart;
        Quaternion paintingStartRotation;
        Vector3 guardStart;
        Vector3[] guardPathSamples;
        float[] guardPathDistances;
        float guardPathLength;
        int lastFootstepFrame = -1;
        bool finished;
        bool playedMagneticClick;
        bool playedDoor;
        bool playedWoodTap;
        CC03SubtitleCue activeVoiceCue;

        void Awake()
        {
            if (sceneCamera == null) sceneCamera = Camera.main;
            cameraStart = sceneCamera != null ? sceneCamera.transform.position : Vector3.zero;
            paintingStart = finalPainting != null ? finalPainting.transform.position : Vector3.zero;
            paintingStartRotation = finalPainting != null ? finalPainting.transform.rotation : Quaternion.identity;
            guardStart = guard != null ? guard.position : Vector3.zero;
            BuildGuardPath();

            if (galleryRoot != null) galleryRoot.SetActive(true);
            if (archiveRoot != null) archiveRoot.SetActive(false);
            if (subtitleText != null)
            {
                subtitleText.text = string.Empty;
                subtitleText.enabled = false;
            }
            if (subtitleBackground != null) subtitleBackground.enabled = false;
            if (galleryDimImage != null) galleryDimImage.color = new Color(0f, 0f, 0f, 0f);
            if (archiveShadow != null) archiveShadow.enabled = false;
            if (archiveSpotlightFlash != null) archiveSpotlightFlash.enabled = false;
            if (fadeImage != null) fadeImage.color = Color.black;

            ControlsLocked = true;
            lastRealtime = Time.realtimeSinceStartupAsDouble;
            if (ambientSource != null && galleryNight != null)
            {
                ambientSource.clip = galleryNight;
                ambientSource.loop = true;
                ambientSource.volume = 0.22f;
                ambientSource.Play();
            }
            if (musicSource != null && quietGalleryPhrase != null)
            {
                musicSource.clip = quietGalleryPhrase;
                musicSource.loop = false;
                musicSource.volume = 0.12f;
                musicSource.Play();
            }
        }

        void Update()
        {
            if (finished) return;

            double now = Time.realtimeSinceStartupAsDouble;
            elapsed = Mathf.Min(Duration, elapsed + (float)Math.Min(0.05, Math.Max(0.0, now - lastRealtime)));
            lastRealtime = now;

            UpdateShotAndCamera();
            UpdatePainting();
            UpdateGuard();
            UpdatePlaqueLabels();
            UpdateMoonbeam();
            UpdateArchiveShadowAndLight();
            UpdateSubtitleAndVoice();
            UpdateSoundCues();
            UpdateAudioMix();
            if (elapsed >= Duration) Finish();
        }

        void UpdateShotAndCamera()
        {
            bool archiveShot = elapsed >= ArchiveCut;
            if (galleryRoot != null && galleryRoot.activeSelf == archiveShot) galleryRoot.SetActive(!archiveShot);
            if (archiveRoot != null && archiveRoot.activeSelf != archiveShot) archiveRoot.SetActive(archiveShot);

            if (sceneCamera != null)
            {
                if (archiveShot)
                {
                    sceneCamera.transform.position = archiveCameraPosition;
                    sceneCamera.orthographicSize = 1f;
                }
                else
                {
                    float reveal = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.25f, 3.05f, elapsed));
                    sceneCamera.transform.position = Vector3.Lerp(cameraStart, galleryWidePosition, reveal);
                    float targetSize = Mathf.Lerp(0.60f, 1f, reveal);
                    sceneCamera.orthographicSize = targetSize;
                }
            }

            float openingFade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.28f, elapsed));
            float cutFade = elapsed < ArchiveCut
                ? Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(6.24f, ArchiveCut, elapsed))
                : 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(ArchiveCut, 6.78f, elapsed));
            float exitFade = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(8.72f, Duration, elapsed));
            if (fadeImage != null) fadeImage.color = new Color(0f, 0f, 0f, Mathf.Max(openingFade, Mathf.Max(cutFade, exitFade)));
            if (vignetteImage != null)
                vignetteImage.color = new Color(1f, 1f, 1f, archiveShot ? 0.50f : 0.42f);

            if (galleryDimImage != null)
            {
                float dim = archiveShot ? 0f : Mathf.SmoothStep(0f, 0.36f, Mathf.InverseLerp(5.66f, 6.24f, elapsed));
                galleryDimImage.color = new Color(0.015f, 0.025f, 0.065f, dim);
            }
        }

        void UpdatePainting()
        {
            if (finalPainting == null || finalPaintingTarget == null) return;
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(PaintingSettleStart, PaintingSettleEnd, elapsed));
            float settle = elapsed >= PaintingSettleEnd
                ? Mathf.Sin(Mathf.Clamp01((elapsed - PaintingSettleEnd) / 0.24f) * Mathf.PI) * 0.008f
                : 0f;
            finalPainting.transform.position = Vector3.Lerp(paintingStart, finalPaintingTarget.position, progress) + Vector3.up * settle;
            finalPainting.transform.rotation = Quaternion.Slerp(paintingStartRotation, Quaternion.identity, progress);
            if (finalPainting.color.a != 1f)
            {
                Color color = finalPainting.color;
                color.a = 1f;
                finalPainting.color = color;
            }
        }

        void UpdateGuard()
        {
            if (guard == null || guardRenderer == null) return;
            if (sceneCamera != null)
            {
                float viewportX = sceneCamera.WorldToViewportPoint(guard.position).x;
                Color tint = guardRenderer.color;
                tint.a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.025f, 0.19f, viewportX));
                guardRenderer.color = tint;
            }
            bool walking = elapsed >= WalkStart && elapsed < WalkEnd;
            guardRenderer.flipX = false;
            guard.rotation = Quaternion.identity;
            if (!walking)
            {
                SetFrame(guardIdleBack, elapsed >= WalkEnd ? 0 : Mathf.FloorToInt(elapsed * 2f) % Mathf.Max(1, guardIdleBack != null ? guardIdleBack.Length : 1));
                guard.position = elapsed < WalkStart ? guardStart + new Vector3(0f, Mathf.Sin(elapsed * 1.6f) * 0.0025f, 0f) : GuardPath(WalkEnd);
                guard.localScale = new Vector3(guardStartScale.x, guardStartScale.y, guardStartScale.z);
                return;
            }

            float cycle = Mathf.Repeat((elapsed - WalkStart) * 2f, 1f);
            Vector3 pos = GuardPath(elapsed);
            pos.y += (1f - Mathf.Cos(cycle * Mathf.PI * 2f)) * 0.0045f;
            guard.position = pos;
            float squash = 1f - 0.014f * (1f - Mathf.Cos(cycle * Mathf.PI * 2f)) * 0.5f;
            guard.localScale = new Vector3(guardStartScale.x, guardStartScale.y * squash, guardStartScale.z);
            int frameCount = guardWalkBack != null ? guardWalkBack.Length : 1;
            SetFrame(guardWalkBack, Mathf.FloorToInt((elapsed - WalkStart) * WalkFramesPerSecond) % Mathf.Max(1, frameCount));
        }

        Vector3 guardStartScale = Vector3.one;

        Vector3 GuardPath(float time)
        {
            if (guardPathSamples == null || guardPathSamples.Length < 2 || guardPathLength <= 0f) return guardStart;
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(WalkStart, WalkEnd, time));
            float distance = progress * guardPathLength;
            int upper = 1;
            while (upper < guardPathDistances.Length - 1 && guardPathDistances[upper] < distance) upper++;
            int lower = upper - 1;
            float length = guardPathDistances[upper] - guardPathDistances[lower];
            float local = length > 0.0001f ? (distance - guardPathDistances[lower]) / length : 0f;
            return Vector3.Lerp(guardPathSamples[lower], guardPathSamples[upper], local);
        }

        void BuildGuardPath()
        {
            guardStartScale = guard != null ? guard.localScale : Vector3.one;
            Vector3[] points =
            {
                guardStart,
                new Vector3(0.20f, -0.50f, guardStart.z),
                new Vector3(0.72f, -0.50f, guardStart.z),
                new Vector3(1.31f, -0.49f, guardStart.z)
            };
            const int samplesPerSegment = 20;
            int segments = points.Length - 1;
            guardPathSamples = new Vector3[segments * samplesPerSegment + 1];
            guardPathDistances = new float[guardPathSamples.Length];
            int sample = 0;
            for (int segment = 0; segment < segments; segment++)
            {
                Vector3 p0 = points[Mathf.Max(0, segment - 1)];
                Vector3 p1 = points[segment];
                Vector3 p2 = points[segment + 1];
                Vector3 p3 = points[Mathf.Min(points.Length - 1, segment + 2)];
                for (int step = 0; step < samplesPerSegment; step++)
                    guardPathSamples[sample++] = CatmullRom(p0, p1, p2, p3, step / (float)samplesPerSegment);
            }
            guardPathSamples[sample] = points[points.Length - 1];
            guardPathLength = 0f;
            for (int i = 1; i < guardPathSamples.Length; i++)
            {
                guardPathLength += Vector3.Distance(guardPathSamples[i - 1], guardPathSamples[i]);
                guardPathDistances[i] = guardPathLength;
            }
        }

        static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            return 0.5f * ((2f * p1) + (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        void UpdatePlaqueLabels()
        {
            if (plaqueLabels == null || plaqueAnchors == null || sceneCamera == null) return;
            float closeScale = Mathf.Clamp(0.60f / sceneCamera.orthographicSize, 0.52f, 1f);
            for (int i = 0; i < plaqueLabels.Length && i < plaqueAnchors.Length; i++)
            {
                Text label = plaqueLabels[i];
                Transform anchor = plaqueAnchors[i];
                if (label == null || anchor == null) continue;
                label.enabled = elapsed < ArchiveCut;
                label.rectTransform.position = sceneCamera.WorldToScreenPoint(anchor.position);
                label.rectTransform.sizeDelta = new Vector2(54f * closeScale, 24f * closeScale);
                label.fontSize = Mathf.Max(4, Mathf.RoundToInt(7f * closeScale));
                float reveal = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.82f, 1.30f, elapsed));
                Color color = label.color;
                color.a = i == 2 ? reveal : 1f;
                label.color = color;
            }
        }

        void UpdateMoonbeam()
        {
            if (moonbeamRenderer == null) return;
            bool visible = elapsed >= 3.05f && elapsed < 6.24f;
            moonbeamRenderer.enabled = visible;
            if (!visible) return;

            float progress = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(3.05f, 6.24f, elapsed));
            float fadeIn = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(3.05f, 3.50f, elapsed));
            float fadeOut = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(5.80f, 6.24f, elapsed));
            moonbeamRenderer.transform.position = new Vector3(Mathf.Lerp(-0.60f, 0.92f, progress), -0.40f, moonbeamRenderer.transform.position.z);
            Color color = moonbeamRenderer.color;
            color.a = fadeIn * fadeOut * 0.32f;
            moonbeamRenderer.color = color;
        }

        void UpdateArchiveShadowAndLight()
        {
            if (archiveShadow != null)
            {
                bool visible = elapsed >= 6.88f && elapsed < 8.62f;
                archiveShadow.enabled = visible;
                if (visible)
                {
                    float progress = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(6.88f, 8.48f, elapsed));
                    archiveShadow.transform.position = new Vector3(Mathf.Lerp(-0.32f, 0.32f, progress),
                        -0.42f + Mathf.Sin(progress * Mathf.PI) * 0.012f, archiveShadow.transform.position.z);
                    Color color = archiveShadow.color;
                    color.a = Mathf.Sin(progress * Mathf.PI) * 0.92f;
                    archiveShadow.color = color;
                }
            }

            if (archiveSpotlightFlash != null)
            {
                float pulse = 0f;
                if (elapsed >= 7.28f && elapsed < 7.56f)
                    pulse = Mathf.Sin(Mathf.InverseLerp(7.28f, 7.56f, elapsed) * Mathf.PI);
                archiveSpotlightFlash.enabled = pulse > 0.01f;
                Color color = archiveSpotlightFlash.color;
                color.a = pulse * 0.68f;
                archiveSpotlightFlash.color = color;
            }
        }

        void UpdateSubtitleAndVoice()
        {
            CC03SubtitleCue current = null;
            foreach (CC03SubtitleCue cue in subtitles)
            {
                if (elapsed >= cue.start && elapsed < cue.end) { current = cue; break; }
            }
            if (subtitleText != null)
            {
                subtitleText.text = current == null ? string.Empty : "<b>" + current.speaker + "</b>  " + current.text;
                subtitleText.enabled = current != null;
            }
            if (subtitleBackground != null) subtitleBackground.enabled = current != null;

            if (current != activeVoiceCue && voiceSource != null)
            {
                if (voiceSource.isPlaying) voiceSource.Stop();
                activeVoiceCue = current;
                if (current != null && current.voice != null)
                {
                    voiceSource.loop = false;
                    voiceSource.clip = current.voice;
                    voiceSource.Play();
                }
            }
        }

        void UpdateSoundCues()
        {
            if (!playedMagneticClick && elapsed >= 1.05f)
            {
                playedMagneticClick = true;
                PlayEffect(magneticClick);
            }
            if (!playedDoor && elapsed >= 6.42f)
            {
                playedDoor = true;
                PlayEffect(archiveDoor);
            }
            if (!playedWoodTap && elapsed >= 7.40f)
            {
                playedWoodTap = true;
                PlayEffect(distantWoodTap);
            }
            if (elapsed >= WalkStart && elapsed < WalkEnd)
            {
                int animationFrame = Mathf.FloorToInt((elapsed - WalkStart) * WalkFramesPerSecond);
                while (lastFootstepFrame < animationFrame)
                {
                    lastFootstepFrame++;
                    int cycleFrame = lastFootstepFrame % 8;
                    if (cycleFrame == 1 || cycleFrame == 5) PlayEffect(softStep);
                }
            }
        }

        void UpdateAudioMix()
        {
            float exitMix = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(8.45f, Duration, elapsed));
            float voiceMix = activeVoiceCue != null ? 0.42f : 1f;
            if (musicSource != null) musicSource.volume = 0.12f * exitMix * voiceMix;
            if (ambientSource != null) ambientSource.volume = 0.22f * exitMix;
        }

        void SetFrame(Sprite[] frames, int index)
        {
            if (guardRenderer != null && frames != null && frames.Length > 0)
                guardRenderer.sprite = frames[Mathf.Clamp(index, 0, frames.Length - 1)];
        }

        void PlayEffect(AudioClip clip)
        {
            if (clip != null && effectsSource != null) effectsSource.PlayOneShot(clip);
        }

        void Finish()
        {
            if (finished) return;
            finished = true;
            ControlsLocked = false;
            if (voiceSource != null) voiceSource.Stop();
            if (ambientSource != null) ambientSource.Stop();
            if (musicSource != null) musicSource.Stop();
            onCutsceneFinished?.Invoke();
        }
    }
}
