using System.Collections.Generic;
using System.Text;
using BlockProperty = UnityEditor.ShaderFoundry.BlockVariable;

namespace UnityEditor.ShaderFoundry
{
    internal static class MaterialPropertyDeclaration
    {
        internal static void Declare(ShaderBuilder builder, BlockProperty variable)
        {
            var propInfo = PropertyDeclarations.Extract(variable.Type, variable.Name, variable.Attributes);
            if (propInfo != null && propInfo.MaterialPropertyDeclarations != null)
            {
                foreach (var propDeclaration in propInfo.MaterialPropertyDeclarations)
                    propDeclaration.Declare(builder);
            }
        }
    }
}
