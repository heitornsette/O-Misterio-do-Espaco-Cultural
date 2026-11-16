using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Misterio.Cutscenes
{
    [Serializable]
    public sealed class CC10SubtitleCue
    {
        public float start;
        public float end;
        public string speaker;
        [TextArea] public string text;
        public AudioClip voice;
    }

    /// <summary>Fifteen-second gentle timeout ending for the unfinished main hall circuit.</summary>
    public sealed class CC10CutsceneController : MonoBehaviour
    {
        public const float Duration = 15f;

        public bool ControlsLocked { get; private set; } = true;
        public float Elapsed => elapsed;
        public string GlobalClockValue => globalClockValue;

        [Header("Salão e planos")]
        [SerializeField] Camera sceneCamera;
        [SerializeField] GameObject hallRoot;
        [SerializeField] GameObject entranceRoot;
        [SerializeField] SpriteRenderer nightSky;
        [SerializeField] SpriteRenderer dawnSky;
        [SerializeField] SpriteRenderer conduitRoute;
        [SerializeField] SpriteRenderer wireSpark;
        [SerializeField] SpriteRenderer artwork;
        [SerializeField] SpriteRenderer panelOpen;
        [SerializeField] SpriteRenderer panelClosedBase;
        [SerializeField] SpriteRenderer panelDoor;

        [Header("Segurança e rádio")]
        [SerializeField] Transform guard;
        [SerializeField] SpriteRenderer guardRenderer;
        [SerializeField] SpriteRenderer guardShadow;
        [SerializeField] Sprite[] guardIdleFront;
        [SerializeField] Sprite[] guardIdleBack;
        [SerializeField] Transform radio;
        [SerializeField] SpriteRenderer radioScreen;

        [Header("Equipe na entrada")]
        [SerializeField] SpriteRenderer exteriorArchitecture;
        [SerializeField] SpriteRenderer exteriorFloor;
        [SerializeField] Transform[] entranceTeam;
        [SerializeField] SpriteRenderer[] entranceTeamRenderers;
        [SerializeField] SpriteRenderer[] entranceTeamShadows;
        [SerializeField] Sprite[] rectorIdleFrames;

        [Header("Relógio, legendas e resultado")]
        [SerializeField] Image clockHud;
        [SerializeField] Text clockHudLabel;
        [SerializeField] Text clockHudValue;
        [SerializeField] Image subtitleBackground;
        [SerializeField] Text subtitleText;
        [SerializeField] Image fadeImage;
        [SerializeField] Image resultBackdrop;
        [SerializeField] CanvasGroup resultCanvasGroup;
        [SerializeField] Text resultKicker;
        [SerializeField] Text resultTitle;
        [SerializeField] Text resultDescription;
        [SerializeField] Button retryButton;
        [SerializeField] Button menuButton;
        [SerializeField] CC10SubtitleCue[] subtitles =
        {
            new CC10SubtitleCue { start = 0.08f, end = 2.88f, speaker = "NARRADOR", text = "A última conexão não se fechou a tempo." },
            new CC10SubtitleCue { start = 3.06f, end = 6.90f, speaker = "NARRADOR", text = "A manhã encontrou a obra intacta, mas o salão ainda precisava de energia." },
            new CC10SubtitleCue { start = 7.02f, end = 8.80f, speaker = "SEGURANÇA", text = "O Salão Principal precisa de uma revisão antes de abrir." },
            new CC10SubtitleCue { start = 8.82f, end = 10.94f, speaker = "CURADORIA", text = "Vamos manter esta sala fechada. O restante está seguro." },
            new CC10SubtitleCue { start = 11.18f, end = 14.92f, speaker = "NARRADOR", text = "A abertura daquela sala ficou para depois." }
        };

        [Header("Som")]
        [SerializeField] AudioSource voiceSource;
        [SerializeField] AudioSource ambienceSource;
        [SerializeField] AudioSource effectsSource;
        [SerializeField] AudioSource musicSource;
        [SerializeField] AudioClip indoorAmbience;
        [SerializeField] AudioClip exteriorAmbience;
        [SerializeField] AudioClip clockChime;
        [SerializeField] AudioClip radioClick;
        [SerializeField] AudioClip panelLatch;
        [SerializeField] AudioClip gentleEndingNote;
        [SerializeField] AudioClip gentleMusic;

        [Header("Integração")]
        [SerializeField] bool playOnAwake = true;
        [SerializeField] UnityEvent onCutsceneStarted = new UnityEvent();
        [SerializeField] UnityEvent onCutsceneFinished = new UnityEvent();
        [SerializeField] UnityEvent onRetryRequested = new UnityEvent();
        [SerializeField] UnityEvent onReturnToMenu = new UnityEvent();

        const float ExteriorShotStart = 8.62f;
        const float ExteriorShotEnd = 11.02f;
        float elapsed;
        double lastRealtime;
        string globalClockValue = "06:00";
        bool playing;
        bool finished;
        bool clockCuePlayed;
        bool radioCuePlayed;
        bool latchCuePlayed;
        bool noteCuePlayed;
        bool musicCuePlayed;
        bool outsideAmbienceStarted;
        int activeSubtitle = -1;
        Vector3 cameraStartPosition;
        float cameraStartSize;
        Vector3 guardStart;
        Vector3 guardScale;
        Vector3 guardShadowStart;
        Vector3 radioStart;
        Vector3 panelDoorStart;
        Vector3 panelDoorScale;
        Vector3[] entranceTeamStarts;
        Color[] entranceTeamStartColors;
        Color[] entranceTeamShadowStartColors;

        void Awake()
        {
            cameraStartPosition = sceneCamera != null ? sceneCamera.transform.position : new Vector3(0f, 0f, -10f);
            cameraStartSize = sceneCamera != null ? sceneCamera.orthographicSize : 1.7f;
            guardStart = guard != null ? guard.position : new Vector3(-3.42f, -0.50f, -0.08f);
            guardScale = guard != null ? guard.localScale : Vector3.one;
            guardShadowStart = guardShadow != null ? guardShadow.transform.position : guardStart + Vector3.down * 0.05f;
            radioStart = radio != null ? radio.localPosition : new Vector3(0.13f, 0.24f, -0.04f);
            panelDoorStart = panelDoor != null ? panelDoor.transform.position : new Vector3(-4.20f, 0.83f, -0.24f);
            panelDoorScale = panelDoor != null ? panelDoor.transform.localScale : Vector3.one;
            entranceTeamStarts = CapturePositions(entranceTeam);
            entranceTeamStartColors = CaptureColors(entranceTeamRenderers);
            entranceTeamShadowStartColors = CaptureColors(entranceTeamShadows);
            if (clockHudValue != null) clockHudValue.text = globalClockValue;
            ResetVisuals();
            lastRealtime = Time.realtimeSinceStartupAsDouble;
            if (playOnAwake) Begin();
        }

        void Update()
        {
            if (!playing || finished) return;
            double now = Time.realtimeSinceStartupAsDouble;
            elapsed = Mathf.Min(Duration, elapsed + (float)Math.Max(0d, now - lastRealtime));
            lastRealtime = now;
            EvaluateVisuals(elapsed, true);
            UpdateAudio();
            if (elapsed >= Duration) Finish();
        }

        /// <summary>The timeout ending always freezes the global clock at 06:00.</summary>
        public void SetGlobalClock(string value)
        {
            globalClockValue = string.IsNullOrWhiteSpace(value) ? "06:00" : value.Trim();
            if (clockHudValue != null) clockHudValue.text = globalClockValue;
        }

        public void PlayCutscene()
        {
            if (playing && !finished) return;
            Begin();
        }

        public void RetrySameAttempt()
        {
            onRetryRequested?.Invoke();
            Begin();
        }

        public void ReturnToMenu() => onReturnToMenu?.Invoke();

#if UNITY_EDITOR
        public void PrepareEditorFrameReviewCapture()
        {
            playing = false;
            finished = false;
            elapsed = 0f;
            activeSubtitle = -1;
            ControlsLocked = true;
            ResetCueState();
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
            ResetCueState();
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

        void ResetCueState()
        {
            clockCuePlayed = radioCuePlayed = latchCuePlayed = noteCuePlayed = musicCuePlayed = outsideAmbienceStarted = false;
        }

        void StartAudio()
        {
            if (ambienceSource == null || indoorAmbience == null) return;
            ambienceSource.clip = indoorAmbience;
            ambienceSource.loop = true;
            ambienceSource.volume = 0.07f;
            ambienceSource.Play();
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
            SetShotRoot(false);
            if (nightSky != null) nightSky.color = new Color(1f, 1f, 1f, 0.70f);
            if (dawnSky != null) dawnSky.color = new Color(1f, 1f, 1f, 0f);
            if (conduitRoute != null) conduitRoute.color = new Color(0.74f, 0.85f, 0.95f, 0.22f);
            if (wireSpark != null) { wireSpark.enabled = true; wireSpark.color = new Color(1f, 1f, 1f, 0.22f); }
            if (artwork != null) artwork.color = new Color(0.71f, 0.77f, 0.91f, 1f);
            if (panelOpen != null) { panelOpen.enabled = true; panelOpen.color = Color.white; }
            if (panelClosedBase != null) { panelClosedBase.enabled = true; panelClosedBase.color = new Color(1f, 1f, 1f, 0f); }
            if (panelDoor != null)
            {
                panelDoor.enabled = true;
                panelDoor.color = new Color(1f, 1f, 1f, 0f);
                panelDoor.transform.position = panelDoorStart;
                panelDoor.transform.localScale = new Vector3(0.02f, panelDoorScale.y, panelDoorScale.z);
            }
            if (guard != null) { guard.position = guardStart; guard.localScale = guardScale; guard.rotation = Quaternion.identity; }
            if (guardRenderer != null) { guardRenderer.color = Color.white; guardRenderer.enabled = true; SetFrame(guardRenderer, guardIdleBack, 0); }
            if (guardShadow != null) { guardShadow.enabled = true; guardShadow.transform.position = guardShadowStart; }
            if (radio != null) radio.localPosition = radioStart;
            if (radioScreen != null) radioScreen.color = new Color(0.52f, 0.82f, 1f, 0.08f);
            RestorePositions(entranceTeam, entranceTeamStarts);
            RestoreColors(entranceTeamRenderers, entranceTeamStartColors, 0f);
            RestoreColors(entranceTeamShadows, entranceTeamShadowStartColors, 0f);
            if (exteriorArchitecture != null) exteriorArchitecture.color = Color.white;
            if (exteriorFloor != null) exteriorFloor.color = Color.white;
            if (clockHud != null) clockHud.color = new Color(0.012f, 0.022f, 0.052f, 0f);
            SetTextAlpha(clockHudLabel, 0f);
            SetTextAlpha(clockHudValue, 0f);
            if (clockHudValue != null) clockHudValue.text = globalClockValue;
            if (subtitleText != null) { subtitleText.text = string.Empty; subtitleText.enabled = false; subtitleText.color = Color.white; }
            if (subtitleBackground != null) subtitleBackground.color = new Color(0.012f, 0.018f, 0.038f, 0f);
            if (fadeImage != null) fadeImage.color = new Color(0f, 0f, 0f, 0f);
            if (resultBackdrop != null) resultBackdrop.color = new Color(0.012f, 0.023f, 0.052f, 0f);
            if (resultCanvasGroup != null) { resultCanvasGroup.alpha = 0f; resultCanvasGroup.interactable = false; resultCanvasGroup.blocksRaycasts = false; }
            if (retryButton != null) retryButton.interactable = false;
            if (menuButton != null) menuButton.interactable = false;
        }

        void EvaluateVisuals(float time, bool playVoice)
        {
            bool exterior = time >= ExteriorShotStart && time < ExteriorShotEnd;
            SetShotRoot(exterior);
            UpdateCamera(time, exterior);
            UpdateHall(time);
            UpdateEntrance(time, exterior);
            UpdateClock(time);
            UpdateSubtitle(time, playVoice);
            UpdateResult(time);
        }

        void SetShotRoot(bool exterior)
        {
            if (hallRoot != null && hallRoot.activeSelf == exterior) hallRoot.SetActive(!exterior);
            if (entranceRoot != null && entranceRoot.activeSelf != exterior) entranceRoot.SetActive(exterior);
        }

        void UpdateCamera(float time, bool exterior)
        {
            if (sceneCamera == null) return;
            Vector3 position;
            float size;
            if (time < 0.86f)
            {
                position = new Vector3(2.39f, 0.79f, cameraStartPosition.z);
                size = 0.78f;
            }
            else if (time < 3.02f)
            {
                position = new Vector3(-4.02f, 0.72f, cameraStartPosition.z);
                size = 1.12f;
            }
            else if (time < 7.02f)
            {
                position = new Vector3(0.54f, 0.30f, cameraStartPosition.z);
                size = 1.72f;
            }
            else if (time < ExteriorShotStart)
            {
                position = new Vector3(-3.15f, 0.05f, cameraStartPosition.z);
                size = 1.20f;
            }
            else if (exterior)
            {
                position = new Vector3(0.03f, 0.02f, cameraStartPosition.z);
                size = 1.68f;
            }
            else if (time < 12.22f)
            {
                position = new Vector3(-4.03f, 0.76f, cameraStartPosition.z);
                size = 1.12f;
            }
            else
            {
                position = new Vector3(-0.10f, 0.08f, cameraStartPosition.z);
                size = 2.24f;
            }
            sceneCamera.transform.position = position;
            sceneCamera.orthographicSize = size;
        }

        void UpdateHall(float time)
        {
            float dawn = Smooth(Mathf.InverseLerp(2.55f, 6.85f, time));
            if (nightSky != null) nightSky.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.70f, 0f, dawn));
            if (dawnSky != null) dawnSky.color = new Color(1f, 1f, 1f, 0.76f * dawn);
            if (conduitRoute != null)
            {
                float routeAlpha = 0.22f * (1f - Smooth(Mathf.InverseLerp(0.02f, 0.36f, time)));
                conduitRoute.color = new Color(0.74f, 0.85f, 0.95f, routeAlpha);
            }
            if (wireSpark != null)
            {
                float sparkAlpha = 0.22f * (1f - Smooth(Mathf.InverseLerp(0f, 0.18f, time)));
                wireSpark.color = new Color(1f, 1f, 1f, sparkAlpha);
                wireSpark.enabled = sparkAlpha > 0.01f;
            }
            if (artwork != null)
                artwork.color = Color.Lerp(new Color(0.71f, 0.77f, 0.91f, 1f), new Color(0.86f, 0.83f, 0.76f, 1f), dawn * 0.72f);

            if (guard != null)
            {
                Vector3 target = guardStart;
                if (time >= 0.86f && time < 3.02f)
                    target += new Vector3(-0.12f * Smooth(Mathf.InverseLerp(0.86f, 1.28f, time)), -0.025f, 0f);
                else if (time >= 7.02f && time < ExteriorShotStart)
                    target = new Vector3(-3.15f, -0.48f, guardStart.z);
                else if (time >= 11.02f)
                    target = new Vector3(-3.43f, -0.45f, guardStart.z);
                guard.position = target + Vector3.up * (0.008f * Mathf.Sin(time * 2.2f));
                float idleRate = time >= 7.02f && time < ExteriorShotStart ? 3.2f : 2.0f;
                SetFrame(guardRenderer, time >= 7.02f && time < ExteriorShotStart ? guardIdleFront : guardIdleBack,
                    Mathf.FloorToInt(time * idleRate));
                if (guardShadow != null)
                    guardShadow.transform.position = new Vector3(guard.position.x, guardShadowStart.y, guardShadowStart.z);
            }

            if (radio != null)
            {
                float raise = Smooth(Mathf.InverseLerp(7.08f, 7.64f, time));
                float lower = Smooth(Mathf.InverseLerp(8.35f, ExteriorShotStart, time));
                radio.localPosition = radioStart + Vector3.up * (0.18f * raise * (1f - lower));
            }
            if (radioScreen != null)
            {
                float on = Smooth(Mathf.InverseLerp(7.18f, 7.48f, time)) * (1f - Smooth(Mathf.InverseLerp(8.22f, 8.58f, time)));
                float signal = 0.44f + 0.13f * Mathf.Sin(time * 17f);
                radioScreen.color = new Color(0.53f, 0.84f, 1f, 0.08f + on * signal);
            }

            float closing = Smooth(Mathf.InverseLerp(11.22f, 11.88f, time));
            float appear = Smooth(Mathf.InverseLerp(11.08f, 11.38f, time));
            if (panelOpen != null)
            {
                Color openColor = Color.white;
                openColor.a = 1f - appear;
                panelOpen.color = openColor;
            }
            if (panelClosedBase != null)
            {
                panelClosedBase.enabled = appear > 0.01f;
                Color baseColor = Color.white;
                baseColor.a = appear;
                panelClosedBase.color = baseColor;
            }
            if (panelDoor != null)
            {
                panelDoor.enabled = appear > 0.01f;
                panelDoor.transform.position = panelDoorStart + new Vector3(0f, 0.015f * (1f - closing), 0f);
                panelDoor.transform.localScale = new Vector3(Mathf.Lerp(0.02f, panelDoorScale.x, closing), panelDoorScale.y, panelDoorScale.z);
                Color doorColor = Color.white;
                doorColor.a = appear * Mathf.Lerp(0.50f, 1f, closing);
                panelDoor.color = doorColor;
            }
        }

        void UpdateEntrance(float time, bool exterior)
        {
            if (!exterior) return;
            float reveal = Smooth(Mathf.InverseLerp(8.62f, 8.92f, time));
            for (int i = 0; i < entranceTeam.Length; i++)
            {
                Transform actor = entranceTeam[i];
                if (actor == null) continue;
                Vector3 start = i < entranceTeamStarts.Length ? entranceTeamStarts[i] : actor.position;
                actor.position = start + new Vector3(0.025f * reveal, 0.008f * Mathf.Sin(time * 2f + i), 0f);
                if (i < entranceTeamRenderers.Length && entranceTeamRenderers[i] != null)
                {
                    Color c = i < entranceTeamStartColors.Length ? entranceTeamStartColors[i] : Color.white;
                    c.a *= reveal;
                    entranceTeamRenderers[i].color = c;
                }
                if (i < entranceTeamShadows.Length && entranceTeamShadows[i] != null)
                {
                    Color shadowColor = i < entranceTeamShadowStartColors.Length ? entranceTeamShadowStartColors[i] : Color.white;
                    shadowColor.a *= reveal;
                    entranceTeamShadows[i].color = shadowColor;
                }
            }
            if (entranceTeamRenderers.Length > 0 && entranceTeamRenderers[0] != null)
                SetFrame(entranceTeamRenderers[0], rectorIdleFrames, Mathf.FloorToInt(time * 3.8f));
        }

        void UpdateClock(float time)
        {
            float alpha = Smooth(Mathf.InverseLerp(0.28f, 0.60f, time));
            if (clockHud != null)
            {
                Color c = clockHud.color;
                c.a = 0.84f * alpha;
                clockHud.color = c;
            }
            SetTextAlpha(clockHudLabel, alpha);
            SetTextAlpha(clockHudValue, alpha);
            if (clockHudValue != null) clockHudValue.text = globalClockValue;
        }

        void UpdateSubtitle(float time, bool playVoice)
        {
            CC10SubtitleCue cue = null;
            int index = -1;
            if (subtitles != null)
                for (int i = 0; i < subtitles.Length; i++)
                    if (subtitles[i] != null && time >= subtitles[i].start && time < subtitles[i].end)
                    { cue = subtitles[i]; index = i; break; }

            float alpha = cue == null ? 0f : Smooth(Mathf.InverseLerp(cue.start, cue.start + 0.12f, time)) *
                (1f - Smooth(Mathf.InverseLerp(cue.end - 0.14f, cue.end, time)));
            if (subtitleText != null)
            {
                subtitleText.enabled = cue != null;
                if (index != activeSubtitle)
                    subtitleText.text = cue == null ? string.Empty : cue.speaker + "\n“" + cue.text + "”";
                Color c = subtitleText.color;
                c.a = alpha;
                subtitleText.color = c;
            }
            if (subtitleBackground != null)
            {
                Color c = subtitleBackground.color;
                c.a = 0.80f * alpha;
                subtitleBackground.color = c;
            }
            if (index != activeSubtitle)
            {
                activeSubtitle = index;
                if (playVoice && voiceSource != null && cue != null && cue.voice != null)
                {
                    voiceSource.Stop();
                    voiceSource.PlayOneShot(cue.voice, 1f);
                }
            }
        }

        void UpdateResult(float time)
        {
            float fadeToBlack = Smooth(Mathf.InverseLerp(12.02f, 12.28f, time));
            float fadeFromBlack = Smooth(Mathf.InverseLerp(12.28f, 12.64f, time));
            if (fadeImage != null)
                fadeImage.color = new Color(0f, 0f, 0f, fadeToBlack * (1f - fadeFromBlack));

            float visible = Smooth(Mathf.InverseLerp(12.34f, 12.76f, time));
            if (resultBackdrop != null)
            {
                Color c = resultBackdrop.color;
                c.a = 0.79f * visible;
                resultBackdrop.color = c;
            }
            if (resultCanvasGroup != null)
            {
                resultCanvasGroup.alpha = visible;
                resultCanvasGroup.interactable = visible >= 0.95f && time >= 13.00f;
                resultCanvasGroup.blocksRaycasts = visible >= 0.95f && time >= 13.00f;
            }
            bool canChoose = visible >= 0.95f && time >= 13.00f;
            if (retryButton != null) retryButton.interactable = canChoose;
            if (menuButton != null) menuButton.interactable = canChoose;
        }

        void UpdateAudio()
        {
            if (!clockCuePlayed && elapsed >= 0.12f)
            {
                clockCuePlayed = true;
                if (effectsSource != null && clockChime != null) effectsSource.PlayOneShot(clockChime, 0.10f);
            }
            if (!radioCuePlayed && elapsed >= 7.12f)
            {
                radioCuePlayed = true;
                if (effectsSource != null && radioClick != null) effectsSource.PlayOneShot(radioClick, 0.12f);
            }
            if (!outsideAmbienceStarted && elapsed >= ExteriorShotStart)
            {
                outsideAmbienceStarted = true;
                if (ambienceSource != null && exteriorAmbience != null)
                {
                    ambienceSource.Stop();
                    ambienceSource.clip = exteriorAmbience;
                    ambienceSource.loop = true;
                    ambienceSource.volume = 0.10f;
                    ambienceSource.Play();
                }
            }
            if (!latchCuePlayed && elapsed >= 11.50f)
            {
                latchCuePlayed = true;
                if (effectsSource != null && panelLatch != null) effectsSource.PlayOneShot(panelLatch, 0.12f);
            }
            if (!noteCuePlayed && elapsed >= 12.68f)
            {
                noteCuePlayed = true;
                if (effectsSource != null && gentleEndingNote != null) effectsSource.PlayOneShot(gentleEndingNote, 0.16f);
            }
            if (!musicCuePlayed && elapsed >= 3f && musicSource != null && gentleMusic != null)
            {
                musicCuePlayed = true;
                musicSource.clip = gentleMusic;
                musicSource.loop = false;
                musicSource.volume = 0.10f;
                musicSource.Play();
            }
        }

        void Finish()
        {
            elapsed = Duration;
            playing = false;
            finished = true;
            ControlsLocked = true;
            EvaluateVisuals(Duration, true);
            if (retryButton != null) retryButton.interactable = true;
            if (menuButton != null) menuButton.interactable = true;
            if (resultCanvasGroup != null) { resultCanvasGroup.interactable = true; resultCanvasGroup.blocksRaycasts = true; }
            onCutsceneFinished?.Invoke();
        }

        static Vector3[] CapturePositions(Transform[] objects)
        {
            if (objects == null) return Array.Empty<Vector3>();
            var values = new Vector3[objects.Length];
            for (int i = 0; i < objects.Length; i++) values[i] = objects[i] != null ? objects[i].position : Vector3.zero;
            return values;
        }

        static Color[] CaptureColors(SpriteRenderer[] renderers)
        {
            if (renderers == null) return Array.Empty<Color>();
            var values = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) values[i] = renderers[i] != null ? renderers[i].color : Color.white;
            return values;
        }

        static void RestorePositions(Transform[] objects, Vector3[] values)
        {
            if (objects == null || values == null) return;
            for (int i = 0; i < objects.Length && i < values.Length; i++) if (objects[i] != null) objects[i].position = values[i];
        }

        static void RestoreColors(SpriteRenderer[] renderers, Color[] values, float alpha)
        {
            if (renderers == null || values == null) return;
            for (int i = 0; i < renderers.Length && i < values.Length; i++)
                if (renderers[i] != null) { Color c = values[i]; c.a = alpha; renderers[i].color = c; }
        }

        static void SetFrame(SpriteRenderer renderer, Sprite[] frames, int index)
        {
            if (renderer == null || frames == null || frames.Length == 0) return;
            Sprite frame = frames[Mathf.Abs(index) % frames.Length];
            if (frame != null) renderer.sprite = frame;
        }

        static void SetTextAlpha(Text text, float alpha)
        {
            if (text == null) return;
            Color c = text.color;
            c.a = alpha;
            text.color = c;
        }

        static float Smooth(float value) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(value));
    }
}
