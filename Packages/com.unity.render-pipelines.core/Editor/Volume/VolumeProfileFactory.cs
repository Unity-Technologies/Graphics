using System;
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
        [MenuItem("Assets/Create/Rendering/Volume Profile", priority = 10)]
        static void CreateVolumeProfile()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                0,
                ScriptableObject.CreateInstance<CreateVolumeProfileAction>(),
                "New Volume Profile.asset",
                CoreUtils.GetIconForType<VolumeProfile>(),
                null
            );
        }

        /// <summary>
        /// Asks for editor user input for the asset name, creates a <see cref="VolumeProfile"/> Asset, saves it at the
        /// given path and invokes the callback.
        /// </summary>
        /// <param name="fullPath">The path to save the asset to.</param>
        /// <param name="callback">Callback to invoke after the asset has been created.</param>
        public static void CreateVolumeProfileWithCallback(string fullPath, Action<VolumeProfile> callback)
        {
            var assetCreator = ScriptableObject.CreateInstance<CreateVolumeProfileWithCallbackAction>();
            assetCreator.callback = callback;
            CoreUtils.EnsureFolderTreeInAssetFilePath(fullPath);

            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                assetCreator.GetInstanceID(),
                assetCreator,
                fullPath,
                CoreUtils.GetIconForType<VolumeProfile>(),
                null);
        }

        /// <summary>
        /// Creates a <see cref="VolumeProfile"/> Asset and saves it at the given path.
        /// </summary>
        /// <param name="path">The path to save the Asset to, relative to the Project folder.</param>
        /// <returns>The newly created <see cref="VolumeProfile"/>.</returns>
        public static VolumeProfile CreateVolumeProfileAtPath(string path) => CreateVolumeProfileAtPath(path, null);

        /// <summary>
        /// Creates a <see cref="VolumeProfile"/> Asset and saves it at the given path.
        /// </summary>
        /// <param name="path">The path to save the Asset to, relative to the Project folder.</param>
        /// <param name="dataSource">Another `VolumeProfile` that Unity uses as a data source.</param>
        /// <returns>The newly created <see cref="VolumeProfile"/>.</returns>
        public static VolumeProfile CreateVolumeProfileAtPath(string path, VolumeProfile dataSource)
        {
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = Path.GetFileName(path);
            AssetDatabase.CreateAsset(profile, path);

            if (dataSource != null)
            {
                foreach (var sourceComponent in dataSource.components)
                {
                    var profileComponent = profile.Add(sourceComponent.GetType());
                    for (int i = 0; i < sourceComponent.parameters.Count; i++)
                        profileComponent.parameters[i].overrideState = sourceComponent.parameters[i].overrideState;
                    VolumeProfileUtils.CopyValuesToComponent(sourceComponent, profileComponent, true);
                    AssetDatabase.AddObjectToAsset(profileComponent, profile);
                }
            }

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
            return CoreEditorUtils.CreateAssetAt<VolumeProfile>(scene, targetName);
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
        /// <returns>The newly created component of type <typeparamref name="T"/>.</returns>
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

    class CreateVolumeProfileAction : EndNameEditAction
    {
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            var profile = VolumeProfileFactory.CreateVolumeProfileAtPath(pathName);
            ProjectWindowUtil.ShowCreatedAsset(profile);
        }
    }

    class CreateVolumeProfileWithCallbackAction : EndNameEditAction
    {
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            var profile = VolumeProfileFactory.CreateVolumeProfileAtPath(pathName);
            ProjectWindowUtil.ShowCreatedAsset(profile);
            callback?.Invoke(profile);
        }

        internal Action<VolumeProfile> callback { get; set; }
    }
}
