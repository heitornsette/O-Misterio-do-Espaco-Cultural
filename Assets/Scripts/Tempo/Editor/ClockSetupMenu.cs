using System.IO;
using EspacoCultural.Tempo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace EspacoCultural.Tempo.EditorTools
{
    /// <summary>
    /// Menu "Espaço Cultural > Relógio > Montar na cena atual".
    /// Cria o objeto GameClock (relógio + debug + final de tempo) e o HUD (Canvas 640×360).
    /// </summary>
    public static class ClockSetupMenu
    {
        private const string IconPath = "Assets/Scripts/Tempo/Art/ampulheta.png";

        [MenuItem("Espaço Cultural/Relógio/Montar na cena atual")]
        public static void Build()
        {
            // ---------- GameClock ----------
            var clockGo = GameObject.Find("GameClock");
            if (clockGo == null)
            {
                clockGo = new GameObject("GameClock");
                Undo.RegisterCreatedObjectUndo(clockGo, "Criar GameClock");
            }
            Ensure<GameClock>(clockGo);
            Ensure<GameClockDebug>(clockGo);
            Ensure<TimeUpHandler>(clockGo);

            // ---------- HUD ----------
            var old = GameObject.Find("HUD_Relogio");
            if (old != null) Undo.DestroyObjectImmediate(old);

            var canvasGo = new GameObject("HUD_Relogio", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(canvasGo, "Criar HUD");
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvas.pixelPerfect = true;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(640, 360);
            scaler.matchWidthOrHeight = 0.5f;

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Painel do horário (centralizado no topo)
            var panel = NewUI("Painel", canvasGo.transform);
            SetRect(panel, new Vector2(0.5f, 1), new Vector2(0, -6), new Vector2(78, 30));
            var bg = panel.gameObject.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.07f, 0.16f, 0.78f);

            var timeRt = NewUI("Horario", panel);
            Stretch(timeRt);
            var timeText = timeRt.gameObject.AddComponent<Text>();
            timeText.font = font;
            timeText.fontSize = 20;
            timeText.alignment = TextAnchor.MiddleCenter;
            timeText.text = "02:00";
            timeText.raycastTarget = false;

            // Grupo de crédito (ampulheta + saldo), centralizado abaixo do painel
            var credit = NewUI("Credito", canvasGo.transform);
            SetRect(credit, new Vector2(0.5f, 1), new Vector2(0, -38), new Vector2(56, 18));
            var cbg = credit.gameObject.AddComponent<Image>();
            cbg.color = new Color(0.04f, 0.07f, 0.16f, 0.78f);

            var icon = NewUI("Ampulheta", credit);
            icon.anchorMin = icon.anchorMax = new Vector2(0, 0.5f);
            icon.pivot = new Vector2(0, 0.5f);
            icon.anchoredPosition = new Vector2(5, 0);
            icon.sizeDelta = new Vector2(10, 14);
            var iconImg = icon.gameObject.AddComponent<Image>();
            iconImg.sprite = GetOrCreateHourglass();
            iconImg.color = new Color(0.55f, 0.85f, 1f);
            iconImg.raycastTarget = false;

            var ctRt = NewUI("Saldo", credit);
            ctRt.anchorMin = new Vector2(0, 0); ctRt.anchorMax = new Vector2(1, 1);
            ctRt.offsetMin = new Vector2(18, 0); ctRt.offsetMax = new Vector2(-3, 0);
            var creditText = ctRt.gameObject.AddComponent<Text>();
            creditText.font = font;
            creditText.fontSize = 13;
            creditText.alignment = TextAnchor.MiddleCenter;
            creditText.text = "0:30";
            creditText.raycastTarget = false;

            var hud = canvasGo.AddComponent<ClockHUD>();
            hud.timeText = timeText;
            hud.creditGroup = credit.gameObject;
            hud.creditText = creditText;
            credit.gameObject.SetActive(false);

            Selection.activeGameObject = clockGo;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[Relógio] GameClock e HUD_Relogio criados. Dê Play e use Enter, 1-4, C, F, T, R para testar.");
        }

        private static T Ensure<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : Undo.AddComponent<T>(go);
        }

        private static RectTransform NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.layer = LayerMask.NameToLayer("UI");
            return (RectTransform)go.transform;
        }

        private static void SetRect(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        /// <summary>Gera uma ampulheta em pixel art 7×10 (provisória — troque pelo sprite final).</summary>
        private static Sprite GetOrCreateHourglass()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(IconPath);
            if (existing != null) return existing;

            string[] rows =
            {
                "#######",
                "#.....#",
                ".#...#.",
                "..#.#..",
                "...#...",
                "...#...",
                "..#.#..",
                ".#.#.#.",
                "#.###.#",
                "#######",
            };
            int w = 7, h = rows.Length;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, h - 1 - y, rows[y][x] == '#' ? Color.white : Color.clear);
            tex.Apply();

            Directory.CreateDirectory(Path.GetDirectoryName(IconPath));
            File.WriteAllBytes(IconPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(IconPath);

            var imp = (TextureImporter)AssetImporter.GetAtPath(IconPath);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.filterMode = FilterMode.Point;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.mipmapEnabled = false;
            imp.spritePixelsPerUnit = 16;
            imp.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(IconPath);
        }
    }
}
