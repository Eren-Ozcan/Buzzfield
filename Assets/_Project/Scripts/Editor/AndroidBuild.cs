using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Buzzfield.Editor
{
    /// <summary>
    /// Menu "Buzzfield > Build Android Dev APK": a development APK of the scenes in the build
    /// settings, written to Builds/Android. Batchmode:
    /// Unity.exe -batchmode -quit -buildTarget Android -projectPath . -executeMethod Buzzfield.Editor.AndroidBuild.BuildDevApk
    /// A failed build throws, so batchmode exits with a non-zero code.
    /// </summary>
    public static class AndroidBuild
    {
        public const string OutputPath = "Builds/Android/Buzzfield-dev.apk";

        [MenuItem("Buzzfield/Build Android Dev APK", priority = 20)]
        public static void BuildDevApk()
        {
            string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            if (scenes.Length == 0)
                throw new InvalidOperationException("No scenes in the build settings. Run Buzzfield > Build Greybox Scene first.");

            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
            EditorUserBuildSettings.buildAppBundle = false;
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = OutputPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.Development,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
                throw new Exception($"Android dev build {summary.result} with {summary.totalErrors} error(s); see the log above.");

            long bytes = new FileInfo(OutputPath).Length;
            Debug.Log($"Android dev APK: {OutputPath}, {bytes / (1024.0 * 1024.0):0.0} MB, "
                + $"{summary.totalTime.TotalSeconds:0} s, {summary.totalWarnings} warning(s).");
        }
    }
}
