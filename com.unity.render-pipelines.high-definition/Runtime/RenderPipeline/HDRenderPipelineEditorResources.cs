#if UNITY_EDITOR //file must be in realtime assembly folder to be found in HDRPAsset
using System;

namespace UnityEngine.Rendering.HighDefinition
{
    [HelpURL(Documentation.baseURL + Documentation.version + Documentation.subURL + "HDRP-Asset" + Documentation.endURL)]
    public partial class HDRenderPipelineEditorResources : ScriptableObject
    {
        [Reload(new[]
        {
            "Runtime/RenderPipelineResources/Skin Diffusion Profile.asset",
            "Runtime/RenderPipelineResources/Foliage Diffusion Profile.asset"
        })]
        [SerializeField]
        internal DiffusionProfileSettings[] defaultDiffusionProfileSettingsList;

        [Reload("Editor/RenderPipelineResources/DefaultSettingsVolumeProfile.asset")]
        public VolumeProfile defaultSettingsVolumeProfile;

        [Serializable, ReloadGroup]
        public sealed class ShaderResources
        {
            public Shader terrainDetailLitShader;
            public Shader terrainDetailGrassShader;
            public Shader terrainDetailGrassBillboardShader;
        }

        [Serializable, ReloadGroup]
        public sealed class MaterialResources
        {
            // Defaults
            [Reload("Runtime/RenderPipelineResources/Material/DefaultHDMaterial.mat")]
            public Material defaultDiffuseMat;
            [Reload("Runtime/RenderPipelineResources/Material/DefaultHDMirrorMaterial.mat")]
            public Material defaultMirrorMat;
            [Reload("Runtime/RenderPipelineResources/Material/DefaultHDDecalMaterial.mat")]
            public Material defaultDecalMat;
            [Reload("Runtime/RenderPipelineResources/Material/DefaultHDParticleMaterial.mat")]
            public Material defaultParticleMat;
            [Reload("Runtime/RenderPipelineResources/Material/DefaultHDTerrainMaterial.mat")]
            public Material defaultTerrainMat;
            [Reload("Editor/RenderPipelineResources/Material/GUITextureBlit2SRGB.mat")]
            public Material GUITextureBlit2SRGB;
        }

        [Serializable, ReloadGroup]
        public sealed class TextureResources
        {
            // Light Unit Icons
            [Reload("Editor/RenderPipelineResources/Texture/LightUnitIcons/AreaExterior.png")]
            public Texture2D iconAreaExterior;
            [Reload("Editor/RenderPipelineResources/Texture/LightUnitIcons/BrightSky.png")]
            public Texture2D iconBrightSky;
            [Reload("Editor/RenderPipelineResources/Texture/LightUnitIcons/Candle.png")]
            public Texture2D iconCandle;
            [Reload("Editor/RenderPipelineResources/Texture/LightUnitIcons/ClearSky.png")]
            public Texture2D iconClearSky;
            [Reload("Editor/RenderPipelineResources/Texture/LightUnitIcons/Decorative.png")]
            public Texture2D iconDecorative;
            [Reload("Editor/RenderPipelineResources/Texture/LightUnitIcons/DecorativeArea.png")]
            public Texture2D iconDecorativeArea;
            [Reload("Editor/RenderPipelineResources/Texture/LightUnitIcons/DirectSunlight.png")]
            public Texture2D iconDirectSunlight;
            [Reload("Editor/RenderPipelineResources/Texture/LightUnitIcons/Exterior.png")]
            public Texture2D iconExterior;
            [Reload("Editor/RenderPipelineResources/Texture/LightUnitIcons/Interior.png")]
            public Texture2D iconInterior;
            [Reload("Editor/RenderPipelineResources/Texture/LightUnitIcons/InteriorArea.png")]
            public Texture2D iconInteriorArea;
            [Reload("Editor/RenderPipelineResources/Texture/LightUnitIcons/MoonlessNight.png")]
            public Texture2D iconMoonlessNight;
            [Reload("Editor/RenderPipelineResources/Texture/LightUnitIcons/MoonlitSky.png")]
            public Texture2D iconMoonlitSky;
            [Reload("Editor/RenderPipelineResources/Texture/LightUnitIcons/OvercastSky.png")]
            public Texture2D iconOvercastSky;
            [Reload("Editor/RenderPipelineResources/Texture/LightUnitIcons/SunriseSunset.png")]
            public Texture2D iconSunriseSunset;
        }

        [Serializable, ReloadGroup]
        public sealed class ShaderGraphResources
        {
            [Reload("Runtime/RenderPipelineResources/ShaderGraph/AutodeskInteractive.ShaderGraph")]
            public Shader autodeskInteractive;
            [Reload("Runtime/RenderPipelineResources/ShaderGraph/AutodeskInteractiveMasked.ShaderGraph")]
            public Shader autodeskInteractiveMasked;
            [Reload("Runtime/RenderPipelineResources/ShaderGraph/AutodeskInteractiveTransparent.ShaderGraph")]
            public Shader autodeskInteractiveTransparent;
        }

        [Serializable, ReloadGroup]
        public sealed class LookDevResources
        {
            [Reload("Editor/RenderPipelineResources/DefaultLookDevProfile.asset")]
            public VolumeProfile defaultLookDevVolumeProfile;
        }

        public ShaderResources shaders;
        public MaterialResources materials;
        public TextureResources textures;
        public ShaderGraphResources shaderGraphs;
        public LookDevResources lookDev;
    }

    [UnityEditor.CustomEditor(typeof(HDRenderPipelineEditorResources))]
    class HDRenderPipelineEditorResourcesEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            // Add a "Reload All" button in inspector when we are in developer's mode
            if (UnityEditor.EditorPrefs.GetBool("DeveloperMode")
                && GUILayout.Button("Reload All"))
            {
                foreach(var field in typeof(HDRenderPipelineEditorResources).GetFields())
                    field.SetValue(target, null);

                ResourceReloader.ReloadAllNullIn(target, HDUtils.GetHDRenderPipelinePath());
            }
        }
    }
}
#endif
