#if UNITY_EDITOR //file must be in realtime assembly folder to be found in HDRPAsset
using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public partial class HDRenderPipelineEditorResources : ScriptableObject
    {
        [Reload("Editor/DefaultScene/DefaultSceneRoot.prefab")]
        public GameObject defaultScene;
        [Reload("Editor/DefaultScene/DefaultRenderingSettings.asset")]
        public VolumeProfile defaultRenderSettingsProfile;
        [Reload("Editor/DefaultScene/DefaultPostProcessingSettings.asset")]
        public VolumeProfile defaultPostProcessingProfile;
        [Reload(new[]
        {
            "Runtime/RenderPipelineResources/Skin Diffusion Profile.asset",
            "Runtime/RenderPipelineResources/Foliage Diffusion Profile.asset"
        })]
        public DiffusionProfileSettings[] defaultDiffusionProfileSettingsList;

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
            [Reload("Runtime/RenderPipelineResources/Material/DefaultHDTerrainMaterial.mat")]
            public Material defaultTerrainMat;
            [Reload("Editor/RenderPipelineResources/Materials/GUITextureBlit2SRGB.mat")]
            public Material GUITextureBlit2SRGB;
        }

        [Serializable, ReloadGroup]
        public sealed class TextureResources
        {
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

        public ShaderResources shaders;
        public MaterialResources materials;
        public TextureResources textures;
        public ShaderGraphResources shaderGraphs;
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
                var resources = target as HDRenderPipelineEditorResources;
                resources.defaultScene = null;
                resources.defaultRenderSettingsProfile = null;
                resources.defaultPostProcessingProfile = null;
                resources.defaultDiffusionProfileSettingsList = null;
                resources.materials = null;
                resources.textures = null;
                resources.shaders = null;
                resources.shaderGraphs = null;
                ResourceReloader.ReloadAllNullIn(target, HDUtils.GetHDRenderPipelinePath());
            }
        }
    }
}
#endif
