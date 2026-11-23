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
        float time;

        public void Configure(List<Mote> dustMotes, List<SpriteRenderer> lightViews, Transform breathe)
        {
            motes = dustMotes ?? new List<Mote>();
            lights = lightViews ?? new List<SpriteRenderer>();
            lightBaseAlpha.Clear();
            foreach (var light in lights)
                lightBaseAlpha.Add(light != null ? light.color.a : 0f);
            breathingTarget = breathe;
            if (breathingTarget != null) breathingBase = breathingTarget.localPosition;
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
                float breath = Mathf.Sin(time * breathingSpeed) * breathingAmplitude;
                breathingTarget.localPosition = breathingBase + new Vector3(0f, breath, 0f);
            }
        }
    }
}
