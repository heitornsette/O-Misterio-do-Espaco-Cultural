using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Misterio.Cutscenes
{
    [Serializable]
    public sealed class CC02SubtitleCue
    {
        public float start;
        public float end;
        public string speaker;
        [TextArea] public string text;
        public AudioClip voice;
    }

    /// <summary>Eight-second transition from the statue puzzle to the painting gallery.</summary>
    public sealed class CC02CutsceneController : MonoBehaviour
    {
        public const float Duration = 8f;
        const float StatueSlideStart = 0.35f;
        const float StatueSlideEnd = 1.25f;
        const float WalkStart = 5f;
        const float WalkEnd = 7.62f;
        const float ExitFadeStart = 7.68f;
        const float WalkFramesPerSecond = 16f;

        public bool ControlsLocked { get; private set; } = true;
        public float Elapsed => elapsed;

        [Header("Scene")]
        [SerializeField] Camera sceneCamera;
        [SerializeField] Vector3 wideCameraPosition = new Vector3(0.1f, 0.02f, -10f);
        [SerializeField] Transform guard;
        [SerializeField] SpriteRenderer guardRenderer;
        [SerializeField] Sprite[] guardIdleFront;
        [SerializeField] Sprite[] guardIdleBack;
        [SerializeField] Sprite[] guardWalkBack;
        [SerializeField] Transform finalStatue;
        [SerializeField] Transform finalStatueTarget;
        [SerializeField] SpriteRenderer finalPlacementMark;
        [SerializeField] SpriteRenderer flashlightBeam;
        [SerializeField] SpriteRenderer[] dustPuffs;
        [SerializeField] SpriteRenderer stairLightGlow;

        [Header("UI")]
        [SerializeField] Text subtitleText;
        [SerializeField] Image subtitleBackground;
        [SerializeField] Image vignetteImage;
        [SerializeField] Image fadeImage;
        [SerializeField] CC02SubtitleCue[] subtitles =
        {
            new CC02SubtitleCue
            {
                start = 2.05f,
                end = 4.72f,
                speaker = "SEGURANÇA",
                text = "Estátuas no lugar. Agora, as pinturas."
            }
        };

        [Header("Audio")]
        [SerializeField] AudioSource voiceSource;
        [SerializeField] AudioSource ambientSource;
        [SerializeField] AudioSource effectsSource;
        [SerializeField] AudioSource musicSource;
        [SerializeField] AudioClip ambientNight;
        [SerializeField] AudioClip xylophonePhrase;
        [SerializeField] AudioClip woodenSettle;
        [SerializeField] AudioClip completionChime;
        [SerializeField] AudioClip stairLatch;
        [SerializeField] AudioClip stairFootstep;

        [Header("Integration")]
        [SerializeField] UnityEvent onCutsceneFinished = new UnityEvent();

        float elapsed;
        double lastRealtime;
        bool finished;
        bool playedSettle;
        bool playedCompletion;
        bool playedLatch;
        int lastFootstepAnimationFrame = -1;
        Vector3 cameraStart;
        Vector3 guardStart;
        Vector3 statueStart;
        Quaternion statueStartRotation;
        Vector3 guardBaseScale;
        Vector3[] dustStarts;
        Vector3[] dustBaseScales;
        Vector3[] guardPathSamples;
        float[] guardPathDistances;
        float guardPathLength;
        CC02SubtitleCue activeVoiceCue;

        void Awake()
        {
            if (sceneCamera == null) sceneCamera = Camera.main;
            cameraStart = sceneCamera != null ? sceneCamera.transform.position : Vector3.zero;
            guardStart = guard != null ? guard.position : Vector3.zero;
            BuildGuardPath();
            guardBaseScale = guard != null ? guard.localScale : Vector3.one;
            statueStart = finalStatue != null ? finalStatue.position : Vector3.zero;
            statueStartRotation = finalStatue != null ? finalStatue.rotation : Quaternion.identity;
            dustStarts = CapturePositions(dustPuffs);
            dustBaseScales = CaptureScales(dustPuffs);

            ControlsLocked = true;
            lastRealtime = Time.realtimeSinceStartupAsDouble;
            if (subtitleText != null)
            {
                subtitleText.text = string.Empty;
                subtitleText.enabled = false;
            }
            if (subtitleBackground != null) subtitleBackground.enabled = false;
            if (flashlightBeam != null) flashlightBeam.enabled = false;
            if (stairLightGlow != null) stairLightGlow.enabled = false;
            SetEnabled(dustPuffs, false);
            if (fadeImage != null) fadeImage.color = Color.black;

            if (ambientSource != null && ambientNight != null)
            {
                ambientSource.clip = ambientNight;
                ambientSource.loop = true;
                ambientSource.volume = 0.28f;
                ambientSource.Play();
            }
            if (musicSource != null && xylophonePhrase != null)
            {
                musicSource.clip = xylophonePhrase;
                musicSource.loop = false;
                musicSource.volume = 0.11f;
                musicSource.Play();
            }
        }

        void Update()
        {
            if (finished) return;

            double now = Time.realtimeSinceStartupAsDouble;
            elapsed = Mathf.Min(Duration, elapsed + (float)Math.Min(0.05, Math.Max(0.0, now - lastRealtime)));
            lastRealtime = now;

            UpdateVisuals();
            UpdateSubtitleAndVoice();
            UpdateSoundCues();
            UpdateAudioMix();
            if (elapsed >= Duration) Finish();
        }

        void UpdateVisuals()
        {
            float reveal = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 1.9f, elapsed));
            float pan = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.7f, 5.2f, elapsed));
            if (sceneCamera != null)
            {
                Vector3 target = Vector3.Lerp(cameraStart, wideCameraPosition, pan);
                float ease = 1f - Mathf.Exp(-Time.unscaledDeltaTime * 3.8f);
                sceneCamera.transform.position = Vector3.Lerp(sceneCamera.transform.position, target, ease);
                float targetSize = Mathf.Lerp(0.82f, 1f, reveal);
                sceneCamera.orthographicSize = Mathf.Lerp(sceneCamera.orthographicSize, targetSize, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 3f));
            }

            float openingFade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.34f, elapsed));
            float exitFade = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(ExitFadeStart, Duration, elapsed));
            if (fadeImage != null) fadeImage.color = new Color(0f, 0f, 0f, Mathf.Max(openingFade, exitFade));
            if (vignetteImage != null) vignetteImage.color = new Color(1f, 1f, 1f, elapsed < 1.9f ? 0.82f : 0.72f);

            UpdateFinalStatue();
            UpdateFlashlight();
            UpdateDustPuffs();
            UpdateGuard();

            if (stairLightGlow != null)
            {
                bool visible = elapsed >= 2.8f && elapsed < 5.7f;
                stairLightGlow.enabled = visible;
                if (visible)
                {
                    float warmUp = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(2.8f, 3.15f, elapsed));
                    float pulse = 0.88f + 0.12f * Mathf.Sin((elapsed - 3f) * 4f);
                    stairLightGlow.color = new Color(0.76f, 0.88f, 1f, warmUp * pulse * 0.72f);
                }
            }
        }

        void UpdateFinalStatue()
        {
            if (finalStatue == null || finalStatueTarget == null) return;
            float slide = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(StatueSlideStart, StatueSlideEnd, elapsed));
            float settle = elapsed >= StatueSlideEnd ? Mathf.Sin(Mathf.Clamp01((elapsed - StatueSlideEnd) / 0.28f) * Mathf.PI) * 0.018f : 0f;
            finalStatue.position = Vector3.Lerp(statueStart, finalStatueTarget.position, slide) + Vector3.up * settle;
            finalStatue.rotation = Quaternion.Slerp(statueStartRotation, Quaternion.identity, slide);

            if (finalPlacementMark != null)
            {
                float alpha = Mathf.Lerp(0.42f, 0.88f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1.2f, elapsed)));
                float pulse = elapsed < 1.6f ? 0.9f + 0.1f * Mathf.Sin(elapsed * 7f) : 1f;
                Color color = finalPlacementMark.color;
                color.a = alpha * pulse;
                finalPlacementMark.color = color;
            }
        }

        void UpdateFlashlight()
        {
            if (flashlightBeam == null || guard == null || finalStatueTarget == null || flashlightBeam.sprite == null) return;
            bool visible = elapsed >= 0.28f && elapsed < 2.15f;
            flashlightBeam.enabled = visible;
            if (!visible) return;

            Vector3 origin = guard.position + new Vector3(0.16f, 0.26f, -0.1f);
            Vector3 target = finalStatueTarget.position + new Vector3(0f, 0.09f, -0.1f);
            Vector3 direction = target - origin;
            flashlightBeam.transform.position = (origin + target) * 0.5f;
            flashlightBeam.transform.rotation = Quaternion.FromToRotation(Vector3.right, direction.normalized);
            float length = flashlightBeam.sprite.bounds.size.x;
            flashlightBeam.transform.localScale = new Vector3(Mathf.Max(0.1f, direction.magnitude / length), 0.42f, 1f);
            float fadeIn = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.28f, 0.55f, elapsed));
            float fadeOut = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.75f, 2.15f, elapsed));
            flashlightBeam.color = new Color(1f, 0.97f, 0.80f, 0.42f * fadeIn * fadeOut);
        }

        void UpdateDustPuffs()
        {
            if (dustPuffs == null) return;
            float progress = Mathf.Clamp01(Mathf.InverseLerp(0.66f, 1.48f, elapsed));
            bool visible = elapsed >= 0.66f && elapsed < 1.5f;
            for (int i = 0; i < dustPuffs.Length; i++)
            {
                SpriteRenderer puff = dustPuffs[i];
                if (puff == null) continue;
                puff.enabled = visible;
                if (!visible) continue;
                float phase = Mathf.Repeat(progress + i * 0.17f, 1f);
                puff.transform.position = dustStarts[i] + new Vector3((i - 1.5f) * 0.008f, phase * 0.055f, 0f);
                puff.transform.localScale = dustBaseScales[i] * (0.55f + phase * 0.65f);
                Color color = puff.color;
                color.a = (1f - phase) * 0.62f;
                puff.color = color;
            }
        }

        void UpdateGuard()
        {
            if (guard == null || guardRenderer == null) return;
            if (elapsed < WalkStart)
            {
                float turn = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.65f, 2.05f, elapsed));
                bool lookingUp = turn >= 0.5f;
                Sprite[] frames = lookingUp ? guardIdleBack : guardIdleFront;
                SetFrame(frames, Mathf.FloorToInt(elapsed * 2f) % Mathf.Max(1, frames != null ? frames.Length : 1));
                guardRenderer.flipX = !lookingUp;
                float turnCollapse = 1f - Mathf.Abs(turn * 2f - 1f);
                float width = 1f - 0.42f * turnCollapse;
                float breath = 0.003f * Mathf.Sin(elapsed * 2.2f);
                guard.localScale = new Vector3(guardBaseScale.x * width, guardBaseScale.y * (1f - 0.025f * turnCollapse), guardBaseScale.z);
                guard.rotation = Quaternion.Euler(0f, 0f, -1.2f * turnCollapse);
                guard.position = guardStart + new Vector3(0f, breath, 0f);
                return;
            }

            guardRenderer.flipX = false;
            bool walking = elapsed < WalkEnd;
            float cycle = walking ? Mathf.Repeat((elapsed - WalkStart) * 2f, 1f) : 0f;
            float lift = walking ? (1f - Mathf.Cos(cycle * Mathf.PI * 2f)) * 0.006f : 0f;
            guard.position = WalkPath(elapsed) + Vector3.up * lift;
            SetFrame(walking ? guardWalkBack : guardIdleBack,
                walking ? Mathf.FloorToInt((elapsed - WalkStart) * WalkFramesPerSecond) % Mathf.Max(1, guardWalkBack != null ? guardWalkBack.Length : 1) : 0);
            float compression = walking ? 1f - 0.018f * (1f - Mathf.Cos(cycle * Mathf.PI * 2f)) * 0.5f : 1f;
            guard.localScale = new Vector3(guardBaseScale.x, guardBaseScale.y * compression, guardBaseScale.z);
            guard.rotation = Quaternion.identity;
        }

        Vector3 WalkPath(float time)
        {
            if (guardPathSamples == null || guardPathSamples.Length < 2 || guardPathLength <= 0f) return guardStart;
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(WalkStart, WalkEnd, time));
            float targetDistance = progress * guardPathLength;
            int upper = 1;
            while (upper < guardPathDistances.Length - 1 && guardPathDistances[upper] < targetDistance) upper++;
            int lower = upper - 1;
            float span = guardPathDistances[upper] - guardPathDistances[lower];
            float local = span > 0.0001f ? (targetDistance - guardPathDistances[lower]) / span : 0f;
            return Vector3.Lerp(guardPathSamples[lower], guardPathSamples[upper], local);
        }

        void BuildGuardPath()
        {
            Vector3[] points =
            {
                guardStart,
                new Vector3(-0.65f, -0.26f, guardStart.z),
                new Vector3(0.02f, -0.20f, guardStart.z),
                new Vector3(0.72f, -0.14f, guardStart.z),
                new Vector3(1.24f, -0.09f, guardStart.z),
                new Vector3(1.43f, -0.02f, guardStart.z),
                new Vector3(1.54f, 0.11f, guardStart.z),
                new Vector3(1.65f, 0.26f, guardStart.z)
            };
            const int samplesPerSegment = 16;
            int segmentCount = points.Length - 1;
            guardPathSamples = new Vector3[segmentCount * samplesPerSegment + 1];
            guardPathDistances = new float[guardPathSamples.Length];
            int sampleIndex = 0;
            for (int segment = 0; segment < segmentCount; segment++)
            {
                Vector3 p0 = points[Mathf.Max(0, segment - 1)];
                Vector3 p1 = points[segment];
                Vector3 p2 = points[segment + 1];
                Vector3 p3 = points[Mathf.Min(points.Length - 1, segment + 2)];
                for (int step = 0; step < samplesPerSegment; step++)
                {
                    float t = step / (float)samplesPerSegment;
                    guardPathSamples[sampleIndex++] = CatmullRom(p0, p1, p2, p3, t);
                }
            }
            guardPathSamples[sampleIndex] = points[points.Length - 1];
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

        void UpdateSubtitleAndVoice()
        {
            CC02SubtitleCue current = null;
            foreach (CC02SubtitleCue cue in subtitles)
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
            if (!playedSettle && elapsed >= 0.82f)
            {
                playedSettle = true;
                PlayEffect(woodenSettle);
            }
            if (!playedCompletion && elapsed >= 1.30f)
            {
                playedCompletion = true;
                PlayEffect(completionChime);
            }
            if (!playedLatch && elapsed >= 3.02f)
            {
                playedLatch = true;
                PlayEffect(stairLatch);
            }
            if (elapsed >= WalkStart && elapsed < WalkEnd)
            {
                int animationFrame = Mathf.FloorToInt((elapsed - WalkStart) * WalkFramesPerSecond);
                while (lastFootstepAnimationFrame < animationFrame)
                {
                    lastFootstepAnimationFrame++;
                    int cycleFrame = lastFootstepAnimationFrame % 8;
                    if (cycleFrame == 1 || cycleFrame == 5) PlayEffect(stairFootstep);
                }
            }
        }

        void UpdateAudioMix()
        {
            float exitMix = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(7.4f, Duration, elapsed));
            float voiceMix = voiceSource != null && voiceSource.isPlaying ? 0.34f : 1f;
            if (musicSource != null) musicSource.volume = 0.11f * exitMix * voiceMix;
            if (ambientSource != null) ambientSource.volume = 0.28f * exitMix;
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

        static Vector3[] CapturePositions(SpriteRenderer[] renderers)
        {
            if (renderers == null) return Array.Empty<Vector3>();
            Vector3[] positions = new Vector3[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
                positions[i] = renderers[i] != null ? renderers[i].transform.position : Vector3.zero;
            return positions;
        }

        static Vector3[] CaptureScales(SpriteRenderer[] renderers)
        {
            if (renderers == null) return Array.Empty<Vector3>();
            Vector3[] scales = new Vector3[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
                scales[i] = renderers[i] != null ? renderers[i].transform.localScale : Vector3.one;
            return scales;
        }

        static void SetEnabled(SpriteRenderer[] renderers, bool enabled)
        {
            if (renderers == null) return;
            foreach (SpriteRenderer renderer in renderers)
                if (renderer != null) renderer.enabled = enabled;
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
