using Misterio.Gameplay;
using UnityEditor;
using UnityEngine;

namespace Misterio.EditorTools
{
    /// <summary>Builds the main hall floor: the conduit panel, its cursor and the ambient glow.</summary>
    public static class Floor4SceneBuilder
    {
        const string ScenePath = "Assets/Scenes/Floor4_Salao.unity";
        const string BackgroundPath = "Assets/Art/CC05/fundo_andar4.png";
        const string AmbiencePath = "Assets/Audio/CC05/cc05_zumbido_eletrico.wav";

        [MenuItem("Misterio/Build Floor 4 - Salao")]
        public static void Build()
        {
            var camera = FloorSceneKit.NewDioramaScene(
                "Andar 4 - Salão Principal",
                new Color(0.025f, 0.03f, 0.075f),
                1.92f,
                new Vector2(-2.48f, 0.10f),
                "Camera - Andar 4 (384x216)");

            FloorSceneKit.AddBackground(camera, BackgroundPath, "Salão - fundo");

            Vector2 panelOrigin = new Vector2(-0.72f, -0.18f);
            Vector2 cellSize = new Vector2(0.28f, 0.24f);
            int width = 5, height = 4;

            var bodySprite = PixelArtFactory.ConduitBody();
            var socketSprite = PixelArtFactory.ConduitSocket();

            // A soft glow behind the panel sells the "damaged circuit" mood of the cutscene.
            var glowSprite = PixelArtFactory.Glow();
            var glow = FloorSceneKit.MakeSprite("Brilho do painel", glowSprite,
                panelOrigin + new Vector2((width - 1) * cellSize.x * 0.5f, (height - 1) * cellSize.y * 0.5f), 13);
            glow.transform.localScale = new Vector3(2.6f, 2.0f, 1f);
            var glowColor = glow.color;
            glowColor.a = 0.22f;
            glow.color = glowColor;

            var views = new ConduitTileView[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    Vector2 position = panelOrigin + new Vector2(x * cellSize.x, y * cellSize.y);
                    var host = new GameObject("Conduíte " + x + "," + y);
                    host.transform.position = new Vector3(position.x, position.y, 0f);

                    var body = host.AddComponent<SpriteRenderer>();
                    body.sprite = bodySprite;
                    body.sortingOrder = 15;
                    body.transform.localScale = new Vector3(0.80f, 0.80f, 1f);

                    var view = host.AddComponent<ConduitTileView>();
                    FloorSceneKit.ApplyField(view, "body", body);
                    FloorSceneKit.ApplyField(view, "socketNorth", MakeSocket(host.transform, "N", new Vector2(0f, 0.095f), socketSprite, 0f));
                    FloorSceneKit.ApplyField(view, "socketEast", MakeSocket(host.transform, "L", new Vector2(0.095f, 0f), socketSprite, 90f));
                    FloorSceneKit.ApplyField(view, "socketSouth", MakeSocket(host.transform, "S", new Vector2(0f, -0.095f), socketSprite, 0f));
                    FloorSceneKit.ApplyField(view, "socketWest", MakeSocket(host.transform, "O", new Vector2(-0.095f, 0f), socketSprite, 90f));
                    views[index] = view;
                }
            }

            var cursorSprite = PixelArtFactory.Cursor();
            var cursor = FloorSceneKit.MakeSprite("Cursor do painel", cursorSprite, panelOrigin, 25);
            cursor.transform.localScale = new Vector3(0.92f, 0.92f, 1f);

            var guard = FloorSceneKit.AddGuard(new Vector2(-1.85f, -0.20f));
            FloorSceneKit.AddVignette(camera);

            var audio = FloorSceneKit.AddAudio(AmbiencePath);
            if (audio.ambient.clip != null) audio.ambient.Play();
            FloorSceneKit.AddSfx();

            var controllerObject = new GameObject("Andar 4 - Controlador");
            var controller = controllerObject.AddComponent<Floor4Controller>();
            FloorSceneKit.ApplyField(controller, "floorNumber", 4);
            FloorSceneKit.ApplyField(controller, "objectiveText", "Gire os conduítes até a energia chegar ao painel.");
            FloorSceneKit.ApplyField(controller, "panelOrigin", panelOrigin);
            FloorSceneKit.ApplyField(controller, "cellSize", cellSize);
            FloorSceneKit.ApplyField(controller, "width", width);
            FloorSceneKit.ApplyField(controller, "height", height);
            FloorSceneKit.ApplyField(controller, "source", new Vector2Int(0, 1));
            FloorSceneKit.ApplyField(controller, "target", new Vector2Int(4, 2));
            FloorSceneKit.ApplyField(controller, "cursorView", cursor.transform);
            FloorSceneKit.SetArray(controller, "tileViews", views);

            FloorSceneKit.SaveAndRegister(ScenePath);
            Debug.Log("Floor 4 (Salao) built at " + ScenePath);
        }

        static SpriteRenderer MakeSocket(Transform parent, string name, Vector2 offset, Sprite sprite, float rotation)
        {
            var host = new GameObject("Bocal " + name);
            host.transform.SetParent(parent, false);
            host.transform.localPosition = new Vector3(offset.x, offset.y, 0f);
            host.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
            var renderer = host.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 16;
            renderer.transform.localScale = new Vector3(0.75f, 0.75f, 1f);
            return renderer;
        }
    }
}
