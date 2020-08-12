using System;
using System.ComponentModel;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Rendering.Universal
{
    [MovedFrom("UnityEngine.Rendering.LWRP")] public abstract partial class ScriptableRenderer
    {
        // Deprecated in 10.x
        [Obsolete("cameraDepth has been renamed to cameraDepthTarget. (UnityUpgradable) -> cameraDepthTarget")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public RenderTargetIdentifier cameraDepth
        {
            get => m_CameraDepthTarget;
        }
    }
}
