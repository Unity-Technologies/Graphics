using System;
using System.Collections.Generic;
using System.Linq;
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
            m_Layers.Add(new Layer());
        }

        public void RemoveLayer()
        {
            m_Layers.Remove(m_Layers.Last());
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
            PreviewMode pmode;
            return GetShader(masterNode as AbstractMaterialNode, mode, name, out configuredTextures, out pmode);
        }
    }
}
