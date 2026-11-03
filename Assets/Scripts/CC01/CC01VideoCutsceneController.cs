using System;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Misterio.Cutscenes
{
    /// <summary>Plays the pre-rendered CC01 while keeping dialogue, captions and game lock in Unity.</summary>
    [RequireComponent(typeof(VideoPlayer))]
    public sealed class CC01VideoCutsceneController : MonoBehaviour
    {
        public const string VideoRelativePath = "Cinematics/CC01_A_Madrugada_Comeca.webm";
        const string LegacyScenePath = "Assets/Scenes/CC01_A_Madrugada_Comeca.unity";

        [Serializable]
        sealed class DialogueCue
        {
            public float start;
            public float end;
            public string speaker;
            [TextArea] public string text;
            public AudioClip voice;
        }

        [SerializeField] Text subtitleText;
        [SerializeField] AudioSource voiceSource;
        [SerializeField] AudioSource ambientSource;
        [SerializeField] AudioSource effectsSource;
        [SerializeField] AudioSource musicSource;
        [SerializeField] DialogueCue[] subtitles =
        {
            new DialogueCue { start = 0.55f, end = 3.75f, speaker = "NARRADOR", text = "Às duas da manhã, o Espaço Cultural ainda dormia." },
            new DialogueCue { start = 4.05f, end = 6.10f, speaker = "NARRADOR", text = "Faltavam quatro horas para a inauguração." },
            new DialogueCue { start = 10.05f, end = 13.80f, speaker = "NARRADOR", text = "A ronda deveria ser tranquila." },
            new DialogueCue { start = 14.10f, end = 17.30f, speaker = "NARRADOR", text = "O barulho no andar de cima mudou os planos." },
            new DialogueCue { start = 17.35f, end = 19.75f, speaker = "SEGURANÇA", text = "Eu só queria uma noite tranquila." },
            new DialogueCue { start = 20.10f, end = 22.70f, speaker = "NARRADOR", text = "Na poeira, três dedos deixaram a primeira pista." },
            new DialogueCue { start = 22.75f, end = 25.80f, speaker = "SEGURANÇA", text = "Isso não parece pegada de sapato." },
            new DialogueCue { start = 26.10f, end = 29.40f, speaker = "NARRADOR", text = "A trilha começava ali, mas sua razão ainda estava escondida." },
            new DialogueCue { start = 29.45f, end = 31.80f, speaker = "SEGURANÇA", text = "Vamos ver o que aconteceu." }
        };
        [SerializeField] UnityEvent onCutsceneFinished = new UnityEvent();

        VideoPlayer player;
        DialogueCue activeCue;
        CC01Soundscape soundscape;
        bool completed;
        bool playedRiser;
        bool playedImpact;
        bool playedClick;
        bool playedRustle;
        bool playedEvidence;
        bool playedChime;
        bool playedFlutter;
        bool playedTitleHit;
        int footstepsPlayed;
        bool ControlsLocked { get; set; } = true;
        public bool IsControlsLocked => ControlsLocked;
        public double PlaybackTime => player != null ? player.time : 0d;
        public event Action Finished;

        void Awake()
        {
            ControlsLocked = true;
            player = GetComponent<VideoPlayer>();
            player.playOnAwake = false;
            player.isLooping = false;
            player.waitForFirstFrame = true;
            player.audioOutputMode = VideoAudioOutputMode.None;
            player.renderMode = VideoRenderMode.CameraNearPlane;
            player.aspectRatio = VideoAspectRatio.FitInside;
            player.prepareCompleted += OnPrepared;
            player.loopPointReached += OnVideoEnded;
            if (subtitleText != null) subtitleText.text = string.Empty;
            if (voiceSource != null) voiceSource.playOnAwake = false;
            soundscape = GetComponent<CC01Soundscape>();
            if (soundscape == null) soundscape = gameObject.AddComponent<CC01Soundscape>();
        }

        void Start()
        {
            string path = Path.Combine(Application.streamingAssetsPath, VideoRelativePath);
            if (!File.Exists(path))
            {
                Debug.LogWarning($"CC01 video not found at '{path}'. Loading the existing Unity cutscene until the WebM is exported.", this);
                SceneManager.LoadScene("CC01_A_Madrugada_Comeca", LoadSceneMode.Single);
                return;
            }

            player.source = VideoSource.Url;
            player.url = path;
            player.Prepare();
        }

        void Update()
        {
            if (completed || player == null || !player.isPlaying) return;
            UpdateDialogue(player.time);
            UpdateSound(player.time);
            if (player.frameCount > 0 && player.frame >= (long)player.frameCount - 1L)
                Finish();
        }

        void OnPrepared(VideoPlayer source)
        {
            source.Play();
            StartSoundscape();
        }

        void StartSoundscape()
        {
            if (soundscape == null) return;
            if (ambientSource != null && soundscape.Ambient != null)
            {
                ambientSource.clip = soundscape.Ambient;
                ambientSource.loop = true;
                ambientSource.volume = 1f;
                ambientSource.Play();
            }
            if (musicSource != null && soundscape.Xylophone != null)
            {
                musicSource.clip = soundscape.Xylophone;
                musicSource.loop = true;
                musicSource.volume = 0.12f;
                musicSource.Play();
            }
        }

        void UpdateSound(double time)
        {
            float t = (float)time;
            if (soundscape == null) return;
            if (t >= 9.35f && !playedRiser)
            {
                // The first riser leads into the metallic sound above the stairs.
                PlayEffect(soundscape.TensionRiser);
                playedRiser = true;
            }
            if (t >= 10.3f && !playedImpact)
            {
                PlayEffect(soundscape.Impact);
                if (musicSource != null) musicSource.Pause();
                playedImpact = true;
            }
            if (t >= 14.05f && !playedClick)
            {
                if (musicSource != null) musicSource.UnPause();
                PlayEffect(soundscape.Click);
                playedClick = true;
            }
            if (t >= 20.05f && !playedEvidence)
            {
                PlayEffect(soundscape.Rustle);
                playedEvidence = true;
            }
            if (t >= 20.2f && !playedChime)
            {
                PlayEffect(soundscape.EvidenceChime);
                playedChime = true;
            }
            if (t >= 27.1f && !playedFlutter)
            {
                PlayEffect(soundscape.Flutter);
                playedFlutter = true;
            }
            while (footstepsPlayed < 10 && t >= 26.35f + footstepsPlayed * 0.5f)
            {
                PlayEffect(soundscape.Step);
                footstepsPlayed++;
            }
            if (t >= 32.62f && !playedTitleHit)
            {
                PlayEffect(soundscape.Impact);
                playedTitleHit = true;
            }

            if (musicSource != null)
            {
                float voiceMix = voiceSource != null && voiceSource.isPlaying ? 0.30f : 1f;
                float titleMix = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(31.65f, 31.95f, t));
                musicSource.volume = 0.12f * voiceMix * titleMix;
            }
            if (ambientSource != null)
                ambientSource.volume = Mathf.Lerp(1f, 0.08f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(31.55f, 31.95f, t)));
        }

        void PlayEffect(AudioClip clip)
        {
            if (clip != null && effectsSource != null) effectsSource.PlayOneShot(clip);
        }

        void OnVideoEnded(VideoPlayer source)
        {
            Finish();
        }

        void UpdateDialogue(double time)
        {
            DialogueCue current = null;
            if (subtitles != null)
                foreach (var cue in subtitles)
                    if (cue != null && time >= cue.start && time < cue.end) { current = cue; break; }

            if (subtitleText != null)
                subtitleText.text = current == null ? string.Empty : $"<b>{current.speaker}</b>  {current.text}";

            if (current == activeCue || voiceSource == null) return;
            if (voiceSource.isPlaying) voiceSource.Stop();
            activeCue = current;
            if (current == null || current.voice == null) return;
            voiceSource.clip = current.voice;
            voiceSource.loop = false;
            voiceSource.Play();
        }

        void Finish()
        {
            if (completed) return;
            completed = true;
            ControlsLocked = false;
            if (subtitleText != null) subtitleText.text = string.Empty;
            if (voiceSource != null && voiceSource.isPlaying) voiceSource.Stop();
            onCutsceneFinished?.Invoke();
            Finished?.Invoke();
        }

        void OnDestroy()
        {
            if (player == null) return;
            player.prepareCompleted -= OnPrepared;
            player.loopPointReached -= OnVideoEnded;
        }
    }
}
