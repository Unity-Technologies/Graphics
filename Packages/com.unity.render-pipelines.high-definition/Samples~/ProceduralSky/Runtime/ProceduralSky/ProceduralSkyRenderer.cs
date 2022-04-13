using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEngine.Rendering.HighDefinition
{
    class ProceduralSkyRenderer : SkyRenderer
    {
        Material m_ProceduralSkyMaterial;
        MaterialPropertyBlock m_PropertyBlock = new MaterialPropertyBlock();

        readonly int _SkyIntensity = Shader.PropertyToID("_SkyIntensity");
        readonly int _PixelCoordToViewDirWS = Shader.PropertyToID("_PixelCoordToViewDirWS");
        readonly int _SunSizeParam = Shader.PropertyToID("_SunSize");
        readonly int _SunSizeConvergenceParam = Shader.PropertyToID("_SunSizeConvergence");
        readonly int _AtmoshpereThicknessParam = Shader.PropertyToID("_AtmosphereThickness");
        readonly int _SkyTintParam = Shader.PropertyToID("_SkyTint");
        readonly int _GroundColorParam = Shader.PropertyToID("_GroundColor");
        readonly int _SunColorParam = Shader.PropertyToID("_SunColor");
        readonly int _SunDirectionParam = Shader.PropertyToID("_SunDirection");

        public ProceduralSkyRenderer()
        {
        }

        public override void Build()
        {
            m_ProceduralSkyMaterial = CoreUtils.CreateEngineMaterial(Shader.Find("Hidden/HDRP/Sky/ProceduralSky"));
        }

        public override void Cleanup()
        {
            CoreUtils.Destroy(m_ProceduralSkyMaterial);
        }

        public override void RenderSky(BuiltinSkyParameters builtinParams, bool renderForCubemap, bool renderSunDisk)
        {
            ProceduralSky skySettings = builtinParams.skySettings as ProceduralSky;
            CoreUtils.SetKeyword(m_ProceduralSkyMaterial, "_ENABLE_SUN_DISK", skySettings.enableSunDisk.value);

            // Default values when no sun is provided
            Color sunColor = Color.white;
            Vector3 sunDirection = Vector3.zero;
            float sunSize = 0.0f;

            if (builtinParams.sunLight != null)
            {
                sunColor = builtinParams.sunLight.color * builtinParams.sunLight.intensity;
                sunDirection = -builtinParams.sunLight.transform.forward;
                sunSize = skySettings.sunSize.value;
            }

            if (!renderSunDisk)
                sunSize = 0.0f;

            m_PropertyBlock.SetFloat(_SkyIntensity, GetSkyIntensity(skySettings, builtinParams.debugSettings));
            m_PropertyBlock.SetFloat(_SunSizeParam, sunSize);
            m_PropertyBlock.SetFloat(_SunSizeConvergenceParam, skySettings.sunSizeConvergence.value);
            m_PropertyBlock.SetFloat(_AtmoshpereThicknessParam, skySettings.atmosphereThickness.value);
            m_PropertyBlock.SetColor(_SkyTintParam, skySettings.skyTint.value);
            m_PropertyBlock.SetColor(_GroundColorParam, skySettings.groundColor.value);
            m_PropertyBlock.SetColor(_SunColorParam, sunColor);
            m_PropertyBlock.SetVector(_SunDirectionParam, sunDirection);
            m_PropertyBlock.SetMatrix(_PixelCoordToViewDirWS, builtinParams.pixelCoordToViewDirMatrix);

            CoreUtils.DrawFullScreen(builtinParams.commandBuffer, m_ProceduralSkyMaterial, m_PropertyBlock, renderForCubemap ? 0 : 1);
        }
    }
}
