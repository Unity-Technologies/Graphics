using UnityEngine;
using UnityEditor.ProjectWindowCallback;
using System.IO;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// A utility class to create Volume Profiles and components.
    /// </summary>
    public static class VolumeProfileFactory
    {
        [MenuItem("Assets/Create/Volume Profile", priority = 201)]
        static void CreateVolumeProfile()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                0,
                ScriptableObject.CreateInstance<DoCreatePostProcessProfile>(),
                "New Volume Profile.asset",
                null,
                null
                );
        }

        /// <summary>
        /// Creates a <see cref="VolumeProfile"/> Asset and saves it at the given path.
        /// </summary>
        /// <param name="path">The path to save the Asset to, relative to the Project folder.</param>
        /// <returns>The newly created <see cref="VolumeProfile"/>.</returns>
        public static VolumeProfile CreateVolumeProfileAtPath(string path)
        {
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = Path.GetFileName(path);
            AssetDatabase.CreateAsset(profile, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return profile;
        }

        /// <summary>
        /// Creates a <see cref="VolumeProfile"/> Asset and saves it in a folder next to the Scene.
        /// </summary>
        /// <param name="scene">The Scene to save the Profile next to.</param>
        /// <param name="targetName">A name to use for the Asset filename.</param>
        /// <returns>The newly created <see cref="VolumeProfile"/>.</returns>
        public static VolumeProfile CreateVolumeProfile(Scene scene, string targetName)
        {
            string path;

            if (string.IsNullOrEmpty(scene.path))
            {
                path = "Assets/";
            }
            else
            {
                var scenePath = Path.GetDirectoryName(scene.path);
                var extPath = scene.name;
                var profilePath = scenePath + "/" + extPath;

                if (!AssetDatabase.IsValidFolder(profilePath))
                    AssetDatabase.CreateFolder(scenePath, extPath);

                path = profilePath + "/";
            }

            path += targetName + " Profile.asset";
            path = AssetDatabase.GenerateUniqueAssetPath(path);

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return profile;
        }

        /// <summary>
        /// Creates a <see cref="VolumeComponent"/> in an existing <see cref="VolumeProfile"/>.
        /// </summary>
        /// <typeparam name="T">A type of <see cref="VolumeComponent"/>.</typeparam>
        /// <param name="profile">The profile to store the new component in.</param>
        /// <param name="overrides">specifies whether to override the parameters in the component or not.</param>
        /// <param name="saveAsset">Specifies whether to save the Profile Asset or not. This is useful when you need to
        /// create several components in a row and only want to save the Profile Asset after adding the last one,
        /// because saving Assets to disk can be slow.</param>
        /// <returns></returns>
        public static T CreateVolumeComponent<T>(VolumeProfile profile, bool overrides = false, bool saveAsset = true)
            where T : VolumeComponent
        {
            var comp = profile.Add<T>(overrides);
            comp.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(comp, profile);

            if (saveAsset)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            return comp;
        }
    }

    class DoCreatePostProcessProfile : EndNameEditAction
    {
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            var profile = VolumeProfileFactory.CreateVolumeProfileAtPath(pathName);
            ProjectWindowUtil.ShowCreatedAsset(profile);
        }
    }
}
