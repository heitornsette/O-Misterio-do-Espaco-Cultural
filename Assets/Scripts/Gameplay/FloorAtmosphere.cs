using System.Collections.Generic;
using UnityEngine;

namespace Misterio.Gameplay
{
    /// <summary>
    /// Living detail for the floors: drifting dust motes, a slow flicker on the ceiling lights and a
    /// gentle breathing motion on the guard. All of it is deterministic and cheap, and it is what
    /// keeps a static diorama from feeling like a screenshot.
    /// </summary>
    public sealed class FloorAtmosphere : MonoBehaviour
    {
        [System.Serializable]
        public sealed class Mote
        {
            public SpriteRenderer View;
            public Vector3 Start;
            public float Speed;
            public float Amplitude;
            public float Phase;
        }

        [SerializeField] List<Mote> motes = new List<Mote>();
        [SerializeField] List<SpriteRenderer> lights = new List<SpriteRenderer>();
        [SerializeField] List<float> lightBaseAlpha = new List<float>();
        [SerializeField] Transform breathingTarget;
        [SerializeField] float breathingAmplitude = 0.004f;
        [SerializeField] float breathingSpeed = 1.6f;

        Vector3 breathingBase;
        Vector3 breathingApplied;
        bool breathingCaptured;
        float time;

        void Awake() => CaptureBreathingBase();



        /// <summary>
        /// The base position must be captured at runtime: Configure runs at build time in the editor
        /// and its result is not serialised, so a player build would otherwise start from zero.
        /// </summary>
        void CaptureBreathingBase()
        {
            if (breathingTarget == null) return;
            breathingBase = breathingTarget.localPosition;
            breathingApplied = breathingBase;
            breathingCaptured = true;
        }

        public void Configure(List<Mote> dustMotes, List<SpriteRenderer> lightViews, Transform breathe)
        {
            motes = dustMotes ?? new List<Mote>();
            lights = lightViews ?? new List<SpriteRenderer>();
            lightBaseAlpha.Clear();
            foreach (var light in lights)
                lightBaseAlpha.Add(light != null ? light.color.a : 0f);
            breathingTarget = breathe;
            breathingCaptured = false;
            CaptureBreathingBase();
        }

        void Update()
        {
            float delta = Time.deltaTime;
            time += delta;

            for (int i = 0; i < motes.Count; i++)
            {
                var mote = motes[i];
                if (mote?.View == null) continue;
                // Motes drift up slowly and sway sideways on a sine.
                float rise = Mathf.Repeat(time * mote.Speed + mote.Phase, 1f);
                mote.View.transform.position = mote.Start + new Vector3(
                    Mathf.Sin((time + mote.Phase) * 0.9f) * mote.Amplitude,
                    rise * mote.Amplitude * 2.2f,
                    0f);
                var color = mote.View.color;
                // Fade in and out over the cycle so motes never pop.
                color.a = Mathf.Sin(rise * Mathf.PI) * 0.5f;
                mote.View.color = color;
            }

            for (int i = 0; i < lights.Count; i++)
            {
                var light = lights[i];
                if (light == null) continue;
                float baseAlpha = i < lightBaseAlpha.Count ? lightBaseAlpha[i] : light.color.a;
                // A slow, shallow flicker, never a strobe.
                float flicker = 0.94f + 0.06f * Mathf.Sin(time * 1.7f + i * 1.3f);
                var color = light.color;
                color.a = baseAlpha * flicker;
                light.color = color;
            }

            if (breathingTarget != null)
            {
                if (!breathingCaptured) CaptureBreathingBase();

                // If the target is not where the breathing left it, the game moved it. Adopt that
                // position; otherwise this would drag the guard back every frame and he could never
                // walk anywhere.
                if ((breathingTarget.localPosition - breathingApplied).sqrMagnitude > 0.0000001f)
                    breathingBase = breathingTarget.localPosition - (breathingApplied - breathingBase);

                float breath = Mathf.Sin(time * breathingSpeed) * breathingAmplitude;
                var pose = breathingBase + new Vector3(0f, breath, 0f);
                breathingTarget.localPosition = pose;
                breathingApplied = pose;
            }
        }
    }
}
