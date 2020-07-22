using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Experimental.Rendering.Universal
{
    internal interface IRenderPass2D
    {
        Renderer2DData rendererData { get; }
    }
}
