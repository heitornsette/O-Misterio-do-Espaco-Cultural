using UnityEngine;
using UnityEngine.UI;

namespace EspacoCultural.Menu
{
    /// <summary>
    /// Anima uma spritesheet horizontal (quadros lado a lado) num RawImage, trocando o uvRect.
    /// Não precisa fatiar a sheet no Sprite Editor.
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public class UISpriteSheetAnimator : MonoBehaviour
    {
        public int frames = 4;
        public float fps = 6f;
        public bool flipX;
        public bool randomStart = true;

        private RawImage img;
        private float offset;

        private void Awake()
        {
            img = GetComponent<RawImage>();
            if (randomStart) offset = Random.Range(0f, 10f);
            Apply(0);
        }

        private void Update()
        {
            int f = Mathf.FloorToInt((Time.unscaledTime + offset) * fps) % Mathf.Max(1, frames);
            Apply(f);
        }

        private void Apply(int f)
        {
            float w = 1f / Mathf.Max(1, frames);
            img.uvRect = flipX ? new Rect((f + 1) * w, 0, -w, 1) : new Rect(f * w, 0, w, 1);
        }
    }
}
