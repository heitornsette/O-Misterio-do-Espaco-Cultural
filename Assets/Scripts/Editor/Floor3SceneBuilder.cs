using Misterio.Gameplay;
using UnityEditor;
using UnityEngine;

namespace Misterio.EditorTools
{
    /// <summary>Builds the archive floor: Moema's patrol, the loose boards and the four pieces.</summary>
    public static class Floor3SceneBuilder
    {
        const string ScenePath = "Assets/Scenes/Floor3_Acervo.unity";
        const string BackgroundPath = "Assets/Art/CC04A/fundo_andar3.png";
        const string AmbiencePath = "Assets/Audio/CC04/cc04_ambiente_acervo.wav";

        [MenuItem("Misterio/Build Floor 3 - Acervo")]
        public static void Build()
        {
            var camera = FloorSceneKit.NewDioramaScene(
                "Andar 3 - Acervo",
                new Color(0.055f, 0.075f, 0.15f),
                2.35f,
                new Vector2(-0.35f, -0.20f),
                "Camera - Andar 3 (384x216)");

            FloorSceneKit.AddBackground(camera, BackgroundPath, "Acervo - fundo");

            Vector2[] patrol =
            {
                new Vector2(-1.35f, 0.30f), new Vector2(0.05f, 0.30f),
                new Vector2(0.05f, -0.62f), new Vector2(-1.35f, -0.62f)
            };
            Vector2[] pieces =
            {
                new Vector2(-1.10f, -0.30f), new Vector2(0.40f, 0.28f),
                new Vector2(1.15f, -0.10f), new Vector2(1.45f, 0.35f)
            };
            Rect[] boards =
            {
                new Rect(-0.55f, -0.20f, 0.30f, 0.16f),
                new Rect(0.65f, 0.05f, 0.30f, 0.16f),
                new Rect(1.20f, -0.55f, 0.30f, 0.16f)
            };
            Rect[] cases =
            {
                new Rect(-0.30f, 0.55f, 0.55f, 0.22f),
                new Rect(0.85f, 0.55f, 0.55f, 0.22f)
            };

            var boardSprite = PixelArtFactory.LooseBoard();
            var pieceSprite = PixelArtFactory.ArchivePiece();
            var caseSprite = PixelArtFactory.DisplayCase();

            var boardViews = new Transform[boards.Length];
            for (int i = 0; i < boards.Length; i++)
            {
                var rect = boards[i];
                var view = FloorSceneKit.MakeSprite("Tábua solta " + (i + 1), boardSprite,
                    new Vector2(rect.x + rect.width * 0.5f, rect.y + rect.height * 0.5f), 3);
                view.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
                boardViews[i] = view.transform;
            }

            for (int i = 0; i < cases.Length; i++)
            {
                var rect = cases[i];
                var view = FloorSceneKit.MakeSprite("Vitrine " + (i + 1), caseSprite,
                    new Vector2(rect.x + rect.width * 0.5f, rect.y + rect.height * 0.5f), 5);
                view.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
            }

            var pieceViews = new Transform[pieces.Length];
            for (int i = 0; i < pieces.Length; i++)
            {
                var view = FloorSceneKit.MakeSprite("Peça " + (i + 1), pieceSprite, pieces[i], 9);
                view.transform.localScale = new Vector3(0.55f, 0.55f, 1f);
                pieceViews[i] = view.transform;
            }

            var moemaSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Moema_Base.png");
            if (moemaSprite == null)
                moemaSprite = PixelArtFactory.Build("moema_fallback", 26, 52, (x, y) => new Color(0.72f, 0.70f, 0.64f, 1f));
            var moema = FloorSceneKit.MakeSprite("Moema - patrulha", moemaSprite, patrol[0], 11);
            moema.transform.localScale = new Vector3(0.42f, 0.42f, 1f);

            var coneObject = new GameObject("Cone de visão de Moema");
            var cone = coneObject.AddComponent<LineRenderer>();
            cone.useWorldSpace = true;
            cone.positionCount = 3;
            cone.startWidth = 0.014f;
            cone.endWidth = 0.014f;
            cone.material = new Material(Shader.Find("Sprites/Default"));
            cone.startColor = new Color(1f, 0.85f, 0.55f, 0.34f);
            cone.endColor = new Color(1f, 0.85f, 0.55f, 0.06f);
            cone.sortingOrder = 12;

            var guard = FloorSceneKit.AddGuard(new Vector2(-1.45f, 0.72f));
            FloorSceneKit.AddVignette(camera);

            var audio = FloorSceneKit.AddAudio(AmbiencePath);
            if (audio.ambient.clip != null) audio.ambient.Play();
            FloorSceneKit.AddSfx();

            var controllerObject = new GameObject("Andar 3 - Controlador");
            var controller = controllerObject.AddComponent<Floor3Controller>();
            FloorSceneKit.ApplyField(controller, "floorNumber", 3);
            FloorSceneKit.ApplyField(controller, "objectiveText", "Recolha as peças do acervo sem ser visto por Moema.");
            FloorSceneKit.ApplyField(controller, "moemaView", moema.transform);
            FloorSceneKit.ApplyField(controller, "sightCone", cone);
            FloorSceneKit.ApplyField(controller, "patrolSpeed", 0.42f);
            FloorSceneKit.ApplyField(controller, "viewDistance", 1.25f);
            FloorSceneKit.ApplyField(controller, "halfAngleDegrees", 38f);
            FloorSceneKit.SetVector2Array(controller, "patrolPoints", patrol);
            FloorSceneKit.SetVector2Array(controller, "piecePoints", pieces);
            FloorSceneKit.SetRectArray(controller, "noisyBoards", boards);
            FloorSceneKit.SetRectArray(controller, "caseBlockers", cases);
            FloorSceneKit.SetArray(controller, "pieceViews", pieceViews);
            FloorSceneKit.SetArray(controller, "boardViews", boardViews);

            FloorSceneKit.SaveAndRegister(ScenePath);
            Debug.Log("Floor 3 (Acervo) built at " + ScenePath);
        }
    }
}
