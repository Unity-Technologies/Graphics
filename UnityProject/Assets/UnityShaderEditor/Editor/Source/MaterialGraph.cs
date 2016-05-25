using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    [Serializable]
    public class MaterialGraph
    {
        [SerializeField]
        private MaterialOptions m_MaterialOptions = new MaterialOptions();

        [SerializeField]
        private PixelGraph m_PixelGraph;

        [SerializeField]
        private string m_Name;
 
        public string name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }

        public MaterialGraph()
        {
            m_PixelGraph = new PixelGraph();
        }

        public MaterialOptions materialOptions
        {
            get { return m_MaterialOptions; }
        }

        public AbstractMaterialGraph currentGraph
        {
            get { return m_PixelGraph; }
        }

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

        public void PostCreate()
        {
            m_PixelGraph.AddNode(new PixelShaderNode(m_PixelGraph));
        }
    }
}
