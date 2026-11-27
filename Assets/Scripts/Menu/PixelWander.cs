using UnityEngine;
using UnityEngine.UI;

namespace EspacoCultural.Menu
{
    /// <summary>Movimento sinuoso da mariposa, sempre em pixels inteiros; vira para o lado em que voa.</summary>
    public class PixelWander : MonoBehaviour
    {
        public Vector2 amplitude = new Vector2(40, 16);
        public Vector2 speed = new Vector2(0.35f, 0.9f);

        private RectTransform rt;
        private Vector2 basePos;
        private float lastX;

        private void Awake()
        {
            rt = (RectTransform)transform;
            basePos = rt.anchoredPosition;
            lastX = basePos.x;
        }

        private void Update()
        {
            float t = Time.unscaledTime;
            float x = Mathf.Sin(t * speed.x) * amplitude.x + Mathf.Sin(t * speed.x * 2.3f) * amplitude.x * 0.25f;
            float y = Mathf.Sin(t * speed.y) * amplitude.y + Mathf.Cos(t * speed.y * 1.7f) * amplitude.y * 0.4f;
            var p = basePos + new Vector2(Mathf.Round(x), Mathf.Round(y));
            rt.anchoredPosition = p;
            if (Mathf.Abs(p.x - lastX) >= 1f)
            {
                var s = rt.localScale;
                s.x = p.x < lastX ? -Mathf.Abs(s.x) : Mathf.Abs(s.x);
                rt.localScale = s;
                lastX = p.x;
            }
        }
    }
}
