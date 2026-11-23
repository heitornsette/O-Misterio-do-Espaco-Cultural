using UnityEngine;

namespace Misterio.Gameplay
{
    /// <summary>
    /// Small one-shot sound kit for the floors. It reuses the effects already produced for the
    /// cutscenes, so the gameplay sounds consistent with the videos without new assets.
    /// </summary>
    public sealed class FloorSfx : MonoBehaviour
    {
        [SerializeField] AudioSource source;
        [SerializeField] AudioClip push;
        [SerializeField] AudioClip confirm;
        [SerializeField] AudioClip error;
        [SerializeField] AudioClip pickup;
        [SerializeField] AudioClip turn;
        [SerializeField] AudioClip locked;

        public static FloorSfx Instance { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            if (source == null) source = GetComponent<AudioSource>();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Push() => Play(push, 0.55f);
        public void Confirm() => Play(confirm, 0.65f);
        public void Error() => Play(error, 0.45f);
        public void Pickup() => Play(pickup, 0.55f);
        public void Turn() => Play(turn, 0.35f);
        public void Locked() => Play(locked, 0.40f);

        void Play(AudioClip clip, float volume)
        {
            if (source == null || clip == null) return;
            source.PlayOneShot(clip, volume);
        }
    }
}
