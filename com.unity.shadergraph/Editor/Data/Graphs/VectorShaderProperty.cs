using System;
using System.Text;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public abstract class VectorShaderProperty : AbstractShaderProperty<Vector4>
    {
        public override string GetPropertyBlockString()
        {
            var result = new StringBuilder();
            result.Append(referenceName);
            result.Append("(\"");
            result.Append(displayName);
            result.Append("\", Vector) = (");
            result.Append(value.x);
            result.Append(",");
            result.Append(value.y);
            result.Append(",");
            result.Append(value.z);
            result.Append(",");
            result.Append(value.w);
            result.Append(")");
            return result.ToString();
        }
    }
}
