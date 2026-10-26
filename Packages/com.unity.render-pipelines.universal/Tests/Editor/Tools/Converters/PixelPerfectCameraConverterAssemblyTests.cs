using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor.Compilation;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.Rendering.Universal.Tools
{
    [Category("Graphics Tools")]
    class PixelPerfectCameraConverterAssemblyTests
    {
        const string k_EditorAssemblyName = "Unity.RenderPipelines.Universal.Editor";
        const string k_PixelPerfectPackageName = "com.unity.2d.pixel-perfect";
        const string k_PixelPerfectDefine = "PIXEL_PERFECT_2D_EXISTS";

        // ShadowPackages/com.unity.2d.pixel-perfect/Runtime/Unity.2D.PixelPerfect.asmdef
        const string k_PixelPerfectAssemblyReference = "GUID:476f7c6c6dfeed041b063446a926e656";

        [Serializable]
        class VersionDefine
        {
            public string name;
            public string expression;
            public string define;
        }

        [Serializable]
        class AssemblyDefinition
        {
            public string[] references;
            public VersionDefine[] versionDefines;
        }

        static AssemblyDefinition LoadEditorAssemblyDefinition()
        {
            var path = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(k_EditorAssemblyName);
            Assert.IsNotNull(path, $"Could not find the assembly definition file of {k_EditorAssemblyName}.");

            var asset = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(path);
            Assert.IsNotNull(asset, $"Could not load the assembly definition file at {path}.");

            return JsonUtility.FromJson<AssemblyDefinition>(asset.text);
        }

        [Test]
        public void PixelPerfectVersionDefine_NamesThePixelPerfectPackage()
        {
            var packageNames = LoadEditorAssemblyDefinition().versionDefines
                .Where(versionDefine => versionDefine.define == k_PixelPerfectDefine)
                .Select(versionDefine => versionDefine.name)
                .ToArray();

            CollectionAssert.AreEqual(new[] { k_PixelPerfectPackageName }, packageNames,
                $"{k_PixelPerfectDefine} has to come from {k_PixelPerfectPackageName}, any other package name leaves the Pixel Perfect Camera converter permanently disabled.");
        }

        [Test]
        public void EditorAssembly_ReferencesThePixelPerfectAssembly()
        {
            var references = LoadEditorAssemblyDefinition().references;

            CollectionAssert.Contains(references, k_PixelPerfectAssemblyReference,
                $"The Pixel Perfect Camera converter uses UnityEngine.U2D.PixelPerfectCamera, so {k_EditorAssemblyName} has to reference Unity.2D.PixelPerfect.");
        }
    }
}
