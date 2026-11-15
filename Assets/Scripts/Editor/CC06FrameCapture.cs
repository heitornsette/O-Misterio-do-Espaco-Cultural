#if UNITY_EDITOR
using System.IO;
using Misterio.Cutscenes;
using UnityEditor;
using UnityEngine;

namespace Misterio.EditorTools
{
    [InitializeOnLoad]
    public static class CC06FrameCapture
    {
        const string CaptureKey = "Misterio.CC06.FrameCapture.Armed";
        const string FrameKey = "Misterio.CC06.FrameCapture.Index";
        const string FrameDirectory = "Library/CC06ReviewFrames/sequence";
        const int SampleCount = 721;

        static bool armed;
        static bool reviewPrepared;
        static bool samplePrepared;
        static int frameIndex;
        static string pendingImagePath;
        static long pendingFileSize = -1;
        static int stableFileFrames;
        static int captureWaitFrames;

        static CC06FrameCapture()
        {
            armed = SessionState.GetBool(CaptureKey, false);
            frameIndex = SessionState.GetInt(FrameKey, 0);
            EditorApplication.update += CaptureUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        [MenuItem("Misterio/Review/Arm CC06 frame capture")]
        public static void Arm()
        {
            Directory.CreateDirectory(FrameDirectory);
            foreach (string file in Directory.GetFiles(FrameDirectory, "cc06_*.png")) File.Delete(file);
            string timesPath = Path.Combine(FrameDirectory, "frame_times.csv");
            if (File.Exists(timesPath)) File.Delete(timesPath);
            frameIndex = 0;
            armed = true;
            reviewPrepared = false;
            samplePrepared = false;
            pendingImagePath = null;
            pendingFileSize = -1;
            stableFileFrames = 0;
            captureWaitFrames = 0;
            SessionState.SetBool(CaptureKey, true);
            SessionState.SetInt(FrameKey, 0);
            SessionState.SetInt("Misterio.CC06.FrameCapture.TargetFrameRate", Application.targetFrameRate);
            SessionState.SetInt("Misterio.CC06.FrameCapture.VSync", QualitySettings.vSyncCount);
            Debug.Log("CC06 exact frame review armed: sampling the complete twelve-second timeline at 60 fps.");
        }

        static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && armed)
            {
                frameIndex = SessionState.GetInt(FrameKey, 0);
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
                Application.targetFrameRate = SessionState.GetInt("Misterio.CC06.FrameCapture.TargetFrameRate", -1);
                QualitySettings.vSyncCount = SessionState.GetInt("Misterio.CC06.FrameCapture.VSync", 1);
            }
        }

        static void CaptureUpdate()
        {
            if (!armed || !EditorApplication.isPlaying || EditorApplication.isPaused) return;
            CC06CutsceneController controller = Object.FindAnyObjectByType<CC06CutsceneController>();
            if (controller == null) return;
            if (!reviewPrepared)
            {
                controller.PrepareEditorFrameReviewCapture();
                frameIndex = 0;
                reviewPrepared = true;
            }
            if (!samplePrepared)
            {
                controller.SetEditorReviewTime(frameIndex / 60f);
                Canvas.ForceUpdateCanvases();
                samplePrepared = true;
                return;
            }

            Directory.CreateDirectory(FrameDirectory);
            if (string.IsNullOrEmpty(pendingImagePath))
            {
                pendingImagePath = Path.GetFullPath(Path.Combine(FrameDirectory, $"cc06_{frameIndex:0000}.png"));
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
                Debug.LogError($"CC06 frame {frameIndex} was not written completely: {pendingImagePath}");
                armed = false;
                SessionState.SetBool(CaptureKey, false);
                return;
            }

            File.AppendAllText(Path.Combine(FrameDirectory, "frame_times.csv"),
                frameIndex + "," + (frameIndex / 60f).ToString("F6", System.Globalization.CultureInfo.InvariantCulture) + "\n");
            frameIndex++;
            SessionState.SetInt(FrameKey, frameIndex);
            samplePrepared = false;
            pendingImagePath = null;
            pendingFileSize = -1;
            stableFileFrames = 0;
            captureWaitFrames = 0;
            if (frameIndex < SampleCount) return;
            armed = false;
            SessionState.SetBool(CaptureKey, false);
            Debug.Log($"CC06 exact frame review complete: {frameIndex} samples in {FrameDirectory}.");
        }
    }
}
#endif
