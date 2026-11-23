using System.Collections;
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
        public const string Cutscene01 = "CC01_A_Madrugada_Comeca";
        public const string Floor1 = "Floor1_Estatuas";
        public const string Cutscene02 = "CC02_Acesso_a_Galeria";
        public const string Floor2 = "Floor2_Galeria";
        public const string Cutscene03 = "CC03_A_Galeria_Reorganizada";
        public const string Floor3 = "Floor3_Acervo";
        public const string Cutscene04 = "CC04_Passagem_Silenciosa";
        public const string Cutscene04A = "CC04A_Alerta_de_Moema";
        public const string Cutscene05 = "CC05_A_Obra_no_Escuro";
        public const string Floor4 = "Floor4_Salao";
        public const string Cutscene06 = "CC06_A_Energia_Retorna";
        public const string Cutscene07 = "CC07_A_Trilha_de_Moema";
        public const string Cutscene08 = "CC08_A_Abertura_do_Dia";
        public const string Cutscene09 = "CC09_A_Abertura_Precisa_Esperar";
        public const string Cutscene10 = "CC10_A_Ultima_Sala_Ainda_no_Escuro";
        public const string Credits = "Credits";

        public static GameFlow Instance { get; private set; }

        Image fadeImage;
        bool loading;

        /// <summary>Where the success route continues after each scene.</summary>
        static readonly string[] SuccessChain =
        {
            Cutscene01, Floor1, Cutscene02, Floor2, Cutscene03, Floor3,
            Cutscene04, Cutscene05, Floor4, Cutscene06, Cutscene07, Cutscene08, Credits
        };

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
            StartCoroutine(LoadRoutine(sceneName));
        }

        IEnumerator LoadRoutine(string sceneName)
        {
            loading = true;
            yield return FadeTo(1f, 0.45f);
            var session = GameSession.EnsureExists();
            session.SetClockRunning(false);
            var operation = SceneManager.LoadSceneAsync(sceneName);
            if (operation != null)
                while (!operation.isDone) yield return null;
            yield return null;
            yield return FadeTo(0f, 0.45f);
            loading = false;
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
