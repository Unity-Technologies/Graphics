using System;

namespace UnityEngine.Rendering.Universal
{
    public class UniversalRenderPipelineEditorResources : ScriptableObject
    {
        [Serializable, ReloadGroup]
        public sealed class ShaderResources
        {
            [Reload("Shaders/AutodeskInteractive/AutodeskInteractive.shadergraph")]
            public Shader autodeskInteractivePS;

            [Reload("Shaders/AutodeskInteractive/AutodeskInteractiveTransparent.shadergraph")]
            public Shader autodeskInteractiveTransparentPS;

            [Reload("Shaders/AutodeskInteractive/AutodeskInteractiveMasked.shadergraph")]
            public Shader autodeskInteractiveMaskedPS;

            [Reload("Shaders/Terrain/TerrainDetailLit.shader")]
            public Shader terrainDetailLitPS;

            [Reload("Shaders/Terrain/WavingGrass.shader")]
            public Shader terrainDetailGrassPS;

            [Reload("Shaders/Terrain/WavingGrassBillboard.shader")]
            public Shader terrainDetailGrassBillboardPS;

            [Reload("Shaders/Nature/SpeedTree7.shader")]
            public Shader defaultSpeedTree7PS;

            [Reload("Shaders/Nature/SpeedTree8_PBRLit.shadergraph")]
            public Shader defaultSpeedTree8PS;
        }

        [Serializable, ReloadGroup]
        public sealed class MaterialResources
        {
            [Reload("Runtime/Materials/Lit.mat")]
            public Material lit;

            // particleLit is the URP default material for new particle systems.
            // ParticlesUnlit.mat is closest match to the built-in shader.
            // This is correct (current 22.2) despite the Lit/Unlit naming conflict.
            [Reload("Runtime/Materials/ParticlesUnlit.mat")]
            public Material particleLit;

            [Reload("Runtime/Materials/TerrainLit.mat")]
            public Material terrainLit;

            [Reload("Runtime/Materials/Decal.mat")]
            public Material decal;
        }

        public ShaderResources shaders;
        public MaterialResources materials;
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(UniversalRenderPipelineEditorResources), true)]
    class UniversalRenderPipelineEditorResourcesEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            // Add a "Reload All" button in inspector when we are in developer's mode
            if (UnityEditor.EditorPrefs.GetBool("DeveloperMode") && GUILayout.Button("Reload All"))
            {
                var resources = target as UniversalRenderPipelineEditorResources;
                resources.materials = null;
                resources.shaders = null;
                ResourceReloader.ReloadAllNullIn(target, UniversalRenderPipelineAsset.packagePath);
            }
        }
    }
#endif
}
