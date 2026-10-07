using System;
using Unity.ProjectAuditor.Editor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.ProjectAuditor
{
    class UrpAssetCreator : IRenderPipelineAssetCreator
    {
        const string k_2DTargetPipelineContainer = "BuiltInToURP2D";

        // Null tells the wizard to show its "assign one manually" guidance.
        public RenderPipelineAsset CreateAndAssignDefault(string targetPipeline)
        {
            try
            {
                return CreateAndAssign(targetPipeline == k_2DTargetPipelineContainer);
            }
            catch (Exception e)
            {
                Debug.LogError($"Could not create a URP Render Pipeline Asset: {e.Message}\n{e}");
                return null;
            }
        }

        public bool MatchesTargetPipeline(RenderPipelineAsset asset, string targetPipeline)
        {
            if (asset is not UniversalRenderPipelineAsset urpAsset)
                return false;

            var wants2DRenderer = targetPipeline == k_2DTargetPipelineContainer;
            return (urpAsset.scriptableRenderer is Renderer2D) == wants2DRenderer;
        }

        static RenderPipelineAsset CreateAndAssign(bool use2DRenderer)
        {
            var folder = $"Assets/{UniversalProjectSettings.projectSettingsFolderPath}";

            // Load-or-create, matching the converter, so a later converter run reuses these assets. Each target
            // pipeline gets its own asset names, so switching pipelines doesn't reuse the wrong renderer.
            var rendererPath = use2DRenderer ? $"{folder}/Default_2D_Renderer.asset" : $"{folder}/Default_Forward_Renderer.asset";
            var rendererData = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(rendererPath);
            if (rendererData == null)
            {
                CoreUtils.EnsureFolderTreeInAssetFilePath(rendererPath);

                // Renderer2DMenus.CreateRendererAsset actually creates a Renderer2DData; the RendererType overload
                // on UniversalRenderPipelineAsset has no case for _2DRenderer and would silently create another
                // UniversalRendererData instead.
                rendererData = use2DRenderer
                    ? Renderer2DMenus.CreateRendererAsset(rendererPath, RendererType._2DRenderer, false)
                    : UniversalRenderPipelineAsset.CreateRendererAsset(rendererPath, RendererType.UniversalRenderer, false);

                if (rendererData == null)
                    return null;
            }

            var urpAssetPath = use2DRenderer ? $"{folder}/UniversalRenderPipelineAsset_2D.asset" : $"{folder}/UniversalRenderPipelineAsset.asset";
            var urpAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(urpAssetPath);
            if (urpAsset == null)
            {
                urpAsset = UniversalRenderPipelineAsset.Create(rendererData);
                if (urpAsset == null)
                    return null;

                AssetDatabase.CreateAsset(urpAsset, urpAssetPath);
            }

            AssetDatabase.SaveAssets();

            GraphicsSettings.defaultRenderPipeline = urpAsset;

            return urpAsset;
        }
    }
}
