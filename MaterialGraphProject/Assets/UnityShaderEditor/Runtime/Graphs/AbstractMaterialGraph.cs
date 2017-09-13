using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    public interface IAbstractShaderProperty
    {
        string name { get; set; }
        string description { get; set; }
        string propertyName { get; }
        PropertyType propertyType { get; }
        Guid guid { get; }
    }

    [Serializable]
    public abstract class AbstractShaderProperty<T> : ISerializationCallbackReceiver, IAbstractShaderProperty
    {
        [SerializeField]
        private T m_Value;

        [SerializeField]
        private string m_Description;

        [SerializeField]
        private string m_Name;

        [NonSerialized]
        private Guid m_Guid;

        [SerializeField]
        private string m_GuidSerialized;

        protected AbstractShaderProperty()
        {
            m_Guid = Guid.NewGuid();
        }

        public T value
        {
            get { return m_Value; }
            set { m_Value = value; }
        }

        public string name
        {
            get
            {
                if (string.IsNullOrEmpty(m_Name))
                    return m_Guid.ToString();
                return m_Name;
            }
            set { m_Name = value; }
        }

        public string description
        {
            get
            {
                return string.IsNullOrEmpty(m_Description) ? name : m_Description;
            }
            set { m_Description = value; }
        }

        public string propertyName
        {
            get
            {
                return string.Format("{0}_Uniform", name);
            }
        }

        public abstract PropertyType propertyType { get; }

        public Guid guid
        {
            get { return m_Guid; }
        }

        public virtual void OnBeforeSerialize()
        {
            m_GuidSerialized = m_Guid.ToString();
        }

        public virtual void OnAfterDeserialize()
        {
            if (!string.IsNullOrEmpty(m_GuidSerialized))
                m_Guid = new Guid(m_GuidSerialized);
            else
                m_Guid = Guid.NewGuid();
        }
    }

    [Serializable]
    public class FloatShaderProperty : AbstractShaderProperty<float>
    {
        public override PropertyType propertyType
        {
            get { return PropertyType.Float; }
        }
    }

    [Serializable]
    public class Vector2ShaderProperty : AbstractShaderProperty<Vector2>
    {
        public override PropertyType propertyType
        {
            get { return PropertyType.Vector2; }
        }
    }

    [Serializable]
    public class Vector3ShaderProperty : AbstractShaderProperty<Vector3>
    {
        public override PropertyType propertyType
        {
            get { return PropertyType.Vector2; }
        }
    }

    [Serializable]
    public class Vector4ShaderProperty : AbstractShaderProperty<Vector4>
    {
        public override PropertyType propertyType
        {
            get { return PropertyType.Vector4; }
        }
    }

    [Serializable]
    public class ColorShaderProperty : AbstractShaderProperty<Color>
    {
        [SerializeField]
        private bool m_HDR;

        public bool HDR
        {
            get { return m_HDR; }
            set
            {
                if (m_HDR == value)
                    return;

                m_HDR = value;
            }
        }

        public override PropertyType propertyType
        {
            get { return PropertyType.Color; }
        }

        public void GeneratePropertyBlock(PropertyGenerator visitor, GenerationMode generationMode)
        {
            visitor.AddShaderProperty(new ColorPropertyChunk(propertyName, description, value, m_HDR ? ColorPropertyChunk.ColorType.HDR : ColorPropertyChunk.ColorType.Default , PropertyChunk.HideState.Visible));
        }

        public void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode)
        {
            visitor.AddShaderChunk("float4 " + propertyName + ";", true);
        }

        public PreviewProperty GetPreviewProperty()
        {
            return new PreviewProperty
            {
                m_Name = propertyName,
                m_PropType = PropertyType.Color,
                m_Color = value
            };
        }
    }

    [Serializable]
    public class SerializableTexture
    {
        [SerializeField] private string m_SerializedTexture;

        [Serializable]
        private class TextureHelper
        {
            public Texture texture;
        }

#if UNITY_EDITOR
        public Texture texture
        {
            get
            {
                if (string.IsNullOrEmpty(m_SerializedTexture))
                    return null;

                var tex = new TextureHelper();
                EditorJsonUtility.FromJsonOverwrite(m_SerializedTexture, tex);
                return tex.texture;
            }
            set
            {
                if (texture == value)
                    return;

                var tex = new TextureHelper();
                tex.texture = value;
                m_SerializedTexture = EditorJsonUtility.ToJson(tex, true);
            }
        }
#else
        public Texture defaultTexture {get; set; }
#endif

    }

    [Serializable]
    public class TextureShaderProperty : AbstractShaderProperty<SerializableTexture>
    {
        [SerializeField]
        private bool m_Modifiable;

        public TextureShaderProperty()
        {
            value = new SerializableTexture();
        }

        public override PropertyType propertyType
        {
            get { return PropertyType.Texture; }
        }

        // Properties
        public void GeneratePropertyBlock(PropertyGenerator visitor, GenerationMode generationMode)
        {
            visitor.AddShaderProperty(
                new TexturePropertyChunk(
                    propertyName,
                    description,
                    value.texture, TextureType.Black,
                    PropertyChunk.HideState.Visible,
                    m_Modifiable ?
                        TexturePropertyChunk.ModifiableState.Modifiable
                        : TexturePropertyChunk.ModifiableState.NonModifiable));
        }

        public void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode)
        {
            visitor.AddShaderChunk("UNITY_DECLARE_TEX2D(" + propertyName + ");", true);
        }

        public PreviewProperty GetPreviewProperty()
        {
            return new PreviewProperty
            {
                m_Name = propertyName,
                m_PropType = PropertyType.Texture,
                m_Texture = value.texture
            };
        }
    }

    [Serializable]
    public abstract class AbstractMaterialGraph : SerializableGraph
    {
        [NonSerialized]
        private List<IAbstractShaderProperty> m_Properties = new List<IAbstractShaderProperty>();

        [SerializeField]
        private List<SerializationHelper.JSONSerializedElement> m_SerializedProperties = new List<SerializationHelper.JSONSerializedElement>();


        public IEnumerable<IAbstractShaderProperty> properties
        {
            get { return m_Properties; }
        }

        public override void AddNode(INode node)
        {
            if (node is AbstractMaterialNode)
            {
                base.AddNode(node);
            }
            else
            {
                Debug.LogWarningFormat("Trying to add node {0} to Material graph, but it is not a {1}", node, typeof(AbstractMaterialNode));
            }
        }

        public void AddShaderProperty(IAbstractShaderProperty property)
        {
            if (property == null)
                return;

            if (m_Properties.Contains(property))
                return;

            m_Properties.Add(property);
        }

        public void RemoveShaderProperty(Guid guid)
        {
            m_Properties.RemoveAll(x => x.guid == guid);
        }

        public override Dictionary<SerializationHelper.TypeSerializationInfo, SerializationHelper.TypeSerializationInfo> GetLegacyTypeRemapping()
        {
            var result = base.GetLegacyTypeRemapping();
            var viewNode = new SerializationHelper.TypeSerializationInfo
            {
                fullName = "UnityEngine.MaterialGraph.ViewDirectionNode",
                assemblyName = "Assembly-CSharp"
            };
            result[viewNode] = SerializationHelper.GetTypeSerializableAsString(typeof(WorldSpaceViewDirectionNode));

            var normalNode = new SerializationHelper.TypeSerializationInfo
            {
                fullName = "UnityEngine.MaterialGraph.NormalNode",
                assemblyName = "Assembly-CSharp"
            };
            result[normalNode] = SerializationHelper.GetTypeSerializableAsString(typeof(WorldSpaceNormalNode));

            var worldPosNode = new SerializationHelper.TypeSerializationInfo
            {
                fullName = "UnityEngine.MaterialGraph.WorldPosNode",
                assemblyName = "Assembly-CSharp"
            };
            result[worldPosNode] = SerializationHelper.GetTypeSerializableAsString(typeof(WorldSpacePositionNode));


            return result;
        }

        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();
            m_SerializedProperties = SerializationHelper.Serialize<IAbstractShaderProperty>(m_Properties);
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            m_Properties = SerializationHelper.Deserialize<IAbstractShaderProperty>(m_SerializedProperties, null);
        }
    }
}
