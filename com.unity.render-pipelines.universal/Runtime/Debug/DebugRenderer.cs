namespace UnityEngine.Rendering.Universal

{
    internal class DebugRenderer : ScriptableRenderer
    {
        int m_DebugMaterialIndexId = Shader.PropertyToID("_DebugMaterialIndex");
        int m_DebugLightingIndexId = Shader.PropertyToID("_DebugLightingIndex");
        int m_DebugVertexAttributesIndexId = Shader.PropertyToID("_DebugAttributesIndex");
        int m_DebugPBRLightingMask = Shader.PropertyToID("_DebugPBRLightingMask");
        int m_DebugValidationIndexId = Shader.PropertyToID("_DebugValidationIndex");
        int m_DebugAlbedoMinLuminance = Shader.PropertyToID("_AlbedoMinLuminance");
        int m_DebugAlbedoMaxLuminance = Shader.PropertyToID("_AlbedoMaxLuminance");
        int m_DebugAlbedoSaturationTolerance = Shader.PropertyToID("_AlbedoSaturationTolerance");
        int m_DebugAlbedoHueTolerance = Shader.PropertyToID("_AlbedoHueTolerance");
        int m_DebugAlbedoCompareColor = Shader.PropertyToID("_AlbedoCompareColor");
        int m_DebugMipIndexId = Shader.PropertyToID("_DebugMipIndex");
        
        DebugPass m_DebugPass;
        
        public DebugRenderer(DebugRendererData data) : base(data)
        {
            Material fullScreenDebugMaterial = CoreUtils.CreateEngineMaterial(data.shaders.fullScreenDebugPS);
            m_DebugPass = new DebugPass(RenderPassEvent.AfterRendering, fullScreenDebugMaterial);
        }

        public override void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            SetupDebugRendering(context);
        }
        
        void SetupDebugRendering(ScriptableRenderContext context)
        {
            debugMaterialIndex = DebugDisplaySettings.Instance.materialSettings.DebugMaterialIndexData;
            lightingDebugMode = DebugDisplaySettings.Instance.Lighting.m_LightingDebugMode;
            attributeDebugIndex = DebugDisplaySettings.Instance.materialSettings.VertexAttributeDebugIndexData;
            PBRLightingDebugMode pbrLightingDebugMode = DebugDisplaySettings.Instance.Lighting.m_PBRLightingDebugMode;
            pbrLightingDebugModeMask = (int) pbrLightingDebugMode;
            debugMipInfo = DebugDisplaySettings.Instance.renderingSettings.mipInfoDebugMode;

            var cmd = CommandBufferPool.Get("");
            cmd.SetGlobalFloat(m_DebugMaterialIndexId, (int)debugMaterialIndex);
            cmd.SetGlobalFloat(m_DebugLightingIndexId, (int)lightingDebugMode);
            cmd.SetGlobalFloat(m_DebugVertexAttributesIndexId, (int)attributeDebugIndex);
            cmd.SetGlobalInt(m_DebugPBRLightingMask, (int)pbrLightingDebugModeMask);
            cmd.SetGlobalInt(m_DebugMipIndexId, (int)debugMipInfo);
            cmd.SetGlobalInt(m_DebugValidationIndexId, (int)DebugDisplaySettings.Instance.Validation.validationMode);

            cmd.SetGlobalFloat(m_DebugAlbedoMinLuminance, DebugDisplaySettings.Instance.Validation.AlbedoMinLuminance);
            cmd.SetGlobalFloat(m_DebugAlbedoMaxLuminance, DebugDisplaySettings.Instance.Validation.AlbedoMaxLuminance);
            cmd.SetGlobalFloat(m_DebugAlbedoSaturationTolerance, DebugDisplaySettings.Instance.Validation.AlbedoSaturationTolerance);
            cmd.SetGlobalFloat(m_DebugAlbedoHueTolerance, DebugDisplaySettings.Instance.Validation.AlbedoHueTolerance);
            cmd.SetGlobalColor(m_DebugAlbedoCompareColor, DebugDisplaySettings.Instance.Validation.AlbedoCompareColor.linear);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
