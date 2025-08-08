using System;
using System.IO;
using UnityEngine;

namespace UnityEditor.RenderPipelines.Core
{
    internal static class AssetCreationUtil
    {
        /// <summary>
        /// Prompts the user to save a new <seealso cref="Shader"/> asset created from the shader template, and creates and <seealso cref="Material"/> with it.
        /// </summary>
        /// <param name="name">Default material name.</param>
        /// <param name="callback">A delegate (callback) that will be invoked with the <see cref="Material"/>.</param>
        /// <param name="shaderTemplateAssetPath">Path of the Shader Template file.</param>
        internal static void CreateShaderAndMaterial(string name, Action<Material> callback, string shaderTemplateAssetPath)
        {
            CreateShader(
                name,
                (shader) =>
                {
                    Material material = new Material(shader);
                    var path = AssetDatabase.GetAssetPath(shader);
                    AssetDatabase.CreateAsset(material, Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + ".mat"));
                    AssetDatabase.SaveAssetIfDirty(material);
                    AssetDatabase.Refresh(ImportAssetOptions.Default);
                    callback?.Invoke(material);
                },
                shaderTemplateAssetPath);
        }

        /// <summary>
        /// Prompts the user to save a new <seealso cref="Material"/> asset created using the <paramref name="shader"/>.
        /// </summary>
        /// <param name="name">Default material name.</param>
        /// <param name="callback">A delegate (callback) that will be invoked with the <see cref="Material"/>.</param>
        /// <param name="shader"><seealso cref="Shader"/> to create the new <seealso cref="Material"/> with.</param>
        internal static void CreateMaterial(string name, Action<Material> callback, Shader shader)
        {
            if (shader == null)
            {
                Debug.LogError($"Null Shader reference. Cannot create Material {name}.");
                return;
            }

            CreateAsset(
                name,
                (materialPath) =>
                {
                    Material material = new Material(shader);
                    AssetDatabase.CreateAsset(material, materialPath);
                    AssetDatabase.SaveAssetIfDirty(material);
                    AssetDatabase.Refresh(ImportAssetOptions.Default);
                    callback?.Invoke(material);
                },
                "mat",
                typeof(Material)
            );
        }

        internal static void CreateShader(string name, Action<Shader> callback, string shaderTemplateAssetPath)
        {
            if (!AssetDatabase.AssetPathExists(shaderTemplateAssetPath))
            {
                Debug.LogError($"Shader Template File missing at path: {shaderTemplateAssetPath}.");
                return;
            }

            CreateAsset(
                name,
                (shaderPath) =>
                {
                    string fileName = Path.GetFileNameWithoutExtension(shaderPath);
                    string templateCode = File.ReadAllText(shaderTemplateAssetPath);
                    templateCode = templateCode.Replace("#SCRIPTNAME#", fileName);
                    File.WriteAllText(shaderPath, templateCode);

                    AssetDatabase.Refresh();
                    AssetDatabase.ImportAsset(shaderPath);
                    Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);

                    callback?.Invoke(shader);
                },
                "shader",
                typeof(Shader)
            );
        }

        static void CreateAsset(string name, Action<string> callback = null, string extension = "asset", Type type = null)
        {
            AssetCreationCallback assetCreationCallback = ScriptableObject.CreateInstance<AssetCreationCallback>();
            assetCreationCallback.callback = callback;
            assetCreationCallback.extension = extension;

            var icon = AssetPreview.GetMiniTypeThumbnail(type);
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, assetCreationCallback, name, icon, null, false);
        }

        class AssetCreationCallback : ProjectWindowCallback.EndNameEditAction
        {
            public Action<string> callback;
            public string extension;

            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                string path = AssetDatabase.GenerateUniqueAssetPath(pathName + $".{extension}");
                callback?.Invoke(path);
            }
        }
    }
}
