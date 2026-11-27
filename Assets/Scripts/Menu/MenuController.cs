using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EspacoCultural.Menu
{
    /// <summary>
    /// Tela de início: INICIAR, CONFIGURAÇÕES, CRÉDITOS e SAIR.
    /// - Teclado (setas/WASD + Enter/Espaço), controle e mouse.
    /// - Esc / botão B (controle) volta dos painéis para o menu.
    /// - Fade para preto ao iniciar e ao sair.
    /// </summary>
    public class MenuController : MonoBehaviour
    {
        [Header("Painéis")]
        public GameObject mainPanel;
        public GameObject configPanel;
        public GameObject creditsPanel;

        [Header("Primeira seleção de cada painel")]
        public Selectable firstMain;
        public Selectable firstConfig;
        public Selectable firstCredits;

        [Header("Botões do menu (para voltar ao item certo)")]
        public Selectable configButton;
        public Selectable creditsButton;

        [Header("Configurações")]
        public OptionRow musicRow;
        public OptionRow sfxRow;
        public OptionRow fullscreenRow;
        public OptionRow resolutionRow;

        [Header("Fade")]
        public Image fadeImage;
        public float fadeDuration = 0.5f;

        [Header("Jogo")]
        [Tooltip("Cena carregada ao escolher INICIAR (precisa estar no Build Profile / Scene List).")]
        public string gameSceneName = "SampleScene";

        private static MenuController instance;
        private GameObject lastSelected;
        private bool busy;

        private void Awake()
        {
            instance = this;
            SetupConfigRows();
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        private void Start()
        {
            ShowMain(firstMain);
            if (fadeImage != null) StartCoroutine(Fade(1f, 0f));
        }

        /// <summary>Chamado pelos itens quando são selecionados, para lembrar a última seleção.</summary>
        public static void NotifySelected(GameObject go)
        {
            if (instance != null) instance.lastSelected = go;
        }

        // ---------------- Ações dos botões ----------------

        public void OnIniciar()
        {
            if (busy) return;
            if (!Application.CanStreamedLevelBeLoaded(gameSceneName))
            {
                Debug.LogWarning($"[Menu] A cena '{gameSceneName}' não está na lista de cenas do build. " +
                                 "Adicione em File › Build Profiles › Scene List.");
                return;
            }
            EspacoCultural.Audio.GameAudio.Sfx("sfx_menu_confirmar");
            StartCoroutine(StartGame());
        }

        public void OnConfiguracoes() => ShowPanel(configPanel, firstConfig);
        public void OnCreditos() => ShowPanel(creditsPanel, firstCredits);

        public void OnSair()
        {
            if (busy) return;
            StartCoroutine(Quit());
        }

        public void OnVoltar()
        {
            Selectable back = configPanel != null && configPanel.activeSelf ? configButton
                            : creditsPanel != null && creditsPanel.activeSelf ? creditsButton
                            : firstMain;
            ShowMain(back != null ? back : firstMain);
        }

        // ---------------- Painéis ----------------

        private void ShowMain(Selectable select)
        {
            if (configPanel != null) configPanel.SetActive(false);
            if (creditsPanel != null) creditsPanel.SetActive(false);
            if (mainPanel != null) mainPanel.SetActive(true);
            Select(select);
        }

        private void ShowPanel(GameObject panel, Selectable select)
        {
            if (busy || panel == null) return;
            EspacoCultural.Audio.GameAudio.Sfx("sfx_menu_confirmar");
            if (mainPanel != null) mainPanel.SetActive(false);
            if (configPanel != null) configPanel.SetActive(panel == configPanel);
            if (creditsPanel != null) creditsPanel.SetActive(panel == creditsPanel);
            Select(select);
        }

        private void Select(Selectable s)
        {
            if (s == null || EventSystem.current == null) return;
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(s.gameObject);
            lastSelected = s.gameObject;
        }

        private void Update()
        {
            if (busy) return;

            // Voltar com Esc / B do controle / botão direito do mouse.
            bool back = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                     || (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame)
                     || (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame);
            bool inSub = (configPanel != null && configPanel.activeSelf) || (creditsPanel != null && creditsPanel.activeSelf);
            if (back && inSub) { OnVoltar(); return; }

            // Clicar no vazio tira a seleção; devolve para o último item para o teclado continuar funcionando.
            var es = EventSystem.current;
            if (es != null && es.currentSelectedGameObject == null && lastSelected != null && lastSelected.activeInHierarchy)
                es.SetSelectedGameObject(lastSelected);
        }

        // ---------------- Configurações ----------------

        private void SetupConfigRows()
        {
            if (musicRow != null)
            {
                musicRow.SetValue(GameSettings.MusicLevel, false);
                musicRow.onValueChanged.AddListener(v => GameSettings.MusicLevel = v);
            }
            if (sfxRow != null)
            {
                sfxRow.SetValue(GameSettings.SfxLevel, false);
                sfxRow.onValueChanged.AddListener(v => GameSettings.SfxLevel = v);
            }
            if (fullscreenRow != null)
            {
                fullscreenRow.SetChoices(new[] { "NÃO", "SIM" });
                fullscreenRow.SetValue(GameSettings.Fullscreen ? 1 : 0, false);
                fullscreenRow.onValueChanged.AddListener(v => GameSettings.Fullscreen = v == 1);
            }
            if (resolutionRow != null)
            {
                var list = GameSettings.AvailableResolutions();
                var labels = new string[list.Count];
                for (int i = 0; i < list.Count; i++) labels[i] = GameSettings.ResolutionLabel(list[i]);
                resolutionRow.SetChoices(labels);
                resolutionRow.SetValue(GameSettings.ResolutionIndex, false);
                resolutionRow.onValueChanged.AddListener(v => GameSettings.ResolutionIndex = v);
            }
        }

        // ---------------- Transições ----------------

        private IEnumerator StartGame()
        {
            busy = true;
            yield return Fade(0f, 1f);
            var clock = EspacoCultural.Tempo.GameClock.Instance;
            if (clock != null) clock.ResetClock();
            EspacoCultural.Jogo.GameState.NewGame();
            Misterio.Core.GameSession.Instance.TimedOutFloor = 0;
            SceneManager.LoadScene(gameSceneName);
        }

        private IEnumerator Quit()
        {
            busy = true;
            yield return Fade(0f, 1f);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private IEnumerator Fade(float from, float to)
        {
            if (fadeImage == null) yield break;
            fadeImage.raycastTarget = true;
            var c = fadeImage.color;
            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.unscaledDeltaTime;
                // Fade em degraus (8 níveis), mais coerente com o pixel art.
                float k = Mathf.Clamp01(t / fadeDuration);
                k = Mathf.Round(k * 8f) / 8f;
                c.a = Mathf.Lerp(from, to, k);
                fadeImage.color = c;
                yield return null;
            }
            c.a = to;
            fadeImage.color = c;
            fadeImage.raycastTarget = to > 0.01f;
        }
    }
}
