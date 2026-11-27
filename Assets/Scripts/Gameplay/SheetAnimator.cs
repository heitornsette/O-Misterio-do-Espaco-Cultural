using System;
using UnityEngine;
using UnityEngine.UI;

namespace EspacoCultural.Gameplay
{
    /// <summary>
    /// Toca uma sprite sheet horizontal (quadros lado a lado) num RawImage trocando o uvRect.
    /// Por padrão o número de quadros é largura / altura (quadros quadrados de 128×128).
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public class SheetAnimator : MonoBehaviour
    {
        private RawImage img;
        private Texture2D sheet;
        private int frames = 1;
        private float fps = 8f;
        private bool loop = true;
        private float time;
        private bool done;
        private bool flip;
        private Action onDone;

        public int Frame { get; private set; }
        public Texture2D Current => sheet;
        public bool Done => done;

        private void Awake() => img = GetComponent<RawImage>();

        public bool FlipX
        {
            get => flip;
            set { if (flip == value) return; flip = value; Apply(Frame); }
        }

        /// <summary>Toca a sheet. Se já estiver tocando a mesma sheet em loop, só ajusta a velocidade.</summary>
        public void Play(Texture2D s, float framesPerSecond, bool looping = true, Action finished = null, int frameCount = 0, bool restart = false)
        {
            if (img == null) img = GetComponent<RawImage>();
            fps = framesPerSecond;
            if (!restart && s == sheet && looping && loop && !done) return;
            sheet = s;
            frames = frameCount > 0 ? frameCount : (s != null && s.height > 0 ? Mathf.Max(1, s.width / s.height) : 1);
            loop = looping;
            onDone = finished;
            time = 0f;
            done = false;
            img.texture = s;
            Apply(0);
        }

        /// <summary>Mostra um quadro fixo (para a pose parada).</summary>
        public void Hold(Texture2D s, int frame, int frameCount = 0)
        {
            Play(s, 0f, true, null, frameCount, true);
            Apply(Mathf.Clamp(frame, 0, frames - 1));
        }

        private void Update()
        {
            if (sheet == null || done || fps <= 0f) return;
            time += Time.deltaTime;
            int f = Mathf.FloorToInt(time * fps);
            if (f >= frames)
            {
                if (loop) f %= frames;
                else
                {
                    done = true;
                    Apply(frames - 1);
                    var cb = onDone; onDone = null;
                    cb?.Invoke();
                    return;
                }
            }
            if (f != Frame) Apply(f);
        }

        private void Apply(int f)
        {
            Frame = f;
            if (img == null) return;
            float w = 1f / frames;
            img.uvRect = flip ? new Rect((f + 1) * w, 0f, -w, 1f) : new Rect(f * w, 0f, w, 1f);
        }
    }
}
