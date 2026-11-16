using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Misterio.Cutscenes
{
    [Serializable]
    public sealed class CC08SubtitleCue
    {
        public float start;
        public float end;
        public string speaker;
        [TextArea] public string text;
        public AudioClip voice;
    }

    /// <summary>Twenty-five-second success epilogue for the opening of the exhibition.</summary>
    public sealed class CC08CutsceneController : MonoBehaviour
    {
        public const float Duration = 25f;
        const float LobbyCut = 4f;
        const float LawnCut = 20f;

        public bool ControlsLocked { get; private set; }
        public float Elapsed => elapsed;
        public string GlobalClockValue => globalClockValue;

        [Header("Planos e câmera")]
        [SerializeField] Camera sceneCamera;
        [SerializeField] GameObject[] sceneRoots;
        [SerializeField] SpriteRenderer exteriorArchitecture;
        [SerializeField] SpriteRenderer exteriorFloor;
        [SerializeField] SpriteRenderer lobbyGlassReflection;
        [SerializeField] Transform exteriorLeftDoor;
        [SerializeField] Transform exteriorRightDoor;
        [SerializeField] Transform lobbyLeftDoor;
        [SerializeField] Transform lobbyRightDoor;

        [Header("Equipe de curadoria")]
        [SerializeField] Transform[] exteriorTeam;
        [SerializeField] SpriteRenderer[] exteriorTeamRenderers;
        [SerializeField] Transform[] lobbyTeam;
        [SerializeField] SpriteRenderer[] lobbyTeamRenderers;
        [SerializeField] Sprite[] rectorIdleFrames;
        [SerializeField] Sprite rectorWaveSprite;

        [Header("Segurança e rádio")]
        [SerializeField] Transform guard;
        [SerializeField] SpriteRenderer guardRenderer;
        [SerializeField] Sprite[] guardIdleFrames;
        [SerializeField] SpriteRenderer guardWaveArm;
        [SerializeField] Transform radio;
        [SerializeField] SpriteRenderer radioScreen;

        [Header("Gramado, Moema e mariposa")]
        [SerializeField] Transform moema;
        [SerializeField] SpriteRenderer moemaRenderer;
        [SerializeField] Sprite[] moemaIdleFrames;
        [SerializeField] SpriteRenderer moemaBlink;
        [SerializeField] Transform landingLeaf;
        [SerializeField] Transform moth;
        [SerializeField] SpriteRenderer mothRenderer;
        [SerializeField] Sprite[] mothFlightFrames;

        [Header("Luz e legendas")]
        [SerializeField] Image subtitleBackground;
        [SerializeField] Text subtitleText;
        [SerializeField] Image lobbyDaylight;
        [SerializeField] Image finalFade;
        [SerializeField] CC08SubtitleCue[] subtitles =
        {
            new CC08SubtitleCue { start = 0.16f, end = 3.78f, speaker = "NARRADOR", text = "Às seis, a madrugada cedeu lugar ao dia." },
            new CC08SubtitleCue { start = 4.12f, end = 8.74f, speaker = "NARRADOR", text = "A exposição estava pronta para receber seus visitantes." },
            new CC08SubtitleCue { start = 9.22f, end = 12.00f, speaker = "CURADORA", text = "Está tudo pronto para a inauguração." },
            new CC08SubtitleCue { start = 12.18f, end = 15.76f, speaker = "SEGURANÇA", text = "Bom dia. Tudo em ordem." },
            new CC08SubtitleCue { start = 16.18f, end = 19.86f, speaker = "NARRADOR", text = "Para o segurança, a madrugada tinha terminado." },
            new CC08SubtitleCue { start = 20.24f, end = 24.55f, speaker = "NARRADOR", text = "No gramado, a mariposa pousou. Moema enfim descansou." }
        };

        [Header("Som")]
        [SerializeField] AudioSource voiceSource;
        [SerializeField] AudioSource ambienceSource;
        [SerializeField] AudioSource effectsSource;
        [SerializeField] AudioSource musicSource;
        [SerializeField] AudioClip exteriorAmbience;
        [SerializeField] AudioClip lobbyAmbience;
        [SerializeField] AudioClip morningTheme;
        [SerializeField] AudioClip footsteps;
        [SerializeField] AudioClip doorOpening;
        [SerializeField] AudioClip radioClick;
        [SerializeField] AudioClip mothWings;
        [SerializeField] AudioClip finalXylophone;

        [Header("Integração")]
        [SerializeField] bool playOnAwake = true;
        [SerializeField] UnityEvent onCutsceneStarted = new UnityEvent();
        [SerializeField] UnityEvent onCutsceneFinished = new UnityEvent();

        float elapsed;
        double lastRealtime;
        string globalClockValue = "06:00";
        int activeScene = -1;
        int activeSubtitle = -1;
        int nextStepCue;
        bool playing;
        bool finished;
        bool doorCuePlayed;
        bool radioCuePlayed;
        bool flightCuePlayed;
        bool landingCuePlayed;
        bool finalCuePlayed;

        Vector3 cameraStartPosition;
        float cameraStartSize;
        Vector3 exteriorArchitectureStart;
        Vector3 exteriorFloorStart;
        Vector3[] doorStartPositions = new Vector3[4];
        Vector3[] exteriorTeamStarts;
        Vector3[] lobbyTeamTargets;
        Vector3[] lobbyTeamStarts;
        Vector3 guardStart;
        Vector3 radioStart;
        Vector3 moemaStart;
        Vector3 moemaScale;
        Vector3 mothStart;
        Vector3 leafStart;

        static readonly float[] FootstepTimes = { 0.28f, 0.78f, 1.28f, 1.78f, 2.28f, 2.78f, 3.25f };

        void Awake()
        {
            cameraStartPosition = sceneCamera != null ? sceneCamera.transform.position : new Vector3(0f, 0f, -10f);
            cameraStartSize = sceneCamera != null ? sceneCamera.orthographicSize : 1.6875f;
            exteriorArchitectureStart = exteriorArchitecture != null ? exteriorArchitecture.transform.position : Vector3.zero;
            exteriorFloorStart = exteriorFloor != null ? exteriorFloor.transform.position : Vector3.zero;
            exteriorTeamStarts = CapturePositions(exteriorTeam);
            lobbyTeamTargets = CapturePositions(lobbyTeam);
            lobbyTeamStarts = new Vector3[lobbyTeamTargets.Length];
            for (int i = 0; i < lobbyTeamTargets.Length; i++) lobbyTeamStarts[i] = lobbyTeamTargets[i] + Vector3.left * (i == 1 ? 0.32f : 0.48f);
            guardStart = guard != null ? guard.position : new Vector3(0.74f, -1.08f, -0.10f);
            radioStart = radio != null ? radio.localPosition : new Vector3(0.12f, -0.10f, -0.04f);
            moemaStart = moema != null ? moema.position : new Vector3(-1.58f, -1.28f, -0.10f);
            moemaScale = moema != null ? moema.localScale : Vector3.one;
            mothStart = moth != null ? moth.position : new Vector3(-0.08f, -0.16f, -0.12f);
            leafStart = landingLeaf != null ? landingLeaf.position : new Vector3(-1.08f, -0.22f, -0.09f);
            if (exteriorLeftDoor != null) doorStartPositions[0] = exteriorLeftDoor.position;
            if (exteriorRightDoor != null) doorStartPositions[1] = exteriorRightDoor.position;
            if (lobbyLeftDoor != null) doorStartPositions[2] = lobbyLeftDoor.position;
            if (lobbyRightDoor != null) doorStartPositions[3] = lobbyRightDoor.position;
            ResetVisuals();
            ControlsLocked = playOnAwake;
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

        /// <summary>The success epilogue represents the 06:00 opening and never advances game time.</summary>
        public void SetGlobalClock(string value)
        {
            globalClockValue = string.IsNullOrWhiteSpace(value) ? "06:00" : value.Trim();
        }

        public void PlayCutscene()
        {
            if (playing && !finished) return;
            Begin();
        }

#if UNITY_EDITOR
        public void PrepareEditorFrameReviewCapture()
        {
            playing = false;
            finished = false;
            elapsed = 0f;
            ControlsLocked = true;
            activeSubtitle = -1;
            nextStepCue = 0;
            doorCuePlayed = radioCuePlayed = flightCuePlayed = landingCuePlayed = finalCuePlayed = false;
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
            nextStepCue = 0;
            doorCuePlayed = radioCuePlayed = flightCuePlayed = landingCuePlayed = finalCuePlayed = false;
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

        void ResetVisuals()
        {
            activeScene = -1;
            if (sceneCamera != null)
            {
                sceneCamera.transform.position = cameraStartPosition;
                sceneCamera.orthographicSize = cameraStartSize;
            }
            if (exteriorArchitecture != null) exteriorArchitecture.transform.position = exteriorArchitectureStart;
            if (exteriorFloor != null) exteriorFloor.transform.position = exteriorFloorStart;
            RestorePositions(exteriorTeam, exteriorTeamStarts);
            RestorePositions(lobbyTeam, lobbyTeamStarts);
            RestoreDoor(exteriorLeftDoor, doorStartPositions[0]);
            RestoreDoor(exteriorRightDoor, doorStartPositions[1]);
            RestoreDoor(lobbyLeftDoor, doorStartPositions[2]);
            RestoreDoor(lobbyRightDoor, doorStartPositions[3]);
            if (guard != null) { guard.position = guardStart; guard.rotation = Quaternion.identity; }
            if (guardWaveArm != null) { guardWaveArm.enabled = false; guardWaveArm.transform.localRotation = Quaternion.identity; }
            if (radio != null) { radio.localPosition = radioStart; radio.gameObject.SetActive(true); }
            if (radioScreen != null) radioScreen.color = new Color(0.46f, 0.78f, 0.98f, 0.18f);
            if (lobbyGlassReflection != null) lobbyGlassReflection.color = new Color(0.73f, 0.90f, 1f, 0.08f);
            if (moema != null) { moema.position = moemaStart; moema.localScale = moemaScale; moema.rotation = Quaternion.identity; }
            if (moemaRenderer != null) { moemaRenderer.enabled = true; moemaRenderer.color = Color.white; }
            if (moemaBlink != null) { moemaBlink.enabled = false; moemaBlink.color = Color.white; }
            if (landingLeaf != null) { landingLeaf.position = leafStart; landingLeaf.rotation = Quaternion.identity; }
            if (moth != null) { moth.position = mothStart; moth.rotation = Quaternion.identity; moth.gameObject.SetActive(true); }
            if (mothRenderer != null) { mothRenderer.enabled = true; mothRenderer.color = Color.white; }
            if (subtitleText != null) { subtitleText.text = string.Empty; subtitleText.enabled = false; subtitleText.color = Color.white; }
            if (subtitleBackground != null) subtitleBackground.color = new Color(0.012f, 0.018f, 0.038f, 0f);
            if (lobbyDaylight != null) lobbyDaylight.color = new Color(1f, 0.92f, 0.78f, 0.025f);
            if (finalFade != null) finalFade.color = new Color(0f, 0f, 0f, 0f);
            SetSceneRoot(0);
        }

        void EvaluateVisuals(float time, bool playVoice)
        {
            int scene = SceneAt(time);
            SetSceneRoot(scene);
            UpdateCamera(time, scene);
            UpdateExterior(time, scene);
            UpdateLobby(time, scene);
            UpdateGarden(time, scene);
            UpdateSubtitle(time, playVoice);
            UpdateFinalFade(time);
        }

        void SetSceneRoot(int index)
        {
            activeScene = index;
            if (sceneRoots == null) return;
            for (int i = 0; i < sceneRoots.Length; i++)
                if (sceneRoots[i] != null && sceneRoots[i].activeSelf != (i == index)) sceneRoots[i].SetActive(i == index);
        }

        void UpdateCamera(float time, int scene)
        {
            if (sceneCamera == null) return;
            Vector3 p;
            float size;
            if (scene == 0)
            {
                float u = Smooth(Mathf.InverseLerp(0f, 3.9f, time));
                p = new Vector3(Mathf.Lerp(-0.10f, 0.12f, u), 0.02f, -10f);
                size = 1.6875f;
            }
            else if (scene == 1 && time < 9f)
            {
                float u = Smooth(Mathf.InverseLerp(4f, 5.05f, time));
                p = Vector3.Lerp(new Vector3(0f, 0.02f, -10f), new Vector3(-0.16f, -0.03f, -10f), u);
                size = Mathf.Lerp(1.60f, 1.48f, u);
            }
            else if (scene == 1 && time < 16f)
            {
                float u = Smooth(Mathf.InverseLerp(9f, 9.70f, time));
                p = Vector3.Lerp(new Vector3(-0.16f, -0.03f, -10f), new Vector3(-0.32f, -0.02f, -10f), u);
                size = 1.43f;
            }
            else if (scene == 1)
            {
                float u = Smooth(Mathf.InverseLerp(16f, 16.9f, time));
                p = Vector3.Lerp(new Vector3(-0.02f, -0.02f, -10f), new Vector3(0.74f, -0.36f, -10f), u);
                size = Mathf.Lerp(1.43f, 1.02f, u);
            }
            else
            {
                float u = Smooth(Mathf.InverseLerp(20f, 21.15f, time));
                p = Vector3.Lerp(new Vector3(0.16f, -0.04f, -10f), new Vector3(-0.70f, -0.23f, -10f), u);
                size = Mathf.Lerp(1.6875f, 1.45f, u);
            }
            sceneCamera.transform.position = p;
            sceneCamera.orthographicSize = size;
        }

        void UpdateExterior(float time, int scene)
        {
            if (exteriorArchitecture != null && scene == 0)
            {
                float warm = Smooth(Mathf.InverseLerp(0f, 3.8f, time));
                exteriorArchitecture.color = Color.Lerp(new Color(0.94f, 0.96f, 1f, 1f), Color.white, warm);
                exteriorArchitecture.transform.position = exteriorArchitectureStart + new Vector3(-0.026f * warm, 0f, 0f);
            }
            if (exteriorFloor != null && scene == 0)
                exteriorFloor.transform.position = exteriorFloorStart + new Vector3(-0.055f * Smooth(Mathf.InverseLerp(0f, 3.8f, time)), 0f, 0f);

            float arrival = Smooth(Mathf.InverseLerp(0.18f, 3.78f, time));
            for (int i = 0; i < exteriorTeam.Length; i++)
            {
                Transform actor = exteriorTeam[i];
                if (actor == null) continue;
                Vector3 start = i < exteriorTeamStarts.Length ? exteriorTeamStarts[i] : actor.position;
                float amount = i == 0 ? 0.26f : i == 1 ? 0.22f : 0.24f;
                float phase = i * 1.8f;
                Vector3 p = start + Vector3.right * amount * arrival;
                p.y += Mathf.Abs(Mathf.Sin(time * 6.2f + phase)) * 0.010f * (1f - Smooth(Mathf.InverseLerp(2.8f, 3.8f, time)));
                actor.position = p;
            }
            if (exteriorTeamRenderers != null && exteriorTeamRenderers.Length > 0)
            {
                SpriteRenderer rector = exteriorTeamRenderers[0];
                if (rector != null)
                {
                    if (time > 2.72f && time < 4f && rectorWaveSprite != null) rector.sprite = rectorWaveSprite;
                    else SetFrame(rector, rectorIdleFrames, Mathf.FloorToInt(time * 5f));
                }
            }
            ApplyDoorPair(exteriorLeftDoor, exteriorRightDoor, Smooth(Mathf.InverseLerp(3.32f, 4.05f, time)));
        }

        void UpdateLobby(float time, int scene)
        {
            float arrival = Smooth(Mathf.InverseLerp(4.05f, 6.55f, time));
            for (int i = 0; i < lobbyTeam.Length; i++)
            {
                Transform actor = lobbyTeam[i];
                if (actor == null) continue;
                Vector3 start = i < lobbyTeamStarts.Length ? lobbyTeamStarts[i] : actor.position;
                Vector3 target = i < lobbyTeamTargets.Length ? lobbyTeamTargets[i] : actor.position;
                Vector3 p = Vector3.Lerp(start, target, arrival);
                float step = (time < 6.55f) ? Mathf.Abs(Mathf.Sin(time * 5.2f + i * 1.6f)) * 0.008f * (1f - arrival) : 0f;
                p.y += step;
                actor.position = p;
            }
            if (lobbyTeamRenderers != null && lobbyTeamRenderers.Length > 0)
            {
                SpriteRenderer rector = lobbyTeamRenderers[0];
                if (rector != null) SetFrame(rector, rectorIdleFrames, Mathf.FloorToInt(time * 4.5f));
                if (lobbyTeamRenderers.Length > 1 && lobbyTeamRenderers[1] != null)
                {
                    float smile = Smooth(Mathf.InverseLerp(8.9f, 9.8f, time));
                    lobbyTeamRenderers[1].color = Color.Lerp(new Color(0.97f, 0.97f, 0.99f, 1f), Color.white, smile);
                }
            }
            if (guardRenderer != null)
            {
                SetFrame(guardRenderer, guardIdleFrames, Mathf.FloorToInt(time * 3.2f));
                if (scene == 1 && time >= 12.18f && time < 13.45f && guard != null)
                {
                    float wave = Mathf.Sin((time - 12.18f) * Mathf.PI * 2.4f);
                    guard.rotation = Quaternion.Euler(0f, 0f, wave * 1.4f);
                    if (guardWaveArm != null)
                    {
                        guardWaveArm.enabled = true;
                        guardWaveArm.transform.localRotation = Quaternion.Euler(0f, 0f, wave * 12f);
                    }
                }
                else if (guard != null)
                {
                    float settle = Smooth(Mathf.InverseLerp(17.10f, 18.15f, time));
                    guard.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0.7f, 0f, settle));
                    if (guardWaveArm != null) guardWaveArm.enabled = false;
                }
            }
            if (radio != null)
            {
                float adjust = Smooth(Mathf.InverseLerp(16.18f, 16.85f, time));
                float release = Smooth(Mathf.InverseLerp(17.35f, 18.10f, time));
                radio.localPosition = radioStart + Vector3.up * (0.075f * adjust - 0.045f * release);
            }
            if (radioScreen != null)
            {
                float glint = Mathf.Clamp01(1f - Mathf.Abs(time - 16.70f) / 0.28f);
                radioScreen.color = new Color(0.46f, 0.78f, 0.98f, 0.16f + glint * 0.48f);
            }
            if (lobbyGlassReflection != null)
            {
                float reflection = Smooth(Mathf.InverseLerp(4.15f, 5.15f, time));
                lobbyGlassReflection.color = new Color(0.76f, 0.92f, 1f, Mathf.Lerp(0.06f, 0.30f, reflection));
            }
            if (lobbyDaylight != null)
            {
                lobbyDaylight.enabled = scene == 1;
                float day = Smooth(Mathf.InverseLerp(4f, 7f, time));
                lobbyDaylight.color = new Color(1f, 0.92f, 0.78f, Mathf.Lerp(0.025f, 0.065f, day));
            }
            float lobbyOpening = Mathf.Lerp(0.60f, 1f, Smooth(Mathf.InverseLerp(4f, 5.05f, time)));
            ApplyDoorPair(lobbyLeftDoor, lobbyRightDoor, lobbyOpening);
        }

        void UpdateGarden(float time, int scene)
        {
            if (scene != 2) return;
            float arrival = Smooth(Mathf.InverseLerp(20.25f, 22.65f, time));
            if (moema != null)
            {
                moema.position = moemaStart + new Vector3(0.08f * arrival, 0.06f * arrival, 0f);
                float settle = Smooth(Mathf.InverseLerp(22.0f, 22.8f, time));
                moema.localScale = new Vector3(moemaScale.x, moemaScale.y * Mathf.Lerp(1f, 0.965f, settle), moemaScale.z);
                moema.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(1.8f, 0f, settle));
            }
            if (moemaRenderer != null) SetFrame(moemaRenderer, moemaIdleFrames, Mathf.FloorToInt(time * 3.1f));
            if (moemaBlink != null)
            {
                bool blink = time >= 23.16f && time < 23.32f;
                moemaBlink.enabled = blink;
            }
            if (landingLeaf != null)
            {
                float settledLeaf = Smooth(Mathf.InverseLerp(22.05f, 22.5f, time));
                landingLeaf.position = leafStart + Vector3.up * (0.015f * Mathf.Sin(time * 2.0f) * settledLeaf);
                landingLeaf.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(time * 1.2f) * 2.0f);
            }
            if (moth != null)
            {
                float u = Smooth(Mathf.InverseLerp(20.22f, 22.34f, time));
                Vector3 p = Vector3.Lerp(mothStart, leafStart + new Vector3(0.02f, 0.075f, -0.03f), u);
                p.y += Mathf.Sin(Mathf.Clamp01(u) * Mathf.PI) * 0.56f;
                moth.position = p;
                float wingScale = 0.92f + Mathf.Abs(Mathf.Sin(time * 10f)) * 0.08f;
                moth.localScale = new Vector3(wingScale, wingScale, 1f) * 0.36f;
                if (mothRenderer != null)
                {
                    bool landed = time >= 22.34f;
                    SetFrame(mothRenderer, mothFlightFrames, landed ? 1 : Mathf.FloorToInt(time * 10f));
                }
            }
        }

        void UpdateSubtitle(float time, bool playVoice)
        {
            bool finalShot = time >= LawnCut;
            if (subtitleBackground != null)
            {
                RectTransform panel = subtitleBackground.rectTransform;
                panel.anchorMin = finalShot ? new Vector2(0f, 1f) : Vector2.zero;
                panel.anchorMax = finalShot ? Vector2.one : new Vector2(1f, 0f);
                panel.offsetMin = finalShot ? new Vector2(0f, -58f) : Vector2.zero;
                panel.offsetMax = finalShot ? Vector2.zero : new Vector2(0f, 58f);
            }
            if (subtitleText != null)
            {
                RectTransform textRect = subtitleText.rectTransform;
                textRect.anchorMin = finalShot ? new Vector2(0.045f, 1f) : new Vector2(0.045f, 0f);
                textRect.anchorMax = finalShot ? new Vector2(0.955f, 1f) : new Vector2(0.955f, 0f);
                textRect.offsetMin = finalShot ? new Vector2(0f, -56f) : new Vector2(0f, 2f);
                textRect.offsetMax = finalShot ? new Vector2(0f, -2f) : new Vector2(0f, 56f);
            }
            int index = -1;
            CC08SubtitleCue cue = null;
            if (subtitles != null)
            {
                for (int i = 0; i < subtitles.Length; i++)
                    if (subtitles[i] != null && time >= subtitles[i].start && time < subtitles[i].end)
                    { cue = subtitles[i]; index = i; break; }
            }
            float alpha = 0f;
            if (cue != null)
            {
                alpha = Mathf.Min(Smooth(Mathf.InverseLerp(cue.start, cue.start + 0.16f, time)),
                    1f - Smooth(Mathf.InverseLerp(cue.end - 0.16f, cue.end, time)));
            }
            if (subtitleText != null)
            {
                subtitleText.enabled = cue != null;
                if (index != activeSubtitle) subtitleText.text = cue == null ? string.Empty : cue.speaker + "\n“" + cue.text + "”";
                Color c = subtitleText.color;
                c.a = alpha;
                subtitleText.color = c;
            }
            if (subtitleBackground != null)
            {
                Color c = subtitleBackground.color;
                c.a = 0.82f * alpha;
                subtitleBackground.color = c;
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

        void UpdateFinalFade(float time)
        {
            if (finalFade == null) return;
            float alpha = Smooth(Mathf.InverseLerp(24.05f, 25f, time));
            finalFade.color = new Color(0f, 0f, 0f, alpha);
        }

        void ApplyDoorPair(Transform left, Transform right, float amount)
        {
            const float slideDistance = 0.28f;
            if (left != null) left.position = doorStartPositions[DoorIndex(left)] + Vector3.left * (slideDistance * amount);
            if (right != null) right.position = doorStartPositions[DoorIndex(right)] + Vector3.right * (slideDistance * amount);
        }

        int DoorIndex(Transform door)
        {
            if (door == exteriorLeftDoor) return 0;
            if (door == exteriorRightDoor) return 1;
            if (door == lobbyLeftDoor) return 2;
            return 3;
        }

        void StartAudio()
        {
            if (ambienceSource != null)
            {
                ambienceSource.clip = exteriorAmbience;
                ambienceSource.loop = true;
                ambienceSource.volume = 0.055f;
                if (ambienceSource.clip != null) ambienceSource.Play();
            }
            if (musicSource != null && morningTheme != null)
            {
                musicSource.clip = morningTheme;
                musicSource.loop = true;
                musicSource.volume = 0.035f;
                musicSource.Play();
            }
        }

        void UpdateAudio()
        {
            if (ambienceSource != null && activeScene != -1)
            {
                AudioClip desired = activeScene == 1 ? lobbyAmbience : exteriorAmbience;
                if (ambienceSource.clip != desired)
                {
                    ambienceSource.Stop();
                    ambienceSource.clip = desired;
                    ambienceSource.loop = true;
                    ambienceSource.volume = activeScene == 2 ? 0.075f : 0.055f;
                    if (desired != null) ambienceSource.Play();
                }
            }
            while (nextStepCue < FootstepTimes.Length && elapsed >= FootstepTimes[nextStepCue])
            {
                if (effectsSource != null && footsteps != null) effectsSource.PlayOneShot(footsteps, 0.085f);
                nextStepCue++;
            }
            PlayCueOnce(ref doorCuePlayed, 4.16f, doorOpening, 0.13f);
            PlayCueOnce(ref radioCuePlayed, 16.37f, radioClick, 0.12f);
            PlayCueOnce(ref flightCuePlayed, 20.48f, mothWings, 0.08f);
            PlayCueOnce(ref landingCuePlayed, 22.25f, mothWings, 0.055f);
            PlayCueOnce(ref finalCuePlayed, 24.42f, finalXylophone, 0.19f);
            if (musicSource != null && musicSource.isPlaying)
            {
                float duck = activeSubtitle >= 0 ? 0.62f : 1f;
                float resolution = Smooth(Mathf.InverseLerp(15.7f, 18.0f, elapsed));
                musicSource.volume = Mathf.Lerp(0.031f, 0.044f, resolution) * duck;
            }
        }

        void PlayCueOnce(ref bool played, float time, AudioClip clip, float volume)
        {
            if (played || elapsed < time) return;
            played = true;
            if (effectsSource != null && clip != null) effectsSource.PlayOneShot(clip, volume);
        }

        void StopAudio()
        {
            if (voiceSource != null) voiceSource.Stop();
            if (ambienceSource != null) ambienceSource.Stop();
            if (effectsSource != null) effectsSource.Stop();
            if (musicSource != null) musicSource.Stop();
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

        static int SceneAt(float time) => time < LobbyCut ? 0 : time < LawnCut ? 1 : 2;
        static float Smooth(float x) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(x));

        static void SetFrame(SpriteRenderer renderer, Sprite[] frames, int frame)
        {
            if (renderer == null || frames == null || frames.Length == 0) return;
            renderer.sprite = frames[Mathf.Abs(frame) % frames.Length];
        }

        static Vector3[] CapturePositions(Transform[] transforms)
        {
            if (transforms == null) return Array.Empty<Vector3>();
            var positions = new Vector3[transforms.Length];
            for (int i = 0; i < positions.Length; i++) positions[i] = transforms[i] != null ? transforms[i].position : Vector3.zero;
            return positions;
        }

        static void RestorePositions(Transform[] transforms, Vector3[] positions)
        {
            if (transforms == null || positions == null) return;
            for (int i = 0; i < transforms.Length && i < positions.Length; i++)
                if (transforms[i] != null) transforms[i].position = positions[i];
        }

        static void RestoreDoor(Transform door, Vector3 position)
        {
            if (door != null) door.position = position;
        }
    }
}
