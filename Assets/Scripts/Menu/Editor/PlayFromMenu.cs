using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EspacoCultural.EditorTools
{
    /// <summary>
    /// Faz o botão Play do Editor sempre começar pelo MenuInicial, qualquer que seja a cena aberta
    /// (o Unity, por padrão, roda a cena que está aberta). Ao sair do Play, o Editor volta para a cena
    /// que você estava editando. Também garante que o MenuInicial seja a cena 0 do build.
    /// Desligue em "Espaço Cultural › Play começa pelo menu inicial" para testar uma cena isolada.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayFromMenu
    {
        private const string MenuScenePath = "Assets/Scenes/MenuInicial.unity";
        private const string PrefKey = "EspacoCultural.PlayFromMenu";
        private const string MenuPath = "Espaço Cultural/Play começa pelo menu inicial";

        static PlayFromMenu()
        {
            EditorApplication.delayCall += Apply;
        }

        private static bool Enabled
        {
            get => EditorPrefs.GetBool(PrefKey, true);
            set => EditorPrefs.SetBool(PrefKey, value);
        }

        [UnityEditor.MenuItem(MenuPath, false, 0)]
        private static void Toggle()
        {
            Enabled = !Enabled;
            Apply();
            Debug.Log(Enabled ? "[Play] O Play agora começa pelo MenuInicial."
                              : "[Play] O Play agora começa pela cena aberta.");
        }

        [UnityEditor.MenuItem(MenuPath, true)]
        private static bool ToggleValidate()
        {
            UnityEditor.Menu.SetChecked(MenuPath, Enabled);
            return true;
        }

        private static void Apply()
        {
            var menu = AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuScenePath);
            if (menu == null)
            {
                EditorSceneManager.playModeStartScene = null;
                Debug.LogWarning("[Play] MenuInicial não encontrado em " + MenuScenePath);
                return;
            }
            EditorSceneManager.playModeStartScene = Enabled ? menu : null;

            // O menu precisa ser a primeira cena do build (é ela que abre no jogo exportado).
            var list = EditorBuildSettings.scenes.ToList();
            int i = list.FindIndex(s => s.path == MenuScenePath);
            if (i == 0 && list[0].enabled) return;
            if (i > 0) list.RemoveAt(i);
            if (i != 0) list.Insert(0, new EditorBuildSettingsScene(MenuScenePath, true));
            else list[0] = new EditorBuildSettingsScene(MenuScenePath, true);
            EditorBuildSettings.scenes = list.ToArray();
            Debug.Log("[Play] MenuInicial colocado como cena 0 do build.");
        }
    }
}
