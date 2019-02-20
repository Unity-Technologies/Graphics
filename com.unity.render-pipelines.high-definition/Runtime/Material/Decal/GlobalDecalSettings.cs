using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    // RenderRenderPipelineSettings represent settings that are immutable at runtime.
    // There is a dedicated RenderRenderPipelineSettings for each platform

    [Serializable]
    public struct GlobalDecalSettings
    {
        /// <summary>Default GlobalDecalSettings</summary>
        public static readonly GlobalDecalSettings @default = new GlobalDecalSettings()
        {
            drawDistance = 1000,
            atlasWidth = 4096,
            atlasHeight = 4096
        };

        public int drawDistance;
        public int atlasWidth;
        public int atlasHeight;
        public bool perChannelMask;
    }
}
