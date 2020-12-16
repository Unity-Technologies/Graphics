
using System.Diagnostics;
using UnityEditor.Rendering;
using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    public class DebugHandler
    {
        private readonly Material m_FullScreenDebugMaterial;
        private readonly Texture2D m_NumberFontTexture;
        private readonly Material m_ReplacementMaterial;

        private readonly int m_DebugMaterialIndexId;
        private readonly int m_DebugLightingIndexId;
        private readonly int m_DebugVertexAttributesIndexId;
        private readonly int m_DebugLightingFeatureMaskId;
        private readonly int m_DebugValidationIndexId;
        private readonly int m_DebugAlbedoMinLuminanceId;
        private readonly int m_DebugAlbedoMaxLuminanceId;
        private readonly int m_DebugAlbedoSaturationToleranceId;
        private readonly int m_DebugAlbedoHueToleranceId;
        private readonly int m_DebugAlbedoCompareColorId;
        private readonly int m_DebugMipIndexId;

        private readonly DebugDisplaySettings m_DebugDisplaySettings;

        private DebugDisplaySettingsLighting LightingSettings => m_DebugDisplaySettings.Lighting;
        private DebugMaterialSettings MaterialSettings => m_DebugDisplaySettings.materialSettings;
        private DebugDisplaySettingsRendering RenderingSettings => m_DebugDisplaySettings.renderingSettings;
        private DebugDisplaySettingsValidation ValidationSettings => m_DebugDisplaySettings.Validation;

        public bool IsSceneOverrideActive => RenderingSettings.sceneOverrides != SceneOverrides.None;
        public bool IsVertexAttributeOverrideActive => MaterialSettings.VertexAttributeDebugIndexData != VertexAttributeDebugMode.None;
        public bool IsLightingDebugActive => LightingSettings.m_LightingDebugMode != LightingDebugMode.None;
        public bool IsLightingFeatureActive => (int)LightingSettings.m_DebugLightingFeatureMask != 0;
        public bool IsMaterialOverrideActive => MaterialSettings.DebugMaterialIndexData != DebugMaterialIndex.None;
        public bool AreShadowCascadesActive => LightingSettings.m_LightingDebugMode == LightingDebugMode.ShadowCascades;
        public bool IsMipInfoDebugActive => RenderingSettings.mipInfoDebugMode != DebugMipInfo.None;

        public bool IsReplacementMaterialNeeded => IsSceneOverrideActive || IsVertexAttributeOverrideActive;

        public bool IsDebugMaterialActive
        {
            get
            {
                bool isMaterialDebugActive = IsLightingDebugActive || IsMaterialOverrideActive || IsLightingFeatureActive ||
                                             IsVertexAttributeOverrideActive || IsMipInfoDebugActive ||
                                             ValidationSettings.validationMode == DebugValidationMode.ValidateAlbedo;

                return isMaterialDebugActive;
            }
        }

        public DebugHandler(ScriptableRendererData scriptableRendererData)
        {
            Texture2D numberFontTexture = scriptableRendererData.NumberFont;
            Shader fullScreenDebugShader = scriptableRendererData.fullScreenDebugPS;
            Shader debugReplacementShader = scriptableRendererData.debugReplacementPS;

            m_DebugDisplaySettings = DebugDisplaySettings.Instance;

            m_NumberFontTexture = numberFontTexture;
            m_FullScreenDebugMaterial = (fullScreenDebugShader == null) ? null : CoreUtils.CreateEngineMaterial(fullScreenDebugShader);
            m_ReplacementMaterial = (debugReplacementShader == null) ? null : CoreUtils.CreateEngineMaterial(debugReplacementShader);

            m_DebugMaterialIndexId = Shader.PropertyToID("_DebugMaterialIndex");
            m_DebugLightingIndexId = Shader.PropertyToID("_DebugLightingIndex");
            m_DebugVertexAttributesIndexId = Shader.PropertyToID("_DebugAttributesIndex");
            m_DebugLightingFeatureMaskId = Shader.PropertyToID("_DebugLightingFeatureMask");
            m_DebugValidationIndexId = Shader.PropertyToID("_DebugValidationIndex");
            m_DebugAlbedoMinLuminanceId = Shader.PropertyToID("_AlbedoMinLuminance");
            m_DebugAlbedoMaxLuminanceId = Shader.PropertyToID("_AlbedoMaxLuminance");
            m_DebugAlbedoSaturationToleranceId = Shader.PropertyToID("_AlbedoSaturationTolerance");
            m_DebugAlbedoHueToleranceId = Shader.PropertyToID("_AlbedoHueTolerance");
            m_DebugAlbedoCompareColorId = Shader.PropertyToID("_AlbedoCompareColor");
            m_DebugMipIndexId = Shader.PropertyToID("_DebugMipIndex");
        }

        internal DebugPass CreatePass(RenderPassEvent evt)
        {
            return new DebugPass(evt, m_FullScreenDebugMaterial);
        }

        public bool TryGetReplacementMaterial(out Material replacementMaterial)
        {
            if(IsReplacementMaterialNeeded)
            {
                replacementMaterial = m_ReplacementMaterial;
                return true;
            }
            else
            {
                replacementMaterial = default;
                return false;
            }
        }

        public bool TryGetSceneOverride(out SceneOverrides sceneOverride)
        {
            sceneOverride = RenderingSettings.sceneOverrides;
            return IsSceneOverrideActive;
        }

        public bool TryGetFullscreenDebugMode(out FullScreenDebugMode fullScreenDebugMode)
        {
            fullScreenDebugMode = RenderingSettings.fullScreenDebugMode;
            return fullScreenDebugMode != FullScreenDebugMode.None;
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        public void Setup(ScriptableRenderContext context)
        {
            var cmd = CommandBufferPool.Get("");
            cmd.SetGlobalFloat(m_DebugMaterialIndexId, (int)MaterialSettings.DebugMaterialIndexData);
            cmd.SetGlobalFloat(m_DebugLightingIndexId, (int)LightingSettings.m_LightingDebugMode);
			cmd.SetGlobalFloat(m_DebugVertexAttributesIndexId, (int)MaterialSettings.VertexAttributeDebugIndexData);
            cmd.SetGlobalInt(m_DebugLightingFeatureMaskId, (int)LightingSettings.m_DebugLightingFeatureMask);
            cmd.SetGlobalInt(m_DebugMipIndexId, (int)RenderingSettings.mipInfoDebugMode);
            cmd.SetGlobalInt(m_DebugValidationIndexId, (int)ValidationSettings.validationMode);

            cmd.SetGlobalFloat(m_DebugAlbedoMinLuminanceId, ValidationSettings.AlbedoMinLuminance);
            cmd.SetGlobalFloat(m_DebugAlbedoMaxLuminanceId, ValidationSettings.AlbedoMaxLuminance);
            cmd.SetGlobalFloat(m_DebugAlbedoSaturationToleranceId, ValidationSettings.AlbedoSaturationTolerance);
            cmd.SetGlobalFloat(m_DebugAlbedoHueToleranceId, ValidationSettings.AlbedoHueTolerance);
            cmd.SetGlobalColor(m_DebugAlbedoCompareColorId, ValidationSettings.AlbedoCompareColor.linear);

            cmd.SetGlobalTexture("_DebugNumberTexture", m_NumberFontTexture);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
