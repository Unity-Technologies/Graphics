using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [GenerateHLSL]
    public enum MaterialInstanceFlags
    {
        PerPixelDisplacementLockObjectScale = 1,
        DisplacementLockTilingScale = 2
    }
} // namespace UnityEditor
