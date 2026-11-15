#if UNITY_EDITOR
using System.IO;
using Misterio.Cutscenes;
using UnityEditor;
using UnityEngine;

namespace Misterio.EditorTools
{
    [InitializeOnLoad]
    public static class CC04AFrameCapture
    {
        const string CaptureKey = "Misterio.CC04A.FrameCapture.Armed";
        const string FrameKey = "Misterio.CC04A.FrameCapture.Index";
        const string FrameDirectory = "Library/CC04AReviewFrames/sequence";
        const int SampleCount = 241;

        static bool armed;
        static bool reviewPrepared;
        static bool samplePrepared;
        static int frameIndex;

        static CC04AFrameCapture()
        {
            armed = SessionState.GetBool(CaptureKey, false);
            frameIndex = SessionState.GetInt(FrameKey, 0);
            EditorApplication.update += CaptureUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        [MenuItem("Misterio/Review/Arm CC04A frame capture")]
        public static void Arm()
        {
            Directory.CreateDirectory(FrameDirectory);
            foreach (string file in Directory.GetFiles(FrameDirectory, "cc04a_*.png")) File.Delete(file);
            string timesPath = Path.Combine(FrameDirectory, "frame_times.csv");
            if (File.Exists(timesPath)) File.Delete(timesPath);

            frameIndex = 0;
            armed = true;
            reviewPrepared = false;
            samplePrepared = false;
            SessionState.SetBool(CaptureKey, true);
            SessionState.SetInt(FrameKey, 0);
            SessionState.SetInt("Misterio.CC04A.FrameCapture.TargetFrameRate", Application.targetFrameRate);
            SessionState.SetInt("Misterio.CC04A.FrameCapture.VSync", QualitySettings.vSyncCount);
            Debug.Log("CC04A exact frame review armed: sampling the full four-second timeline at 60 fps.");
        }

        static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && armed)
            {
                frameIndex = SessionState.GetInt(FrameKey, 0);
                reviewPrepared = false;
                samplePrepared = false;
                Application.targetFrameRate = 60;
                QualitySettings.vSyncCount = 0;
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                Application.targetFrameRate = SessionState.GetInt("Misterio.CC04A.FrameCapture.TargetFrameRate", -1);
                QualitySettings.vSyncCount = SessionState.GetInt("Misterio.CC04A.FrameCapture.VSync", 1);
            }
        }

        static void CaptureUpdate()
        {
            if (!armed || !EditorApplication.isPlaying || EditorApplication.isPaused) return;

            CC04ACutsceneController controller = Object.FindFirstObjectByType<CC04ACutsceneController>();
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
                samplePrepared = true;
                return;
            }

            Directory.CreateDirectory(FrameDirectory);
            string imagePath = Path.GetFullPath(Path.Combine(FrameDirectory, $"cc04a_{frameIndex:0000}.png"));
            ScreenCapture.CaptureScreenshot(imagePath);
            File.AppendAllText(Path.Combine(FrameDirectory, "frame_times.csv"),
                frameIndex + "," + (frameIndex / 60f).ToString("F6", System.Globalization.CultureInfo.InvariantCulture) + "\n");

            frameIndex++;
            SessionState.SetInt(FrameKey, frameIndex);
            samplePrepared = false;
            if (frameIndex < SampleCount) return;
            armed = false;
            SessionState.SetBool(CaptureKey, false);
            Debug.Log($"CC04A exact frame review complete: {frameIndex} frames in {FrameDirectory}.");
        }
    }
}
#endif
