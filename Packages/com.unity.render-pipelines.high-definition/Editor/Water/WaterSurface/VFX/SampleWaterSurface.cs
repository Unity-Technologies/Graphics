using System;
using UnityEngine;
using UnityEditor.VFX;
using UnityEngine.VFX;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [VFXHelpURL("Operator-SampleWaterSurface")]
    [VFXInfo(category = "Sampling")]
    class SampleWaterSurface : VFXOperator
    {
        override public string name { get { return "Sample Water Surface"; } }

        static string m_SampleWaterSurface = "Packages/com.unity.render-pipelines.high-definition/Runtime/Water/Shaders/SampleWaterSurface.hlsl";

        [VFXSetting(VFXSettingAttribute.VisibleFlags.Default)]
        [Tooltip("Target error value at which the algorithm should stop.")]
        public float error = 0.01f;
        [VFXSetting(VFXSettingAttribute.VisibleFlags.Default)]
        [Tooltip(" Number of iterations of the search algorithm.")]
        public int maxIterations = 8;
        [VFXSetting(VFXSettingAttribute.VisibleFlags.Default)]
        [Tooltip("Specifies the nature of the water body that the VFX is sampling.")]
        public WaterSurfaceType surfaceType = WaterSurfaceType.OceanSeaLake;
        [VFXSetting(VFXSettingAttribute.VisibleFlags.Default)]
        [Tooltip("Specifies if the search should evaluate ripples.")]
        protected bool evaluateRipples = false;
        [VFXSetting(VFXSettingAttribute.VisibleFlags.Default)]
        [Tooltip("Specifies if the search should include deformation.")]
        protected bool includeDeformation = false;
        [VFXSetting(VFXSettingAttribute.VisibleFlags.Default)]
        [Tooltip("Specifies if the search should sample the current map.")]
        protected bool includeCurrent = false;

        public class InputProperties
        {
            [Tooltip("Position under which the water surface height should be evaluated.")]
            public Position position;
        }

        public class OutputProperties
        {
            [Tooltip("Returns the position projected on the Water Surface along the up vector of the water surface.")]
            public Position projectedPosition;
            [Tooltip("Returns the height of the position relative to the Water Surface.")]
            public float height;
            [Tooltip("Returns the normal of the Water Surface at the sampled position.")]
            public Vector3 normal;
            [Tooltip("Vector that gives the local current orientation.")]
            public Vector3 currentDirectionWS;
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var baseCode = "\n";
            baseCode += "#define WATER_DISPLACEMENT\n";
            baseCode += "#define IGNORE_WATER_DEFORMATION\n";
            baseCode += "#define IGNORE_HQ_NORMAL_SAMPLE\n";
            baseCode += "#define IGNORE_WATER_FADE\n"; // didn't profile but probably faster

            int bandCount = HDRenderPipeline.EvaluateBandCount(surfaceType, evaluateRipples);
            if (bandCount == 1)
                baseCode += "#define WATER_ONE_BAND\n";
            else if (bandCount == 2)
                baseCode += "#define WATER_TWO_BANDS\n";
            else
                baseCode += "#define WATER_THREE_BANDS\n";

            if (includeDeformation) baseCode += "#define WATER_POST_INCLUDE_DEFORMATION\n";
            if (includeCurrent) baseCode += "#define WATER_LOCAL_CURRENT\n";

            baseCode += $"#include \"{m_SampleWaterSurface}\"\n";

            string FindVerticalDisplacements = $"float error; int steps; float3 normal; float2 current; float height = FindVerticalDisplacement(positionWS, {maxIterations}, {error.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}, steps, error, normal, current);";

            string ProjectPoint = baseCode + "float3 ProjectPoint(float3 positionWS) { " + FindVerticalDisplacements + " positionWS.y -= height; return positionWS; }";
            string EvaluateHeight = baseCode + "float EvaluateHeight(float3 positionWS) { " + FindVerticalDisplacements + " return height; }";
            string EvaluateNormal = baseCode + "float3 EvaluateNormal(float3 positionWS) { " + FindVerticalDisplacements + " return normal; }";
            string EvaluateCurrent = baseCode + "float2 EvaluateCurrent(float3 positionWS) { " + FindVerticalDisplacements + " return current; }";

            VFXExpression outputPosition = new VFXExpressionHLSL("ProjectPoint", ProjectPoint, typeof(Vector3), inputExpression, Array.Empty<string>());
            VFXExpression outputHeight = new VFXExpressionHLSL("EvaluateHeight", EvaluateHeight, typeof(float), inputExpression, Array.Empty<string>());
            VFXExpression outputNormal = new VFXExpressionHLSL("EvaluateNormal", EvaluateNormal, typeof(Vector3), inputExpression, Array.Empty<string>());
            VFXExpression outputCurrent = new VFXExpressionHLSL("EvaluateCurrent", EvaluateCurrent, typeof(Vector3), inputExpression, Array.Empty<string>());
            return new [] { outputPosition, outputHeight, outputNormal, outputCurrent };
        }
    }
}
