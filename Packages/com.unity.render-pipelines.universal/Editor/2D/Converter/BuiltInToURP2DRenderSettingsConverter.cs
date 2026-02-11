using System;
using UnityEditor.Rendering.Converter;
using UnityEngine;
using UnityEngine.Categorization;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [Serializable]
    [PipelineConverter("Built-in", "Universal Render Pipeline (2D Renderer)")]
    [ElementInfo(Name = "Rendering Settings",
                 Order = int.MinValue,
                 Description = "This converter creates Universal Render Pipeline (URP) assets and corresponding Renderer assets, configuring their settings to match the equivalent settings from the Built-in Render Pipeline.")]
    class BuiltInToURP2DRenderSettingsConverter : RenderSettingsConverter
    {
        public override bool isEnabled => true;

        public override string isDisabledMessage => string.Empty;

        protected override RenderPipelineAsset CreateAsset(string name)
        {
            string path = $"Assets/{UniversalProjectSettings.projectSettingsFolderPath}/{name}.asset";
            if (AssetDatabase.AssetPathExists(path))
                return AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);

            try
            {
                CoreUtils.EnsureFolderTreeInAssetFilePath(path);
                var asset = ScriptableObject.CreateInstance(typeof(UniversalRenderPipelineAsset)) as UniversalRenderPipelineAsset;
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssetIfDirty(asset);
                return asset;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Unable to create asset at path {path} with exception {ex.Message}");
                return null;
            }
        }

        Renderer2DData CreateRenderer2DDataAsset()
        {
            string path = $"Assets/{UniversalProjectSettings.projectSettingsFolderPath}/Default_2D_Renderer.asset";
            if (AssetDatabase.AssetPathExists(path))
                return AssetDatabase.LoadAssetAtPath<Renderer2DData>(path);

            CoreUtils.EnsureFolderTreeInAssetFilePath(path);

            var asset = Renderer2DMenus.CreateRendererAsset(path, RendererType._2DRenderer, relativePath: false) as Renderer2DData;
            
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);

            return asset;
        }

        void GetRenderers(out ScriptableRendererData[] renderers, out int defaultIndex)
        {
            defaultIndex = 0;


            using (ListPool<ScriptableRendererData>.Get(out var tmp))
            {
                tmp.Add(CreateRenderer2DDataAsset());

                // In case we need multiple renderers modify the defaultIndex and add more renderers here
                // ...

                renderers = tmp.ToArray();
            }

            // Tell the asset database to regenerate the fileId, otherwise when adding the reference to the URP
            // asset the fileId might not be computed and the reference might be lost.
            AssetDatabase.Refresh();
        }

        protected override void SetPipelineSettings(RenderPipelineAsset asset)
        {
            if (asset is not UniversalRenderPipelineAsset urpAsset)
                return;

            GetRenderers(out var renderers, out var defaultIndex);
            urpAsset.m_RendererDataList = renderers;
            urpAsset.m_DefaultRendererIndex = defaultIndex;
        }
    }
}
