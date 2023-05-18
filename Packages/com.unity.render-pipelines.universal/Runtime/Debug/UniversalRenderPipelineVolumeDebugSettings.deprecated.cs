using System;

namespace UnityEngine.Rendering.Universal
{
    public partial class UniversalRenderPipelineVolumeDebugSettings
    {
        /// <summary>
        /// Specifies the render pipeline for this volume settings
        /// </summary>
        [Obsolete("This property is obsolete and kept only for not breaking user code. VolumeDebugSettings will use current pipeline when it needs to gather volume component types and paths. #from(23.2)", false)]
        public override Type targetRenderPipeline => typeof(UniversalRenderPipeline);
    }
}
