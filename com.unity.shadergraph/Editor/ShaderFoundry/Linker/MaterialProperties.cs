using System.Collections.Generic;
using System.Text;
using BlockProperty = UnityEditor.ShaderFoundry.BlockVariable;

namespace UnityEditor.ShaderFoundry
{
    internal static class MaterialPropertyDeclaration
    {
        internal static void Declare(ShaderBuilder builder, BlockProperty variable)
        {
            var propInfo = PropertyInfo.Extract(variable.Type, variable.Name, variable.Attributes);
            if(propInfo != null && propInfo.MaterialPropertyDeclarations != null)
            {
                foreach (var matPropInfo in propInfo.MaterialPropertyDeclarations)
                    matPropInfo.Declare(builder);
            }
        }
    }
}
