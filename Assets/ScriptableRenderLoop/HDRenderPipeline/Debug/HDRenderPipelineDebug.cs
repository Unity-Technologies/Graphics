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

        public void OnValidate()
        {
            lightingDebugParameters.OnValidate();
        }
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

    [GenerateHLSL]
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
        public float                overrideSmoothnessValue = 0.5f;
        public Color                debugLightingAlbedo = new Color(0.5f, 0.5f, 0.5f);

        public void OnValidate()
        {
            overrideSmoothnessValue = Mathf.Clamp(overrideSmoothnessValue, 0.0f, 1.0f);
        }
    }
}
