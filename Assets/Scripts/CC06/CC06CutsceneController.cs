using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Misterio.Cutscenes
{
    [Serializable]
    public sealed class CC06SubtitleCue
    {
        public float start;
        public float end;
        public string speaker;
        [TextArea] public string text;
        public AudioClip voice;
    }

    /// <summary>Twelve-second restoration reveal. The global game clock is supplied by the caller.</summary>
    public sealed class CC06CutsceneController : MonoBehaviour
    {
        public const float Duration = 12f;
        const float CircuitCloseStart = 1.16f;
        const float CircuitCloseEnd = 1.74f;
        const float PulseStart = 1.95f;
        const float PulseEnd = 4.92f;
        const float MothFlightStart = 5.28f;
        const float MothFlightEnd = 8.78f;

        static readonly Vector3[] CircuitPath =
        {
            new Vector3(2.40f, 0.82f, -0.16f),
            new Vector3(2.28f, 0.76f, -0.16f),
            new Vector3(1.52f, 0.76f, -0.16f),
            new Vector3(1.34f, 1.00f, -0.16f),
            new Vector3(0.98f, 1.30f, -0.16f),
            new Vector3(-1.08f, 1.30f, -0.16f),
            new Vector3(-1.58f, 1.33f, -0.16f),
            new Vector3(-3.55f, 1.33f, -0.16f),
            new Vector3(-4.00f, 1.30f, -0.16f),
            new Vector3(-4.08f, 1.15f, -0.16f),
            new Vector3(-4.08f, 0.92f, -0.16f)
        };

        public bool ControlsLocked { get; private set; }
        public float Elapsed => elapsed;
        public string GlobalClockValue => globalClockValue;

        [Header("Câmera e salão")]
        [SerializeField] Camera sceneCamera;
        [SerializeField] SpriteRenderer nightSky;
        [SerializeField] SpriteRenderer dawnSky;
        [SerializeField] SpriteRenderer conduitRoute;
        [SerializeField] SpriteRenderer connectionLeads;
        [SerializeField] SpriteRenderer connectionBridge;
        [SerializeField] SpriteRenderer[] pulseNodes;
        [SerializeField] SpriteRenderer[] lightPools;
        [SerializeField] SpriteRenderer[] fixtures;
        [SerializeField] SpriteRenderer sculptureRenderer;
        [SerializeField] SpriteRenderer artworkGlow;
        [SerializeField] SpriteRenderer floorReflection;

        [Header("Segurança e mariposa")]
        [SerializeField] Transform guard;
        [SerializeField] SpriteRenderer guardRenderer;
        [SerializeField] Sprite[] guardIdleFront;
        [SerializeField] Sprite[] guardIdleBack;
        [SerializeField] SpriteRenderer guardShadow;
        [SerializeField] Transform moth;
        [SerializeField] SpriteRenderer mothRenderer;
        [SerializeField] SpriteRenderer mothShadow;

        [Header("Legendas e relógio global")]
        [SerializeField] Text subtitleText;
        [SerializeField] Image subtitleBackground;
        [SerializeField] Image clockPlate;
        [SerializeField] Text clockLabel;
        [SerializeField] Text clockValueText;
        [SerializeField] Image fadeImage;
        [SerializeField] CC06SubtitleCue[] subtitles =
        {
            new CC06SubtitleCue { start = 0.10f, end = 1.92f, speaker = "NARRADOR", text = "O último conduíte encontrou seu caminho." },
            new CC06SubtitleCue { start = 2.14f, end = 4.92f, speaker = "NARRADOR", text = "Uma a uma, as luzes despertaram o salão." },
            new CC06SubtitleCue { start = 5.55f, end = 8.80f, speaker = "NARRADOR", text = "Uma mariposa saiu de trás do painel." },
            new CC06SubtitleCue { start = 9.02f, end = 11.90f, speaker = "SEGURANÇA", text = "Conseguimos. A exposição está pronta." }
        };

        [Header("Som")]
        [SerializeField] AudioSource voiceSource;
        [SerializeField] AudioSource ambienceSource;
        [SerializeField] AudioSource effectsSource;
        [SerializeField] AudioSource musicSource;
        [SerializeField] AudioClip electricalHum;
        [SerializeField] AudioClip circuitClick;
        [SerializeField] AudioClip lightChime;
        [SerializeField] AudioClip mothWings;
        [SerializeField] AudioClip resolvedMusic;

        [Header("Integração")]
        [SerializeField] bool playOnAwake = true;
        [SerializeField] UnityEvent onCutsceneStarted = new UnityEvent();
        [SerializeField] UnityEvent onCutsceneFinished = new UnityEvent();
        [SerializeField, HideInInspector] bool startPausedForEditorReview;

        float elapsed;
        double lastRealtime;
        Vector3 cameraStartPosition;
        float cameraStartSize;
        Vector3 guardStart;
        Vector3 guardScale;
        Vector3 mothStart;
        Vector3 mothScale;
        Vector3 guardShadowStart;
        Vector3 mothShadowStart;
        Vector3[] mothFlightSamples;
        float[] mothFlightDistances;
        float mothFlightLength;
        string globalClockValue = "--:--";
        int activeSubtitle = -1;
        bool playing;
        bool finished;
        bool playedClick;
        bool[] playedLightChimes;

        void Awake()
        {
            cameraStartPosition = sceneCamera != null ? sceneCamera.transform.position : new Vector3(2.40f, 0.78f, -10f);
            cameraStartSize = sceneCamera != null ? sceneCamera.orthographicSize : 0.72f;
            guardStart = guard != null ? guard.position : new Vector3(-2.40f, -1.26f, -0.08f);
            guardScale = guard != null ? guard.localScale : Vector3.one;
            mothStart = moth != null ? moth.position : new Vector3(-4.02f, 0.92f, -0.10f);
            mothScale = moth != null ? moth.localScale : Vector3.one;
            guardShadowStart = guardShadow != null ? guardShadow.transform.position : Vector3.zero;
            mothShadowStart = mothShadow != null ? mothShadow.transform.position : Vector3.zero;
            BuildMothFlightPath();
            playedLightChimes = new bool[3];
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

        public void PlayCutscene()
        {
            if (playing && !finished) return;
            startPausedForEditorReview = false;
            Begin();
        }

        /// <summary>The cutscene displays the supplied run time and never advances it.</summary>
        public void SetGlobalClock(string value)
        {
            globalClockValue = string.IsNullOrWhiteSpace(value) ? "--:--" : value.Trim();
            if (clockValueText != null) clockValueText.text = globalClockValue;
        }

#if UNITY_EDITOR
        public void PrepareEditorFrameReviewCapture()
        {
            playing = false;
            finished = false;
            elapsed = 0f;
            ControlsLocked = true;
            activeSubtitle = -1;
            StopAudio();
            ResetVisuals();
            EvaluateVisuals(0f, false);
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
            playedClick = false;
            Array.Clear(playedLightChimes, 0, playedLightChimes.Length);
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
                ambienceSource.volume = 0.12f;
                ambienceSource.Play();
            }
            if (musicSource != null && resolvedMusic != null)
            {
                musicSource.clip = resolvedMusic;
                musicSource.loop = false;
                musicSource.volume = 0.055f;
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
            if (sceneCamera != null)
            {
                sceneCamera.transform.position = cameraStartPosition;
                sceneCamera.orthographicSize = cameraStartSize;
            }
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
            }
            if (moth != null)
            {
                moth.position = mothStart;
                moth.localScale = mothScale;
                moth.rotation = Quaternion.identity;
                moth.gameObject.SetActive(true);
            }
            if (mothRenderer != null)
            {
                mothRenderer.enabled = true;
                mothRenderer.color = new Color(1f, 1f, 1f, 0f);
                mothRenderer.flipX = false;
            }
            if (mothShadow != null) mothShadow.enabled = false;
            if (nightSky != null) nightSky.color = new Color(1f, 1f, 1f, 0.70f);
            if (dawnSky != null) dawnSky.color = new Color(1f, 1f, 1f, 0f);
            if (conduitRoute != null) conduitRoute.color = new Color(0.72f, 0.89f, 1f, 0.12f);
            if (connectionLeads != null) connectionLeads.color = new Color(0.52f, 0.58f, 0.67f, 0.76f);
            if (connectionBridge != null) connectionBridge.color = new Color(0.62f, 0.88f, 1f, 0f);
            SetAlpha(pulseNodes, 0f);
            SetAlpha(lightPools, 0f);
            SetAlpha(fixtures, 0f);
            if (sculptureRenderer != null) sculptureRenderer.color = new Color(0.71f, 0.77f, 0.91f, 1f);
            if (artworkGlow != null) artworkGlow.color = new Color(1f, 0.70f, 0.42f, 0f);
            if (floorReflection != null) floorReflection.color = new Color(1f, 0.70f, 0.42f, 0f);
            if (subtitleText != null)
            {
                subtitleText.text = string.Empty;
                subtitleText.enabled = false;
                subtitleText.color = Color.white;
            }
            if (subtitleBackground != null) subtitleBackground.enabled = false;
            if (clockPlate != null) clockPlate.color = new Color(0.012f, 0.022f, 0.052f, 0.84f);
            if (clockLabel != null) clockLabel.text = "HORA GLOBAL";
            if (clockValueText != null) clockValueText.text = globalClockValue;
            if (fadeImage != null) fadeImage.color = new Color(0f, 0f, 0f, 0f);
        }

        void EvaluateVisuals(float time, bool playVoice)
        {
            UpdateCamera(time);
            UpdateCircuit(time);
            UpdateRoomLights(time);
            UpdateMoth(time);
            UpdateGuard(time);
            UpdateDawn(time);
            UpdateClockAndSubtitles(time, playVoice);
            if (fadeImage != null)
            {
                float inFade = 1f - Smooth(Mathf.InverseLerp(0f, 0.20f, time));
                float outFade = Smooth(Mathf.InverseLerp(11.78f, Duration, time));
                fadeImage.color = new Color(0f, 0f, 0f, Mathf.Max(inFade, outFade));
            }
        }

        void UpdateCamera(float time)
        {
            if (sceneCamera == null) return;
            Vector3 target;
            float size;
            if (time < 1.64f)
            {
                float settle = Smooth(Mathf.InverseLerp(0f, 1.64f, time));
                target = new Vector3(2.39f, 0.77f, cameraStartPosition.z);
                size = Mathf.Lerp(cameraStartSize * 0.96f, cameraStartSize, settle);
            }
            else if (time < 5.12f)
            {
                float u = Smooth(Mathf.InverseLerp(1.64f, 5.12f, time));
                target = Vector3.Lerp(new Vector3(2.39f, 0.77f, cameraStartPosition.z), new Vector3(0f, 0.14f, cameraStartPosition.z), u);
                size = Mathf.Lerp(cameraStartSize, 2.78f, u);
            }
            else if (time < 8.90f)
            {
                float u = Smooth(Mathf.InverseLerp(5.12f, 8.90f, time));
                target = Vector3.Lerp(new Vector3(0f, 0.14f, cameraStartPosition.z), new Vector3(-0.10f, 0.18f, cameraStartPosition.z), u);
                size = Mathf.Lerp(2.78f, 2.54f, u);
            }
            else
            {
                float u = Smooth(Mathf.InverseLerp(8.90f, 9.80f, time));
                target = Vector3.Lerp(new Vector3(-0.10f, 0.18f, cameraStartPosition.z), new Vector3(-0.42f, 0.04f, cameraStartPosition.z), u);
                size = Mathf.Lerp(2.54f, 2.36f, u);
            }
            sceneCamera.transform.position = target;
            sceneCamera.orthographicSize = size;
        }

        void UpdateCircuit(float time)
        {
            float joined = Smooth(Mathf.InverseLerp(CircuitCloseStart, CircuitCloseEnd, time));
            float routeOn = Smooth(Mathf.InverseLerp(PulseStart, PulseEnd, time));
            if (connectionLeads != null)
                connectionLeads.color = Color.Lerp(new Color(0.52f, 0.58f, 0.67f, 0.76f),
                    new Color(0.66f, 0.80f, 0.92f, 0.84f), routeOn);
            if (connectionBridge != null)
            {
                float flashOut = Smooth(Mathf.InverseLerp(CircuitCloseEnd + 0.06f, CircuitCloseEnd + 0.44f, time));
                float glint = joined * (1f - flashOut);
                Color wire = new Color(0.27f, 0.37f, 0.47f, 0.20f * joined);
                Color flash = new Color(0.70f, 0.91f, 1f, joined * (0.70f + 0.30f * Mathf.Sin(Mathf.Clamp01(joined) * Mathf.PI)));
                connectionBridge.color = Color.Lerp(flash, wire, flashOut);
            }
            if (conduitRoute != null) conduitRoute.color = new Color(0.82f, 0.93f, 1f, 0.08f + routeOn * 0.11f);

            float progress = Mathf.Clamp01(Mathf.InverseLerp(PulseStart, PulseEnd, time));
            float spread = 0.045f;
            if (pulseNodes != null)
            {
                for (int i = 0; i < pulseNodes.Length; i++)
                {
                    SpriteRenderer node = pulseNodes[i];
                    if (node == null) continue;
                    float p = progress - i * spread;
                    bool visible = time >= PulseStart && time < PulseEnd + 0.20f && p >= 0f && p <= 1f;
                    node.enabled = visible;
                    if (!visible) continue;
                    node.transform.position = SamplePath(CircuitPath, p);
                    float envelope = Mathf.SmoothStep(0f, 1f, Mathf.Min(p * 8f, (1f - p) * 8f));
                    float alpha = 0.42f + 0.28f * envelope;
                    node.color = new Color(0.78f, 0.94f, 1f, alpha);
                    float pulse = 0.62f + 0.08f * Mathf.Sin((time * 14f) + i * 1.5f);
                    node.transform.localScale = Vector3.one * pulse;
                }
            }
        }

        void UpdateRoomLights(float time)
        {
            // The repaired branch sits on the right; let the current reveal those spots first.
            float[] starts = { 4.10f, 3.42f, 2.73f };
            float[] ends = { 4.62f, 3.88f, 3.18f };
            for (int i = 0; i < 3; i++)
            {
                float level = Smooth(Mathf.InverseLerp(starts[i], ends[i], time));
                float breathing = 0.96f + 0.04f * Mathf.Sin(time * 1.6f + i * 1.1f);
                if (lightPools != null && i < lightPools.Length && lightPools[i] != null)
                    lightPools[i].color = new Color(1f, 0.78f, 0.52f, 0.19f * level * breathing);
                if (fixtures != null && i < fixtures.Length && fixtures[i] != null)
                    fixtures[i].color = new Color(1f, 0.90f, 0.69f, 0.75f * level * breathing);
                if (!playedLightChimes[i] && elapsed >= starts[i] && playing)
                {
                    playedLightChimes[i] = true;
                    if (effectsSource != null && lightChime != null)
                        effectsSource.PlayOneShot(lightChime, 0.11f + 0.01f * i);
                }
            }

            float artOn = Smooth(Mathf.InverseLerp(4.35f, 6.10f, time));
            if (sculptureRenderer != null)
                sculptureRenderer.color = Color.Lerp(new Color(0.71f, 0.77f, 0.91f, 1f), new Color(1f, 0.96f, 0.84f, 1f), artOn * 0.56f);
            float warm = Smooth(Mathf.InverseLerp(5.2f, 8.7f, time));
            if (artworkGlow != null) artworkGlow.color = new Color(1f, 0.67f, 0.37f, 0.09f * warm);
            if (floorReflection != null) floorReflection.color = new Color(1f, 0.72f, 0.42f, 0.085f * artOn);
        }

        void UpdateMoth(float time)
        {
            if (moth == null || mothRenderer == null) return;
            float show = Smooth(Mathf.InverseLerp(MothFlightStart, MothFlightStart + 0.24f, time));
            float gone = Smooth(Mathf.InverseLerp(MothFlightEnd - 0.20f, MothFlightEnd, time));
            float p = Mathf.Clamp01(Mathf.InverseLerp(MothFlightStart, MothFlightEnd, time));
            float flutter = 0.025f * Mathf.Sin((p * 8f + time * 0.24f) * Mathf.PI * 2f);
            moth.position = SampleArcPath(mothFlightSamples, mothFlightDistances, mothFlightLength, p) + Vector3.up * flutter;
            moth.localScale = new Vector3(mothScale.x, mothScale.y * (0.80f + 0.20f * Mathf.Abs(Mathf.Sin(time * 15f))), mothScale.z);
            moth.rotation = Quaternion.Euler(0f, 0f, 5f * Mathf.Sin(time * 3.0f));
            Color tint = Color.white;
            tint.a = show * (1f - gone);
            mothRenderer.color = tint;
            if (mothShadow != null)
            {
                mothShadow.enabled = tint.a > 0.05f;
                mothShadow.transform.position = new Vector3(moth.position.x, moth.position.y - 0.24f, mothShadowStart.z);
                mothShadow.color = new Color(0.016f, 0.019f, 0.030f, 0.18f * tint.a);
            }
        }

        void UpdateGuard(float time)
        {
            if (guard == null || guardRenderer == null) return;
            float relax = Smooth(Mathf.InverseLerp(8.90f, 10.30f, time));
            float look = Smooth(Mathf.InverseLerp(5.35f, 6.2f, time)) * (1f - Smooth(Mathf.InverseLerp(8.80f, 9.3f, time)));
            guard.position = guardStart + new Vector3(0.04f * look, 0.010f * Mathf.Sin(time * 2.0f), 0f);
            float breath = Mathf.Sin(time * 2.35f) * 0.004f;
            guard.localScale = new Vector3(guardScale.x, guardScale.y * (1f - 0.012f * relax + breath), guardScale.z);
            guard.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, 0.65f, look));
            SetFrame(guardRenderer, time < 5.35f ? guardIdleFront : guardIdleBack, Mathf.FloorToInt(time * 2.2f));
            if (guardShadow != null)
            {
                guardShadow.transform.position = new Vector3(guard.position.x, guardShadowStart.y, guardShadowStart.z);
                guardShadow.color = new Color(0.015f, 0.020f, 0.035f, 0.48f);
            }
        }

        void UpdateDawn(float time)
        {
            float dawn = Smooth(Mathf.InverseLerp(4.1f, 10.2f, time)) * 0.76f;
            if (nightSky != null) nightSky.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.70f, 0f, dawn));
            if (dawnSky != null) dawnSky.color = new Color(1f, 1f, 1f, dawn);
        }

        void UpdateClockAndSubtitles(float time, bool playVoice)
        {
            float clockAlpha = Smooth(Mathf.InverseLerp(0.28f, 0.62f, time));
            if (clockPlate != null)
            {
                clockPlate.enabled = clockAlpha > 0.01f;
                Color c = clockPlate.color;
                c.a = 0.84f * clockAlpha;
                clockPlate.color = c;
            }
            SetTextAlpha(clockLabel, clockAlpha);
            SetTextAlpha(clockValueText, clockAlpha);
            if (clockValueText != null) clockValueText.text = globalClockValue;

            CC06SubtitleCue cue = null;
            int cueIndex = -1;
            if (subtitles != null)
                for (int i = 0; i < subtitles.Length; i++)
                    if (subtitles[i] != null && time >= subtitles[i].start && time < subtitles[i].end)
                    {
                        cue = subtitles[i];
                        cueIndex = i;
                        break;
                    }
            float alpha = cue == null ? 0f : Smooth(Mathf.InverseLerp(cue.start, cue.start + 0.13f, time)) *
                                                   (1f - Smooth(Mathf.InverseLerp(cue.end - 0.14f, cue.end, time)));
            if (subtitleText != null)
            {
                subtitleText.enabled = cue != null;
                if (cueIndex != activeSubtitle)
                    subtitleText.text = cue == null ? string.Empty : cue.speaker + "\n“" + cue.text + "”";
                Color c = subtitleText.color;
                c.a = alpha;
                subtitleText.color = c;
            }
            if (subtitleBackground != null)
            {
                subtitleBackground.enabled = cue != null;
                Color c = subtitleBackground.color;
                c.a = 0.80f * alpha;
                subtitleBackground.color = c;
            }
            if (cueIndex != activeSubtitle)
            {
                activeSubtitle = cueIndex;
                if (playVoice && voiceSource != null)
                {
                    voiceSource.Stop();
                    if (cue != null && cue.voice != null)
                    {
                        voiceSource.clip = cue.voice;
                        voiceSource.Play();
                    }
                }
            }
        }

        void UpdateAudio()
        {
            if (!playedClick && elapsed >= CircuitCloseEnd)
            {
                playedClick = true;
                if (effectsSource != null && circuitClick != null) effectsSource.PlayOneShot(circuitClick, 0.25f);
            }
            if (ambienceSource != null && ambienceSource.isPlaying)
                ambienceSource.volume = Mathf.Lerp(0.12f, 0.045f, Smooth(Mathf.InverseLerp(1.7f, 5.0f, elapsed)));
            if (musicSource != null && musicSource.isPlaying)
            {
                float sustain = elapsed < 8.9f ? 0.052f : Mathf.Lerp(0.052f, 0.075f, Smooth(Mathf.InverseLerp(9f, 9.65f, elapsed)));
                musicSource.volume = sustain * (1f - 0.24f * Smooth(Mathf.InverseLerp(9.0f, 9.25f, elapsed)) *
                                                     (1f - Smooth(Mathf.InverseLerp(11.75f, 12f, elapsed))));
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
            if (onCutsceneFinished != null) onCutsceneFinished.Invoke();
        }

        static void SetAlpha(SpriteRenderer[] renderers, float alpha)
        {
            if (renderers == null) return;
            foreach (SpriteRenderer renderer in renderers)
                if (renderer != null)
                {
                    Color c = renderer.color;
                    c.a = alpha;
                    renderer.color = c;
                }
        }

        static void SetFrame(SpriteRenderer renderer, Sprite[] frames, int index)
        {
            if (renderer == null || frames == null || frames.Length == 0) return;
            renderer.sprite = frames[Mathf.Abs(index) % frames.Length];
        }

        static void SetTextAlpha(Text text, float alpha)
        {
            if (text == null) return;
            text.enabled = alpha > 0.01f;
            Color c = text.color;
            c.a = alpha;
            text.color = c;
        }

        static float Smooth(float value) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(value));

        void BuildMothFlightPath()
        {
            Vector3[] controlPoints =
            {
                mothStart,
                new Vector3(-3.60f, 1.04f, mothStart.z),
                new Vector3(-2.65f, 0.72f, mothStart.z),
                new Vector3(-1.35f, 0.87f, mothStart.z),
                new Vector3(-0.10f, 1.18f, mothStart.z),
                new Vector3(1.30f, 1.23f, mothStart.z),
                new Vector3(2.18f, 0.96f, mothStart.z)
            };
            const int subdivisionsPerSegment = 18;
            mothFlightSamples = new Vector3[(controlPoints.Length - 1) * subdivisionsPerSegment + 1];
            mothFlightSamples[0] = controlPoints[0];
            int sample = 1;
            for (int segment = 0; segment < controlPoints.Length - 1; segment++)
                for (int step = 1; step <= subdivisionsPerSegment; step++)
                    mothFlightSamples[sample++] = SampleCatmullRom(controlPoints, segment, step / (float)subdivisionsPerSegment);

            mothFlightDistances = new float[mothFlightSamples.Length];
            mothFlightLength = 0f;
            for (int i = 1; i < mothFlightSamples.Length; i++)
            {
                mothFlightLength += Vector3.Distance(mothFlightSamples[i - 1], mothFlightSamples[i]);
                mothFlightDistances[i] = mothFlightLength;
            }
        }

        static Vector3 SampleCatmullRom(Vector3[] path, int segment, float t)
        {
            Vector3 p0 = path[Mathf.Max(segment - 1, 0)];
            Vector3 p1 = path[segment];
            Vector3 p2 = path[segment + 1];
            Vector3 p3 = path[Mathf.Min(segment + 2, path.Length - 1)];
            return 0.5f * ((2f * p1) + (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t * t * t);
        }

        static Vector3 SampleArcPath(Vector3[] samples, float[] distances, float length, float progress)
        {
            if (samples == null || samples.Length == 0) return Vector3.zero;
            if (samples.Length == 1 || length <= 0.0001f) return samples[0];
            float target = Mathf.Clamp01(progress) * length;
            int low = 1;
            int high = distances.Length - 1;
            while (low < high)
            {
                int middle = (low + high) / 2;
                if (distances[middle] < target) low = middle + 1;
                else high = middle;
            }
            int next = low;
            int previous = Mathf.Max(0, next - 1);
            float segmentLength = distances[next] - distances[previous];
            float u = segmentLength < 0.0001f ? 0f : (target - distances[previous]) / segmentLength;
            return Vector3.Lerp(samples[previous], samples[next], u);
        }

        static Vector3 SamplePath(Vector3[] path, float progress)
        {
            if (path == null || path.Length == 0) return Vector3.zero;
            if (path.Length == 1) return path[0];
            float total = 0f;
            for (int i = 1; i < path.Length; i++) total += Vector3.Distance(path[i - 1], path[i]);
            float target = Mathf.Clamp01(progress) * total;
            for (int i = 1; i < path.Length; i++)
            {
                float segment = Vector3.Distance(path[i - 1], path[i]);
                if (target <= segment)
                    return Vector3.Lerp(path[i - 1], path[i], segment < 0.0001f ? 0f : target / segment);
                target -= segment;
            }
            return path[path.Length - 1];
        }
    }
}
