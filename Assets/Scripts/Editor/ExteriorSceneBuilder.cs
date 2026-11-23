using System.Collections.Generic;
using Misterio.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Misterio.EditorTools
{
    /// <summary>
    /// Builds the outdoor approach: a wide strip with five parallax layers, lamps, benches and the
    /// facade, ending at the building entrance.
    /// </summary>
    public static class ExteriorSceneBuilder
    {
        const string ScenePath = "Assets/Scenes/Exterior_Chegada.unity";

        [MenuItem("Misterio/Build Exterior - Chegada")]
        public static void Build()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Camera - Exterior");
            var camera = cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 1.05f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = ExteriorArtFactory.SkyTop;
            camera.transform.position = new Vector3(0.9f, 0f, -10f);
            cameraObject.AddComponent<AudioListener>();

            var world = new GameObject("Mundo - chegada");

            // Layer 1: sky, fixed to the camera.
            var sky = FloorSceneKit.MakeSprite("Céu", ExteriorArtFactory.Sky(), new Vector2(0.9f, 0f), -100);
            sky.transform.localScale = new Vector3(1f, 1f, 1f);
            AttachParallax(sky.transform, camera.transform, 0f, 0f, new Vector2(0.9f, 0f));

            // Layer 2: distant skyline.
            var skyline = FloorSceneKit.MakeSprite("Skyline", ExteriorArtFactory.Skyline(), new Vector2(-2f, -0.10f), -80);
            skyline.transform.localScale = new Vector3(1.6f, 1.2f, 1f);
            AttachParallax(skyline.transform, camera.transform, 0.12f, 8.19f, new Vector2(-2f, -0.10f));

            // Layer 3: tree line.
            var trees = FloorSceneKit.MakeSprite("Arvores", ExteriorArtFactory.TreeLine(), new Vector2(-2f, -0.05f), -60);
            trees.transform.localScale = new Vector3(1.5f, 1.1f, 1f);
            AttachParallax(trees.transform, camera.transform, 0.32f, 7.68f, new Vector2(-2f, -0.05f));

            // Layer 4: plaza paving, near the ground plane.
            var ground = FloorSceneKit.MakeSprite("Praca - piso", ExteriorArtFactory.PlazaGround(), new Vector2(-2f, -0.92f), -40);
            ground.transform.localScale = new Vector3(1.5f, 1f, 1f);
            AttachParallax(ground.transform, camera.transform, 0.85f, 7.68f, new Vector2(-2f, -0.92f));

            // Layer 5: lamps along the walk, at near depth so they sell the speed.
            var lampSprite = ExteriorArtFactory.LampPost();
            for (int i = 0; i < 7; i++)
            {
                float x = -1.2f + i * 2.15f;
                var lamp = FloorSceneKit.MakeSprite("Poste " + i, lampSprite, new Vector2(x, -0.78f), -10);
                lamp.transform.localScale = new Vector3(0.62f, 0.62f, 1f);
                // A warm pool under each lamp.
                var glow = FloorSceneKit.MakeSprite("Luz do poste " + i, LightArtFactory.WarmRim(),
                                                    new Vector2(x, -0.86f), -9);
                glow.transform.localScale = new Vector3(1.1f, 0.55f, 1f);
            }

            // The building the player is heading to.
            var facade = FloorSceneKit.MakeSprite("Fachada", ExteriorArtFactory.Facade(), new Vector2(13.4f, -0.28f), 5);
            facade.transform.localScale = new Vector3(1.15f, 1.15f, 1f);

            // A soft halo in the doorway so the entrance reads as the goal.
            var doorway = FloorSceneKit.MakeSprite("Brilho da entrada", LightArtFactory.AmbientPool(),
                                                  new Vector2(13.4f, -0.55f), 6);
            doorway.transform.localScale = new Vector3(0.9f, 0.7f, 1f);
            var doorColor = doorway.color;
            doorColor.a = 0.30f;
            doorway.color = doorColor;

            var guard = FloorSceneKit.AddGuard(new Vector2(-0.9f, -0.80f));
            guard.Configure(new Rect(-1.95f, -0.95f, 15.6f, 0.30f), new List<Rect>());

            // Dust and a light layer keep the outdoor scene from feeling flat.
            var lights = new List<SpriteRenderer> { doorway };
            FloorSceneKit.AddAtmosphere(camera, lights, guard.transform, 20);
            FloorSceneKit.AddVignette(camera);

            var audio = FloorSceneKit.AddAudio("Assets/Audio/CC02/cc02_ambiente_noturno.wav");
            if (audio.ambient.clip != null) audio.ambient.Play();
            FloorSceneKit.AddSfx();

            var controllerObject = new GameObject("Exterior - Controlador");
            var controller = controllerObject.AddComponent<ExteriorScene>();
            FloorSceneKit.ApplyField(controller, "guard", guard);
            FloorSceneKit.ApplyField(controller, "cameraTransform", camera.transform);
            FloorSceneKit.ApplyField(controller, "worldLeft", -1.90f);
            FloorSceneKit.ApplyField(controller, "worldRight", 13.0f);
            FloorSceneKit.ApplyField(controller, "cameraOffset", 0.9f);
            FloorSceneKit.ApplyField(controller, "entranceX", 12.6f);
            FloorSceneKit.ApplyField(controller, "objectiveText", "Atravesse a praça e entre no Espaço Cultural.");

            FloorSceneKit.SaveAndRegister(ScenePath);
            Debug.Log("Exterior (Chegada) built at " + ScenePath);
            EditorApplication.Exit(0);
        }

        static void AttachParallax(Transform target, Transform cameraTransform, float factor, float width, Vector2 origin)
        {
            var layer = target.gameObject.AddComponent<ParallaxLayer>();
            layer.Configure(cameraTransform, factor, width, origin);
        }
    }
}
