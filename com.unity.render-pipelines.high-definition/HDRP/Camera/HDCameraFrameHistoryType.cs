using UnityEngine.Serialization;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public enum HDCameraFrameHistoryType
    {
        DepthPyramid       = 0,
        ColorPyramid       = 1,
        VolumetricLighting = 2,
        Count              = 3 // Keep this last
    }
}
