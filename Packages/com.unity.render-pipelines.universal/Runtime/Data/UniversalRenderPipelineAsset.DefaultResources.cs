using System;
using UnityEditor;

namespace UnityEngine.Rendering.Universal
{
    internal enum DefaultMaterialType
    {
        Default,
        Particle,
        Terrain,
        Sprite,
        SpriteMask,
        Decal
    }

    public partial class UniversalRenderPipelineAsset
    {
        #region Materials

        Material GetMaterial(DefaultMaterialType materialType)
        {
#if UNITY_EDITOR
            Material material = null;

            if (scriptableRendererData != null)
                material = scriptableRendererData.GetDefaultMaterial(materialType);

            if (material == null)
            {
                if (GraphicsSettings.TryGetRenderPipelineSettings<UniversalRenderPipelineEditorMaterials>(out var defaultMaterials))
                {
                    return materialType switch
                    {
                      DefaultMaterialType.Default => defaultMaterials.defaultMaterial,
                      DefaultMaterialType.Particle => defaultMaterials.defaultParticleUnlitMaterial,
                      DefaultMaterialType.Terrain => defaultMaterials.defaultTerrainLitMaterial,
                      DefaultMaterialType.Decal => defaultMaterials.defaultDecalMaterial,
                      _ => null
                    };
                }
            }

            return material;
#else
            return null;
#endif
        }

        /// <summary>
        /// Returns the default Material.
        /// </summary>
        /// <returns>Returns the default Material.</returns>
        public override Material defaultMaterial => GetMaterial(DefaultMaterialType.Default);

        /// <summary>
        /// Returns the default particle Material.
        /// </summary>
        /// <returns>Returns the default particle Material.</returns>
        public override Material defaultParticleMaterial => GetMaterial(DefaultMaterialType.Particle);

        /// <summary>
        /// Returns the default line Material.
        /// </summary>
        /// <returns>Returns the default line Material.</returns>
        public override Material defaultLineMaterial => GetMaterial(DefaultMaterialType.Particle);

        /// <summary>
        /// Returns the default terrain Material.
        /// </summary>
        /// <returns>Returns the default terrain Material.</returns>
        public override Material defaultTerrainMaterial => GetMaterial(DefaultMaterialType.Terrain);

        /// <summary>
        /// Returns the default material for the 2D renderer.
        /// </summary>
        /// <returns>Returns the material containing the default lit and unlit shader passes for sprites in the 2D renderer.</returns>
        public override Material default2DMaterial => GetMaterial(DefaultMaterialType.Sprite);

        /// <summary>
        /// Returns the default sprite mask material for the 2D renderer.
        /// </summary>
        /// <returns>Returns the material containing the default shader pass for sprite mask in the 2D renderer.</returns>
        public override Material default2DMaskMaterial => GetMaterial(DefaultMaterialType.SpriteMask);

        /// <summary>
        /// Returns the Material that Unity uses to render decals.
        /// </summary>
        /// <returns>Returns the Material containing the Unity decal shader.</returns>
        public Material decalMaterial => GetMaterial(DefaultMaterialType.Decal);

        #endregion

        #region Shaders

#if UNITY_EDITOR
        private UniversalRenderPipelineEditorShaders defaultShaders =>
            GraphicsSettings.GetRenderPipelineSettings<UniversalRenderPipelineEditorShaders>();
#endif

        Shader m_DefaultShader;

        /// <summary>
        /// Returns the default shader for the specified renderer. When creating new objects in the editor, the materials of those objects will use the selected default shader.
        /// </summary>
        /// <returns>Returns the default shader for the specified renderer.</returns>
        public override Shader defaultShader
        {
            get
            {
#if UNITY_EDITOR
                // TODO: When importing project, AssetPreviewUpdater:CreatePreviewForAsset will be called multiple time
                // which in turns calls this property to get the default shader.
                // The property should never return null as, when null, it loads the data using AssetDatabase.LoadAssetAtPath.
                // However it seems there's an issue that LoadAssetAtPath will not load the asset in some cases. so adding the null check
                // here to fix template tests.
                if (scriptableRendererData != null)
                {
                    Shader defaultShader = scriptableRendererData.GetDefaultShader();
                    if (defaultShader != null)
                        return defaultShader;
                }

                if (m_DefaultShader == null)
                {
                    string path = AssetDatabase.GUIDToAssetPath(ShaderUtils.GetShaderGUID(ShaderPathID.Lit));
                    m_DefaultShader = AssetDatabase.LoadAssetAtPath<Shader>(path);
                }
#endif

                if (m_DefaultShader == null)
                    m_DefaultShader = Shader.Find(ShaderUtils.GetShaderPath(ShaderPathID.Lit));

                return m_DefaultShader;
            }
        }

#if UNITY_EDITOR

        #region Autodesk

        /// <summary>
        /// Returns the Autodesk Interactive shader that this asset uses.
        /// </summary>
        /// <returns>Returns the Autodesk Interactive shader that this asset uses.</returns>
        public override Shader autodeskInteractiveShader => defaultShaders?.autodeskInteractiveShader;

        /// <summary>
        /// Returns the Autodesk Interactive transparent shader that this asset uses.
        /// </summary>
        /// <returns>Returns the Autodesk Interactive transparent shader that this asset uses.</returns>
        public override Shader autodeskInteractiveTransparentShader => defaultShaders?.autodeskInteractiveTransparentShader;

        /// <summary>
        /// Returns the Autodesk Interactive mask shader that this asset uses.
        /// </summary>
        /// <returns>Returns the Autodesk Interactive mask shader that this asset uses</returns>
        public override Shader autodeskInteractiveMaskedShader => defaultShaders?.autodeskInteractiveMaskedShader;

        #endregion

        #region Terrain

        /// <summary>
        /// Returns the terrain detail lit shader that this asset uses.
        /// </summary>
        /// <returns>Returns the terrain detail lit shader that this asset uses.</returns>
        public override Shader terrainDetailLitShader => defaultShaders?.terrainDetailLitShader;

        /// <summary>
        /// Returns the terrain detail grass shader that this asset uses.
        /// </summary>
        /// <returns>Returns the terrain detail grass shader that this asset uses.</returns>
        public override Shader terrainDetailGrassShader => defaultShaders?.terrainDetailGrassShader;

        /// <summary>
        /// Returns the terrain detail grass billboard shader that this asset uses.
        /// </summary>
        /// <returns>Returns the terrain detail grass billboard shader that this asset uses.</returns>
        public override Shader terrainDetailGrassBillboardShader => defaultShaders?.terrainDetailGrassBillboardShader;

        #endregion

        #region SpeedTree

        /// <summary>
        /// Returns the default SpeedTree7 shader that this asset uses.
        /// </summary>
        /// <returns>Returns the default SpeedTree7 shader that this asset uses.</returns>
        public override Shader defaultSpeedTree7Shader => defaultShaders?.defaultSpeedTree7Shader;

        /// <summary>
        /// Returns the default SpeedTree8 shader that this asset uses.
        /// </summary>
        /// <returns>Returns the default SpeedTree8 shader that this asset uses.</returns>
        public override Shader defaultSpeedTree8Shader => defaultShaders?.defaultSpeedTree8Shader;

        /// <summary>
        /// Returns the default SpeedTree9 shader that this asset uses.
        /// </summary>
        /// <returns>Returns the default SpeedTree9 shader that this asset uses.</returns>
        public override Shader defaultSpeedTree9Shader => defaultShaders?.defaultSpeedTree9Shader;

        #endregion

#endif

        #endregion
    }
}
