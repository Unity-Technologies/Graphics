using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEditor.ShaderGraph;

namespace UnityEditor.ShaderGraph.Internal
{
    interface ISubShader
    {
        string GetSubshader(AbstractMaterialNode outputNode, ITarget target, GenerationMode mode, List<string> sourceAssetDependencyPaths = null);
    }
}
