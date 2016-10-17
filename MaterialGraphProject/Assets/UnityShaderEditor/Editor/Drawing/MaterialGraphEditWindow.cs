using System.Collections.Generic;
using System.IO;
using System.Linq;
using RMGUI.GraphView;
using UnityEditor.Graphing.Drawing;
using UnityEngine;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing
{
    public class MaterialGraphEditWindow : AbstractGraphEditWindow<IMaterialGraphAsset>
    {
        [MenuItem("Window/Material Editor")]
        public static void OpenMenu()
        {
            GetWindow<MaterialGraphEditWindow>();
        }

        public override AbstractGraphDataSource CreateDataSource()
        {
            return CreateInstance<MaterialGraphDataSource>();
        }

        public override GraphView CreateGraphView()
        {
            return new MaterialGraphView();
        }

        private string m_LastPath;

        public void Export(bool quickExport)
        {
            var path = quickExport ? m_LastPath : EditorUtility.SaveFilePanelInProject("Export shader to file...", "shader.shader", "shader", "Enter file name");
            m_LastPath = path; // For quick exporting

            var ds = graphView.dataSource as AbstractGraphDataSource;
            if (ds != null && !string.IsNullOrEmpty(path))
            {
                ExportShader(ds.graphAsset as MaterialGraphAsset, path);
            }
            else
                EditorUtility.DisplayDialog("Export Shader Error", "Cannot export shader", "Ok");
        }

        public static Shader ExportShader(MaterialGraphAsset graphAsset, string path)
        {
            if (graphAsset == null)
                return null;

            var materialGraph = graphAsset.graph as PixelGraph;
            if (materialGraph == null)
                return null;

            List<PropertyGenerator.TextureInfo> configuredTextures;
            var shaderString = ShaderGenerator.GenerateSurfaceShader(materialGraph.pixelMasterNode, new MaterialOptions(), materialGraph.name, false, out configuredTextures);
            File.WriteAllText(path, shaderString);
            AssetDatabase.Refresh(); // Investigate if this is optimal

            var shader = AssetDatabase.LoadAssetAtPath(path, typeof(Shader)) as Shader;
            if (shader == null)
                return null;

            var shaderImporter = AssetImporter.GetAtPath(path) as ShaderImporter;
            if (shaderImporter == null)
                return null;

            var textureNames = new List<string>();
            var textures = new List<Texture>();
            foreach (var textureInfo in configuredTextures.Where(x => x.modifiable == TexturePropertyChunk.ModifiableState.Modifiable))
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
            foreach (var textureInfo in configuredTextures.Where(x => x.modifiable == TexturePropertyChunk.ModifiableState.NonModifiable))
            {
                var texture = EditorUtility.InstanceIDToObject(textureInfo.textureId) as Texture;
                if (texture == null)
                    continue;
                textureNames.Add(textureInfo.name);
                textures.Add(texture);
            }
            shaderImporter.SetNonModifiableTextures(textureNames.ToArray(), textures.ToArray());

            shaderImporter.SaveAndReimport();

            return shaderImporter.GetShader();
        }
    }
}
