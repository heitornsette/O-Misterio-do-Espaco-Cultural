using Misterio.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Misterio.EditorTools
{
    /// <summary>
    /// Builds the epilogue on the campus lawn: the dawn garden from CC08, the tree Moema rests
    /// under, the moth settling beside her and the guard walking out of the building.
    /// </summary>
    public static class EpilogueSceneBuilder
    {
        const string ScenePath = "Assets/Scenes/Epilogue_Gramado.unity";

        [MenuItem("Misterio/Build Epilogue - Gramado")]
        public static void Build()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Camera - Epílogo");
            var camera = cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 1.05f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.42f, 0.46f, 0.56f);
            camera.transform.position = new Vector3(0.9f, 0f, -10f);
            cameraObject.AddComponent<AudioListener>();

            // Dawn sky, then the garden the CC08 cutscene already established.
            var sky = FloorSceneKit.MakeSprite("Céu do amanhecer", ExteriorArtFactory.Sky(), new Vector2(0.9f, 0f), -100);
            AttachParallax(sky.transform, camera.transform, 0f, 0f, new Vector2(0.9f, 0f));

            var garden = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/CC08/jardim_amanhecer_arquitetura.png");
            if (garden != null)
            {
                var view = FloorSceneKit.MakeSprite("Jardim - arquitetura", garden, new Vector2(4f, 0.1f), -70);
                view.transform.localScale = new Vector3(3.2f, 1.0f, 1f);
                AttachParallax(view.transform, camera.transform, 0.15f, 12f, new Vector2(4f, 0.1f));
            }

            var trees = FloorSceneKit.MakeSprite("Árvores do campus", ExteriorArtFactory.TreeLine(), new Vector2(-2f, 0.05f), -60);
            trees.transform.localScale = new Vector3(2.4f, 1.15f, 1f);
            AttachParallax(trees.transform, camera.transform, 0.30f, 12f, new Vector2(-2f, 0.05f));

            var ground = FloorSceneKit.MakeSprite("Gramado", ExteriorArtFactory.PlazaGround(), new Vector2(-2f, -0.95f), -40);
            ground.transform.localScale = new Vector3(2.4f, 1.1f, 1f);
            AttachParallax(ground.transform, camera.transform, 0.85f, 12f, new Vector2(-2f, -0.95f));

            // The tree where Moema finally stops following the moth.
            var tree = FloorSceneKit.MakeSprite("Árvore de Moema", ExteriorArtFactory.TreeLine(), new Vector2(16.2f, -0.10f), 4);
            tree.transform.localScale = new Vector3(1.0f, 1.35f, 1f);

            // Moema resting under it.
            var moemaSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Moema_Base.png");
            if (moemaSprite != null)
            {
                var moema = FloorSceneKit.MakeSprite("Moema descansando", moemaSprite, new Vector2(16.0f, -0.78f), 8);
                moema.transform.localScale = new Vector3(0.40f, 0.40f, 1f);
                FloorSceneKit.AddShadow(new Vector2(16.0f, -0.86f), 0.5f, 3);
            }

            // The moth settles on the tree instead of flying away.
            var mothSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Mariposa_Base.png");
            if (mothSprite != null)
            {
                var moth = FloorSceneKit.MakeSprite("Mariposa pousada", mothSprite, new Vector2(16.6f, -0.22f), 9);
                moth.transform.localScale = new Vector3(0.55f, 0.55f, 1f);
            }

            // Warm dawn light across the lawn.
            var lights = new System.Collections.Generic.List<SpriteRenderer>();
            for (int i = 0; i < 5; i++)
            {
                var pool = FloorSceneKit.MakeSprite("Luz do amanhecer " + i, LightArtFactory.AmbientPool(),
                    new Vector2(-1.0f + i * 4.2f, -0.55f), -20);
                pool.transform.localScale = new Vector3(1.4f, 0.8f, 1f);
                var color = pool.color;
                color.a = 0.20f;
                pool.color = color;
                lights.Add(pool);
            }

            var guard = FloorSceneKit.AddGuard(new Vector2(-0.9f, -0.82f));
            guard.Configure(new Rect(-1.55f, -0.95f, 19.6f, 0.55f), new System.Collections.Generic.List<Rect>());
            FloorSceneKit.AssertGuardCanStand(guard, new Vector2(-0.9f, -0.82f));

            FloorSceneKit.AddAtmosphere(camera, lights, guard.transform, 18);
            FloorSceneKit.AddVignette(camera);

            var audio = FloorSceneKit.AddAudio("Assets/Audio/CC08/cc08_ambiencia_exterior.wav");
            if (audio.ambient.clip != null) audio.ambient.Play();

            var controllerObject = new GameObject("Epílogo - Controlador");
            var controller = controllerObject.AddComponent<EpilogueScene>();
            FloorSceneKit.ApplyField(controller, "guard", guard);
            FloorSceneKit.ApplyField(controller, "cameraTransform", camera.transform);
            FloorSceneKit.ApplyField(controller, "worldLeft", -1.50f);
            FloorSceneKit.ApplyField(controller, "worldRight", 17.0f);
            FloorSceneKit.ApplyField(controller, "cameraOffset", 1.0f);
            FloorSceneKit.ApplyField(controller, "treeX", 15.6f);
            FloorSceneKit.ApplyField(controller, "objectiveText", "A inauguração começou. Atravesse o gramado.");

            FloorSceneKit.EnsureBootstrap();
            FloorSceneKit.SaveAndRegister(ScenePath);
            Debug.Log("Epilogue (Gramado) built at " + ScenePath);
            EditorApplication.Exit(0);
        }

        static void AttachParallax(Transform target, Transform cameraTransform, float factor, float width, Vector2 origin)
        {
            var layer = target.gameObject.AddComponent<ParallaxLayer>();
            layer.Configure(cameraTransform, factor, width, origin);
        }
    }
}
