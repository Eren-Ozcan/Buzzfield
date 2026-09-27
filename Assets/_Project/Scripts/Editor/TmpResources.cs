using UnityEditor;
using UnityEngine;

namespace Buzzfield.Editor
{
    /// <summary>Imports the TextMeshPro Essential Resources (default font and settings) if they are missing.</summary>
    internal static class TmpResources
    {
        const string PackageFile = "/Package Resources/TMP Essential Resources.unitypackage";

        public static bool IsImported => AssetDatabase.FindAssets("t:TMP_Settings").Length > 0;

        public static void EnsureImported()
        {
            if (IsImported)
                return;
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.ugui");
            if (package == null)
            {
                Debug.LogError("com.unity.ugui is not installed; TextMeshPro resources cannot be imported.");
                return;
            }
            AssetDatabase.ImportPackage(package.resolvedPath + PackageFile, false);
            Debug.Log("Importing TMP Essential Resources.");
        }

        /// <summary>
        /// Batchmode entry: package import finishes after the calling method returns, so
        /// "-quit" would exit too early. Run without "-quit"; this exits when the import is done.
        /// </summary>
        public static void ImportAndExit()
        {
            if (IsImported)
            {
                EditorApplication.Exit(0);
                return;
            }
            AssetDatabase.importPackageCompleted += _ => EditorApplication.Exit(0);
            AssetDatabase.importPackageFailed += (_, error) =>
            {
                Debug.LogError($"TMP import failed: {error}");
                EditorApplication.Exit(1);
            };
            EnsureImported();
        }
    }
}
