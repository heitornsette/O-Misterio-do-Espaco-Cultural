using EspacoCultural.Jogo;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace EspacoCultural.Campus
{
    /// <summary>
    /// O guarda nas cenas do campus: movimento livre (SETAS/WASD) dentro de uma faixa do chão,
    /// animação pelas sheets de idle/andar (frente e costas, espelhando para a esquerda),
    /// ordem de desenho pela altura dos pés e uma lanterna (Light 2D em cone) que aponta para onde ele anda.
    /// Cansado, anda mais devagar e a lanterna falha de vez em quando.
    /// </summary>
    public class Walker : MonoBehaviour
    {
        public float speed = 90f;
        public Rect area;                 // faixa onde ele pode andar (unidades = pixels)
        public readonly System.Collections.Generic.List<Rect> blockers = new System.Collections.Generic.List<Rect>();
        public bool locked;

        public SpriteRenderer sr;
        public Light2D lantern;
        public Vector2 Facing { get; private set; } = Vector2.right;
        public bool Moving { get; private set; }

        private Sprite[] idleFront, idleBack, walkFront, walkBack;
        private Sprite[] current;
        private float animTime, fps = 6f;
        private bool flip;
        private float flickerTimer;

        public static Walker Create(Transform parent, Vector2 pos, Rect area)
        {
            var go = new GameObject("Guarda");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(Mathf.Round(pos.x), Mathf.Round(pos.y), 0);
            var w = go.AddComponent<Walker>();
            w.area = area;
            w.sr = CampusArt.AddRenderer(go);
            w.idleFront = CampusArt.Sheet("guarda_idle_frente_sheet", 113);
            w.idleBack = CampusArt.Sheet("guarda_idle_costas_sheet", 120);
            w.walkFront = CampusArt.Sheet("guarda_andar_frente_sheet", 113);
            w.walkBack = CampusArt.Sheet("guarda_andar_costas_sheet", 120);
            w.Play(w.idleFront, 6f);

            var lg = new GameObject("Lanterna");
            lg.transform.SetParent(go.transform, false);
            lg.transform.localPosition = new Vector3(10, 52, 0);
            w.lantern = lg.AddComponent<Light2D>();
            w.lantern.lightType = Light2D.LightType.Point;
            w.lantern.color = new Color(1f, 0.95f, 0.8f);
            w.lantern.intensity = 1.1f;
            w.lantern.pointLightInnerRadius = 10f;
            w.lantern.pointLightOuterRadius = 150f;
            w.lantern.pointLightInnerAngle = 30f;
            w.lantern.pointLightOuterAngle = 62f;
            w.lantern.falloffIntensity = 0.6f;
            w.AimLantern();
            return w;
        }

        public void Face(Vector2 dir)
        {
            if (dir == Vector2.zero) return;
            Facing = dir;
            if (dir.x < -0.01f) flip = true;
            else if (dir.x > 0.01f) flip = false;
            AimLantern();
        }

        private void AimLantern()
        {
            if (lantern == null) return;
            float ang = Mathf.Atan2(Facing.y, Facing.x) * Mathf.Rad2Deg - 90f;   // a luz 2D aponta para +Y
            lantern.transform.localRotation = Quaternion.Euler(0, 0, ang);
            lantern.transform.localPosition = new Vector3(flip ? -10 : 10, 52, 0);
        }

        private void Update()
        {
            Vector2 d = Vector2.zero;
            var k = Keyboard.current;
            if (!locked && k != null)
            {
                if (k.leftArrowKey.isPressed || k.aKey.isPressed) d.x -= 1;
                if (k.rightArrowKey.isPressed || k.dKey.isPressed) d.x += 1;
                if (k.upArrowKey.isPressed || k.wKey.isPressed) d.y += 1;
                if (k.downArrowKey.isPressed || k.sKey.isPressed) d.y -= 1;
            }

            Moving = d != Vector2.zero;
            if (Moving)
            {
                d.Normalize();
                Face(d);
                float v = speed / GameState.SlowFactor;
                var p = (Vector2)transform.position + new Vector2(d.x * v, d.y * v * 0.6f) * Time.deltaTime;
                p.x = Mathf.Clamp(p.x, area.xMin, area.xMax);
                p.y = Mathf.Clamp(p.y, area.yMin, area.yMax);
                var cur = (Vector2)transform.position;
                foreach (var b in blockers)
                {
                    if (!b.Contains(p)) continue;
                    // tenta deslizar em um eixo só
                    if (!b.Contains(new Vector2(p.x, cur.y))) p.y = cur.y;
                    else if (!b.Contains(new Vector2(cur.x, p.y))) p.x = cur.x;
                    else p = cur;
                }
                transform.position = new Vector3(p.x, p.y, 0);
                bool back = Mathf.Abs(d.y) > Mathf.Abs(d.x) && d.y > 0;
                Play(back ? walkBack : walkFront, 11f);
            }
            else
            {
                bool back = Facing.y > 0.5f && Mathf.Abs(Facing.x) < 0.5f;
                Play(back ? idleBack : idleFront, 6f);
            }

            animTime += Time.deltaTime;
            if (current != null && current.Length > 0)
                sr.sprite = current[Mathf.FloorToInt(animTime * fps) % current.Length];
            sr.flipX = flip;
            sr.sortingOrder = SortOrder(transform.position.y);

            // Lanterna falhando quando cansado
            if (lantern != null)
            {
                if (GameState.Tired)
                {
                    flickerTimer -= Time.deltaTime;
                    if (flickerTimer <= 0f) flickerTimer = Random.Range(0.05f, 0.9f);
                    lantern.intensity = flickerTimer < 0.12f ? 0.15f : 1.1f;
                }
                else lantern.intensity = 1.1f;
            }
        }

        public void Play(Sprite[] s, float f)
        {
            if (s == current) { fps = f; return; }
            current = s;
            fps = f;
            animTime = 0f;
        }

        /// <summary>Quem está mais embaixo na tela fica na frente.</summary>
        public static int SortOrder(float y) => 1000 - Mathf.RoundToInt(y);
    }
}
