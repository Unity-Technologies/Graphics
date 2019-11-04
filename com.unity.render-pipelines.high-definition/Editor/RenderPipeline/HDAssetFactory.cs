using System.IO;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    using UnityObject = UnityEngine.Object;

    static class HDAssetFactory
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
                // Load default renderPipelineResources / Material / Shader
                newAsset.renderPipelineResources = AssetDatabase.LoadAssetAtPath<RenderPipelineResources>(s_RenderPipelineResourcesPath);
                EditorDefaultSettings.GetOrAssignDefaultVolumeProfile(newAsset);

                //as we must init the editor resources with lazy init, it is not required here

                AssetDatabase.CreateAsset(newAsset, pathName);
                ProjectWindowUtil.ShowCreatedAsset(newAsset);
            }
        }

        [MenuItem("Assets/Create/Rendering/High Definition Render Pipeline Asset", priority = CoreUtils.assetCreateMenuPriority1)]
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
                var newAsset = CreateInstance<RenderPipelineResources>();
                newAsset.name = Path.GetFileName(pathName);

                ResourceReloader.ReloadAllNullIn(newAsset, HDUtils.GetHDRenderPipelinePath());

                AssetDatabase.CreateAsset(newAsset, pathName);
                ProjectWindowUtil.ShowCreatedAsset(newAsset);
            }
        }

        // Hide: User aren't suppose to have to create it.
        //[MenuItem("Assets/Create/Rendering/High Definition Render Pipeline Resources", priority = CoreUtils.assetCreateMenuPriority1)]
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
        //[MenuItem("Assets/Create/Rendering/High Definition Render Pipeline Ray Tracing Resources", priority = CoreUtils.assetCreateMenuPriority1)]
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
        //[MenuItem("Assets/Create/Rendering/High Definition Render Pipeline Editor Resources", priority = CoreUtils.assetCreateMenuPriority1)]
        static void CreateRenderPipelineEditorResources()
        {
            var icon = EditorGUIUtility.FindTexture("ScriptableObject Icon");
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<DoCreateNewAssetHDRenderPipelineEditorResources>(), "New HDRenderPipelineEditorResources.asset", icon, null);
        }
    }
}
