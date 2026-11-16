using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Misterio.Cutscenes
{
    [Serializable]
    public sealed class CC07SubtitleCue
    {
        public float start;
        public float end;
        public string speaker;
        [TextArea] public string text;
        public AudioClip voice;
    }

    /// <summary>Fifteen-second memory sequence explaining Moema's accidental trail.</summary>
    public sealed class CC07CutsceneController : MonoBehaviour
    {
        public const float Duration = 15f;
        const float PresentCut = 3f;
        const float StatueCut = 3.98f;
        const float PaintingCut = 5.14f;
        const float CableCut = 6.68f;
        const float ReturnCut = 11f;

        public bool ControlsLocked { get; private set; }
        public float Elapsed => elapsed;
        public string GlobalClockValue => activeClockValue;

        [Header("Planos e câmera")]
        [SerializeField] Camera sceneCamera;
        [SerializeField] GameObject[] sceneRoots;
        [SerializeField] Transform doorLeaf;
        [SerializeField] Transform bumpedStatue;
        [SerializeField] Transform displacedPainting;
        [SerializeField] SpriteRenderer looseCable;
        [SerializeField] Transform connector;

        [Header("Segurança")]
        [SerializeField] Transform guard;
        [SerializeField] SpriteRenderer guardRenderer;
        [SerializeField] Sprite[] guardIdleFront;
        [SerializeField] Sprite[] guardIdleBack;

        [Header("Moema e mariposa")]
        [SerializeField] Transform[] moemaByScene;
        [SerializeField] SpriteRenderer[] moemaRenderers;
        [SerializeField] Transform returningMoema;
        [SerializeField] SpriteRenderer returningMoemaRenderer;
        [SerializeField] Transform[] mothByScene;
        [SerializeField] SpriteRenderer[] mothRenderers;
        [SerializeField] Sprite[] moemaIdleFront;
        [SerializeField] Sprite[] moemaWalkFront;
        [SerializeField] Sprite[] moemaWalkBack;
        [SerializeField] Sprite[] moemaAlertFront;
        [SerializeField] SpriteRenderer featherAtPanel;
        [SerializeField] SpriteRenderer featherAtCable;

        [Header("Luz, lembrança e legendas")]
        [SerializeField] SpriteRenderer panelGlow;
        [SerializeField] SpriteRenderer memoryRoomLight;
        [SerializeField] Image subtitleBackground;
        [SerializeField] Text subtitleText;
        [SerializeField] Image fadeImage;
        [SerializeField] Image memoryTint;
        [SerializeField] Image memoryBorder;
        [SerializeField] Text memoryLabel;
        [SerializeField] Image blackoutOverlay;
        [SerializeField] CC07SubtitleCue[] subtitles =
        {
            new CC07SubtitleCue { start = 0.12f, end = 2.82f, speaker = "NARRADOR", text = "Aquela pequena visitante era a pista que faltava." },
            new CC07SubtitleCue { start = 3.04f, end = 4.58f, speaker = "NARRADOR", text = "A porta ficara aberta durante os preparativos." },
            new CC07SubtitleCue { start = 4.64f, end = 6.58f, speaker = "NARRADOR", text = "Moema seguiu o inseto, esbarrando nas obras." },
            new CC07SubtitleCue { start = 6.82f, end = 10.86f, speaker = "NARRADOR", text = "No salão, ela mordeu um cabo solto ao tentar alcançá-lo." },
            new CC07SubtitleCue { start = 11.12f, end = 13.08f, speaker = "NARRADOR", text = "Não houve plano nem maldade. Foi curiosidade — e um susto no escuro." },
            new CC07SubtitleCue { start = 13.22f, end = 14.88f, speaker = "SEGURANÇA", text = "Então era só isso..." }
        };

        [Header("Som")]
        [SerializeField] AudioSource voiceSource;
        [SerializeField] AudioSource ambienceSource;
        [SerializeField] AudioSource effectsSource;
        [SerializeField] AudioSource musicSource;
        [SerializeField] AudioClip electricalHum;
        [SerializeField] AudioClip memoryMusic;
        [SerializeField] AudioClip doorCreak;
        [SerializeField] AudioClip softSteps;
        [SerializeField] AudioClip woodTap;
        [SerializeField] AudioClip paintingSlide;
        [SerializeField] AudioClip cableClick;
        [SerializeField] AudioClip electricalSnap;
        [SerializeField] AudioClip featherRustle;

        [Header("Integração")]
        [SerializeField] bool playOnAwake = true;
        [SerializeField] UnityEvent onCutsceneStarted = new UnityEvent();
        [SerializeField] UnityEvent onCutsceneFinished = new UnityEvent();
        [SerializeField, HideInInspector] bool startPausedForEditorReview;

        float elapsed;
        double lastRealtime;
        Vector3 cameraStart;
        float cameraStartSize;
        Vector3 guardStart;
        Vector3 guardScale;
        Vector3[] moemaStarts;
        Vector3[] moemaScales;
        Vector3[] mothStarts;
        Vector3[] mothScales;
        Vector3 returnMoemaStart;
        Vector3 returnMoemaScale;
        Vector3 statueStart;
        Quaternion statueStartRotation;
        Vector3 paintingStart;
        Quaternion paintingStartRotation;
        Vector3 connectorStart;
        Quaternion doorStartRotation;
        int activeScene = -1;
        int activeSubtitle = -1;
        string activeClockValue = "--:--";
        bool playing;
        bool finished;
        bool[] playedCues = new bool[8];

        void Awake()
        {
            cameraStart = sceneCamera != null ? sceneCamera.transform.position : new Vector3(-3.2f, 0.6f, -10f);
            cameraStartSize = sceneCamera != null ? sceneCamera.orthographicSize : 1.35f;
            guardStart = guard != null ? guard.position : new Vector3(-2.24f, -1.18f, -0.08f);
            guardScale = guard != null ? guard.localScale : Vector3.one;
            moemaStarts = CapturePositions(moemaByScene);
            moemaScales = CaptureScales(moemaByScene);
            mothStarts = CapturePositions(mothByScene);
            mothScales = CaptureScales(mothByScene);
            returnMoemaStart = returningMoema != null ? returningMoema.position : new Vector3(1.6f, 0.82f, -0.06f);
            returnMoemaScale = returningMoema != null ? returningMoema.localScale : Vector3.one;
            statueStart = bumpedStatue != null ? bumpedStatue.position : Vector3.zero;
            statueStartRotation = bumpedStatue != null ? bumpedStatue.rotation : Quaternion.identity;
            paintingStart = displacedPainting != null ? displacedPainting.position : Vector3.zero;
            paintingStartRotation = displacedPainting != null ? displacedPainting.rotation : Quaternion.identity;
            connectorStart = connector != null ? connector.position : Vector3.zero;
            doorStartRotation = doorLeaf != null ? doorLeaf.rotation : Quaternion.identity;
            ControlsLocked = playOnAwake || startPausedForEditorReview;
            lastRealtime = Time.realtimeSinceStartupAsDouble;
            ResetVisuals();
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

        /// <summary>The global run clock is frozen by the caller for the complete sequence.</summary>
        public void SetGlobalClock(string value)
        {
            activeClockValue = string.IsNullOrWhiteSpace(value) ? "--:--" : value.Trim();
        }

#if UNITY_EDITOR
        public void PrepareEditorFrameReviewCapture()
        {
            playing = false;
            finished = false;
            elapsed = 0f;
            ControlsLocked = true;
            activeSubtitle = -1;
            Array.Clear(playedCues, 0, playedCues.Length);
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
            Array.Clear(playedCues, 0, playedCues.Length);
            activeScene = -1;
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
                ambienceSource.volume = 0.045f;
                ambienceSource.Play();
            }
            if (musicSource != null && memoryMusic != null)
            {
                musicSource.clip = memoryMusic;
                musicSource.loop = false;
                musicSource.volume = 0.045f;
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
            activeScene = -1;
            if (sceneCamera != null)
            {
                sceneCamera.transform.position = cameraStart;
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
                guardRenderer.flipX = false;
                guardRenderer.color = Color.white;
                SetFrame(guardRenderer, guardIdleBack, 0);
            }
            RestoreTransforms(moemaByScene, moemaStarts, moemaScales);
            RestoreTransforms(mothByScene, mothStarts, mothScales);
            if (moemaRenderers != null)
                foreach (SpriteRenderer renderer in moemaRenderers)
                    if (renderer != null) { renderer.enabled = true; renderer.color = Color.white; renderer.flipX = false; }
            if (mothRenderers != null)
                foreach (SpriteRenderer renderer in mothRenderers)
                    if (renderer != null) { renderer.enabled = true; renderer.color = Color.white; }
            if (returningMoema != null)
            {
                returningMoema.position = returnMoemaStart;
                returningMoema.localScale = returnMoemaScale;
                returningMoema.rotation = Quaternion.identity;
                returningMoema.gameObject.SetActive(true);
            }
            if (returningMoemaRenderer != null)
            {
                returningMoemaRenderer.enabled = true;
                returningMoemaRenderer.color = new Color(1f, 1f, 1f, 0f);
            }
            if (doorLeaf != null) doorLeaf.rotation = doorStartRotation;
            if (bumpedStatue != null)
            {
                bumpedStatue.position = statueStart;
                bumpedStatue.rotation = statueStartRotation;
            }
            if (displacedPainting != null)
            {
                displacedPainting.position = paintingStart;
                displacedPainting.rotation = paintingStartRotation;
            }
            if (connector != null)
            {
                connector.position = connectorStart;
                connector.rotation = Quaternion.identity;
                connector.gameObject.SetActive(true);
            }
            if (looseCable != null) { looseCable.enabled = true; looseCable.color = Color.white; }
            if (featherAtPanel != null) featherAtPanel.enabled = true;
            if (featherAtCable != null) featherAtCable.enabled = true;
            if (panelGlow != null) panelGlow.color = new Color(0.74f, 0.82f, 1f, 0.17f);
            if (memoryRoomLight != null) memoryRoomLight.color = new Color(1f, 0.74f, 0.48f, 0.10f);
            if (subtitleText != null) { subtitleText.text = string.Empty; subtitleText.enabled = false; subtitleText.color = Color.white; }
            if (subtitleBackground != null) subtitleBackground.enabled = false;
            if (memoryTint != null) memoryTint.color = new Color(1f, 0.62f, 0.30f, 0f);
            if (memoryBorder != null) memoryBorder.color = new Color(1f, 0.70f, 0.38f, 0f);
            if (memoryLabel != null) { memoryLabel.text = "LEMBRANÇA"; memoryLabel.enabled = false; memoryLabel.color = Color.white; }
            if (blackoutOverlay != null) blackoutOverlay.color = new Color(0.015f, 0.025f, 0.06f, 0f);
            if (fadeImage != null) fadeImage.color = new Color(0f, 0f, 0f, 0f);
            SetSceneRoot(0);
        }

        void EvaluateVisuals(float time, bool playVoice)
        {
            int sceneIndex = SceneAt(time);
            SetSceneRoot(sceneIndex);
            UpdateCamera(time, sceneIndex);
            UpdateDoor(time, sceneIndex);
            UpdateStatue(time, sceneIndex);
            UpdatePainting(time, sceneIndex);
            UpdateGuard(time, sceneIndex);
            UpdateMoema(time, sceneIndex);
            UpdateMoth(time, sceneIndex);
            UpdateCable(time, sceneIndex);
            UpdateMemoryTreatment(time, sceneIndex);
            UpdateSubtitle(time, playVoice);
        }

        void SetSceneRoot(int sceneIndex)
        {
            activeScene = sceneIndex;
            if (sceneRoots == null) return;
            for (int i = 0; i < sceneRoots.Length; i++)
                if (sceneRoots[i] != null && sceneRoots[i].activeSelf != (i == sceneIndex))
                    sceneRoots[i].SetActive(i == sceneIndex);
        }

        void UpdateCamera(float time, int sceneIndex)
        {
            if (sceneCamera == null) return;
            Vector3 position;
            float size;
            if (sceneIndex == 0 && time < ReturnCut)
            {
                position = new Vector3(-2.55f, 0.68f, -10f);
                size = 1.38f;
            }
            else if (sceneIndex == 0)
            {
                float u = Smooth(Mathf.InverseLerp(ReturnCut, 11.72f, time));
                position = new Vector3(0f, 0f, -10f);
                size = Mathf.Lerp(2.32f, 2.25f, u);
            }
            else if (sceneIndex == 1)
            {
                position = new Vector3(0f, 0.06f, -10f);
                size = 1.10f;
            }
            else if (sceneIndex == 2)
            {
                position = new Vector3(0f, -0.15f, -10f);
                size = 0.88f;
            }
            else if (sceneIndex == 3)
            {
                position = new Vector3(0.12f, 0.10f, -10f);
                size = 0.82f;
            }
            else
            {
                position = new Vector3(2.42f, 0.52f, -10f);
                size = 1.12f;
            }
            sceneCamera.transform.position = position;
            sceneCamera.orthographicSize = size;
        }

        void UpdateDoor(float time, int sceneIndex)
        {
            if (doorLeaf == null) return;
            if (sceneIndex != 1) { doorLeaf.rotation = doorStartRotation; return; }
            float u = Smooth(Mathf.InverseLerp(3.0f, 3.26f, time));
            doorLeaf.rotation = doorStartRotation * Quaternion.Euler(0f, 0f, -2f * u);
        }

        void UpdateStatue(float time, int sceneIndex)
        {
            if (bumpedStatue == null) return;
            if (sceneIndex != 2)
            {
                bumpedStatue.position = statueStart;
                bumpedStatue.rotation = statueStartRotation;
                return;
            }
            float bump = Mathf.Sin(Mathf.Clamp01(Mathf.InverseLerp(4.70f, 5.02f, time)) * Mathf.PI);
            bumpedStatue.position = statueStart + new Vector3(0.035f * bump, -0.022f * bump, 0f);
            bumpedStatue.rotation = statueStartRotation * Quaternion.Euler(0f, 0f, -2.8f * bump);
        }

        void UpdatePainting(float time, int sceneIndex)
        {
            if (displacedPainting == null) return;
            if (sceneIndex != 3)
            {
                displacedPainting.position = paintingStart;
                displacedPainting.rotation = paintingStartRotation;
                return;
            }
            float slide = Smooth(Mathf.InverseLerp(5.62f, 6.48f, time));
            float settle = time > 6.48f ? Mathf.Sin(Mathf.Clamp01((time - 6.48f) / 0.18f) * Mathf.PI) * 0.012f : 0f;
            displacedPainting.position = paintingStart + new Vector3(0.78f * slide, -0.24f * slide + settle, 0f);
            displacedPainting.rotation = paintingStartRotation * Quaternion.Euler(0f, 0f, -Mathf.Lerp(0f, 12f, slide));
        }

        void UpdateGuard(float time, int sceneIndex)
        {
            if (guard == null || guardRenderer == null) return;
            if (sceneIndex != 0 || time < ReturnCut)
            {
                guardRenderer.enabled = sceneIndex == 0 && time >= ReturnCut;
                if (guardRenderer.enabled) SetFrame(guardRenderer, guardIdleFront, Mathf.FloorToInt(time * 2f));
                return;
            }
            guardRenderer.enabled = true;
            SetFrame(guardRenderer, guardIdleFront, Mathf.FloorToInt(time * 2f));
            guard.rotation = Quaternion.Euler(0f, 0f, -0.3f * Mathf.Sin(time * 1.8f));
            float understanding = Smooth(Mathf.InverseLerp(12.35f, 13.15f, time));
            guard.localScale = new Vector3(guardScale.x, guardScale.y * (1f - 0.018f * understanding), guardScale.z);
        }

        void UpdateMoema(float time, int sceneIndex)
        {
            if (moemaRenderers != null)
                for (int i = 1; i < moemaRenderers.Length; i++)
                    if (moemaRenderers[i] != null) moemaRenderers[i].enabled = i == sceneIndex;
            if (sceneIndex > 0 && moemaByScene != null && sceneIndex < moemaByScene.Length)
                UpdateMemoryMoema(time, sceneIndex, moemaByScene[sceneIndex], moemaRenderers[sceneIndex]);

            if (returningMoema == null || returningMoemaRenderer == null) return;
            bool visible = sceneIndex == 0 && time >= ReturnCut && time < 12.72f;
            returningMoemaRenderer.enabled = visible;
            if (!visible)
            {
                returningMoema.position = returnMoemaStart;
                returningMoema.localScale = returnMoemaScale;
                return;
            }
            float flight = Smooth(Mathf.InverseLerp(11.18f, 12.55f, time));
            Vector3 end = new Vector3(2.42f, 1.25f, returnMoemaStart.z);
            returningMoema.position = Vector3.Lerp(returnMoemaStart, end, flight) + Vector3.up * (0.045f * Mathf.Sin(flight * Mathf.PI));
            returningMoema.localScale = returnMoemaScale * Mathf.Lerp(1f, 0.77f, flight);
            SetFrame(returningMoemaRenderer, moemaWalkBack, Mathf.FloorToInt((time - 11.18f) * 15f));
            Color c = Color.white;
            c.a = 1f - Smooth(Mathf.InverseLerp(12.40f, 12.72f, time));
            returningMoemaRenderer.color = c;
        }

        void UpdateMemoryMoema(float time, int sceneIndex, Transform character, SpriteRenderer renderer)
        {
            if (character == null || renderer == null) return;
            float duration = sceneIndex == 1 ? 0.98f : sceneIndex == 2 ? 1.16f : sceneIndex == 3 ? 1.54f : 4.32f;
            float local = Mathf.Clamp01((time - SceneStart(sceneIndex)) / duration);
            Vector3 start = moemaStarts[sceneIndex];
            Vector3 target = start;
            bool walking = true;
            bool alert = false;
            float alpha = 1f;
            if (sceneIndex == 1)
            {
                target = new Vector3(-0.50f, start.y - 0.05f, start.z);
                character.position = Vector3.Lerp(start, target, Smooth(local));
            }
            else if (sceneIndex == 2)
            {
                float bump = Smooth(Mathf.InverseLerp(4.64f, 4.79f, time));
                target = new Vector3(-0.10f, start.y, start.z);
                character.position = Vector3.Lerp(start, target, Smooth(Mathf.Min(1f, local * 1.15f))) + Vector3.left * (0.12f * bump);
                walking = time < 4.70f || time > 4.96f;
            }
            else if (sceneIndex == 3)
            {
                target = new Vector3(0.30f, start.y, start.z);
                character.position = Vector3.Lerp(start, target, Smooth(local));
                walking = time < 5.93f;
            }
            else
            {
                if (time < 8.28f)
                {
                    target = start + new Vector3(0.18f, 0.43f, 0f);
                    float approach = Smooth(Mathf.InverseLerp(6.68f, 8.22f, time));
                    character.position = Vector3.Lerp(start, target, approach) + Vector3.up * (0.12f * Mathf.Sin(approach * Mathf.PI));
                    walking = time < 8.02f;
                }
                else if (time < 8.70f)
                {
                    float peck = Mathf.Sin(Mathf.Clamp01(Mathf.InverseLerp(8.28f, 8.70f, time)) * Mathf.PI);
                    character.position = start + new Vector3(0.18f, 0.43f - 0.028f * peck, 0f);
                    character.rotation = Quaternion.Euler(0f, 0f, 2.6f * peck);
                    walking = false;
                }
                else
                {
                    alert = time < 9.30f;
                    float escape = Smooth(Mathf.InverseLerp(9.14f, 10.72f, time));
                    Vector3 escapeStart = start + new Vector3(0.18f, 0.43f, 0f);
                    Vector3 escapeEnd = new Vector3(4.75f, -0.12f, start.z);
                    character.position = SampleSmoothPath(escapeStart,
                        new Vector3(2.78f, -0.10f, start.z),
                        new Vector3(3.78f, -0.14f, start.z), escapeEnd, escape) +
                        Vector3.up * (0.018f * Mathf.Sin(escape * Mathf.PI * 8f));
                    float fade = Smooth(Mathf.InverseLerp(10.52f, 10.78f, time));
                    alpha = 1f - fade;
                    walking = !alert;
                }
            }

            character.localScale = moemaScales[sceneIndex] * (1f + 0.010f * Mathf.Sin(time * 2.8f));
            if (sceneIndex != 4 || time < 8.70f) character.rotation = Quaternion.Euler(0f, 0f, 0.7f * Mathf.Sin(time * 1.6f));
            renderer.flipX = sceneIndex == 4 && time >= 9.14f;
            SetFrame(renderer, alert ? moemaAlertFront : (walking ? moemaWalkFront : moemaIdleFront),
                Mathf.FloorToInt((time - SceneStart(sceneIndex)) * (alert ? 7f : 14f)));
            Color tint = Color.white;
            tint.a = alpha;
            renderer.color = tint;
            renderer.enabled = alpha > 0.02f;
        }

        void UpdateMoth(float time, int sceneIndex)
        {
            if (mothByScene == null || mothRenderers == null) return;
            for (int i = 0; i < mothRenderers.Length; i++)
                if (mothRenderers[i] != null) mothRenderers[i].enabled = i == sceneIndex;
            if (sceneIndex >= mothByScene.Length || mothByScene[sceneIndex] == null || mothRenderers[sceneIndex] == null) return;
            Transform moth = mothByScene[sceneIndex];
            SpriteRenderer renderer = mothRenderers[sceneIndex];
            Vector3 start = mothStarts[sceneIndex];
            float local;
            if (sceneIndex == 0 && time < ReturnCut) local = Mathf.InverseLerp(0f, PresentCut, time);
            else if (sceneIndex == 0) local = Mathf.InverseLerp(ReturnCut, 12.55f, time);
            else local = Mathf.InverseLerp(SceneStart(sceneIndex), SceneEnd(sceneIndex), time);
            Vector3 end = start;
            if (sceneIndex == 0 && time < ReturnCut) end = new Vector3(-2.10f, 1.17f, start.z);
            else if (sceneIndex == 0) end = new Vector3(2.62f, 1.00f, start.z);
            else if (sceneIndex == 1) end = new Vector3(-0.35f, 0.34f, start.z);
            else if (sceneIndex == 2) end = new Vector3(0.92f, 0.12f, start.z);
            else if (sceneIndex == 3) end = new Vector3(0.82f, 0.43f, start.z);
            else end = new Vector3(2.78f, 0.92f, start.z);
            moth.position = Vector3.Lerp(start, end, Smooth(local)) + Vector3.up * (0.025f * Mathf.Sin(time * 9f));
            moth.localScale = new Vector3(mothScales[sceneIndex].x,
                mothScales[sceneIndex].y * (0.80f + 0.20f * Mathf.Abs(Mathf.Sin(time * 15f))), mothScales[sceneIndex].z);
            moth.rotation = Quaternion.Euler(0f, 0f, 3.5f * Mathf.Sin(time * 2.6f));
            float alpha = 1f;
            if (sceneIndex == 0 && time >= ReturnCut) alpha = 1f - Smooth(Mathf.InverseLerp(12.35f, 12.82f, time));
            Color color = Color.white;
            color.a = alpha;
            renderer.color = color;
        }

        void UpdateCable(float time, int sceneIndex)
        {
            if (featherAtPanel != null) featherAtPanel.enabled = sceneIndex == 0;
            if (featherAtCable != null) featherAtCable.enabled = sceneIndex == 4;
            if (looseCable != null && sceneIndex == 4)
                looseCable.color = new Color(1f, 1f, 1f, 1f - Smooth(Mathf.InverseLerp(8.42f, 8.68f, time)));
            if (connector != null)
            {
                connector.gameObject.SetActive(sceneIndex != 4 || time < 8.60f);
                if (sceneIndex == 4)
                {
                    float detach = Smooth(Mathf.InverseLerp(8.36f, 8.62f, time));
                    connector.position = connectorStart + new Vector3(0.08f * detach, -0.06f * detach, 0f);
                    connector.rotation = Quaternion.Euler(0f, 0f, -12f * detach);
                }
                else connector.position = connectorStart;
            }
            if (panelGlow != null && sceneIndex == 4)
            {
                float power = 1f - Smooth(Mathf.InverseLerp(8.50f, 8.92f, time));
                panelGlow.color = new Color(0.74f, 0.82f, 1f, 0.18f * power);
            }
            if (memoryRoomLight != null && sceneIndex == 4)
                memoryRoomLight.color = new Color(1f, 0.74f, 0.48f, 0.12f * (1f - Smooth(Mathf.InverseLerp(8.55f, 8.96f, time))));
        }

        void UpdateMemoryTreatment(float time, int sceneIndex)
        {
            bool memory = sceneIndex > 0;
            float warm = memory ? 0.12f : 0f;
            if (memoryTint != null) memoryTint.color = new Color(1f, 0.62f, 0.30f, warm);
            if (memoryBorder != null) memoryBorder.color = new Color(1f, 0.70f, 0.38f, memory ? 0.60f : 0f);
            if (memoryLabel != null) memoryLabel.enabled = memory;
            float blackout = sceneIndex == 4
                ? 0.70f * Smooth(Mathf.InverseLerp(8.62f, 8.95f, time)) * (1f - Smooth(Mathf.InverseLerp(10.72f, 11f, time)))
                : 0f;
            if (blackoutOverlay != null) blackoutOverlay.color = new Color(0.012f, 0.020f, 0.052f, blackout);

            float transition = 0f;
            float[] cuts = { PresentCut, StatueCut, PaintingCut, CableCut, ReturnCut };
            for (int i = 0; i < cuts.Length; i++)
            {
                float pulse = 1f - Smooth(Mathf.InverseLerp(0f, 0.105f, Mathf.Abs(time - cuts[i])));
                transition = Mathf.Max(transition, pulse * 0.58f);
            }
            float opening = 1f - Smooth(Mathf.InverseLerp(0f, 0.20f, time));
            float ending = Smooth(Mathf.InverseLerp(14.76f, Duration, time));
            if (fadeImage != null) fadeImage.color = new Color(0f, 0f, 0f, Mathf.Max(transition, Mathf.Max(opening, ending)));
        }

        void UpdateSubtitle(float time, bool playVoice)
        {
            CC07SubtitleCue cue = null;
            int index = -1;
            if (subtitles != null)
                for (int i = 0; i < subtitles.Length; i++)
                    if (subtitles[i] != null && time >= subtitles[i].start && time < subtitles[i].end)
                    { cue = subtitles[i]; index = i; break; }
            float alpha = cue == null ? 0f : Smooth(Mathf.InverseLerp(cue.start, cue.start + 0.12f, time)) *
                                                       (1f - Smooth(Mathf.InverseLerp(cue.end - 0.13f, cue.end, time)));
            if (subtitleText != null)
            {
                subtitleText.enabled = cue != null;
                if (index != activeSubtitle) subtitleText.text = cue == null ? string.Empty : cue.speaker + "\n“" + cue.text + "”";
                Color color = subtitleText.color;
                color.a = alpha;
                subtitleText.color = color;
            }
            if (subtitleBackground != null)
            {
                subtitleBackground.enabled = cue != null;
                Color color = subtitleBackground.color;
                color.a = 0.80f * alpha;
                subtitleBackground.color = color;
            }
            if (index != activeSubtitle)
            {
                activeSubtitle = index;
                if (playVoice && voiceSource != null)
                {
                    voiceSource.Stop();
                    if (cue != null && cue.voice != null) { voiceSource.clip = cue.voice; voiceSource.Play(); }
                }
            }
        }

        void UpdateAudio()
        {
            PlayCueOnce(0, 0.20f, featherRustle, 0.10f);
            PlayCueOnce(1, 3.00f, doorCreak, 0.18f);
            PlayCueOnce(2, 3.32f, softSteps, 0.10f);
            PlayCueOnce(3, 4.70f, woodTap, 0.18f);
            PlayCueOnce(4, 5.82f, paintingSlide != null ? paintingSlide : woodTap, 0.16f);
            PlayCueOnce(5, 8.38f, cableClick, 0.14f);
            PlayCueOnce(6, 8.62f, electricalSnap, 0.08f);
            PlayCueOnce(7, 11.18f, featherRustle, 0.10f);
            if (musicSource != null && musicSource.isPlaying)
            {
                float duck = activeSubtitle >= 0 ? 0.68f : 1f;
                musicSource.volume = Mathf.Lerp(0.045f, 0.070f, Smooth(Mathf.InverseLerp(10.8f, 14.7f, elapsed))) * duck;
            }
        }

        void PlayCueOnce(int index, float time, AudioClip clip, float volume)
        {
            if (playedCues[index] || elapsed < time) return;
            playedCues[index] = true;
            if (effectsSource != null && clip != null) effectsSource.PlayOneShot(clip, volume);
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

        static int SceneAt(float time)
        {
            if (time < PresentCut) return 0;
            if (time < StatueCut) return 1;
            if (time < PaintingCut) return 2;
            if (time < CableCut) return 3;
            if (time < ReturnCut) return 4;
            return 0;
        }

        static float SceneStart(int index)
        {
            if (index == 1) return PresentCut;
            if (index == 2) return StatueCut;
            if (index == 3) return PaintingCut;
            return CableCut;
        }

        static float SceneEnd(int index)
        {
            if (index == 0) return 3f;
            if (index == 1) return StatueCut;
            if (index == 2) return PaintingCut;
            if (index == 3) return CableCut;
            return ReturnCut;
        }

        static float Smooth(float value) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(value));

        static void SetFrame(SpriteRenderer renderer, Sprite[] frames, int frame)
        {
            if (renderer == null || frames == null || frames.Length == 0) return;
            renderer.sprite = frames[Mathf.Abs(frame) % frames.Length];
        }

        static Vector3[] CapturePositions(Transform[] transforms)
        {
            if (transforms == null) return Array.Empty<Vector3>();
            var result = new Vector3[transforms.Length];
            for (int i = 0; i < result.Length; i++) result[i] = transforms[i] != null ? transforms[i].position : Vector3.zero;
            return result;
        }

        static Vector3[] CaptureScales(Transform[] transforms)
        {
            if (transforms == null) return Array.Empty<Vector3>();
            var result = new Vector3[transforms.Length];
            for (int i = 0; i < result.Length; i++) result[i] = transforms[i] != null ? transforms[i].localScale : Vector3.one;
            return result;
        }

        static void RestoreTransforms(Transform[] transforms, Vector3[] positions, Vector3[] scales)
        {
            if (transforms == null) return;
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i] == null) continue;
                transforms[i].position = positions != null && i < positions.Length ? positions[i] : Vector3.zero;
                transforms[i].localScale = scales != null && i < scales.Length ? scales[i] : Vector3.one;
                transforms[i].rotation = Quaternion.identity;
            }
        }

        static Vector3 SampleSmoothPath(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float progress)
        {
            float t = Mathf.Clamp01(progress);
            float inverse = 1f - t;
            return inverse * inverse * inverse * p0 + 3f * inverse * inverse * t * p1 +
                   3f * inverse * t * t * p2 + t * t * t * p3;
        }
    }
}
