using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Buzzfield.Editor
{
    /// <summary>
    /// Android builds of the scenes in the build settings, written to Builds/Android.
    /// Menu "Buzzfield > Android > ...", or batchmode:
    /// Unity.exe -batchmode -quit -buildTarget Android -projectPath . -executeMethod Buzzfield.Editor.AndroidBuild.BuildDevApk
    /// (or BuildReleaseAab, BumpVersionCode). A failed build throws, so batchmode exits with a
    /// non-zero code.
    /// The release AAB is signed with the upload key in android-keystore/ (gitignored; backup in
    /// the private pictures repo). The password is read from BZ_KEYSTORE_PASS, else
    /// android-keystore/buzzfield-upload.pass, and never enters the repo. Development builds use
    /// the debug key and Google's test ad units.
    /// </summary>
    public static class AndroidBuild
    {
        public const string OutputPath = "Builds/Android/Buzzfield-dev.apk";
        private const string OutputDir = "Builds/Android";
        private const string KeystorePath = "android-keystore/buzzfield-upload.jks";
        private const string KeyAlias = "buzzfield";
        private const string PassEnv = "BZ_KEYSTORE_PASS";
        private const string PassFile = "android-keystore/buzzfield-upload.pass";

        [MenuItem("Buzzfield/Android/Build Dev APK", priority = 20)]
        public static void BuildDevApk() => Build(release: false, OutputPath);

        [MenuItem("Buzzfield/Android/Build Release AAB", priority = 21)]
        public static void BuildReleaseAab()
        {
            int code = PlayerSettings.Android.bundleVersionCode;
            string path = $"{OutputDir}/Buzzfield-{PlayerSettings.bundleVersion}-{code}.aab";
            if (File.Exists(path))
                throw new InvalidOperationException($"{path} exists; bump the version code before a new upload.");
            Build(release: true, path);
        }

        /// <summary>Every Play upload needs a higher version code.</summary>
        [MenuItem("Buzzfield/Android/Bump Version Code", priority = 22)]
        public static void BumpVersionCode()
        {
            PlayerSettings.Android.bundleVersionCode++;
            AssetDatabase.SaveAssets();
            Debug.Log($"Android version {PlayerSettings.bundleVersion} code {PlayerSettings.Android.bundleVersionCode}");
        }

        private static void Build(bool release, string path)
        {
            string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            if (scenes.Length == 0)
                throw new InvalidOperationException("No scenes in the build settings. Run Buzzfield > Build Greybox Scene first.");

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            // Play wants 64-bit; ARM64 needs IL2CPP.
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            // The ad and Firebase libraries enter through mainTemplate.gradle, patched by the
            // External Dependency Manager; batchmode never runs its auto-resolve, so force it.
            if (!GooglePlayServices.PlayServicesResolver.ResolveSync(true))
                throw new InvalidOperationException("Android dependency resolution failed.");
            string keystoreBefore = PlayerSettings.Android.keystoreName;
            string aliasBefore = PlayerSettings.Android.keyaliasName;
            if (release)
                ApplyUploadKey();
            // A dirty open scene makes BuildPlayer show a modal nobody can click in automation.
            EditorSceneManager.SaveOpenScenes();

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            EditorUserBuildSettings.buildAppBundle = release;
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = path,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = release ? BuildOptions.None : BuildOptions.Development,
            };

            BuildSummary summary;
            try
            {
                summary = BuildPipeline.BuildPlayer(options).summary;
            }
            finally
            {
                // Leave the project on the debug key so editor Build And Run keeps working, and
                // keep the local keystore path out of ProjectSettings.
                PlayerSettings.Android.useCustomKeystore = false;
                if (release)
                {
                    PlayerSettings.Android.keystoreName = keystoreBefore;
                    PlayerSettings.Android.keyaliasName = aliasBefore;
                }
                EditorUserBuildSettings.buildAppBundle = false;
                AssetDatabase.SaveAssets();
            }
            if (summary.result != BuildResult.Succeeded)
                throw new Exception($"Android build {summary.result} with {summary.totalErrors} error(s); see the log above.");

            long bytes = new FileInfo(path).Length;
            Debug.Log($"Android {(release ? "release AAB" : "dev APK")}: {path}, {bytes / (1024.0 * 1024.0):0.0} MB, "
                + $"version {PlayerSettings.bundleVersion} code {PlayerSettings.Android.bundleVersionCode}, "
                + $"{summary.totalTime.TotalSeconds:0} s, {summary.totalWarnings} warning(s).");
        }

        private static void ApplyUploadKey()
        {
            string pass = Environment.GetEnvironmentVariable(PassEnv);
            if (string.IsNullOrEmpty(pass) && File.Exists(PassFile))
                pass = File.ReadAllText(PassFile).Trim();
            if (string.IsNullOrEmpty(pass) || !File.Exists(KeystorePath))
                throw new InvalidOperationException($"The release AAB needs the upload key ({KeystorePath}, and {PassEnv} or {PassFile}).");
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = Path.GetFullPath(KeystorePath);
            PlayerSettings.Android.keystorePass = pass;
            PlayerSettings.Android.keyaliasName = KeyAlias;
            PlayerSettings.Android.keyaliasPass = pass;
        }
    }
}
