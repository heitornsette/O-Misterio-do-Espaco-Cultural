using UnityEngine;
using UnityEngine.UI;

namespace EspacoCultural.Menu
{
    /// <summary>Pequenas oscilações de intensidade na luz da lanterna.</summary>
    [RequireComponent(typeof(Graphic))]
    public class LightFlicker : MonoBehaviour
    {
        public float minAlpha = 0.75f;
        public float interval = 0.12f;

        private Graphic g;
        private float baseAlpha;
        private float timer;

        private void Awake()
        {
            g = GetComponent<Graphic>();
            baseAlpha = g.color.a;
        }

        private void Update()
        {
            timer -= Time.unscaledDeltaTime;
            if (timer > 0) return;
            timer = interval * Random.Range(0.5f, 1.5f);
            var c = g.color;
            c.a = Random.value < 0.85f ? baseAlpha : baseAlpha * Random.Range(minAlpha, 1f);
            g.color = c;
        }
    }
}
