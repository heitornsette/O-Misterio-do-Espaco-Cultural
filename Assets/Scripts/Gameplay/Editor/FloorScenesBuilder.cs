using System.Collections.Generic;
using EspacoCultural.Menu;
using EspacoCultural.Tempo;
using EspacoCultural.Tempo.EditorTools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EspacoCultural.Gameplay.EditorTools
{
    /// <summary>
    /// Menu "Espaço Cultural › Andares › Criar cenas dos 4 andares".
    /// Cria Assets/Scenes/Andar1..Andar4 (câmera + objeto do andar + relógio/HUD do relógio),
    /// atualiza a lista de cenas do build (MenuInicial, Andar1..4) e faz o INICIAR do menu abrir o Andar1.
    /// O conteúdo de cada andar é montado em tempo de execução pelos scripts em Scripts/Gameplay/Andares.
    /// </summary>
    public static class FloorScenesBuilder
    {
        private const string ScenesDir = "Assets/Scenes";
        private const string MenuScenePath = ScenesDir + "/MenuInicial.unity";

        [UnityEditor.MenuItem("Espaço Cultural/Andares/Criar cenas dos 4 andares")]
        public static void BuildAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            AssetDatabase.Refresh();
            // Garante que a arte em Resources/Jogo passou pelo GameplayArtImporter (Point, sem compressão).
            if (AssetDatabase.IsValidFolder("Assets/Resources/Jogo"))
                AssetDatabase.ImportAsset("Assets/Resources/Jogo", ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);

            var paths = new List<string>
            {
                BuildFloor<Andar1Sokoban>("Andar1"),
                BuildFloor<Andar2Galeria>("Andar2"),
                BuildFloor<Andar3Acervo>("Andar3"),
                BuildFloor<Andar4Salao>("Andar4"),
            };

            // Ordem do jogo, com as cutscenes da equipe (Assets/Scenes/Cutscenes) entre os andares.
            System.Func<string, string> C = n => ScenesDir + "/Cutscenes/" + n + ".unity";
            var order = new List<string>
            {
                MenuScenePath,
                C("CC01_A_Madrugada_Comeca"), paths[0],
                C("CC02_Acesso_a_Galeria"), paths[1],
                C("CC03_A_Galeria_Reorganizada"), paths[2],
                C("CC04A_Alerta_de_Moema"), C("CC04_Passagem_Silenciosa"), C("CC05_A_Obra_no_Escuro"), paths[3],
                C("CC06_A_Energia_Retorna"), C("CC07_A_Trilha_de_Moema"), C("CC08_A_Abertura_do_Dia"),
                C("CC09_A_Abertura_Precisa_Esperar"), C("CC10_A_Ultima_Sala_Ainda_no_Escuro"), C("Credits"),
                // Campus (cenas criadas por Espaço Cultural › Campus › Criar cenas do campus)
                ScenesDir + "/Campus.unity", ScenesDir + "/CC_Interior.unity", ScenesDir + "/Biblioteca.unity", ScenesDir + "/Ginasio.unity",
            };
            var scenes = new List<EditorBuildSettingsScene>();
            foreach (var p in order)
                if (System.IO.File.Exists(p)) scenes.Add(new EditorBuildSettingsScene(p, true));
                else Debug.LogWarning("[Andares] Cena não encontrada, fora do build: " + p);
            EditorBuildSettings.scenes = scenes.ToArray();

            PointMenuToFirstFloor();

            EditorSceneManager.OpenScene(paths[0]);
            Debug.Log("[Andares] Cenas Andar1–Andar4 criadas e adicionadas ao build. Abra MenuInicial e dê Play, ou dê Play direto num andar.");
        }

        private static string BuildFloor<T>(string name) where T : FloorBase
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 1.8f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            camGo.transform.position = new Vector3(0, 0, -10);

            var floorGo = new GameObject(name);
            var floor = floorGo.AddComponent<T>();
            floor.fontTitle = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/PressStart2P-Regular.ttf");
            floor.fontBody = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Silkscreen-Regular.ttf");

            // Relógio + HUD do relógio (o GameClock é único e atravessa as cenas).
            ClockSetupMenu.Build();
            var clockGo = GameObject.Find("GameClock");
            if (clockGo != null)
            {
                // Os atalhos de teste do relógio (R, C, F...) conflitam com os controles dos puzzles.
                var dbg = clockGo.GetComponent<GameClockDebug>();
                if (dbg != null) Object.DestroyImmediate(dbg);
            }

            string path = $"{ScenesDir}/{name}.unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        private static void PointMenuToFirstFloor()
        {
            if (!System.IO.File.Exists(MenuScenePath)) return;
            var menu = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            var mc = Object.FindFirstObjectByType<MenuController>();
            if (mc != null)
            {
                var so = new SerializedObject(mc);
                bool hasCC01 = System.IO.File.Exists(ScenesDir + "/Cutscenes/CC01_A_Madrugada_Comeca.unity");
                so.FindProperty("gameSceneName").stringValue = hasCC01 ? "CC01_A_Madrugada_Comeca" : "Andar1";
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(menu);
                EditorSceneManager.SaveScene(menu);
            }
        }
    }
}
