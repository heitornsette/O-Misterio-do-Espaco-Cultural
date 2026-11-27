using System.Collections.Generic;
using System.Linq;
using EspacoCultural.Tempo;
using EspacoCultural.Tempo.EditorTools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace EspacoCultural.Campus.EditorTools
{
    /// <summary>
    /// Menu "Espaço Cultural › Campus › Criar cenas do campus".
    /// Cria Assets/Scenes/Campus, CC_Interior, Biblioteca e Ginasio: câmera ortográfica com
    /// Pixel Perfect Camera (URP 2D, 640×360, 1 px = 1 unidade), o objeto da cena (o conteúdo é montado
    /// em tempo de execução pelos scripts de Scripts/Campus) e o relógio/HUD do relógio.
    /// Também coloca as quatro cenas na lista do build.
    /// </summary>
    public static class CampusScenesBuilder
    {
        private const string ScenesDir = "Assets/Scenes";

        [MenuItem("Espaço Cultural/Campus/Criar cenas do campus")]
        public static void BuildAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            AssetDatabase.Refresh();
            if (AssetDatabase.IsValidFolder("Assets/Resources/Campus"))
                AssetDatabase.ImportAsset("Assets/Resources/Campus", ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);

            var paths = new List<string>
            {
                Build<CampusExterior>("Campus"),
                Build<CCInterior>("CC_Interior"),
                Build<BibliotecaInterior>("Biblioteca"),
                Build<GinasioInterior>("Ginasio"),
            };
            AddToBuild(paths);
            EditorSceneManager.OpenScene(paths[0]);
            Debug.Log("[Campus] Cenas do campus criadas e adicionadas ao build.");
        }

        public static void AddToBuild(IEnumerable<string> paths)
        {
            var list = EditorBuildSettings.scenes.ToList();
            foreach (var p in paths)
                if (System.IO.File.Exists(p) && !list.Any(s => s.path == p)) list.Add(new EditorBuildSettingsScene(p, true));
            EditorBuildSettings.scenes = list.ToArray();
        }

        private static string Build<T>(string name) where T : CampusScene
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 180f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.06f, 0.14f);
            camGo.transform.position = new Vector3(320, 180, -10);
            var ppc = camGo.AddComponent<PixelPerfectCamera>();
            ppc.assetsPPU = 1;
            ppc.refResolutionX = 640;
            ppc.refResolutionY = 360;
            ppc.gridSnapping = PixelPerfectCamera.GridSnapping.PixelSnapping;

            var go = new GameObject(name);
            var comp = go.AddComponent<T>();
            comp.fontTitle = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/PressStart2P-Regular.ttf");
            comp.fontBody = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Silkscreen-Regular.ttf");

            ClockSetupMenu.Build();
            var clockGo = GameObject.Find("GameClock");
            if (clockGo != null)
            {
                var dbg = clockGo.GetComponent<GameClockDebug>();
                if (dbg != null) Object.DestroyImmediate(dbg);
            }

            string path = $"{ScenesDir}/{name}.unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }
    }
}
