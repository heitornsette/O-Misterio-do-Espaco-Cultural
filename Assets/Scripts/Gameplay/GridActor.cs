using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace EspacoCultural.Gameplay
{
    /// <summary>
    /// Qualquer coisa de pé sobre o grid (guarda, Moema, estátuas, vitrines...).
    /// O sprite é posicionado pelos pés: footRow é a linha (em pixels, a partir do topo da imagem)
    /// onde o pé encosta no chão. A ordem de desenho segue a altura dos pés (quem está mais
    /// embaixo na tela fica na frente).
    /// </summary>
    public class GridActor : MonoBehaviour
    {
        public FloorGrid grid;
        public Vector2Int cell;
        public int footRow = 113;
        public Vector2 offset;            // ajuste fino em pixels (ex.: vitrine centralizada entre 2 células)
        public RawImage image;
        public SheetAnimator anim;
        public bool Moving { get; private set; }

        public Vector2 FeetPos { get; private set; }
        private RectTransform rt;
        private Coroutine moveRoutine;

        public static GridActor Create(string name, Transform parent, FloorGrid grid, Vector2Int cell, Texture2D tex,
                                       int footRow, int frameW = -1, int frameH = -1, bool animated = false)
        {
            var img = Px.Raw(name, parent, tex, 0, 0,
                             frameW > 0 ? frameW : (tex != null ? tex.width : 16),
                             frameH > 0 ? frameH : (tex != null ? tex.height : 16));
            var a = img.gameObject.AddComponent<GridActor>();
            a.grid = grid;
            a.image = img;
            a.footRow = footRow;
            a.rt = img.rectTransform;
            if (animated) a.anim = img.gameObject.AddComponent<SheetAnimator>();
            a.Snap(cell);
            return a;
        }

        public void Snap(Vector2Int c)
        {
            if (moveRoutine != null) { StopCoroutine(moveRoutine); moveRoutine = null; }
            Moving = false;
            cell = c;
            SetFeet(grid.Feet(c));
        }

        public void SetFeet(Vector2 feet)
        {
            if (rt == null) rt = (RectTransform)transform;
            FeetPos = feet;
            var size = rt.sizeDelta;
            Px.Place(rt, feet.x - size.x * 0.5f + offset.x, feet.y - footRow + offset.y, size.x, size.y);
        }

        /// <summary>Anda até a célula em linha reta (o estado lógico muda na hora; o desenho acompanha).</summary>
        public void MoveTo(Vector2Int c, float duration, Action arrived = null)
        {
            if (moveRoutine != null) StopCoroutine(moveRoutine);
            cell = c;
            moveRoutine = StartCoroutine(MoveRoutine(grid.Feet(c), duration, arrived));
        }

        private IEnumerator MoveRoutine(Vector2 to, float duration, Action arrived)
        {
            Moving = true;
            Vector2 from = FeetPos;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                Vector2 p = Vector2.Lerp(from, to, Mathf.Clamp01(t / duration));
                SetFeet(new Vector2(Mathf.Round(p.x), Mathf.Round(p.y)));
                yield return null;
            }
            SetFeet(to);
            Moving = false;
            moveRoutine = null;
            arrived?.Invoke();
        }
    }
}
