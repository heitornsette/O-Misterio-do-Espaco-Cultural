using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Misterio.EditorTools
{
    /// <summary>
    /// Builds a standalone Linux player so the game can be played outside the Editor. This is the
    /// acceptance gate that proves the whole route works with the real build list.
    /// </summary>
    public static class GameBuild
    {
        const string OutputDirectory = "Build/Linux";

        [MenuItem("Misterio/Build Linux Player")]
        public static void BuildLinux()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string output = Path.Combine(projectRoot, OutputDirectory);
            Directory.CreateDirectory(output);

            var options = new BuildPlayerOptions
            {
                scenes = CollectScenes(),
                locationPathName = Path.Combine(output, "MisterioDoEspacoCultural.x86_64"),
                target = BuildTarget.StandaloneLinux64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            string line = "result=" + summary.result +
                          " scenes=" + options.scenes.Length +
                          " errors=" + summary.totalErrors +
                          " size=" + summary.totalSize / (1024 * 1024) + "MB" +
                          " time=" + summary.totalTime;
            Debug.Log("[Build] " + line);

            string reportPath = Path.Combine(output, "build_report.txt");
            File.WriteAllText(reportPath, line + "\n" + summary.outputPath);
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
