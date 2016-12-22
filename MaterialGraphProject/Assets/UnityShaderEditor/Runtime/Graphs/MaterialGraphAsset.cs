using System.Collections.Generic;
using System.IO;
using System.Linq;
#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
#endif
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    public class MaterialGraphAsset : ScriptableObject, IMaterialGraphAsset
    {
        [SerializeField]
        private MaterialGraph m_MaterialGraph = new MaterialGraph();

        [SerializeField]
        private Shader m_GeneratedShader;

        public IGraph graph
        {
            get { return m_MaterialGraph; }
        }

        public bool shouldRepaint
        {
            get { return graph.GetNodes<AbstractMaterialNode>().OfType<IRequiresTime>().Any(); }
        }

        public ScriptableObject GetScriptableObject()
        {
            return this;
        }

        public void OnEnable()
        {
            graph.OnEnable();
        }

#if UNITY_EDITOR
        public static bool ShaderHasError(Shader shader)
        {
            var hasErrorsCall = typeof(ShaderUtil).GetMethod("GetShaderErrorCount", BindingFlags.Static | BindingFlags.NonPublic);
            var result = hasErrorsCall.Invoke(null, new object[] { shader });
            return (int)result != 0;
        }

        public bool RegenerateInternalShader()
        {
            if (m_MaterialGraph.masterNode == null)
                return false;

            var path = "Assets/GraphTemp.shader";
            List<PropertyGenerator.TextureInfo> configuredTextures;
            var shaderString = m_MaterialGraph.masterNode.GetFullShader(GenerationMode.ForReals, out configuredTextures);
            File.WriteAllText(path, shaderString);
            AssetDatabase.ImportAsset(path);

            var shader = AssetDatabase.LoadAssetAtPath(path, typeof(Shader)) as Shader;
            if (shader == null)
                return false;

            var shaderImporter = AssetImporter.GetAtPath(path) as ShaderImporter;
            if (shaderImporter == null)
                return false;

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

            var imported = shaderImporter.GetShader();

            if (m_GeneratedShader == null)
            {
                m_GeneratedShader = Instantiate(imported);
                AssetDatabase.AddObjectToAsset(m_GeneratedShader, this);
            }
            else
            {
                AssetDatabase.CopyAsset(imported, m_GeneratedShader);
                DestroyImmediate(imported, true);
            }
            AssetDatabase.DeleteAsset(path);

            return true;
        }
#endif

        private int GetShaderInstanceID()
        {
            return m_GeneratedShader == null ? 0 : m_GeneratedShader.GetInstanceID();
        }

        [SerializeField]
        private GraphDrawingData m_DrawingData = new GraphDrawingData();

        public GraphDrawingData drawingData
        {
            get { return m_DrawingData; }
        }
    }
}
