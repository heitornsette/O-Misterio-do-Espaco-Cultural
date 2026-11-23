using Misterio.Gameplay;
using UnityEditor;
using UnityEngine;

namespace Misterio.EditorTools
{
    /// <summary>Builds the gallery floor: three paintings, three hooks and the clue plaques.</summary>
    public static class Floor2SceneBuilder
    {
        const string ScenePath = "Assets/Scenes/Floor2_Galeria.unity";
        const string BackgroundPath = "Assets/Art/CC03/galeria_moonlit.png";
        const string AmbiencePath = "Assets/Audio/CC03/cc03_ambiente_galeria.wav";

        [MenuItem("Misterio/Build Floor 2 - Galeria")]
        public static void Build()
        {
            var camera = FloorSceneKit.NewDioramaScene(
                "Andar 2 - Galeria",
                new Color(0.03f, 0.05f, 0.11f),
                0.92f,
                new Vector2(0f, 0.02f),
                "Camera - Andar 2 (384x216)");

            FloorSceneKit.AddBackground(camera, BackgroundPath, "Galeria - fundo");

            var paintingSprites = new[]
            {
                FloorSceneKit.MakePlaceholder("quadro_mare_placeholder", new Color(0.34f, 0.55f, 0.72f), 30, 22),
                FloorSceneKit.MakePlaceholder("quadro_nevoa_placeholder", new Color(0.48f, 0.52f, 0.62f), 30, 22),
                FloorSceneKit.MakePlaceholder("quadro_sertao_placeholder", new Color(0.78f, 0.56f, 0.28f), 30, 22)
            };

            Vector2[] floorPoints =
            {
                new Vector2(-1.05f, -0.62f), new Vector2(0.05f, -0.66f), new Vector2(1.10f, -0.60f)
            };
            Vector2[] hookPoints =
            {
                new Vector2(-0.95f, 0.42f), new Vector2(0.02f, 0.42f), new Vector2(0.98f, 0.42f)
            };

            var floorViews = new Transform[3];
            for (int i = 0; i < 3; i++)
            {
                var view = FloorSceneKit.MakeSprite("Quadro no chão " + (i + 1), paintingSprites[i], floorPoints[i], 6);
                view.transform.localScale = new Vector3(0.55f, 0.55f, 1f);
                floorViews[i] = view.transform;
            }

            var hooks = new Transform[3];
            for (int i = 0; i < 3; i++)
            {
                var hook = FloorSceneKit.MakeSprite("Gancho " + (i + 1), paintingSprites[i], hookPoints[i], 7);
                hook.transform.localScale = new Vector3(0.55f, 0.55f, 1f);
                hook.enabled = false;
                hooks[i] = hook.transform;
            }

            var carried = FloorSceneKit.MakeSprite("Quadro carregado", paintingSprites[0], new Vector2(0f, -0.1f), 20);
            carried.transform.localScale = new Vector3(0.42f, 0.42f, 1f);
            carried.enabled = false;

            var guard = FloorSceneKit.AddGuard(new Vector2(-1.35f, -0.75f));

            var audio = FloorSceneKit.AddAudio(AmbiencePath);
            if (audio.ambient.clip != null) audio.ambient.Play();

            var controllerObject = new GameObject("Andar 2 - Controlador");
            var controller = controllerObject.AddComponent<Floor2Controller>();
            FloorSceneKit.ApplyField(controller, "floorNumber", 2);
            FloorSceneKit.ApplyField(controller, "objectiveText", "Leia as placas e pendure cada quadro no gancho certo.");
            FloorSceneKit.SetArray(controller, "hookViews", hooks);
            FloorSceneKit.SetArray(controller, "floorPaintingViews", floorViews);
            FloorSceneKit.ApplyField(controller, "carriedView", carried);

            FloorSceneKit.SaveAndRegister(ScenePath);
            Debug.Log("Floor 2 (Galeria) built at " + ScenePath);
        }
    }
}
