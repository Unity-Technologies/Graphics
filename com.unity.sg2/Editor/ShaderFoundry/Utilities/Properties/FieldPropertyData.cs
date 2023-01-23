using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderFoundry
{
    class FieldPropertyData
    {
        internal List<MaterialPropertyDeclarationData> MaterialPropertyDeclarations = new List<MaterialPropertyDeclarationData>();
        internal List<UniformDeclarationData> UniformDeclarations = new List<UniformDeclarationData>();
        internal UniformReadingData UniformReadingData;
    }
}
