using Misterio.Gameplay;
using UnityEditor;
using UnityEngine;

namespace Misterio.EditorTools
{
    /// <summary>
    /// Builds the ground floor scene: the statues puzzle in the same diorama the CC01/CC02
    /// cutscenes use, so the hand-off from video to gameplay keeps the same framing.
    /// </summary>
    public static class Floor1SceneBuilder
    {
        const string ScenePath = "Assets/Scenes/Floor1_Estatuas.unity";
        const string BackgroundPath = "Assets/Art/CC02/andar1.jpg";
        const string AmbiencePath = "Assets/Audio/CC02/cc02_ambiente_noturno.wav";

        [MenuItem("Misterio/Build Floor 1 - Estatuas")]
        public static void Build()
        {
            var camera = FloorSceneKit.NewDioramaScene(
                "Andar 1 - Estatuas",
                new Color(0.018f, 0.035f, 0.09f),
                0.82f,
                new Vector2(-0.34f, -0.10f),
                "Camera - Andar 1 (384x216)");

            FloorSceneKit.AddBackground(camera, BackgroundPath, "Térreo - fundo");

            var statueSprites = new[]
            {
                FloorSceneKit.MakePlaceholder("estatua_pescador_placeholder", new Color(0.66f, 0.46f, 0.28f), 28, 56),
                FloorSceneKit.MakePlaceholder("estatua_rendeira_placeholder", new Color(0.72f, 0.52f, 0.32f), 28, 56),
                FloorSceneKit.MakePlaceholder("estatua_vaqueiro_placeholder", new Color(0.60f, 0.42f, 0.26f), 28, 56)
            };
            var markSprite = FloorSceneKit.MakePlaceholder("marcacao_piso_placeholder", new Color(0.92f, 0.94f, 1f), 26, 16);

            var statues = new Transform[3];
            var marks = new Transform[3];
            Vector2[] statueCells = { new Vector2(4f, 2f), new Vector2(6f, 4f), new Vector2(3f, 5f) };
            Vector2[] markCells = { new Vector2(7f, 2f), new Vector2(8f, 4f), new Vector2(5f, 5f) };
            Vector2 gridOrigin = new Vector2(-1.75f, -0.88f);
            Vector2 cellSize = new Vector2(0.24f, 0.2f);

            for (int i = 0; i < 3; i++)
            {
                Vector2 markPosition = gridOrigin + new Vector2(markCells[i].x * cellSize.x, markCells[i].y * cellSize.y);
                var mark = FloorSceneKit.MakeSprite("Marcação " + (i + 1), markSprite, markPosition, 4);
                mark.transform.localScale = new Vector3(0.7f, 0.55f, 1f);
                var markColor = mark.color;
                markColor.a = 0.85f;
                mark.color = markColor;
                marks[i] = mark.transform;

                Vector2 statuePosition = gridOrigin + new Vector2(statueCells[i].x * cellSize.x, statueCells[i].y * cellSize.y);
                var statue = FloorSceneKit.MakeSprite("Estátua " + (i + 1), statueSprites[i], statuePosition, 8);
                statue.transform.localScale = new Vector3(0.42f, 0.42f, 1f);
                statues[i] = statue.transform;
            }

            var guard = FloorSceneKit.AddGuard(new Vector2(-1.28f, -0.48f));

            var audio = FloorSceneKit.AddAudio(AmbiencePath);
            if (audio.ambient.clip != null) audio.ambient.Play();

            var controllerObject = new GameObject("Andar 1 - Controlador");
            var controller = controllerObject.AddComponent<Floor1Controller>();
            FloorSceneKit.ApplyField(controller, "floorNumber", 1);
            FloorSceneKit.ApplyField(controller, "objectiveText", "Devolva as estátuas às marcações no piso.");
            FloorSceneKit.ApplyField(controller, "gridOrigin", gridOrigin);
            FloorSceneKit.ApplyField(controller, "cellSize", cellSize);
            FloorSceneKit.ApplyField(controller, "width", 12);
            FloorSceneKit.ApplyField(controller, "height", 8);
            FloorSceneKit.SetArray(controller, "statueViews", statues);
            FloorSceneKit.SetArray(controller, "markViews", marks);

            FloorSceneKit.SaveAndRegister(ScenePath);
            Debug.Log("Floor 1 (Estatuas) built at " + ScenePath);
        }
    }
}
