using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Buzzfield.Editor
{
    /// <summary>Player settings every build needs: identity and portrait-only orientation.</summary>
    public static class ProjectSetup
    {
        public const string CompanyName = "Yilk Games";
        public const string ProductName = "Buzzfield: Bloom Idle";
        public const string PackageId = "com.yilkgames.buzzfield";

        [MenuItem("Buzzfield/Apply Player Settings", priority = 20)]
        public static void Apply()
        {
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = ProductName;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PackageId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, PackageId);

            // Portrait only: no auto-rotation, no upside-down portrait.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            AssetDatabase.SaveAssets();
            Debug.Log($"Player settings applied: {ProductName} ({PackageId}), portrait only.");
        }
    }
}
