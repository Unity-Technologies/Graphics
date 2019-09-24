using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph.Internal
{
    interface IMasterNode
    {
        ConditionalField[] GetConditionalFields(ShaderPass pass);
        void ProcessPreviewMaterial(Material material);
    }
}
