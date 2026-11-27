using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Misterio.Cutscenes
{
    [Serializable]
    public sealed class CC01SubtitleCue
    {
        public float start;
        public float end;
        public string speaker;
        [TextArea] public string text;
        public AudioClip voice; // Intentionally empty until the narrator and guard are recorded.
    }

    /// <summary>Opening cutscene. Game clock and player input remain locked until the title card ends.</summary>
    public sealed class CC01CutsceneController : MonoBehaviour
    {
        public const float Duration = 35.7f;
        const float WalkStart = 26.35f;
        const float WalkEnd = 31.35f;
        const float CutToBlack = 31.95f;
        const float TitleReveal = 32.62f;
        public bool ControlsLocked { get; private set; } = true;
        public float Elapsed => elapsed;

        [Header("Scene")]
        [SerializeField] Camera sceneCamera;
        [SerializeField] Transform guard;
        [SerializeField] SpriteRenderer guardRenderer;
        [SerializeField] Sprite[] guardIdleFront;
        [SerializeField] Sprite[] guardIdleBack;
        [SerializeField] Sprite[] guardWalkFront;
        [SerializeField] Sprite[] guardWalkBack;
        [SerializeField] SpriteRenderer flashlightBeam;
        [SerializeField] SpriteRenderer[] footprints;
        [SerializeField] SpriteRenderer feather;
        [SerializeField] SpriteRenderer moth;
        [SerializeField] SpriteRenderer mothGlow;
        [SerializeField] SpriteRenderer coffeeCup;
        [SerializeField] SpriteRenderer[] coffeeSteam;
        [SerializeField] SpriteRenderer[] dustMotes;
        [SerializeField] SpriteRenderer[] beamDust;
        [SerializeField] SpriteRenderer serviceDoorGlow;
        [SerializeField] SpriteRenderer stairLightFlash;

        [Header("UI")]
        [SerializeField] Text clockText;
        [SerializeField] Text subtitleText;
        [SerializeField] Text titleText;
        [SerializeField] Image vignetteImage;
        [SerializeField] Image fadeImage;

        [Header("Dialogue - voice clips are recorded later")]
        [SerializeField] CC01SubtitleCue[] subtitles =
        {
            new CC01SubtitleCue { start = 0.55f, end = 3.75f, speaker = "NARRADOR", text = "Às duas da manhã, o Espaço Cultural ainda dormia." },
            new CC01SubtitleCue { start = 4.05f, end = 6.10f, speaker = "NARRADOR", text = "Faltavam quatro horas para a inauguração." },
            new CC01SubtitleCue { start = 10.05f, end = 13.80f, speaker = "NARRADOR", text = "A ronda deveria ser tranquila." },
            new CC01SubtitleCue { start = 14.10f, end = 17.30f, speaker = "NARRADOR", text = "O barulho no andar de cima mudou os planos." },
            new CC01SubtitleCue { start = 17.35f, end = 19.75f, speaker = "SEGURANÇA", text = "Eu só queria uma noite tranquila." },
            new CC01SubtitleCue { start = 20.10f, end = 22.70f, speaker = "NARRADOR", text = "Na poeira, três dedos deixaram a primeira pista." },
            new CC01SubtitleCue { start = 22.75f, end = 25.80f, speaker = "SEGURANÇA", text = "Isso não parece pegada de sapato." },
            new CC01SubtitleCue { start = 26.10f, end = 29.40f, speaker = "NARRADOR", text = "A trilha começava ali, mas sua razão ainda estava escondida." },
            new CC01SubtitleCue { start = 29.45f, end = 31.80f, speaker = "SEGURANÇA", text = "Vamos ver o que aconteceu." }
        };

        [Header("Audio")]
        [SerializeField] AudioSource voiceSource;
        [SerializeField] AudioSource ambientSource;
        [SerializeField] AudioSource effectsSource;
        [SerializeField] AudioSource musicSource;
        [SerializeField] AudioClip ambientHum;
        [SerializeField] AudioClip metallicImpact;
        [SerializeField] AudioClip flashlightClick;
        [SerializeField] AudioClip mothFlutter;
        [SerializeField] AudioClip softXylophone;
        [SerializeField] AudioClip softFootstep;
        [SerializeField] AudioClip uniformRustle;
        [SerializeField] AudioClip evidenceChime;
        [SerializeField] AudioClip tensionRiser;

        [Header("Integration")]
        [SerializeField] UnityEvent onCutsceneFinished = new UnityEvent();

        float elapsed;
        double lastRealtime;
        bool finished;
        bool playedImpact;
        bool playedClick;
        bool playedFlutter;
        bool playedRustle;
        bool playedEvidenceChime;
        bool playedRiser;
        bool playedTitleHit;
        int footstepsPlayed;
        Vector3 guardStart;
        Vector3 cameraStart;
        Vector3 featherStart;
        Vector3[] footprintStarts;
        Vector3[] dustStarts;
        Vector3[] beamDustStarts;
        Vector3[] steamStarts;
        Vector3 guardBaseScale;
        CC01SubtitleCue activeVoiceCue;

        void Awake()
        {
            var soundscape = GetComponent<CC01Soundscape>();
            if (soundscape == null) soundscape = gameObject.AddComponent<CC01Soundscape>();
            if (ambientHum == null) ambientHum = soundscape.Ambient;
            if (metallicImpact == null) metallicImpact = soundscape.Impact;
            if (flashlightClick == null) flashlightClick = soundscape.Click;
            if (mothFlutter == null) mothFlutter = soundscape.Flutter;
            if (softXylophone == null) softXylophone = soundscape.Xylophone;
            if (softFootstep == null) softFootstep = soundscape.Step;
            if (uniformRustle == null) uniformRustle = soundscape.Rustle;
            if (evidenceChime == null) evidenceChime = soundscape.EvidenceChime;
            if (tensionRiser == null) tensionRiser = soundscape.TensionRiser;
            if (sceneCamera == null) sceneCamera = Camera.main;
            guardStart = guard.position;
            guardBaseScale = guard.localScale;
            cameraStart = sceneCamera.transform.position;
            featherStart = feather != null ? feather.transform.position : Vector3.zero;
            footprintStarts = CapturePositions(footprints);
            dustStarts = CapturePositions(dustMotes);
            beamDustStarts = CapturePositions(beamDust);
            steamStarts = CapturePositions(coffeeSteam);
            ControlsLocked = true;
            lastRealtime = Time.realtimeSinceStartupAsDouble;
            if (clockText != null) clockText.text = "02:00";
            if (subtitleText != null) subtitleText.text = string.Empty;
            if (titleText != null) titleText.enabled = false;
            if (flashlightBeam != null) flashlightBeam.enabled = false;
            SetEnabled(footprints, false);
            SetEnabled(dustMotes, true);
            SetEnabled(beamDust, false);
            SetEnabled(coffeeSteam, false);
            if (feather != null) feather.enabled = false;
            if (mothGlow != null) mothGlow.enabled = false;
            if (coffeeCup != null) coffeeCup.enabled = true;
            if (serviceDoorGlow != null) serviceDoorGlow.enabled = false;
            if (stairLightFlash != null) stairLightFlash.enabled = false;
            if (fadeImage != null) fadeImage.color = Color.black;
            if (ambientHum != null && ambientSource != null)
            {
                ambientSource.clip = ambientHum;
                ambientSource.loop = true;
                ambientSource.Play();
            }
            if (softXylophone != null && musicSource != null)
            {
                musicSource.clip = softXylophone;
                musicSource.loop = true;
                musicSource.volume = 0.12f;
                musicSource.Play();
            }
        }

        void Update()
        {
            if (finished) return;
            // A long Editor pause or frame stall must not skip the title card and its sound cue.
            double now = Time.realtimeSinceStartupAsDouble;
            elapsed = Mathf.Min(Duration, elapsed + (float)Math.Min(0.05, Math.Max(0.0, now - lastRealtime)));
            lastRealtime = now;
            UpdateVisuals();
            UpdateSubtitleAndVoice();
            UpdateSoundCues();
            if (elapsed >= Duration) Finish();
        }

        void UpdateVisuals()
        {
            // Small, motivated reframings inside the fixed diorama; no camera rotation or travel outside the room.
            Vector3 target = cameraStart;
            float targetSize = 1f;
            if (elapsed >= 4f && elapsed < 7f) { target += new Vector3(-0.55f, -0.18f, 0f); targetSize = 0.68f; }
            else if (elapsed >= 7f && elapsed < 10f) { target += new Vector3(-0.38f, -0.18f, 0f); targetSize = 0.60f; }
            else if (elapsed >= 10f && elapsed < 14f) { target += new Vector3(0.55f, 0.20f, 0f); targetSize = 0.68f; }
            else if (elapsed >= 14f && elapsed < 20f) { target += new Vector3(-0.30f, -0.12f, 0f); targetSize = 0.62f; }
            else if (elapsed >= 20f && elapsed < 26f) { target += new Vector3(0.12f, -0.38f, 0f); targetSize = 0.58f; }
            else if (elapsed >= 26f)
            {
                float stairFollow = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(29.7f, WalkEnd, elapsed));
                target += new Vector3(0.08f * stairFollow, 0.06f * stairFollow, 0f);
                targetSize = 0.96f;
            }
            float cameraEase = 1f - Mathf.Exp(-Time.unscaledDeltaTime * (elapsed >= 10f && elapsed < 11f ? 5.4f : 3.2f));
            if (elapsed >= 10.30f && elapsed < 10.85f)
            {
                float shock = (1f - Mathf.InverseLerp(10.30f, 10.85f, elapsed)) * 0.012f;
                target += new Vector3(Mathf.Sin(elapsed * 91f) * shock, Mathf.Sin(elapsed * 69f) * shock, 0f);
            }
            sceneCamera.transform.position = Vector3.Lerp(sceneCamera.transform.position, target, cameraEase);
            sceneCamera.orthographicSize = Mathf.Lerp(sceneCamera.orthographicSize, targetSize, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 3f));

            float openingFade = elapsed < 4f ? 1f - Mathf.SmoothStep(0f, 1f, elapsed / 4f) : 0f;
            float impactDim = elapsed >= 10.30f && elapsed < 11.30f
                ? 0.22f * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(10.30f, 11.30f, elapsed))) : 0f;
            float titleCut = elapsed >= CutToBlack ? 1f : 0f;
            float fade = Mathf.Max(Mathf.Max(openingFade, impactDim), titleCut);
            if (fadeImage != null) fadeImage.color = new Color(0f, 0f, 0f, fade);
            if (vignetteImage != null) vignetteImage.color = new Color(1f, 1f, 1f, elapsed >= 10f && elapsed < 14f ? 1f : 0.78f);
            if (clockText != null) clockText.enabled = elapsed >= 0.15f && elapsed < CutToBlack;

            if (coffeeCup != null) coffeeCup.enabled = true;
            if (guardRenderer != null)
            {
                if (elapsed < 10f)
                {
                    SetFrame(guardIdleFront, elapsed >= 7f && elapsed < 8.8f ? 2 : Mathf.FloorToInt(elapsed * 2f) % Mathf.Max(1, guardIdleFront.Length));
                    guardRenderer.flipX = true; // The guard looks toward the clock and coffee on screen-left.
                    float clockGlance = Pulse(elapsed, 4.05f, 0.95f);
                    float coffeeGlance = Pulse(elapsed, 5.15f, 1.4f);
                    float uniformAdjust = Pulse(elapsed, 7.1f, 1.1f);
                    guard.position = guardStart + new Vector3(-0.04f * coffeeGlance, 0.012f * clockGlance + Mathf.Sin(elapsed * 1.6f) * 0.006f - 0.025f * uniformAdjust, 0f);
                    guard.rotation = Quaternion.Euler(0f, 0f, 1.8f * clockGlance - 3f * coffeeGlance + 1.4f * uniformAdjust);
                }
                else if (elapsed < 14f)
                {
                    SetFrame(guardIdleBack, Mathf.FloorToInt(elapsed * 2f) % Mathf.Max(1, guardIdleBack.Length));
                    guardRenderer.flipX = false; // Turn to the upstairs landing at screen-right.
                    guard.position = guardStart + new Vector3(0.02f, 0f, 0f);
                    float turn = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(9.75f, 10.35f, elapsed));
                    guard.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin((elapsed - 10f) * Mathf.PI / 4f) * 1.2f + turn * 1.2f);
                }
                else if (elapsed < 20f)
                {
                    SetFrame(guardIdleBack, Mathf.FloorToInt(elapsed * 2f) % Mathf.Max(1, guardIdleBack.Length));
                    guardRenderer.flipX = false;
                    float approach = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(18f, 19.8f, elapsed));
                    guard.position = guardStart + new Vector3(0.025f + 0.12f * approach, 0.015f * approach, 0f);
                    guard.rotation = Quaternion.Euler(0f, 0f, -1.2f * approach);
                }
                else if (elapsed < 26f)
                {
                    SetFrame(guardIdleFront, 2);
                    guardRenderer.flipX = false;
                    float crouch = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(20f, 20.8f, elapsed));
                    guard.localScale = new Vector3(guardBaseScale.x * Mathf.Lerp(1f, 0.94f, crouch),
                        guardBaseScale.y * Mathf.Lerp(1f, 0.86f, crouch), guardBaseScale.z);
                    guard.position = guardStart + new Vector3(0.08f, -0.035f * crouch, 0f);
                    guard.rotation = Quaternion.Euler(0f, 0f, -3.5f * crouch);
                }
                else
                {
                    guardRenderer.flipX = false;
                    Vector2 pathOffset = GuardPath(elapsed);
                    float standUp = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(26f, WalkStart, elapsed));
                    float climb = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(30.45f, WalkEnd, elapsed));
                    float standingScale = Mathf.Lerp(1f, 0.94f, climb);
                    guard.localScale = new Vector3(guardBaseScale.x * Mathf.Lerp(0.94f, standingScale, standUp),
                        guardBaseScale.y * Mathf.Lerp(0.86f, standingScale, standUp), guardBaseScale.z);
                    bool walking = elapsed >= WalkStart && elapsed < WalkEnd;
                    float stepPhase = Mathf.Repeat((elapsed - WalkStart) * 2f, 1f);
                    SetFrame(walking ? guardWalkBack : guardIdleBack,
                        walking ? Mathf.FloorToInt((elapsed - WalkStart) * 16f) % Mathf.Max(1, guardWalkBack.Length) : 0);
                    float stepBob = walking ? Mathf.Sin(stepPhase * Mathf.PI) * 0.008f : 0f;
                    guard.position = guardStart + new Vector3(pathOffset.x, pathOffset.y + stepBob, 0f);
                    guard.rotation = Quaternion.identity;
                }
            }

            UpdatePropsAndParticles();

            if (flashlightBeam != null)
            {
                flashlightBeam.enabled = elapsed >= 14f && elapsed < CutToBlack;
                if (flashlightBeam.enabled)
                {
                    float reveal = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(14.05f, 14.65f, elapsed));
                    flashlightBeam.color = new Color(1f, 0.98f, 0.88f, reveal * (elapsed >= 20f && elapsed < 26f ? 0.76f : 0.62f));
                    float aimAngle;
                    if (elapsed < 19.45f) aimAngle = Mathf.Lerp(-5f, 12f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(14.05f, 19.45f, elapsed)));
                    else if (elapsed < 21.15f) aimAngle = Mathf.Lerp(12f, -27f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(19.45f, 21.15f, elapsed)));
                    else if (elapsed < 25.75f) aimAngle = Mathf.Lerp(-27f, -7f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(21.15f, 25.75f, elapsed)));
                    else aimAngle = Mathf.Lerp(-7f, 14f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(25.75f, 26.45f, elapsed)));
                    if (elapsed >= 14.65f && elapsed < 19.45f) aimAngle += Mathf.Sin((elapsed - 14.65f) * 1.5f) * 1.2f;
                    Quaternion beamRotation = guard.rotation * Quaternion.Euler(0f, 0f, aimAngle);
                    float beamLength = flashlightBeam.sprite != null
                        ? flashlightBeam.sprite.bounds.size.x * flashlightBeam.transform.localScale.x
                        : 0.72f;
                    Vector3 beamDirection = beamRotation * Vector3.right;
                    // Socket coordinates follow the sprite pivot, pose scale and the drawn hand.
                    bool frontPose = elapsed >= 20f && elapsed < 26f;
                    int walkFrame = elapsed >= WalkStart && elapsed < WalkEnd
                        ? Mathf.FloorToInt((elapsed - WalkStart) * 16f) % Mathf.Max(1, guardWalkBack.Length) : 0;
                    float handSway = elapsed >= WalkStart && elapsed < WalkEnd ? Mathf.Sin(walkFrame * Mathf.PI / 4f) * 0.025f : 0f;
                    Vector3 socket = frontPose ? new Vector3(0.30f, 0.48f, 0f) : new Vector3(0.38f, 0.39f + handSway, 0f);
                    if (guardRenderer.flipX) socket.x = -socket.x;
                    Vector3 lampTip = guard.TransformPoint(socket);
                    flashlightBeam.transform.position = lampTip + beamDirection * (beamLength * 0.5f) + Vector3.back * 0.1f;
                    flashlightBeam.transform.rotation = beamRotation;
                }
            }

            if (feather != null)
            {
                feather.enabled = elapsed >= 26f;
                if (feather.enabled)
                {
                    float wind = Mathf.Sin((elapsed - 26f) * 3.2f);
                    feather.transform.position = featherStart + new Vector3(0.018f * wind + 0.025f * Mathf.Clamp01((elapsed - 28f) / 4f), 0.012f * wind, 0f);
                    feather.transform.rotation = Quaternion.Euler(0f, 0f, wind * 8f);
                }
            }
            if (moth != null)
            {
                moth.enabled = elapsed >= 26f;
                if (moth.enabled)
                {
                    float u = Mathf.Clamp01((elapsed - 26f) / 5f);
                    moth.transform.position = Vector3.Lerp(new Vector3(-0.75f, 0.02f, -0.25f), new Vector3(1.12f, 0.42f, -0.25f), u)
                        + Vector3.up * (Mathf.Sin(elapsed * 7f) * 0.055f);
                    if (mothGlow != null)
                    {
                        mothGlow.enabled = true;
                        mothGlow.transform.position = moth.transform.position + new Vector3(0f, 0f, 0.04f);
                        var glow = mothGlow.color;
                        glow.a = 0.20f + 0.08f * (0.5f + 0.5f * Mathf.Sin(elapsed * 5f));
                        mothGlow.color = glow;
                    }
                }
            }
            if (serviceDoorGlow != null)
            {
                serviceDoorGlow.enabled = elapsed >= 26f;
                if (serviceDoorGlow.enabled) serviceDoorGlow.color = new Color(1f, 0.78f, 0.42f, 0.2f + 0.12f * Mathf.Sin(elapsed * 2f));
            }
            if (stairLightFlash != null)
            {
                stairLightFlash.enabled = elapsed >= 10.4f && elapsed < 11.15f;
                if (stairLightFlash.enabled)
                {
                    float flicker = Mathf.Sin(Mathf.PI * Mathf.InverseLerp(10.4f, 11.15f, elapsed));
                    stairLightFlash.color = new Color(0.64f, 0.84f, 1f, 0.72f * flicker);
                }
            }
            if (titleText != null)
            {
                titleText.enabled = elapsed >= TitleReveal;
                var titleColor = titleText.color;
                float fadeIn = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(TitleReveal, TitleReveal + 0.45f, elapsed));
                float fadeOut = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(35.12f, Duration, elapsed));
                titleColor.a = fadeIn * fadeOut;
                titleText.color = titleColor;
            }
        }

        void UpdateSubtitleAndVoice()
        {
            CC01SubtitleCue current = null;
            foreach (var cue in subtitles)
            {
                if (elapsed >= cue.start && elapsed < cue.end) { current = cue; break; }
            }
            if (subtitleText != null)
                subtitleText.text = current == null ? string.Empty : $"<b>{current.speaker}</b>  {current.text}";
            if (current != activeVoiceCue && voiceSource != null)
            {
                if (voiceSource.isPlaying) voiceSource.Stop();
                activeVoiceCue = current;
                // Voice clips are deliberately unassigned until recording. When supplied, each cue plays at its authored start.
                if (current != null && current.voice != null)
                {
                    voiceSource.loop = false;
                    voiceSource.clip = current.voice;
                    voiceSource.Play();
                }
            }
            UpdateMusicMix();
        }

        void UpdateSoundCues()
        {
            if (!playedRiser && elapsed >= 9.35f) { playedRiser = true; PlayEffect(tensionRiser); }
            if (!playedImpact && elapsed >= 10.3f)
            {
                playedImpact = true;
                if (musicSource != null) musicSource.Pause();
                PlayEffect(metallicImpact);
            }
            if (!playedClick && elapsed >= 14.05f)
            {
                playedClick = true;
                if (musicSource != null) musicSource.UnPause();
                PlayEffect(flashlightClick);
            }
            if (!playedFlutter && elapsed >= 27.1f) { playedFlutter = true; PlayEffect(mothFlutter); }
            if (!playedRustle && elapsed >= 20.05f) { playedRustle = true; PlayEffect(uniformRustle); }
            if (!playedEvidenceChime && elapsed >= 20.2f) { playedEvidenceChime = true; PlayEffect(evidenceChime); }
            while (footstepsPlayed < 10 && elapsed >= WalkStart + footstepsPlayed * 0.5f)
            {
                PlayEffect(softFootstep);
                footstepsPlayed++;
            }
            if (!playedTitleHit && elapsed >= TitleReveal)
            {
                playedTitleHit = true;
                PlayEffect(metallicImpact);
            }
        }

        void SetFrame(Sprite[] frames, int index)
        {
            if (guardRenderer != null && frames != null && frames.Length > 0) guardRenderer.sprite = frames[Mathf.Clamp(index, 0, frames.Length - 1)];
        }

        static Vector2 GuardPath(float time)
        {
            // Continuous travel across the floor, followed by two distinct stair steps.
            Vector2 start = new Vector2(0.08f, -0.035f);
            Vector2 floorOne = new Vector2(0.25f, -0.22f);
            Vector2 floorTwo = new Vector2(0.65f, -0.14f);
            Vector2 floorThree = new Vector2(1.05f, 0.05f);
            Vector2 stairFoot = new Vector2(1.42f, 0.21f);
            Vector2 stairOne = new Vector2(1.44f, 0.29f);
            Vector2 stairTwo = new Vector2(1.46f, 0.37f);
            if (time < 27f) return Vector2.Lerp(start, floorOne, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(WalkStart, 27f, time)));
            if (time < 28f) return Vector2.Lerp(floorOne, floorTwo, Mathf.InverseLerp(27f, 28f, time));
            if (time < 29f) return Vector2.Lerp(floorTwo, floorThree, Mathf.InverseLerp(28f, 29f, time));
            if (time < 30.45f) return Vector2.Lerp(floorThree, stairFoot, Mathf.InverseLerp(29f, 30.45f, time));
            if (time < 30.9f) return Vector2.Lerp(stairFoot, stairOne, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(30.45f, 30.9f, time)));
            if (time < WalkEnd) return Vector2.Lerp(stairOne, stairTwo, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(30.9f, WalkEnd, time)));
            return stairTwo;
        }

        void PlayEffect(AudioClip clip)
        {
            if (clip != null && effectsSource != null) effectsSource.PlayOneShot(clip);
        }

        void UpdatePropsAndParticles()
        {
            if (footprints != null)
            {
                for (int i = 0; i < footprints.Length; i++)
                {
                    var mark = footprints[i];
                    if (mark == null) continue;
                    float markStart = 20.55f + i * 0.72f;
                    mark.enabled = elapsed >= markStart;
                    if (mark.enabled)
                    {
                        float reveal = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(markStart, markStart + 0.55f, elapsed));
                        var color = mark.color;
                        color.a = reveal * 0.62f;
                        mark.color = color;
                    }
                }
            }

            AnimateMotes(dustMotes, dustStarts, elapsed < Duration, 0.22f, 0.04f, elapsed);
            AnimateMotes(beamDust, beamDustStarts, elapsed >= 14.4f && elapsed < 26f, 0.24f, 0.035f, elapsed + 1.7f);

            if (coffeeSteam == null) return;
            for (int i = 0; i < coffeeSteam.Length; i++)
            {
                var steam = coffeeSteam[i];
                if (steam == null) continue;
                bool visible = elapsed >= 4f && elapsed < CutToBlack;
                steam.enabled = visible;
                if (!visible) continue;
                float phase = Mathf.Repeat(elapsed * 0.7f + i * 0.34f, 1f);
                steam.transform.position = steamStarts[i] + new Vector3(Mathf.Sin((elapsed + i) * 2.5f) * 0.018f, phase * 0.12f, 0f);
                var color = steam.color;
                color.a = (1f - phase) * 0.42f * (1f - 0.4f * Mathf.InverseLerp(10f, 18f, elapsed));
                steam.color = color;
            }
        }

        void UpdateMusicMix()
        {
            if (musicSource == null) return;
            float beforeImpact = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(10.05f, 10.35f, elapsed));
            float afterImpact = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(13.85f, 14.15f, elapsed));
            float impactMix = Mathf.Max(beforeImpact, afterImpact);
            float voiceMix = voiceSource != null && voiceSource.isPlaying ? 0.30f : 1f;
            float titleMix = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(31.65f, CutToBlack, elapsed));
            musicSource.volume = 0.12f * impactMix * voiceMix * titleMix;
            if (ambientSource != null)
                ambientSource.volume = Mathf.Lerp(1f, 0.08f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(31.55f, CutToBlack, elapsed)));
        }

        static Vector3[] CapturePositions(SpriteRenderer[] renderers)
        {
            if (renderers == null) return Array.Empty<Vector3>();
            var positions = new Vector3[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
                positions[i] = renderers[i] != null ? renderers[i].transform.position : Vector3.zero;
            return positions;
        }

        static void SetEnabled(SpriteRenderer[] renderers, bool enabled)
        {
            if (renderers == null) return;
            foreach (var renderer in renderers) if (renderer != null) renderer.enabled = enabled;
        }

        static void AnimateMotes(SpriteRenderer[] motes, Vector3[] starts, bool enabled, float maxAlpha, float drift, float time)
        {
            if (motes == null) return;
            for (int i = 0; i < motes.Length; i++)
            {
                var mote = motes[i];
                if (mote == null) continue;
                mote.enabled = enabled;
                if (!enabled) continue;
                float phase = time * (0.8f + i * 0.07f) + i * 1.7f;
                mote.transform.position = starts[i] + new Vector3(Mathf.Sin(phase) * drift, Mathf.Cos(phase * 0.7f) * drift * 0.8f, 0f);
                var color = mote.color;
                color.a = maxAlpha * (0.45f + 0.55f * (0.5f + 0.5f * Mathf.Sin(phase * 1.6f)));
                mote.color = color;
            }
        }

        static float Pulse(float time, float start, float duration)
        {
            return Mathf.Sin(Mathf.PI * Mathf.InverseLerp(start, start + duration, time));
        }

        void Finish()
        {
            if (finished) return;
            finished = true;
            ControlsLocked = false;
            if (voiceSource != null) voiceSource.Stop();
            onCutsceneFinished?.Invoke();
        }
    }
}
