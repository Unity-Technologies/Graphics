using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Serializable]
    public class LayeredShaderGraph : AbstractMaterialGraph, IShaderGraph
    {
        [Serializable]
        public class Layer
        {
            [SerializeField]
            private int m_Layer;

            [SerializeField]
            private Shader m_Shader;

            public Layer()
            {
                m_Layer = Guid.NewGuid().GetHashCode();
            }

            public int layer
            {
                get { return m_Layer; }
            }

            public Shader shader
            {
                get { return m_Shader; }
                set { m_Shader = value; }
            }
        }

        [NonSerialized]
        private List<Layer> m_Layers = new List<Layer>();

        [SerializeField]
        List<SerializationHelper.JSONSerializedElement> m_SerializedLayers = new List<SerializationHelper.JSONSerializedElement>();

        public IEnumerable<Layer> layers
        {
            get { return m_Layers; }
        }

        [NonSerialized]
        private LayerWeightsOutputNode m_OutputNode;

        public LayerWeightsOutputNode outputNode
        {
            get
            {
                // find existing node
                if (m_OutputNode == null)
                    m_OutputNode = GetNodes<LayerWeightsOutputNode>().FirstOrDefault();

                return m_OutputNode;
            }
        }

        public override void AddNode(INode node)
        {
            if (outputNode != null && node is LayerWeightsOutputNode)
            {
                Debug.LogWarning("Attempting to add second LayerWeightsOutputNode to LayeredShaderGraph. This is not allowed.");
                return;
            }

            base.AddNode(node);
        }

        public void AddLayer()
        {
            var layer = new Layer();
            m_Layers.Add(layer);
            NotifyChange(new LayerAdded(layer));

            if (outputNode != null)
                outputNode.onModified(outputNode, ModificationScope.Graph);
        }

        public bool SetLayer(int layerId, Shader newShader)
        {
            try
            {
                var path = AssetDatabase.GetAssetPath(newShader);

                if (!path.EndsWith("shaderGraph", StringComparison.InvariantCultureIgnoreCase))
                    return false;

                var name = Path.GetFileNameWithoutExtension(path);
                var textGraph = File.ReadAllText(path, Encoding.UTF8);
                var graph = JsonUtility.FromJson<MaterialGraph>(textGraph);
                if (graph == null)
                    return false;

                var layer = layers.FirstOrDefault(x => x.layer == layerId);
                if (layer == null)
                    return false;

                layer.shader = newShader;

                if (outputNode != null)
                {
                    outputNode.OnEnable();
                    outputNode.onModified(outputNode, ModificationScope.Graph);
                }

                return true;
            }
            catch (Exception)
            {
                // ignored
            }
            return false;
        }

        public void RemoveLayer(int id)
        {
            var num = m_Layers.RemoveAll(x => x.layer == id);

            if (num > 0)
            {
                NotifyChange(new LayerRemoved(id));

                if (outputNode != null)
                    outputNode.onModified(outputNode, ModificationScope.Graph);
            }

        }

        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();
            m_SerializedLayers = SerializationHelper.Serialize<Layer>(m_Layers);
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            m_OutputNode = null;
            m_Layers = SerializationHelper.Deserialize<Layer>(m_SerializedLayers, null);
            m_SerializedLayers = null;
        }

        public string GetShader(string name, GenerationMode mode, out List<PropertyCollector.TextureInfo> configuredTextures)
        {
            configuredTextures = new List<PropertyCollector.TextureInfo>();
            return string.Empty;
            //PreviewMode pmode;
            //return GetShader(masterNode as AbstractMaterialNode, mode, name, out configuredTextures, out pmode);
        }
    }
}
