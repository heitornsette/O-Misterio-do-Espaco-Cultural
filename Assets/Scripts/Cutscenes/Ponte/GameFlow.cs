using System.Collections;
using EspacoCultural.Tempo;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Misterio.Core
{
    /// <summary>
    /// PONTE: troca de cena com fade, usada pelas cutscenes da equipe (CC09/CC10 chamam
    /// GameFlow.Instance.Load(...)). Os nomes Exterior e Floor4 apontam para as nossas cenas.
    /// </summary>
    public sealed class GameFlow : MonoBehaviour
    {
        /// <summary>No nosso jogo a "volta ao início" é o menu inicial.</summary>
        public const string Exterior = "MenuInicial";
        public const string Floor4 = "Andar4";

        public static GameFlow Instance { get; private set; }

        private Image fade;
        private bool loading;

        public static GameFlow EnsureExists()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("GameFlow");
            DontDestroyOnLoad(go);
            return go.AddComponent<GameFlow>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            var canvasGo = new GameObject("Fade", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 2000;
            var imgGo = new GameObject("Preto", typeof(RectTransform), typeof(Image));
            imgGo.transform.SetParent(canvasGo.transform, false);
            var rt = (RectTransform)imgGo.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            fade = imgGo.GetComponent<Image>();
            fade.color = new Color(0, 0, 0, 0);
            fade.raycastTarget = false;
        }

        public void Load(string sceneName)
        {
            if (loading || string.IsNullOrEmpty(sceneName)) return;
            StartCoroutine(LoadRoutine(sceneName));
        }

        private IEnumerator LoadRoutine(string sceneName)
        {
            loading = true;
            yield return Fade(1f, 0.45f);
            Time.timeScale = 1f;

            // "Tentar de novo" depois das 06:00: a noite recomeça às 02:00 naquele andar.
            var clock = GameClock.Instance;
            if (sceneName.StartsWith("Andar") && clock != null && clock.State == GameClock.ClockState.Finished)
                clock.ResetClock();

            if (Application.CanStreamedLevelBeLoaded(sceneName)) SceneManager.LoadScene(sceneName);
            else Debug.LogWarning($"[Cutscene] A cena '{sceneName}' não está na lista de cenas do build.");
            yield return null;
            yield return Fade(0f, 0.45f);
            loading = false;
        }

        private IEnumerator Fade(float target, float duration)
        {
            float start = fade.color.a;
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                fade.color = new Color(0, 0, 0, Mathf.Lerp(start, target, t / duration));
                yield return null;
            }
            fade.color = new Color(0, 0, 0, target);
        }
    }
}
