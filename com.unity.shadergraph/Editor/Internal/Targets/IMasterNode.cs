using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.ShaderGraph.Internal
{
    interface IMasterNode
    {
        void ProcessPreviewMaterial(Material material);
    }
}
