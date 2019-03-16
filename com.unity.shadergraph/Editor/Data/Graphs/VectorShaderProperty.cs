using System;
using System.Text;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    abstract class VectorShaderProperty : AbstractShaderProperty<Vector4>
    {
        [SerializeField]
        bool    m_Hidden = false;

        public bool hidden
        {
            get { return m_Hidden; }
            set { m_Hidden = value; }
        }

        public override string GetPropertyBlockString()
        {
            var result = new StringBuilder();
            if (hidden)
                result.Append("[HideInInspector] ");
            result.Append(referenceName);
            result.Append("(\"");
            result.Append(displayName);
            result.Append("\", Vector) = (");
            result.Append(NodeUtils.FloatToShaderValue(value.x));
            result.Append(",");
            result.Append(NodeUtils.FloatToShaderValue(value.y));
            result.Append(",");
            result.Append(NodeUtils.FloatToShaderValue(value.z));
            result.Append(",");
            result.Append(NodeUtils.FloatToShaderValue(value.w));
            result.Append(")");
            return result.ToString();
        }
    }
}
