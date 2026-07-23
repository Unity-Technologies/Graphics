using System;
using UnityEditor;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.Rendering.Universal.ShaderGraph;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    internal static class Property
    {
        public static readonly string SpecularWorkflowMode = "_WorkflowMode";
        public static readonly string SurfaceType = "_Surface";
        public static readonly string BlendMode = "_Blend";
        public static readonly string AlphaClip = "_AlphaClip";
        public static readonly string AlphaToMask = "_AlphaToMask";
        public static readonly string SrcBlend = "_SrcBlend";
        public static readonly string DstBlend = "_DstBlend";
        public static readonly string SrcBlendAlpha = "_SrcBlendAlpha";
        public static readonly string DstBlendAlpha = "_DstBlendAlpha";
        public static readonly string BlendModePreserveSpecular = "_BlendModePreserveSpecular";
        public static readonly string ZWrite = "_ZWrite";
        public static readonly string CullMode = "_Cull";
        public static readonly string CastShadows = "_CastShadows";
        public static readonly string ReceiveShadows = "_ReceiveShadows";
        public static readonly string QueueOffset = "_QueueOffset";
        public static readonly string ScreenSpaceReflections = "_ScreenSpaceReflections";
        public static readonly string ScreenSpaceReflectionsContributeTransparent = "_ScreenSpaceReflectionsContributeTransparent";

        // for ShaderGraph shaders only
        public static readonly string ZTest = "_ZTest";
        public static readonly string ZWriteControl = "_ZWriteControl";
        public static readonly string QueueControl = "_QueueControl";
        public static readonly string AddPrecomputedVelocity = "_AddPrecomputedVelocity";
        public static readonly string XrMotionVectorsPass = "_XRMotionVectorsPass";
        public static readonly string StencilRef = "_StencilRef";
        public static readonly string StencilReadMask = "_StencilReadMask";
        public static readonly string StencilWriteMask = "_StencilWriteMask";
        public static readonly string StencilCompFunc = "_StencilCompFunc";
        public static readonly string StencilPassOp = "_StencilPassOp";
        public static readonly string StencilFailOp = "_StencilFailOp";
        public static readonly string StencilZFailOp = "_StencilZFailOp";
        public static readonly string StencilCompFuncBack = "_StencilCompFuncBack";
        public static readonly string StencilPassOpBack = "_StencilPassOpBack";
        public static readonly string StencilFailOpBack = "_StencilFailOpBack";
        public static readonly string StencilZFailOpBack = "_StencilZFailOpBack";
        // No-op default values bound to passes that are NOT enabled in the SG-time pass mask. These
        // are HideInInspector and locked to: Comp = Always (8), Op = Keep (0). With Comp = Always
        // and all ops = Keep, the stencil block has no visible effect regardless of Ref/ReadMask/WriteMask.
        public static readonly string StencilCompFuncDefault = "_StencilCompFuncDefault";
        public static readonly string StencilPassOpDefault = "_StencilPassOpDefault";
        public static readonly string StencilFailOpDefault = "_StencilFailOpDefault";
        public static readonly string StencilZFailOpDefault = "_StencilZFailOpDefault";
        public static readonly string StencilCompFuncDefaultBack = "_StencilCompFuncDefaultBack";
        public static readonly string StencilPassOpDefaultBack = "_StencilPassOpDefaultBack";
        public static readonly string StencilFailOpDefaultBack = "_StencilFailOpDefaultBack";
        public static readonly string StencilZFailOpDefaultBack = "_StencilZFailOpDefaultBack";
        public static readonly string WritesColor = "_WritesColor";

        // Hidden marker: set when the SG-baked stencil mask includes ShadowPass. Material UI reads it
        // to warn when shadowmap stencil isn't enabled on the URP renderer.
        public static readonly string StencilUsesShadowPass = "_StencilUsesShadowPass";

        // Hidden marker: set when the SG-baked stencil mask includes the depth prepass. Material UI
        // reads it to keep the DepthOnly pass enabled even when the material writes no depth.
        public static readonly string StencilUsesPrepass = "_StencilUsesPrepass";

        // Global Illumination requires some properties to be named specifically:
        public static readonly string EmissionMap = "_EmissionMap";
        public static readonly string EmissionColor = "_EmissionColor";
    }
}
