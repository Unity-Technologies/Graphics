using System;
using Unity.ProjectAuditor.Editor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.ProjectAuditor
{
    class UrpAssetCreator : IRenderPipelineAssetCreator
    {
        // Null tells the wizard to show its "assign one manually" guidance.
        public RenderPipelineAsset CreateAndAssignDefault()
        {
            try
            {
                return CreateAndAssign();
            }
            catch (Exception e)
            {
                Debug.LogError($"Could not create a URP Render Pipeline Asset: {e.Message}\n{e}");
                return null;
            }
        }

        static RenderPipelineAsset CreateAndAssign()
        {
            var folder = $"Assets/{UniversalProjectSettings.projectSettingsFolderPath}";

            // Load-or-create, matching the converter, so a later converter run reuses these assets
            var rendererPath = $"{folder}/Default_Forward_Renderer.asset";
            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (rendererData == null)
            {
                CoreUtils.EnsureFolderTreeInAssetFilePath(rendererPath);
                rendererData = UniversalRenderPipelineAsset.CreateRendererAsset(
                    rendererPath, RendererType.UniversalRenderer, false) as UniversalRendererData;

                if (rendererData == null)
                    return null;
            }

            var urpAssetPath = $"{folder}/UniversalRenderPipelineAsset.asset";
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
