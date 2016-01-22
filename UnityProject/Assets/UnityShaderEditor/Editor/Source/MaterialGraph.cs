using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    public class MaterialGraph : ScriptableObject
    {
        [SerializeField]
        private MaterialOptions m_MaterialOptions;

        [SerializeField]
        private PixelGraph m_PixelGraph;
        
        public int GetShaderInstanceID()
        {
            return -1;
            //return m_Shader.GetInstanceID();
        }
        
        public MaterialOptions materialOptions { get { return m_MaterialOptions; } }

        public BaseMaterialGraph currentGraph { get { return m_PixelGraph; } }
        

        public void OnEnable()
        {
            if (m_MaterialOptions == null)
            {
                m_MaterialOptions = CreateInstance<MaterialOptions>();
                m_MaterialOptions.Init();
                m_MaterialOptions.hideFlags = HideFlags.HideInHierarchy;
            }

            if (m_PixelGraph == null)
            {
                m_PixelGraph = CreateInstance<PixelGraph>();
                m_PixelGraph.hideFlags = HideFlags.HideInHierarchy;
                m_PixelGraph.name = name;
            }

            m_PixelGraph.owner = this;
        }

        public void OnDisable()
        {
            //      if (m_MaterialProperties != null)
            //      m_MaterialProperties.OnChangePreviewState -= OnChangePreviewState;
        }
        
        public void CreateSubAssets()
        {
            AssetDatabase.AddObjectToAsset(m_MaterialOptions, this);
            AssetDatabase.AddObjectToAsset(m_PixelGraph, this);
        }
        
        private Material m_Material;
        public Material GetMaterial()
        {
            if (m_PixelGraph == null)
                return null;
            
            return m_PixelGraph.GetMaterial();
        }

        public void ExportShader(string path)
        {
            List<PropertyGenerator.TextureInfo> configuredTextures;
            var shaderString = ShaderGenerator.GenerateSurfaceShader(this, name, false, out configuredTextures);
            File.WriteAllText(path, shaderString);
            AssetDatabase.Refresh(); // Investigate if this is optimal
             
            var shader = AssetDatabase.LoadAssetAtPath(path, typeof(Shader)) as Shader;
            if (shader == null)
                return;

            var shaderImporter = AssetImporter.GetAtPath(path) as ShaderImporter;
            if (shaderImporter == null)
                return;

            var textureNames = new List<string>();
            var textures = new List<Texture>();
            foreach (var textureInfo in configuredTextures.Where(x => x.modifiable))
            {
                var texture = EditorUtility.InstanceIDToObject(textureInfo.textureId) as Texture;
                if (texture == null)
                    continue;
                textureNames.Add(textureInfo.name);
                textures.Add(texture);
            }
            shaderImporter.SetDefaultTextures(textureNames.ToArray(), textures.ToArray());

            textureNames.Clear();
            textures.Clear();
            foreach (var textureInfo in configuredTextures.Where(x => !x.modifiable))
            {
                var texture = EditorUtility.InstanceIDToObject(textureInfo.textureId) as Texture;
                if (texture == null)
                    continue;
                textureNames.Add(textureInfo.name);
                textures.Add(texture);
            }
            shaderImporter.SetNonModifiableTextures(textureNames.ToArray(), textures.ToArray());

            shaderImporter.SaveAndReimport();
        }
    }
}
