using UnityEngine;

namespace EspacoCultural.Menu
{
    /// <summary>
    /// Coloque junto de qualquer AudioSource para ele seguir o volume das Configurações.
    /// Música → "Volume da música"; Efeitos → "Volume dos efeitos".
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class VolumeBinding : MonoBehaviour
    {
        public enum Category { Musica, Efeitos }

        public Category category = Category.Efeitos;
        [Range(0f, 1f)] public float baseVolume = 1f;

        private AudioSource source;

        private void Awake() => source = GetComponent<AudioSource>();

        private void OnEnable()
        {
            GameSettings.Changed += Apply;
            Apply();
        }

        private void OnDisable() => GameSettings.Changed -= Apply;

        private void Apply()
        {
            if (source == null) return;
            float level = category == Category.Musica ? GameSettings.MusicVolume : GameSettings.SfxVolume;
            source.volume = baseVolume * level;
        }
    }
}
