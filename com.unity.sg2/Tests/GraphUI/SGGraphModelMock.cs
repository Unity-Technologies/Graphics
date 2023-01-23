using System;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.GraphUI.UnitTests
{
    /// <summary>
    /// This is a mock ShaderGraphModel, intended to be used in unit testing.
    ///
    /// Depending on the use case, the given registry might need some core definitions (like GraphType) to be useful.
    /// See the tool defaults in ShaderGraphRegistry.InitializeDefaults.
    /// </summary>
    class SGGraphModelMock : SGGraphModel
    {
        ShaderGraphRegistry m_Registry;
        internal override ShaderGraphRegistry RegistryInstance => m_Registry;

        internal SGGraphModelMock(ShaderGraphRegistry registry)
        {
            m_Registry = registry;
        }
    }
}
