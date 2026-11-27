using UnityEngine;
using UnityEngine.UI;

namespace EspacoCultural.Menu
{
    /// <summary>A Moema se esconde atrás da vitrine de tempos em tempos e volta a espiar.</summary>
    public class PeekBehaviour : MonoBehaviour
    {
        public float hideDistance = 58f;
        public Vector2 visibleTime = new Vector2(3f, 6f);
        public float hiddenTime = 1.4f;
        public float moveTime = 0.3f;

        private RectTransform rt;
        private Vector2 basePos;
        private float timer;
        private int phase;       // 0 visível, 1 descendo, 2 escondida, 3 subindo

        private void Awake()
        {
            rt = (RectTransform)transform;
            basePos = rt.anchoredPosition;
            timer = Random.Range(visibleTime.x, visibleTime.y);
        }

        private void Update()
        {
            timer -= Time.unscaledDeltaTime;
            float k = 0f;
            switch (phase)
            {
                case 0: k = 0f; if (timer <= 0) { phase = 1; timer = moveTime; } break;
                case 1: k = 1f - timer / moveTime; if (timer <= 0) { phase = 2; timer = hiddenTime; } break;
                case 2: k = 1f; if (timer <= 0) { phase = 3; timer = moveTime * 2f; } break;
                case 3: k = timer / (moveTime * 2f); if (timer <= 0) { phase = 0; timer = Random.Range(visibleTime.x, visibleTime.y); } break;
            }
            k = Mathf.Clamp01(k);
            rt.anchoredPosition = basePos + new Vector2(0, -Mathf.Round(hideDistance * k * k));
        }
    }
}
