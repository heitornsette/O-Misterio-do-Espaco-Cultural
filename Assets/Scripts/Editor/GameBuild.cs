using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Misterio.EditorTools
{
    /// <summary>
    /// Builds a standalone player so the game can be played outside the Editor. This is the
    /// acceptance gate that proves the whole route works with the real build list.
    /// The target follows the machine running the editor: a Windows developer gets a Windows player,
    /// which is the point of being able to keep developing the project anywhere.
    /// </summary>
    public static class GameBuild
    {
        [MenuItem("Misterio/Build Player (current platform)")]
        public static void BuildPlayer() => BuildFor(EditorUserBuildSettings.activeBuildTarget);

        [MenuItem("Misterio/Build Linux Player")]
        public static void BuildLinux() => BuildFor(BuildTarget.StandaloneLinux64);

        [MenuItem("Misterio/Build Windows Player")]
        public static void BuildWindows() => BuildFor(BuildTarget.StandaloneWindows64);

        static void BuildFor(BuildTarget target)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

            string folder;
            string executable;
            switch (target)
            {
                case BuildTarget.StandaloneWindows64:
                    folder = "Build/Windows";
                    executable = "MisterioDoEspacoCultural.exe";
                    break;
                case BuildTarget.StandaloneOSX:
                    folder = "Build/macOS";
                    executable = "MisterioDoEspacoCultural.app";
                    break;
                default:
                    folder = "Build/Linux";
                    executable = "MisterioDoEspacoCultural.x86_64";
                    break;
            }

            string output = Path.Combine(projectRoot, folder);
            Directory.CreateDirectory(output);

            var options = new BuildPlayerOptions
            {
                scenes = CollectScenes(),
                locationPathName = Path.Combine(output, executable),
                target = target,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            string line = "result=" + summary.result +
                          " target=" + target +
                          " scenes=" + options.scenes.Length +
                          " errors=" + summary.totalErrors +
                          " size=" + summary.totalSize / (1024 * 1024) + "MB" +
                          " time=" + summary.totalTime;
            Debug.Log("[Build] " + line);

            string reportPath = Path.Combine(output, "build_report.txt");
            File.WriteAllText(reportPath, line + "\n" + summary.outputPath);

            // Only exit when running headless; exiting from a menu click would close the editor.
            if (Application.isBatchMode)
                EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
        }

        static string[] CollectScenes()
        {
            var scenes = new System.Collections.Generic.List<string>();
            foreach (var scene in EditorBuildSettings.scenes)
                if (scene.enabled) scenes.Add(scene.path);
            return scenes.ToArray();
        }
    }
}