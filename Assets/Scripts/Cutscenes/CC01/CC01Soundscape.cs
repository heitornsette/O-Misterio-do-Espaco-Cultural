using UnityEngine;

namespace Misterio.Cutscenes
{
    /// <summary>Small, intentionally restrained placeholder sound design made in Unity. Recorded voices replace only the empty voice slots.</summary>
    public sealed class CC01Soundscape : MonoBehaviour
    {
        public AudioClip Ambient { get; private set; }
        public AudioClip Xylophone { get; private set; }
        public AudioClip Impact { get; private set; }
        public AudioClip Click { get; private set; }
        public AudioClip Flutter { get; private set; }
        public AudioClip Step { get; private set; }
        public AudioClip Rustle { get; private set; }
        public AudioClip EvidenceChime { get; private set; }
        public AudioClip TensionRiser { get; private set; }

        const int Rate = 22050;
        static readonly float[] NoteStarts = { 0.30f, 1.55f, 2.85f };
        static readonly float[] NoteFrequencies = { 392f, 311.13f, 261.63f };

        void Awake()
        {
            Ambient = Make("CC01_AmbientNight", 4f, t =>
                0.035f * (Mathf.Sin(2 * Mathf.PI * 60f * t) + 0.22f * Mathf.Sin(2 * Mathf.PI * 120f * t)) +
                0.012f * Mathf.Sin(2 * Mathf.PI * 48f * t + 0.4f) +
                0.008f * (Mathf.PerlinNoise(t * 9f, 0.31f) - 0.5f));
            Xylophone = Make("CC01_XylophoneMotif", 4f, XylophoneSample);
            Impact = Make("CC01_MetallicImpact", 0.72f, ImpactSample);
            Click = Make("CC01_FlashlightClick", 0.10f, ClickSample);
            Flutter = Make("CC01_MothFlutter", 0.60f, FlutterSample);
            Step = Make("CC01_SoftFootstep", 0.20f, StepSample);
            Rustle = Make("CC01_UniformRustle", 0.36f, RustleSample);
            EvidenceChime = Make("CC01_EvidenceChime", 0.72f, EvidenceChimeSample);
            TensionRiser = Make("CC01_TensionRiser", 1.0f, TensionRiserSample);
        }

        static AudioClip Make(string name, float seconds, System.Func<float, float> sample)
        {
            int count = Mathf.CeilToInt(seconds * Rate);
            var data = new float[count];
            for (int i = 0; i < count; i++) data[i] = Mathf.Clamp(sample(i / (float)Rate), -1f, 1f);
            var clip = AudioClip.Create(name, count, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float XylophoneSample(float t)
        {
            float result = 0f;
            for (int i = 0; i < NoteStarts.Length; i++)
            {
                float local = t - NoteStarts[i];
                if (local < 0f || local > 0.62f) continue;
                float envelope = Mathf.Exp(-local * 7f) * Mathf.Clamp01(local * 80f);
                float f = NoteFrequencies[i];
                result += 0.07f * envelope * (Mathf.Sin(2 * Mathf.PI * f * local) + 0.12f * Mathf.Sin(2 * Mathf.PI * f * 2.02f * local));
            }
            return result;
        }

        static float ImpactSample(float t)
        {
            return ImpactStrike(t) + 0.34f * ImpactStrike(t - 0.14f) + 0.13f * ImpactStrike(t - 0.31f)
                + 0.15f * Mathf.Exp(-t * 5f) * Mathf.Sin(2f * Mathf.PI * 58f * t);
        }

        static float ImpactStrike(float t)
        {
            if (t < 0f) return 0f;
            float envelope = Mathf.Exp(-t * 8.5f) * Mathf.Clamp01(t * 100f);
            return 0.30f * envelope * (Mathf.Sin(2 * Mathf.PI * 196f * t) + 0.48f * Mathf.Sin(2 * Mathf.PI * 311f * t) + 0.22f * Mathf.Sin(2 * Mathf.PI * 522f * t));
        }

        static float ClickSample(float t)
        {
            float envelope = Mathf.Exp(-t * 62f) * Mathf.Clamp01(t * 650f);
            return 0.38f * envelope * (Mathf.Sin(2 * Mathf.PI * 940f * t) + 0.3f * Mathf.Sin(2 * Mathf.PI * 1880f * t));
        }

        static float FlutterSample(float t)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(2 * Mathf.PI * 13f * t);
            float tone = Mathf.Sin(2 * Mathf.PI * (420f + 110f * Mathf.Sin(2 * Mathf.PI * 4f * t)) * t);
            return 0.12f * pulse * tone * Mathf.Sin(Mathf.PI * t / 0.6f);
        }

        static float StepSample(float t)
        {
            float envelope = Mathf.Exp(-t * 35f) * Mathf.Clamp01(t * 160f);
            float low = Mathf.Sin(2 * Mathf.PI * 104f * t);
            float tick = Mathf.Sin(2 * Mathf.PI * 520f * t) * 0.15f;
            return 0.22f * envelope * (low + tick);
        }

        static float RustleSample(float t)
        {
            float envelope = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.36f));
            float noise = Mathf.PerlinNoise(t * 170f, 0.73f) * 2f - 1f;
            float fabric = Mathf.Sin(2f * Mathf.PI * 310f * t) * 0.22f;
            return 0.10f * envelope * (noise * 0.72f + fabric);
        }

        static float EvidenceChimeSample(float t)
        {
            float envelope = Mathf.Exp(-t * 6.5f) * Mathf.Clamp01(t * 65f);
            float first = Mathf.Sin(2f * Mathf.PI * 880f * t);
            float fifth = Mathf.Sin(2f * Mathf.PI * 1318.5f * t) * 0.48f;
            float overtone = Mathf.Sin(2f * Mathf.PI * 1760f * t) * 0.16f;
            return 0.17f * envelope * (first + fifth + overtone);
        }

        static float TensionRiserSample(float t)
        {
            float envelope = Mathf.SmoothStep(0f, 1f, t) * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.78f, 1f, t)));
            float noise = Mathf.PerlinNoise(t * 130f, 0.18f) * 2f - 1f;
            return envelope * (0.045f * noise + 0.035f * Mathf.Sin(2f * Mathf.PI * (72f + 26f * t) * t));
        }
    }
}
