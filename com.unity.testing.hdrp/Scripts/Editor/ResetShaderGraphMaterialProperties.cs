using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.ShaderGraph;
using System;
using System.Reflection;
using UnityEditor.Rendering.HighDefinition;

public class ResetMaterialProperties : MonoBehaviour
{
    static readonly string[] floatPropertiesToReset = {
        "_StencilRef", "_StencilWriteMask",
        "_StencilRefDepth", "_StencilWriteMaskDepth",
        "_StencilRefMV", "_StencilWriteMaskMV",
        "_StencilRefDistortionVec", "_StencilWriteMaskDistortionVec",
        "_StencilWriteMaskGBuffer", "_StencilRefGBuffer", "_ZTestGBuffer",
        "_SurfaceType", "_BlendMode", "_SrcBlend", "_DstBlend", "_AlphaSrcBlend", "_AlphaDstBlend",
        "_ZWrite", "_TransparentZWrite", "_CullMode", "_CullModeForward", "_TransparentCullMode",
        "_ZTestDepthEqualForOpaque", "_ZTestDepthEqualForOpaque",
        "_AlphaCutoffEnable",
        "_TransparentSortPriority", "_UseShadowThreshold",
        "_DoubleSidedEnable", "_DoubleSidedNormalMode",
        "_TransparentBackfaceEnable", "_ReceivesSSR", "_RequireSplitLighting"
    };

    static readonly string[] vectorPropertiesToReset = {
        "_DoubleSidedConstants",
    };

    [MenuItem("Edit/Render Pipeline/Reset All ShaderGraph Material Properties %g")]
    static void ResetShaderGraphMaterialProperties()
    {
        var materials = Resources.FindObjectsOfTypeAll< Material >();

        foreach (var mat in materials)
        {
            Type graphUtilType = Type.GetType("UnityEditor.ShaderGraph.GraphUtil, Unity.ShaderGraph.Editor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
            var isShaderGraph = graphUtilType.GetMethod("IsShaderGraph", BindingFlags.Public | BindingFlags.Static);
            if ((bool)isShaderGraph.Invoke(null, new []{mat.shader}))
            {
                var defaultProperties = new Material(mat.shader);

                foreach (var floatToReset in floatPropertiesToReset)
                    if (mat.HasProperty(floatToReset))
                        mat.SetFloat(floatToReset, defaultProperties.GetFloat(floatToReset));
                foreach (var vectorToReset in vectorPropertiesToReset)
                    if (mat.HasProperty(vectorToReset))
                        mat.SetVector(vectorToReset, defaultProperties.GetVector(vectorToReset));

                HDShaderUtils.ResetMaterialKeywords(mat);

                mat.renderQueue = mat.shader.renderQueue;

                defaultProperties = null;
            }
        }
    }
}
