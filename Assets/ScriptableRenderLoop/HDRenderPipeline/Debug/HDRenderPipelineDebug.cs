using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable]
    public class GlobalDebugParameters
    {
        public float debugOverlayRatio = 0.33f;
        public bool displayMaterialDebug = false;
        public bool displayRenderingDebug = false;
        public bool displayLightingDebug = false;

        public MaterialDebugParameters materialDebugParameters = new MaterialDebugParameters();
        public LightingDebugParameters lightingDebugParameters = new LightingDebugParameters();
        public RenderingDebugParameters renderingDebugParametrs = new RenderingDebugParameters();
    }


    [Serializable]
    public class MaterialDebugParameters
    {
        public int debugViewMaterial = 0;
    }

    [Serializable]
    public class RenderingDebugParameters
    {
        public bool displayOpaqueObjects = true;
        public bool displayTransparentObjects = true;
        public bool enableDistortion = true;
    }

    public enum ShadowDebugMode
    {
        None,
        VisualizeAtlas,
        VisualizeShadowMap
    }

    public enum LightingDebugMode
    {
        None,
        DiffuseLighting,
        SpecularLighting
    }

    [Serializable]
    public class LightingDebugParameters
    {
        public bool                 enableShadows = true;
        public ShadowDebugMode      shadowDebugMode = ShadowDebugMode.None;
        public uint                 shadowMapIndex = 0;

        public LightingDebugMode    lightingDebugMode = LightingDebugMode.None;
        public bool                 overrideSmoothness = false;
        public float                overrideSmoothnessValue = 1.0f;
        public Color                debugLightingAlbedo = new Color(0.5f, 0.5f, 0.5f);
    }
}
