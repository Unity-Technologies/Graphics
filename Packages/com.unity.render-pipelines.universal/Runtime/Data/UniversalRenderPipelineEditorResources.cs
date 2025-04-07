using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Class containing shader and texture resources needed in URP.
    /// </summary>
    /// <seealso cref="Shader"/>
    /// <seealso cref="Material"/>
    public class UniversalRenderPipelineEditorResources : ScriptableObject
    {
        /// <summary>
        /// Class containing shader resources used in URP.
        /// </summary>
        [Serializable, ReloadGroup]
        public sealed class ShaderResources
        {
            /// <summary>
            /// Autodesk Interactive ShaderGraph shader.
            /// </summary>
            [Reload("Shaders/AutodeskInteractive/AutodeskInteractive.shadergraph")]
            public Shader autodeskInteractivePS;

            /// <summary>
            /// Autodesk Interactive Transparent ShaderGraph shader.
            /// </summary>
            [Reload("Shaders/AutodeskInteractive/AutodeskInteractiveTransparent.shadergraph")]
            public Shader autodeskInteractiveTransparentPS;

            /// <summary>
            /// Autodesk Interactive Masked ShaderGraph shader.
            /// </summary>
            [Reload("Shaders/AutodeskInteractive/AutodeskInteractiveMasked.shadergraph")]
            public Shader autodeskInteractiveMaskedPS;

            /// <summary>
            /// SpeedTree7 shader.
            /// </summary>
            [Reload("Shaders/Nature/SpeedTree7.shader")]
            public Shader defaultSpeedTree7PS;

            /// <summary>
            /// SpeedTree8 ShaderGraph shader.
            /// </summary>
            [Reload("Shaders/Nature/SpeedTree8_PBRLit.shadergraph")]
            public Shader defaultSpeedTree8PS;
        }

        /// <summary>
        /// Class containing material resources used in URP.
        /// </summary>
        [Serializable, ReloadGroup]
        public sealed class MaterialResources
        {
            /// <summary>
            /// Lit material.
            /// </summary>
            [Reload("Runtime/Materials/Lit.mat")]
            public Material lit;

            // particleLit is the URP default material for new particle systems.
            // ParticlesUnlit.mat is closest match to the built-in shader.
            // This is correct (current 22.2) despite the Lit/Unlit naming conflict.
            /// <summary>
            /// Particle Lit material.
            /// </summary>
            [Reload("Runtime/Materials/ParticlesUnlit.mat")]
            public Material particleLit;

            /// <summary>
            /// Terrain Lit material.
            /// </summary>
            [Reload("Runtime/Materials/TerrainLit.mat")]
            public Material terrainLit;

            /// <summary>
            /// Decal material.
            /// </summary>
            [Reload("Runtime/Materials/Decal.mat")]
            public Material decal;
        }

        /// <summary>
        /// Shader resources used in URP.
        /// </summary>
        public ShaderResources shaders;

        /// <summary>
        /// Material resources used in URP.
        /// </summary>
        public MaterialResources materials;
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(UniversalRenderPipelineEditorResources), true)]
    class UniversalRenderPipelineEditorResourcesEditor : UnityEditor.Editor
    {
        /// <inheritdoc/>
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
