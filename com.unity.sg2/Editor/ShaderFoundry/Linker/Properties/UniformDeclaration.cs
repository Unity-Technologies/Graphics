namespace UnityEditor.ShaderFoundry
{
    internal static class UniformDeclaration
    {
        internal static void Copy(ShaderBuilder builder, VariableLinkInstance variable, VariableLinkInstance parent)
        {
            var propInfo = PropertyDeclarations.Extract(variable.Type, variable.Name, variable.Attributes);
            if (propInfo != null && propInfo.UniformReadingData != null)
            {
                propInfo.UniformReadingData.Copy(builder, parent);
            }
        }
    }
}
