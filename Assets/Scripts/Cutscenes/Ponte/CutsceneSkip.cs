using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Misterio.Core
{
    /// <summary>
    /// PONTE: permite pular as cutscenes da equipe (CC01–CC10) sem alterar o código delas.
    /// O GameBootstrap adiciona este componente em cada cena de cutscene.
    ///
    /// - SEGURAR ESPAÇO, ENTER, ESC, clique do mouse ou A/Start do controle por ~0,6 s pula
    ///   (segurar evita pular sem querer). Um aviso com barra aparece no canto inferior direito
    ///   no começo da cena e sempre que uma tecla é apertada.
    /// - Cutscenes com continuação (CC01–CC08, CC04A): encerra na hora e segue para a próxima
    ///   cena da rota, como se tivesse terminado sozinha.
    /// - CC09/CC10 (fim do tempo): pula direto para a tela de "tentar de novo / voltar ao menu".
    /// - Os Créditos já têm o pulo próprio da equipe e não usam este componente.
    /// Todos os controladores da equipe têm os campos privados elapsed/finished, a constante
    /// Duration e o método Finish(); o acesso é por reflexão.
    /// </summary>
    public sealed class CutsceneSkip : MonoBehaviour
    {
        private const float HoldTime = 0.6f;
        private const float HintSeconds = 3f;

        private MonoBehaviour controller;
        private FieldInfo elapsedField, finishedField;
        private MethodInfo finishMethod;
        private float duration;
        private bool hasNext;
        private bool done;
        private bool armed;   // só conta depois que as teclas foram soltas (quem chega segurando ENTER do menu não pula)

        private float hold, hintUntil;
        private CanvasGroup group;
        private Image fill;
        private const float BarW = 150f;

        public void Init(MonoBehaviour cutscene, string sceneName)
        {
            controller = cutscene;
            const BindingFlags inst = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            const BindingFlags stat = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            var t = cutscene.GetType();
            elapsedField = t.GetField("elapsed", inst);
            finishedField = t.GetField("finished", inst);
            finishMethod = t.GetMethod("Finish", inst, null, System.Type.EmptyTypes, null);
            var d = t.GetField("Duration", stat);
            if (d != null) duration = System.Convert.ToSingle(d.GetValue(null));
            else
            {
                var p = t.GetProperty("Duration", inst | stat);
                if (p != null) duration = System.Convert.ToSingle(p.GetValue(p.GetGetMethod(true).IsStatic ? null : cutscene));
            }
            hasNext = !string.IsNullOrEmpty(Misterio.Puzzles.RouteRules.NextAfter(sceneName));

            if (elapsedField == null || duration <= 0f)
            {
                Debug.LogWarning($"[Cutscene] {t.Name}: não encontrei elapsed/Duration; a cena não poderá ser pulada.");
                enabled = false;
                return;
            }
            BuildUI();
            hintUntil = Time.unscaledTime + HintSeconds;
        }

        private bool Finished => finishedField != null && (bool)finishedField.GetValue(controller);

        private void Update()
        {
            if (controller == null || done) return;
            if (Finished) { done = true; if (group != null) group.alpha = 0f; return; }

            bool any = false, held = false;
            var k = Keyboard.current;
            if (k != null)
            {
                any |= k.anyKey.wasPressedThisFrame;
                held |= k.spaceKey.isPressed || k.enterKey.isPressed || k.numpadEnterKey.isPressed || k.escapeKey.isPressed;
            }
            var m = Mouse.current;
            if (m != null)
            {
                any |= m.leftButton.wasPressedThisFrame;
                held |= m.leftButton.isPressed;
            }
            var g = Gamepad.current;
            if (g != null)
            {
                any |= g.buttonSouth.wasPressedThisFrame || g.startButton.wasPressedThisFrame;
                held |= g.buttonSouth.isPressed || g.startButton.isPressed;
            }

            if (!held) armed = true;
            if (!armed) held = false;
            if (any) hintUntil = Time.unscaledTime + 2.5f;
            hold = held ? hold + Time.unscaledDeltaTime : Mathf.Max(0f, hold - Time.unscaledDeltaTime * 2f);

            if (group != null)
            {
                bool show = Time.unscaledTime < hintUntil || hold > 0f;
                group.alpha = Mathf.MoveTowards(group.alpha, show ? 1f : 0f, Time.unscaledDeltaTime * 4f);
                fill.rectTransform.sizeDelta = new Vector2(Mathf.Round(BarW * Mathf.Clamp01(hold / HoldTime)), 3);
            }

            if (hold >= HoldTime) Skip();
        }

        private void Skip()
        {
            done = true;
            if (group != null) group.alpha = 0f;

            // Silencia falas e efeitos que estavam tocando (a trilha da tela de resultado volta sozinha, se houver).
            foreach (var src in Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
                if (src != null && src.gameObject.scene == gameObject.scene) src.Stop();

            elapsedField.SetValue(controller, duration);
            if (hasNext && finishMethod != null)
            {
                // Encerra já: o GameBootstrap ouve o fim da cutscene e carrega a próxima cena com fade.
                finishMethod.Invoke(controller, null);
            }
            // Sem próxima cena (CC09/CC10): o próprio Update da cutscene desenha o último quadro
            // (tela de escolha) e chama Finish() no frame seguinte.
        }

        // ------------------------------------------------------------

        private void BuildUI()
        {
            var cgo = new GameObject("PularCutscene", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
            cgo.transform.SetParent(transform, false);
            var canvas = cgo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1500;          // abaixo do fade do GameFlow (2000)
            canvas.pixelPerfect = true;
            var scaler = cgo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(640, 360);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            group = cgo.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            var panel = Box("Painel", cgo.transform, new Color(0.04f, 0.07f, 0.16f, 0.82f), -6, 4, BarW + 12, 24);
            // Canto inferior direito, colado na borda: o relógio das cutscenes fica em cima (esquerda
            // ou direita, conforme a cena) e as legendas ficam no centro.
            var txtGo = new GameObject("Texto", typeof(RectTransform), typeof(Text));
            txtGo.transform.SetParent(panel.transform, false);
            var trt = (RectTransform)txtGo.transform;
            trt.anchorMin = trt.anchorMax = trt.pivot = new Vector2(0, 1);
            trt.anchoredPosition = new Vector2(6, -4);
            trt.sizeDelta = new Vector2(BarW, 12);
            var txt = txtGo.GetComponent<Text>();
            txt.font = FindSceneFont();
            txt.fontSize = 8;
            txt.color = new Color32(0x8E, 0xC1, 0xDA, 0xFF);
            txt.alignment = TextAnchor.UpperRight;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.raycastTarget = false;
            txt.text = Gamepad.current != null ? "SEGURE ESPAÇO / A PARA PULAR" : "SEGURE ESPAÇO PARA PULAR";

            var back = Box("Barra", panel.transform, new Color(0.12f, 0.14f, 0.3f, 1f), 0, 0, BarW, 3);
            var brt = back.rectTransform;
            brt.anchorMin = brt.anchorMax = brt.pivot = new Vector2(0, 0);
            brt.anchoredPosition = new Vector2(6, 4);
            fill = Box("Preenchimento", back.transform, new Color32(0xFF, 0xD3, 0x7A, 0xFF), 0, 0, 0, 3);
            var frt = fill.rectTransform;
            frt.anchorMin = frt.anchorMax = frt.pivot = new Vector2(0, 0);
            frt.anchoredPosition = Vector2.zero;
        }

        /// <summary>Caixa ancorada no canto inferior direito (x negativo = para a esquerda).</summary>
        private static Image Box(string name, Transform parent, Color c, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1, 0);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
            var img = go.GetComponent<Image>();
            img.color = c;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>Usa a mesma fonte das legendas da cutscene (pixel); senão, a fonte padrão.</summary>
        private Font FindSceneFont()
        {
            foreach (var t in Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t != null && t.font != null && t.gameObject.scene == gameObject.scene && t.font.name != "LegacyRuntime")
                    return t.font;
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }
}
