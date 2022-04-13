using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    static class LensFlareEditorUtils
    {
        internal static class Icons
        {
            const string k_IconFolder = @"Packages/com.unity.render-pipelines.core/Editor/PostProcessing/LensFlareResource/";
            public static readonly Texture2D circle = CoreEditorUtils.LoadIcon(k_IconFolder, "CircleFlareThumbnail", forceLowRes: false);
            public static readonly Texture2D polygon = CoreEditorUtils.LoadIcon(k_IconFolder, "PolygonFlareThumbnail", forceLowRes: false);
            public static readonly Texture2D generic = CoreEditorUtils.LoadIcon(k_IconFolder, "Flare128", forceLowRes: false);
        }

        #region Asset Factory
        class LensFlareDataSRPCreator : UnityEditor.ProjectWindowCallback.EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                LensFlareDataSRP asset = ScriptableObject.CreateInstance<LensFlareDataSRP>();
                UnityEngine.Assertions.Assert.IsNotNull(asset, $"failed to create instance of {nameof(LensFlareDataSRP)}");

                pathName = AssetDatabase.GenerateUniqueAssetPath(pathName);
                asset.name = Path.GetFileName(pathName);

                AssetDatabase.CreateAsset(asset, pathName);
                ProjectWindowUtil.ShowCreatedAsset(asset);
            }
        }

        [MenuItem("Assets/Create/Lens Flare (SRP)", priority = UnityEngine.Rendering.CoreUtils.Priorities.srpLensFlareMenuPriority)]
        internal static void CreateLensFlareDataSRPAsset()
        {
            const string relativePath = "New Lens Flare (SRP).asset";
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<LensFlareDataSRPCreator>(), relativePath, Icons.generic, null);
        }

        #endregion
    }
}
