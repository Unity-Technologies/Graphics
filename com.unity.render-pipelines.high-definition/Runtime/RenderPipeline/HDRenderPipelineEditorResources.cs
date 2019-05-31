#if UNITY_EDITOR //file must be in realtime assembly folder to be found in HDRPAsset
using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public partial class HDRenderPipelineEditorResources : ScriptableObject
    {
<<<<<<< HEAD
        [Reload("DefaultScene/DefaultSceneRoot.prefab", ReloadAttribute.Package.HDRPEditor)]
        public GameObject defaultScene;
        [Reload("DefaultScene/DefaultRenderingSettings.asset", ReloadAttribute.Package.HDRPEditor)]
        public VolumeProfile defaultRenderSettingsProfile;
        [Reload("DefaultScene/DefaultPostProcessingSettings.asset", ReloadAttribute.Package.HDRPEditor)]
        public VolumeProfile defaultPostProcessingProfile;
        [Reload(new[]
        {
            "RenderPipelineResources/Skin Diffusion Profile.asset",
            "RenderPipelineResources/Foliage Diffusion Profile.asset"
=======
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
>>>>>>> master
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
<<<<<<< HEAD
            [Reload("RenderPipelineResources/Material/DefaultHDMaterial.mat")]
            public Material defaultDiffuseMat;
            [Reload("RenderPipelineResources/Material/DefaultHDMirrorMaterial.mat")]
            public Material defaultMirrorMat;
            [Reload("RenderPipelineResources/Material/DefaultHDDecalMaterial.mat")]
            public Material defaultDecalMat;
            [Reload("RenderPipelineResources/Material/DefaultHDTerrainMaterial.mat")]
=======
            [Reload("Runtime/RenderPipelineResources/Material/DefaultHDMaterial.mat")]
            public Material defaultDiffuseMat;
            [Reload("Runtime/RenderPipelineResources/Material/DefaultHDMirrorMaterial.mat")]
            public Material defaultMirrorMat;
            [Reload("Runtime/RenderPipelineResources/Material/DefaultHDDecalMaterial.mat")]
            public Material defaultDecalMat;
            [Reload("Runtime/RenderPipelineResources/Material/DefaultHDTerrainMaterial.mat")]
>>>>>>> master
            public Material defaultTerrainMat;
        }

        [Serializable, ReloadGroup]
        public sealed class TextureResources
        {
        }

        [Serializable, ReloadGroup]
        public sealed class ShaderGraphResources
        {
<<<<<<< HEAD
            [Reload("RenderPipelineResources/ShaderGraph/AutodeskInteractive.ShaderGraph")]
            public Shader autodeskInteractive;
            [Reload("RenderPipelineResources/ShaderGraph/AutodeskInteractiveMasked.ShaderGraph")]
            public Shader autodeskInteractiveMasked;
            [Reload("RenderPipelineResources/ShaderGraph/AutodeskInteractiveTransparent.ShaderGraph")]
=======
            [Reload("Runtime/RenderPipelineResources/ShaderGraph/AutodeskInteractive.ShaderGraph")]
            public Shader autodeskInteractive;
            [Reload("Runtime/RenderPipelineResources/ShaderGraph/AutodeskInteractiveMasked.ShaderGraph")]
            public Shader autodeskInteractiveMasked;
            [Reload("Runtime/RenderPipelineResources/ShaderGraph/AutodeskInteractiveTransparent.ShaderGraph")]
>>>>>>> master
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
<<<<<<< HEAD
                ResourceReloader.ReloadAllNullIn(target);
=======
                ResourceReloader.ReloadAllNullIn(target, HDUtils.GetHDRenderPipelinePath());
>>>>>>> master
            }
        }
    }
}
#endif
