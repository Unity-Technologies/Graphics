using UnityEditor.ShaderGraph;

namespace UnityEditor.ShaderGraph
{
    // Currently most of Shader Graph relies on AbstractMaterialNode as an abstraction, so it's a bit of a mouthful to
    // remove it just like that. Therefore we have this class that represents an IShaderNode as a AbstractMaterialNode.
    class ProxyShaderNode : AbstractMaterialNode
    {
        IShaderNode m_ShaderNode;
    }
}
