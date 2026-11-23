#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Misterio.EditorTools
{
    /// <summary>Renders one frame of a scene camera into a RenderTexture, screen-space UI included.</summary>
    public static class CutsceneShot
    {
        static readonly List<object[]> restore = new List<object[]>();

        /// <summary>Moves overlay canvases onto the capture camera so the UI lands in the RenderTexture.</summary>
        public static void PrepareUi(Camera cam)
        {
            RestoreUi();
            foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                restore.Add(new object[] { canvas, canvas.worldCamera, canvas.planeDistance });
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 1f;
            }
        }

        public static void RestoreUi()
        {
            foreach (var entry in restore)
            {
                var canvas = (Canvas)entry[0];
                if (canvas == null) continue;
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.worldCamera = (Camera)entry[1];
                canvas.planeDistance = (float)entry[2];
            }
            restore.Clear();
        }

        public static void Save(Camera cam, int width, int height, string path)
        {
            var rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            RenderTexture previousTarget = cam.targetTexture;
            RenderTexture previousActive = RenderTexture.active;

            cam.targetTexture = rt;
            Canvas.ForceUpdateCanvases();
            cam.Render();
            cam.targetTexture = previousTarget;

            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            RenderTexture.active = previousActive;
            RenderTexture.ReleaseTemporary(rt);

            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        public static double AverageLuminance(string path)
        {
            if (!File.Exists(path)) return -1;
            var tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
            tex.LoadImage(File.ReadAllBytes(path));
            var pixels = tex.GetPixels();
            double total = 0;
            foreach (var p in pixels) total += p.r + p.g + p.b;
            UnityEngine.Object.DestroyImmediate(tex);
            return total / (pixels.Length * 3.0);
        }
    }

    /// <summary>
    /// Renders every cutscene to a PNG sequence without entering Play Mode (which never starts in batch mode).
    /// Scenes exposing the Editor frame-review API are sampled exactly; the others have their timeline pinned
    /// and their own Update() invoked so the visuals converge to the requested instant.
    /// </summary>
    public static class CutsceneVideoExporter
    {
        const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags AnyStatic = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        public const int Width = 1280;
        public const int Height = 720;
        public const int Fps = 60;
        const int ConvergePasses = 24;

        sealed class Job
        {
            public string Scene;
            public string ControllerType;
            public string Slug;
            public float Duration;
            public bool Exact;
            public int Floor;
            public string DurationField = "Duration";
        }

        static readonly Job[] Jobs =
        {
            new Job { Scene = "Assets/Scenes/CC01_A_Madrugada_Comeca.unity", ControllerType = "CC01CutsceneController", Slug = "CC01_A_Madrugada_Comeca", Duration = 35.7f },
            new Job { Scene = "Assets/Scenes/CC02_Acesso_a_Galeria.unity", ControllerType = "CC02CutsceneController", Slug = "CC02_Acesso_a_Galeria", Duration = 8f },
            new Job { Scene = "Assets/Scenes/CC03_A_Galeria_Reorganizada.unity", ControllerType = "CC03CutsceneController", Slug = "CC03_A_Galeria_Reorganizada", Duration = 9f },
            new Job { Scene = "Assets/Scenes/CC04_Passagem_Silenciosa.unity", ControllerType = "CC04CutsceneController", Slug = "CC04_Passagem_Silenciosa", Duration = 9f },
            new Job { Scene = "Assets/Scenes/CC04A_Alerta_de_Moema.unity", ControllerType = "CC04ACutsceneController", Slug = "CC04A_Alerta_de_Moema", Duration = 4f, Exact = true },
            new Job { Scene = "Assets/Scenes/CC05_A_Obra_no_Escuro.unity", ControllerType = "CC05CutsceneController", Slug = "CC05_A_Obra_no_Escuro", Duration = 10f, Exact = true },
            new Job { Scene = "Assets/Scenes/CC06_A_Energia_Retorna.unity", ControllerType = "CC06CutsceneController", Slug = "CC06_A_Energia_Retorna", Duration = 12f, Exact = true },
            new Job { Scene = "Assets/Scenes/CC07_A_Trilha_de_Moema.unity", ControllerType = "CC07CutsceneController", Slug = "CC07_A_Trilha_de_Moema", Duration = 15f, Exact = true },
            new Job { Scene = "Assets/Scenes/CC08_A_Abertura_do_Dia.unity", ControllerType = "CC08CutsceneController", Slug = "CC08_A_Abertura_do_Dia", Duration = 25f, Exact = true },
            new Job { Scene = "Assets/Scenes/CC09_A_Abertura_Precisa_Esperar.unity", ControllerType = "CC09CutsceneController", Slug = "CC09_Andar_1_Terreo", Duration = 14f, Exact = true, Floor = 1 },
            new Job { Scene = "Assets/Scenes/CC09_A_Abertura_Precisa_Esperar.unity", ControllerType = "CC09CutsceneController", Slug = "CC09_Andar_2_Galeria", Duration = 14f, Exact = true, Floor = 2 },
            new Job { Scene = "Assets/Scenes/CC09_A_Abertura_Precisa_Esperar.unity", ControllerType = "CC09CutsceneController", Slug = "CC09_Andar_3_Acervo", Duration = 14f, Exact = true, Floor = 3 },
            new Job { Scene = "Assets/Scenes/CC10_A_Ultima_Sala_Ainda_no_Escuro.unity", ControllerType = "CC10CutsceneController", Slug = "CC10_A_Ultima_Sala_Ainda_no_Escuro", Duration = 15f, Exact = true },
            new Job { Scene = "Assets/Scenes/Credits.unity", ControllerType = "CreditsRollController", Slug = "Creditos", Duration = 29f, DurationField = "TotalDuration" },
        };

        static string framesRoot;
        static int jobIndex;
        static int lastJobIndex;
        static Job job;
        static Component controller;
        static Camera cam;
        static float duration;
        static int totalFrames;
        static bool probeMode;
        static int probeFrames;
        static double lastReport;
        static int framesSinceReport;
        static double exportStart;
        static readonly List<string> summary = new List<string>();
        static readonly List<string> warnings = new List<string>();

        [MenuItem("Misterio/Export/Render all cutscenes to PNG frames")]
        public static void ExportAll() => Begin(0, Jobs.Length - 1, false);

        [MenuItem("Misterio/Export/Probe capture (CC01 + CC04A)")]
        public static void Probe() => Begin(0, 4, true);

        [MenuItem("Misterio/Export/Render CC09 floor variants")]
        public static void ExportCc09() => Begin(9, 11, false);

        static void Begin(int first, int last, bool probe)
        {
            probeMode = probe;
            probeFrames = 5;
            framesRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/CutsceneVideoFrames"));
            Directory.CreateDirectory(framesRoot);
            jobIndex = first - 1;
            lastJobIndex = last;
            exportStart = EditorApplication.timeSinceStartup;
            Debug.Log("[EXPORT] start probe=" + probeMode + " root=" + framesRoot + " graphics=" + SystemInfo.graphicsDeviceType);
            RunNextJob();
        }

        static void RunNextJob()
        {
            jobIndex++;
            if (jobIndex > lastJobIndex) { FinishAll(); return; }
            job = Jobs[jobIndex];
            Directory.CreateDirectory(Path.Combine(framesRoot, job.Slug));
            try
            {
                RunJob();
            }
            catch (Exception error)
            {
                Debug.LogError("[EXPORT] failed " + job.Slug + ": " + error);
                summary.Add(job.Slug + " FAILED " + error.GetType().Name + " " + error.Message);
                RunNextJob();
                return;
            }
            summary.Add(job.Slug + " frames=" + totalFrames + " duration=" + duration.ToString("F2", CultureInfo.InvariantCulture));
            Debug.Log("[EXPORT] done " + job.Slug + " frames=" + totalFrames);
            RunNextJob();
        }

        static void RunJob()
        {
            EditorSceneManager.OpenScene(job.Scene, OpenSceneMode.Single);
            controller = FindController(job.ControllerType);
            cam = Camera.main != null ? Camera.main : UnityEngine.Object.FindAnyObjectByType<Camera>();
            if (controller == null) throw new InvalidOperationException("controller " + job.ControllerType + " not found");
            if (cam == null) throw new InvalidOperationException("no camera in scene");

            duration = ReadConstant(job.DurationField, job.Duration);
            totalFrames = Mathf.Max(1, Mathf.RoundToInt(duration * Fps));
            // Awake() does not run for scene objects in edit mode, and every controller caches its
            // start positions there, so initialize explicitly before anything samples the timeline.
            Invoke(controller, "Awake");
            if (job.Floor > 0) Invoke(controller, "SetIncompleteFloor", job.Floor);
            CutsceneShot.PrepareUi(cam);
            if (job.Exact) Invoke(controller, "PrepareEditorFrameReviewCapture");
            else PrepareGeneric();

            int frames = probeMode ? Mathf.Min(probeFrames, totalFrames) : totalFrames;
            lastReport = EditorApplication.timeSinceStartup;
            framesSinceReport = 0;
            for (int index = 0; index < frames; index++)
            {
                float t = index / (float)Fps;
                if (probeMode) t = duration * index / Mathf.Max(1, frames - 1) * 0.999f;
                if (job.Exact) Invoke(controller, "SetEditorReviewTime", t);
                else ApplyGeneric(t);

                string path = Path.Combine(framesRoot, job.Slug, job.Slug + "_" + index.ToString("0000", CultureInfo.InvariantCulture) + ".png");
                CutsceneShot.Save(cam, Width, Height, path);
                framesSinceReport++;

                double now = EditorApplication.timeSinceStartup;
                if (now - lastReport > 5.0 || index == frames - 1)
                {
                    Debug.Log("[EXPORT] " + job.Slug + " " + (index + 1) + "/" + frames +
                              " fps=" + (framesSinceReport / Math.Max(0.001, now - lastReport)).ToString("F1", CultureInfo.InvariantCulture) +
                              " lum=" + CutsceneShot.AverageLuminance(path).ToString("F4", CultureInfo.InvariantCulture));
                    lastReport = now;
                    framesSinceReport = 0;
                }
            }
            CutsceneShot.RestoreUi();
            if (probeMode) Debug.Log("[PROBE] " + job.Slug + " frames=" + frames + " dir=" + Path.Combine(framesRoot, job.Slug));
        }

        static void PrepareGeneric()
        {
            SetField("startPausedForEditorReview", false);
            SetField("skipFirstReviewUpdate", false);
            SetField("finished", false);
        }

        /// <summary>Pins the controller timeline to <paramref name="t"/> and lets its own Update() settle.</summary>
        static void ApplyGeneric(float t)
        {
            var type = controller.GetType();
            var update = type.GetMethod("Update", AnyInstance);
            var elapsedField = type.GetField("elapsed", AnyInstance);
            var lastField = type.GetField("lastRealtime", AnyInstance);
            var finishedField = type.GetField("finished", AnyInstance);
            if (update == null || elapsedField == null)
            {
                warnings.Add("cannot drive " + type.Name);
                return;
            }

            for (int pass = 0; pass < ConvergePasses; pass++)
            {
                if (finishedField != null) finishedField.SetValue(controller, false);
                elapsedField.SetValue(controller, t);
                if (lastField != null) lastField.SetValue(controller, Time.realtimeSinceStartupAsDouble);
                update.Invoke(controller, null);
            }
            if (finishedField != null) finishedField.SetValue(controller, false);
            elapsedField.SetValue(controller, t);
            Canvas.ForceUpdateCanvases();
        }

        static void SetField(string name, object value)
        {
            var field = controller.GetType().GetField(name, AnyInstance);
            if (field != null) field.SetValue(controller, value);
        }

        static void FinishAll()
        {
            File.WriteAllLines(Path.Combine(framesRoot, "export_summary.txt"), summary);
            File.WriteAllLines(Path.Combine(framesRoot, "export_warnings.txt"), warnings);
            Debug.Log("[EXPORT] ALL DONE in " + (EditorApplication.timeSinceStartup - exportStart).ToString("F0") + "s\n" + string.Join("\n", summary));
            EditorApplication.Exit(0);
        }

        static Component FindController(string typeName)
        {
            foreach (var behaviour in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (behaviour != null && behaviour.GetType().Name == typeName) return behaviour;
            return null;
        }

        static float ReadConstant(string fieldName, float fallback)
        {
            var field = controller.GetType().GetField(fieldName, AnyStatic);
            if (field == null) return fallback;
            var value = field.GetValue(null);
            return value is float f ? f : fallback;
        }

        static object Invoke(object target, string methodName, params object[] args)
        {
            var method = target.GetType().GetMethod(methodName, AnyInstance);
            if (method == null) { warnings.Add("missing method " + methodName + " on " + target.GetType().Name); return null; }
            return method.Invoke(target, args);
        }
    }
}
#endif