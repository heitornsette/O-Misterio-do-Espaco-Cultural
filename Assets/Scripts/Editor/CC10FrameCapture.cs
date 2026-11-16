#if UNITY_EDITOR
using System.IO;
using Misterio.Cutscenes;
using UnityEditor;
using UnityEngine;

namespace Misterio.EditorTools
{
    [InitializeOnLoad]
    public static class CC10FrameCapture
    {
        const string ArmedKey = "Misterio.CC10.FrameCapture.Armed";
        const string IndexKey = "Misterio.CC10.FrameCapture.Index";
        const string OutputRoot = "Library/CC10ReviewFrames";
        const int FramesPerSecond = 60;
        const int FrameCount = 901;

        static bool armed;
        static bool reviewPrepared;
        static bool framePrepared;
        static int frameIndex;
        static string pendingPath;
        static long previousLength = -1;
        static int stableCount;
        static int waitCount;

        static CC10FrameCapture()
        {
            armed = SessionState.GetBool(ArmedKey, false);
            frameIndex = SessionState.GetInt(IndexKey, 0);
            EditorApplication.update += CaptureUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        [MenuItem("Misterio/Review/Arm CC10 frame capture")]
        public static void Arm()
        {
            string sequence = Path.Combine(OutputRoot, "sequence");
            Directory.CreateDirectory(sequence);
            foreach (string file in Directory.GetFiles(sequence, "cc10_*.png")) File.Delete(file);
            string times = Path.Combine(OutputRoot, "frame_times.csv");
            if (File.Exists(times)) File.Delete(times);
            frameIndex = 0;
            armed = true;
            reviewPrepared = framePrepared = false;
            pendingPath = null;
            previousLength = -1;
            stableCount = waitCount = 0;
            SessionState.SetBool(ArmedKey, true);
            SessionState.SetInt(IndexKey, 0);
            SessionState.SetInt("Misterio.CC10.FrameCapture.TargetFrameRate", Application.targetFrameRate);
            SessionState.SetInt("Misterio.CC10.FrameCapture.VSync", QualitySettings.vSyncCount);
            Debug.Log("CC10 exact frame review armed: 901 samples from 0 to 15 seconds at 60 fps.");
        }

        static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && armed)
            {
                frameIndex = SessionState.GetInt(IndexKey, 0);
                reviewPrepared = framePrepared = false;
                pendingPath = null;
                previousLength = -1;
                stableCount = waitCount = 0;
                Application.targetFrameRate = FramesPerSecond;
                QualitySettings.vSyncCount = 0;
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                RestoreFrameRate();
            }
        }

        static void CaptureUpdate()
        {
            if (!armed || !EditorApplication.isPlaying || EditorApplication.isPaused) return;
            CC10CutsceneController controller = Object.FindAnyObjectByType<CC10CutsceneController>();
            if (controller == null) return;
            if (!reviewPrepared)
            {
                controller.PrepareEditorFrameReviewCapture();
                reviewPrepared = true;
                framePrepared = false;
            }
            if (!framePrepared)
            {
                controller.SetEditorReviewTime(frameIndex / (float)FramesPerSecond);
                Canvas.ForceUpdateCanvases();
                framePrepared = true;
                return;
            }

            string folder = Path.Combine(OutputRoot, "sequence");
            Directory.CreateDirectory(folder);
            if (string.IsNullOrEmpty(pendingPath))
            {
                pendingPath = Path.GetFullPath(Path.Combine(folder, "cc10_" + frameIndex.ToString("0000") + ".png"));
                ScreenCapture.CaptureScreenshot(pendingPath);
                previousLength = -1;
                stableCount = waitCount = 0;
                return;
            }

            waitCount++;
            if (File.Exists(pendingPath))
            {
                long length = new FileInfo(pendingPath).Length;
                if (length > 0 && length == previousLength) stableCount++;
                else stableCount = 0;
                previousLength = length;
            }
            if (stableCount < 2)
            {
                if (waitCount < 240) return;
                Debug.LogError("CC10 frame " + frameIndex + " was not written completely: " + pendingPath);
                StopCapture();
                return;
            }

            File.AppendAllText(Path.Combine(OutputRoot, "frame_times.csv"),
                frameIndex + "," + (frameIndex / (float)FramesPerSecond).ToString("F6", System.Globalization.CultureInfo.InvariantCulture) + "\n");
            frameIndex++;
            SessionState.SetInt(IndexKey, frameIndex);
            framePrepared = false;
            pendingPath = null;
            previousLength = -1;
            stableCount = waitCount = 0;
            if (frameIndex < FrameCount)
            {
                if (frameIndex % 60 == 0) Debug.Log("CC10 frame review progress: " + frameIndex + "/" + FrameCount + ".");
                return;
            }

            StopCapture();
            Debug.Log("CC10 exact frame review complete: " + FrameCount + " frames in " + OutputRoot + ".");
        }

        static void StopCapture()
        {
            armed = false;
            SessionState.SetBool(ArmedKey, false);
            RestoreFrameRate();
        }

        static void RestoreFrameRate()
        {
            Application.targetFrameRate = SessionState.GetInt("Misterio.CC10.FrameCapture.TargetFrameRate", -1);
            QualitySettings.vSyncCount = SessionState.GetInt("Misterio.CC10.FrameCapture.VSync", 1);
        }
    }
}
#endif
