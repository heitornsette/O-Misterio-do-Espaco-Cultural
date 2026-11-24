using System.Collections;
using Misterio.Puzzles;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Misterio.Core
{
    /// <summary>
    /// Linear scene routing for the whole game: cutscene -> floor -> cutscene ...
    /// A persistent overlay canvas provides the fade, so transitions keep the diorama look.
    /// </summary>
    public sealed class GameFlow : MonoBehaviour
    {
        public const string Exterior = RouteRules.Exterior;
        public const string Cutscene01 = RouteRules.Cutscene01;
        public const string Floor1 = RouteRules.Floor1;
        public const string Cutscene02 = RouteRules.Cutscene02;
        public const string Floor2 = RouteRules.Floor2;
        public const string Cutscene03 = RouteRules.Cutscene03;
        public const string Floor3 = RouteRules.Floor3;
        public const string Cutscene04 = RouteRules.Cutscene04;
        public const string Cutscene04A = RouteRules.Cutscene04A;
        public const string Cutscene05 = RouteRules.Cutscene05;
        public const string Floor4 = RouteRules.Floor4;
        public const string Cutscene06 = RouteRules.Cutscene06;
        public const string Cutscene07 = RouteRules.Cutscene07;
        public const string Cutscene08 = RouteRules.Cutscene08;
        public const string Cutscene09 = RouteRules.Cutscene09;
        public const string Cutscene10 = RouteRules.Cutscene10;
        public const string Epilogue = RouteRules.Epilogue;
        public const string Credits = RouteRules.Credits;

        public static GameFlow Instance { get; private set; }

        Image fadeImage;
        bool loading;

        /// <summary>Where the success route continues after each scene.</summary>
        static readonly string[] SuccessChain = RouteRules.SuccessChain;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildFadeCanvas();
        }

        /// <summary>
        /// Returns the flow, creating it when a scene is played on its own. Floors call this so the
        /// route keeps working even if the scene was opened directly in the Editor.
        /// </summary>
        public static GameFlow EnsureExists()
        {
            if (Instance != null) return Instance;
            var existing = FindAnyObjectByType<GameFlow>();
            if (existing != null) return existing;
            var host = new GameObject("GameFlow");
            return host.AddComponent<GameFlow>();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void BuildFadeCanvas()
        {
            var canvasObject = new GameObject("Fade Canvas");
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;
            var imageObject = new GameObject("Fade");
            imageObject.transform.SetParent(canvasObject.transform, false);
            fadeImage = imageObject.AddComponent<Image>();
            fadeImage.color = new Color(0f, 0f, 0f, 0f);
            fadeImage.raycastTarget = false;
            var rect = fadeImage.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>Next scene on the success route, or null when the chain is done.</summary>
        public static string NextInChain(string currentScene)
        {
            for (int i = 0; i < SuccessChain.Length - 1; i++)
                if (SuccessChain[i] == currentScene) return SuccessChain[i + 1];
            return null;
        }

        public void GoToNext()
        {
            string next = NextInChain(SceneManager.GetActiveScene().name);
            if (next == null)
            {
                Debug.LogWarning("[Flow] no next scene after " + SceneManager.GetActiveScene().name);
                return;
            }
            Load(next);
        }

        public void Load(string sceneName)
        {
            if (loading) return;
            loading = true;
            StartCoroutine(LoadRoutine(sceneName));
        }

        IEnumerator LoadRoutine(string sceneName)
        {
            Debug.Log("[Flow] loading " + sceneName + " (fade out)");
            yield return FadeTo(1f, 0.45f);
            GameSession.EnsureExists().SetClockRunning(false);

            Debug.Log("[Flow] starting async load of " + sceneName);
            var operation = SceneManager.LoadSceneAsync(sceneName);
            if (operation == null)
            {
                Debug.LogError("[Flow] LoadSceneAsync returned null for " + sceneName);
                loading = false;
                yield break;
            }
            while (!operation.isDone) yield return null;
            Debug.Log("[Flow] " + sceneName + " loaded");
            loading = false;
            StartCoroutine(FadeTo(0f, 0.45f));
        }

        public IEnumerator FadeTo(float alpha, float duration)
        {
            if (fadeImage == null) yield break;
            float start = fadeImage.color.a;
            float time = 0f;
            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(time / duration);
                var color = fadeImage.color;
                color.a = Mathf.Lerp(start, alpha, t);
                fadeImage.color = color;
                yield return null;
            }
            var final = fadeImage.color;
            final.a = alpha;
            fadeImage.color = final;
        }

        public void FadeIn(float duration = 0.45f) => StartCoroutine(FadeTo(0f, duration));
    }
}
