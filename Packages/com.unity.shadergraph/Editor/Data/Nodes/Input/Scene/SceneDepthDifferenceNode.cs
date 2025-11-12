using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;
using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [FormerName("UnityEditor.Rendering.HighDefinition.HDSceneDepthDifferenceNode")]
    [Title("Input", "Scene", "Scene Depth Difference")]
    sealed class SceneDepthDifferenceNode : CodeFunctionNode, IMayRequireDepthTexture, IMayRequireScreenPosition, IMayRequirePosition
    {
        [SerializeField]
        private DepthSamplingMode m_DepthSamplingMode = DepthSamplingMode.Linear01;

        [EnumControl("Sampling Mode")]
        public DepthSamplingMode depthSamplingMode
        {
            get { return m_DepthSamplingMode; }
            set
            {
                if (m_DepthSamplingMode == value)
                    return;

                m_DepthSamplingMode = value;
                Dirty(ModificationScope.Graph);
            }
        }

        public SceneDepthDifferenceNode()
        {
            name = "Scene Depth Difference";
            synonyms = new string[] { "zbuffer", "zdepth", "difference" };
            UpdateNodeAfterDeserialization();
        }

        public override bool hasPreview { get { return false; } }

        protected override MethodInfo GetFunctionToConvert()
        {
            switch (m_DepthSamplingMode)
            {
                case DepthSamplingMode.Raw:
                    return GetType().GetMethod("Unity_SceneDepthDifference_Raw", BindingFlags.Static | BindingFlags.NonPublic);
                case DepthSamplingMode.Eye:
                    return GetType().GetMethod("Unity_SceneDepthDifference_Eye", BindingFlags.Static | BindingFlags.NonPublic);
                case DepthSamplingMode.Linear01:
                default:
                    return GetType().GetMethod("Unity_SceneDepthDifference_Linear01", BindingFlags.Static | BindingFlags.NonPublic);
            }
        }

        static string Unity_SceneDepthDifference_Linear01(
            [Slot(0, Binding.None, ShaderStageCapability.Fragment)] out Vector1 Out,
            [Slot(1, Binding.ScreenPosition)] Vector2 SceneUV,
            [Slot(2, Binding.ViewSpacePosition)] Vector2 PositionVS)
        {
            return
@"
{
    $precision dist = Remap01(length(PositionVS), _ProjectionParams.y, _ProjectionParams.z);
#if defined(UNITY_REVERSED_Z)
    Out = Linear01Depth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(SceneUV.xy), _ZBufferParams) - dist;
#else
    Out = dist - Linear01Depth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(SceneUV.xy), _ZBufferParams);
#endif
}
";
        }

        static string Unity_SceneDepthDifference_Raw(
            [Slot(0, Binding.None, ShaderStageCapability.Fragment)] out Vector1 Out,
            [Slot(1, Binding.ScreenPosition)] Vector2 SceneUV,
            [Slot(2, Binding.ViewSpacePosition)] Vector3 PositionVS)
        {
            return
@"
{
    $precision deviceDepth = ComputeNormalizedDeviceCoordinatesWithZ(PositionVS, GetWorldToHClipMatrix()).z;
#if defined(UNITY_REVERSED_Z)
    Out = deviceDepth - SHADERGRAPH_SAMPLE_SCENE_DEPTH(SceneUV.xy);
#else
    Out = SHADERGRAPH_SAMPLE_SCENE_DEPTH(SceneUV.xy) - deviceDepth;
#endif
}
";
        }

        static string Unity_SceneDepthDifference_Eye(
            [Slot(0, Binding.None, ShaderStageCapability.Fragment)] out Vector1 Out,
            [Slot(1, Binding.ScreenPosition)] Vector2 SceneUV,
            [Slot(2, Binding.ViewSpacePosition)] Vector3 PositionVS)
        {
            return
@"
{
    if (IsPerspectiveProjection())
    {
#if defined(UNITY_REVERSED_Z)
        Out = LinearEyeDepth(ComputeWorldSpacePosition(SceneUV.xy, SHADERGRAPH_SAMPLE_SCENE_DEPTH(SceneUV.xy), UNITY_MATRIX_I_VP), UNITY_MATRIX_V) - length(PositionVS);
#else
        Out = length(PositionVS) - LinearEyeDepth(ComputeWorldSpacePosition(SceneUV.xy, SHADERGRAPH_SAMPLE_SCENE_DEPTH(SceneUV.xy), UNITY_MATRIX_I_VP), UNITY_MATRIX_V);
#endif
    }
    else
    {
#if defined(UNITY_REVERSED_Z)
        Out = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(SceneUV.xy), _ZBufferParams) - length(PositionVS);
#else
        Out = length(PositionVS) - LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(SceneUV.xy), _ZBufferParams);
#endif
    }
}
";
        }

        bool IMayRequireDepthTexture.RequiresDepthTexture(ShaderStageCapability stageCapability)
        {
            return true;
        }

        bool IMayRequireScreenPosition.RequiresScreenPosition(ShaderStageCapability stageCapability)
        {
            return true;
        }

        NeededCoordinateSpace IMayRequirePosition.RequiresPosition(ShaderStageCapability stageCapability)
        {
            return NeededCoordinateSpace.View;
        }
    }
}
