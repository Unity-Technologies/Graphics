using System.Collections.Generic;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderFoundry
{
    internal class UniformDeclarationContext
    {
        internal ShaderBuilder PerMaterialBuilder;
        internal ShaderBuilder GlobalBuilder;
    }

    internal static class UniformDeclaration
    {
        internal static void Copy(ShaderFunction.Builder builder, VariableLinkInstance variable, VariableLinkInstance parent)
        {
            var propInfo = PropertyInfo.Extract(variable.Type, variable.Name, variable.Attributes);
            if (propInfo != null && propInfo.UniformReadingInfo != null)
            {
                propInfo.UniformReadingInfo.Copy(builder, parent);
            }
        }

        internal static void Declare(UniformDeclarationContext context, BlockVariable variable)
        {
            var propInfo = PropertyInfo.Extract(variable.Type, variable.Name, variable.Attributes);
            if (propInfo != null && propInfo.UniformDeclarations != null)
            {
                foreach (var uniformDeclInfo in propInfo.UniformDeclarations)
                    uniformDeclInfo.Declare(context);
            }
        }
    }
}
