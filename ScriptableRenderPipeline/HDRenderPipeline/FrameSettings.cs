using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    // The settings here are per frame settings. They can be changed by the renderer based on its need each frame.
    public class LightingSettings
    {
        public float diffuseGlobalDimmer = 1.0f;
        public float specularGlobalDimmer = 1.0f;
    }
}
