using System.Collections;
using System.Collections.Generic;
using EspacoCultural.Menu;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EspacoCultural.Audio
{
    /// <summary>
    /// Som do jogo (provisório): música de fundo + efeitos, seguindo o volume das Configurações.
    /// Nasce sozinho antes da primeira cena (não precisa colocar em cena nenhuma) e atravessa as cenas.
    /// As faixas ficam em Assets/Resources/Audio. Para trocar a música, substitua os .wav mantendo o nome
    /// ou mude MusicFor().
    ///   GameAudio.Sfx("sfx_pegar");          // efeito
    ///   GameAudio.PlayMusic("musica_noite");  // força uma faixa
    /// </summary>
    public class GameAudio : MonoBehaviour
    {
        public static GameAudio Instance { get; private set; }

        [Range(0f, 1f)] public float musicBaseVolume = 0.6f;
        [Range(0f, 1f)] public float sfxBaseVolume = 0.9f;

        private AudioSource music;
        private AudioSource sfx;
        private string currentMusic;
        private float fade = 1f;
        private float quietUntil;
        private Coroutine switching;
        private readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            if (Instance != null) return;
            var go = new GameObject("GameAudio");
            DontDestroyOnLoad(go);
            go.AddComponent<GameAudio>();
#if !UNITY_EDITOR
            // No jogo exportado, aplica a tela cheia e a resolução salvas logo ao abrir.
            GameSettings.ApplyDisplay();
#endif
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            music = gameObject.AddComponent<AudioSource>();
            music.loop = true;
            music.playOnAwake = false;
            music.spatialBlend = 0f;
            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            sfx.spatialBlend = 0f;
            GameSettings.Changed += ApplyVolume;
            SceneManager.sceneLoaded += OnSceneLoaded;
            ApplyVolume();
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            GameSettings.Changed -= ApplyVolume;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            quietUntil = Time.unscaledTime + 0.3f;   // evita o "bip" da seleção inicial dos menus
            string m = MusicFor(scene.name);
            if (m == null) StopMusic();
            else PlayMusic(m);
        }

        public static void StopMusic()
        {
            if (Instance == null || Instance.currentMusic == null) return;
            Instance.currentMusic = null;
            if (Instance.switching != null) Instance.StopCoroutine(Instance.switching);
            Instance.switching = Instance.StartCoroutine(Instance.FadeOut());
        }

        private IEnumerator FadeOut()
        {
            for (float t = 0; t < 0.5f; t += Time.unscaledDeltaTime) { fade = 1f - t / 0.5f; ApplyVolume(); yield return null; }
            music.Stop();
            fade = 1f;
            ApplyVolume();
            switching = null;
        }

        /// <summary>Qual faixa toca em cada cena.</summary>
        private static string MusicFor(string scene)
        {
            // As cutscenes e os créditos têm trilha própria: a nossa música sai de cena.
            if (scene.StartsWith("CC") || scene == "Credits") return null;
            return scene == "Andar3" ? "musica_acervo" : "musica_noite";
        }

        // ---------------- API ----------------

        public static void PlayMusic(string name)
        {
            if (Instance == null || string.IsNullOrEmpty(name) || name == Instance.currentMusic) return;
            var clip = Instance.Load(name);
            if (clip == null) return;
            Instance.currentMusic = name;
            if (Instance.switching != null) Instance.StopCoroutine(Instance.switching);
            Instance.switching = Instance.StartCoroutine(Instance.Switch(clip));
        }

        public static void Sfx(string name, float volume = 1f)
        {
            if (Instance == null || Time.unscaledTime < Instance.quietUntil) return;
            var clip = Instance.Load(name);
            if (clip == null) return;
            Instance.sfx.PlayOneShot(clip, Mathf.Clamp01(volume) * Instance.sfxBaseVolume * GameSettings.SfxVolume);
        }

        // ---------------- Interno ----------------

        private AudioClip Load(string name)
        {
            if (cache.TryGetValue(name, out var c)) return c;
            c = Resources.Load<AudioClip>("Audio/" + name);
            if (c == null) Debug.LogWarning($"[Som] Arquivo não encontrado: Resources/Audio/{name}");
            cache[name] = c;
            return c;
        }

        private IEnumerator Switch(AudioClip clip)
        {
            if (music.isPlaying)
            {
                for (float t = 0; t < 0.6f; t += Time.unscaledDeltaTime) { fade = 1f - t / 0.6f; ApplyVolume(); yield return null; }
            }
            music.clip = clip;
            music.Play();
            for (float t = 0; t < 0.8f; t += Time.unscaledDeltaTime) { fade = t / 0.8f; ApplyVolume(); yield return null; }
            fade = 1f;
            ApplyVolume();
            switching = null;
        }

        private void ApplyVolume()
        {
            if (music != null) music.volume = musicBaseVolume * GameSettings.MusicVolume * fade;
        }
    }
}
