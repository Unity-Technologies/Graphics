using System.IO;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    using UnityObject = UnityEngine.Object;

    static partial class HDAssetFactory
    {
        static string s_RenderPipelineResourcesPath
        {
            get { return HDUtils.GetHDRenderPipelinePath() + "Runtime/RenderPipelineResources/HDRenderPipelineResources.asset"; }
        }

        class DoCreateNewAssetHDRenderPipeline : ProjectWindowCallback.EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var newAsset = CreateInstance<HDRenderPipelineAsset>();
                newAsset.name = Path.GetFileName(pathName);

                AssetDatabase.CreateAsset(newAsset, pathName);
                ProjectWindowUtil.ShowCreatedAsset(newAsset);
            }
        }

        [MenuItem("Assets/Create/Rendering/HDRP Asset", priority = CoreUtils.Sections.section1 + CoreUtils.Priorities.assetsCreateRenderingMenuPriority)]
        static void CreateHDRenderPipeline()
        {
            var icon = EditorGUIUtility.FindTexture("ScriptableObject Icon");
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<DoCreateNewAssetHDRenderPipeline>(), "New HDRenderPipelineAsset.asset", icon, null);
        }

        // Note: move this to a static using once we can target C#6+
        static T Load<T>(string path) where T : UnityObject
        {
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        class DoCreateNewAssetHDRenderPipelineResources : ProjectWindowCallback.EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var newAsset = CreateInstance<HDRenderPipelineRuntimeResources>();
                newAsset.name = Path.GetFileName(pathName);

                // to prevent cases when the asset existed prior but then when upgrading the package, there is null field inside the resource asset
                ResourceReloader.ReloadAllNullIn(newAsset, HDUtils.GetHDRenderPipelinePath());

                AssetDatabase.CreateAsset(newAsset, pathName);
                ProjectWindowUtil.ShowCreatedAsset(newAsset);
            }
        }

        // Hide: User aren't suppose to have to create it.
        //[MenuItem("Assets/Create/Rendering/HDRP Resources", priority = CoreUtils.Sections.section7 + CoreUtils.Priorities.assetsCreateRenderingMenuPriority)]
        static void CreateRenderPipelineResources()
        {
            var icon = EditorGUIUtility.FindTexture("ScriptableObject Icon");
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<DoCreateNewAssetHDRenderPipelineResources>(), "New HDRenderPipelineResources.asset", icon, null);
        }

        class DoCreateNewAssetHDRenderPipelineRayTracingResources : ProjectWindowCallback.EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var newAsset = CreateInstance<HDRenderPipelineRayTracingResources>();
                newAsset.name = Path.GetFileName(pathName);

                ResourceReloader.ReloadAllNullIn(newAsset, HDUtils.GetHDRenderPipelinePath());

                AssetDatabase.CreateAsset(newAsset, pathName);
                ProjectWindowUtil.ShowCreatedAsset(newAsset);
            }
        }

        // Hide: User aren't suppose to have to create it.
        //[MenuItem("Assets/Create/Rendering/HDRP Ray Tracing Resources", priority = CoreUtils.Sections.section7 + CoreUtils.Priorities.assetsCreateRenderingMenuPriority + 2)]
        static void CreateRenderPipelineRayTracingResources()
        {
            var icon = EditorGUIUtility.FindTexture("ScriptableObject Icon");
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<DoCreateNewAssetHDRenderPipelineRayTracingResources>(), "New HDRenderPipelineRayTracingResources.asset", icon, null);
        }

        class DoCreateNewAssetHDRenderPipelineEditorResources : ProjectWindowCallback.EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var newAsset = CreateInstance<HDRenderPipelineEditorResources>();
                newAsset.name = Path.GetFileName(pathName);

                ResourceReloader.ReloadAllNullIn(newAsset, HDUtils.GetHDRenderPipelinePath());

                AssetDatabase.CreateAsset(newAsset, pathName);
                ProjectWindowUtil.ShowCreatedAsset(newAsset);
            }
        }

        // Hide: User aren't suppose to have to create it.
        //[MenuItem("Assets/Create/Rendering/HDRP Editor Resources", priority = CoreUtils.Sections.section7 + CoreUtils.Priorities.assetsCreateRenderingMenuPriority + 1)]
        static void CreateRenderPipelineEditorResources()
        {
            var icon = EditorGUIUtility.FindTexture("ScriptableObject Icon");
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<DoCreateNewAssetHDRenderPipelineEditorResources>(), "New HDRenderPipelineEditorResources.asset", icon, null);
        }
    }
}
