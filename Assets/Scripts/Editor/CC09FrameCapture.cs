#if UNITY_EDITOR
using System.IO;
using Misterio.Cutscenes;
using UnityEditor;
using UnityEngine;

namespace Misterio.EditorTools
{
    [InitializeOnLoad]
    public static class CC09FrameCapture
    {
        const string CaptureKey = "Misterio.CC09.FrameCapture.Armed";
        const string FrameKey = "Misterio.CC09.FrameCapture.Index";
        const string FloorKey = "Misterio.CC09.FrameCapture.Floor";
        const string FrameRoot = "Library/CC09ReviewFrames";
        const int SamplesPerFloor = 841;
        const int FloorCount = 3;

        static bool armed;
        static bool reviewPrepared;
        static bool samplePrepared;
        static int floorIndex;
        static int frameIndex;
        static string pendingImagePath;
        static long pendingFileSize = -1;
        static int stableFileFrames;
        static int captureWaitFrames;

        static CC09FrameCapture()
        {
            armed = SessionState.GetBool(CaptureKey, false);
            frameIndex = SessionState.GetInt(FrameKey, 0);
            floorIndex = SessionState.GetInt(FloorKey, 0);
            EditorApplication.update += CaptureUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        [MenuItem("Misterio/Review/Arm CC09 frame capture (three floors)")]
        public static void Arm()
        {
            Directory.CreateDirectory(FrameRoot);
            for (int floor = 1; floor <= FloorCount; floor++)
            {
                string folder = GetFloorDirectory(floor - 1);
                Directory.CreateDirectory(folder);
                foreach (string file in Directory.GetFiles(folder, "cc09_*.png")) File.Delete(file);
            }
            string timesPath = Path.Combine(FrameRoot, "frame_times.csv");
            if (File.Exists(timesPath)) File.Delete(timesPath);
            frameIndex = 0;
            floorIndex = 0;
            armed = true;
            reviewPrepared = false;
            samplePrepared = false;
            pendingImagePath = null;
            pendingFileSize = -1;
            stableFileFrames = 0;
            captureWaitFrames = 0;
            SessionState.SetBool(CaptureKey, true);
            SessionState.SetInt(FrameKey, 0);
            SessionState.SetInt(FloorKey, 0);
            SessionState.SetInt("Misterio.CC09.FrameCapture.TargetFrameRate", Application.targetFrameRate);
            SessionState.SetInt("Misterio.CC09.FrameCapture.VSync", QualitySettings.vSyncCount);
            Debug.Log("CC09 exact frame review armed: capturing all three fourteen-second floor variants at 60 fps.");
        }

        static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && armed)
            {
                frameIndex = SessionState.GetInt(FrameKey, 0);
                floorIndex = SessionState.GetInt(FloorKey, 0);
                reviewPrepared = false;
                samplePrepared = false;
                pendingImagePath = null;
                pendingFileSize = -1;
                stableFileFrames = 0;
                captureWaitFrames = 0;
                Application.targetFrameRate = 60;
                QualitySettings.vSyncCount = 0;
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                Application.targetFrameRate = SessionState.GetInt("Misterio.CC09.FrameCapture.TargetFrameRate", -1);
                QualitySettings.vSyncCount = SessionState.GetInt("Misterio.CC09.FrameCapture.VSync", 1);
            }
        }

        static void CaptureUpdate()
        {
            if (!armed || !EditorApplication.isPlaying || EditorApplication.isPaused) return;
            CC09CutsceneController controller = Object.FindAnyObjectByType<CC09CutsceneController>();
            if (controller == null) return;
            if (!reviewPrepared)
            {
                controller.SetEditorReviewFloor((CC09FloorVariant)(floorIndex + 1));
                reviewPrepared = true;
                samplePrepared = false;
            }
            if (!samplePrepared)
            {
                controller.SetEditorReviewTime(frameIndex / 60f);
                Canvas.ForceUpdateCanvases();
                samplePrepared = true;
                return;
            }

            string folder = GetFloorDirectory(floorIndex);
            Directory.CreateDirectory(folder);
            if (string.IsNullOrEmpty(pendingImagePath))
            {
                pendingImagePath = Path.GetFullPath(Path.Combine(folder, "cc09_f" + (floorIndex + 1) + "_" + frameIndex.ToString("0000") + ".png"));
                ScreenCapture.CaptureScreenshot(pendingImagePath);
                pendingFileSize = -1;
                stableFileFrames = 0;
                captureWaitFrames = 0;
                return;
            }

            captureWaitFrames++;
            if (File.Exists(pendingImagePath))
            {
                long fileSize = new FileInfo(pendingImagePath).Length;
                if (fileSize > 0 && fileSize == pendingFileSize) stableFileFrames++;
                else stableFileFrames = 0;
                pendingFileSize = fileSize;
            }
            if (stableFileFrames < 2)
            {
                if (captureWaitFrames < 240) return;
                Debug.LogError("CC09 frame " + frameIndex + " of floor " + (floorIndex + 1) + " was not written completely: " + pendingImagePath);
                StopCapture();
                return;
            }

            File.AppendAllText(Path.Combine(FrameRoot, "frame_times.csv"),
                (floorIndex + 1) + "," + frameIndex + "," + (frameIndex / 60f).ToString("F6", System.Globalization.CultureInfo.InvariantCulture) + "\n");
            frameIndex++;
            SessionState.SetInt(FrameKey, frameIndex);
            samplePrepared = false;
            pendingImagePath = null;
            pendingFileSize = -1;
            stableFileFrames = 0;
            captureWaitFrames = 0;
            if (frameIndex < SamplesPerFloor) return;

            floorIndex++;
            SessionState.SetInt(FloorKey, floorIndex);
            if (floorIndex >= FloorCount)
            {
                StopCapture();
                Debug.Log("CC09 exact frame review complete: " + (SamplesPerFloor * FloorCount) + " samples in " + FrameRoot + ".");
                return;
            }

            frameIndex = 0;
            SessionState.SetInt(FrameKey, 0);
            controller.SetEditorReviewFloor((CC09FloorVariant)(floorIndex + 1));
            pendingImagePath = null;
            samplePrepared = false;
        }

        static string GetFloorDirectory(int index)
            => Path.Combine(FrameRoot, "floor_" + (index + 1));

        static void StopCapture()
        {
            armed = false;
            SessionState.SetBool(CaptureKey, false);
        }
    }
}
#endif
