using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.ShaderGraph
{
    [SRPFilter(typeof(HDRenderPipeline))]
    [Title("Utility", "High Definition Render Pipeline", "Jurassic Park", "DisplacementVector")]
    class DisplacementVector : CodeFunctionNode
    {
        public DisplacementVector()
        {
            name = "Displacement Vector (Preview)";
        }

        public override bool hasPreview
        {
            get { return false; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_DisplacementVector", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_DisplacementVector(
            [Slot(0, Binding.None, 0, 0, 0, 0)] Vector3 PositionOS,
            [Slot(1, Binding.None)] out Vector3 DisplacementVector)
        {
            DisplacementVector = Vector3.zero;
            return
@"
                {
                    float3 currentPositionWS = TransformObjectToWorld(PositionOS);
                    float3 previousPositionWS = TransformPreviousObjectToWorld(PositionOS);
                    DisplacementVector = currentPositionWS - previousPositionWS;
                }
                ";
        }
    }
}
