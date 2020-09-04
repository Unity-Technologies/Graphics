using System;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Legacy;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph.Legacy
{
    [FormerName("UnityEditor.Experimental.Rendering.HDPipeline.HDUnlitMasterNode")]
    [FormerName("UnityEditor.Rendering.HighDefinition.HDUnlitMasterNode")]
    class HDUnlitMasterNode1 : AbstractMaterialNode, IMasterNode1
    {
        public enum SurfaceType
        {
            Opaque,
            Transparent
        }

        public enum AlphaMode
        {
            Alpha,
            Premultiply,
            Additive,
        }

        public SurfaceType m_SurfaceType;
        public AlphaMode m_AlphaMode;
        public HDRenderQueue.RenderQueueType m_RenderingPass;
        public bool m_TransparencyFog;
        public bool m_Distortion;
        public DistortionMode m_DistortionMode;
        public bool m_DistortionOnly;
        public bool m_DistortionDepthTest;
        public bool m_AlphaTest;
        public bool m_AlphaToMask;
        public int m_SortPriority;
        public bool m_DoubleSided;
        public bool m_ZWrite;
        public TransparentCullMode m_transparentCullMode;
        public CompareFunction m_ZTest;
        public bool m_AddPrecomputedVelocity;
        public bool m_EnableShadowMatte;
        public bool m_DOTSInstancing;
        public string m_ShaderGUIOverride;
        public bool m_OverrideEnabled;
    }
}
