using System;
using System.Text;

namespace UnityEngine.MaterialGraph
{
    [Serializable]
    public abstract class VectorShaderProperty : AbstractShaderProperty<Vector4>
    {
        public override string GetPropertyBlockString()
        {
            var result = new StringBuilder();
            result.Append(name);
            result.Append("(\"");
            result.Append(description);
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

        public override string GetPropertyDeclarationString()
        {
            return "float4 " + name + ";";
        }
    }
}
