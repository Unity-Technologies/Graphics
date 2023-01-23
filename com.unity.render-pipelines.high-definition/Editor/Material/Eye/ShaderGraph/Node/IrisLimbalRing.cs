using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.ShaderGraph
{
    [SRPFilter(typeof(HDRenderPipeline))]
    [Title("Utility", "High Definition Render Pipeline", "Eye", "IrisLimbalRing")]
    class IrisLimbalRing : CodeFunctionNode
    {
        public IrisLimbalRing()
        {
            name = "Iris Limbal Ring";
        }

        public override bool hasPreview
        {
            get { return false; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_IrisLimbalRing", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_IrisLimbalRing(
            [Slot(0, Binding.None, 0, 0, 0, 0)] Vector2 IrisUV,
            [Slot(1, Binding.None, 0, 0, 0, 0)] Vector3 ViewDirectionOS,
            [Slot(2, Binding.None, 0, 0, 0, 0)] Vector1 LimbalRingSize,
            [Slot(3, Binding.None, 0, 0, 0, 0)] Vector1 LimbalRingFade,
            [Slot(4, Binding.None, 0, 0, 0, 0)] Vector1 LimbalRingIntensity,
            [Slot(5, Binding.None)] out Vector1 LimbalRingFactor)
        {
            LimbalRingFactor = new Vector1();
            return
@"
                {
                    $precision NdotV = dot(float3(0.0, 0.0, 1.0), ViewDirectionOS);

                    // Compute the normalized iris position
                    $precision2 irisUVCentered = (IrisUV - 0.5f) * 2.0f;

                    // Compute the radius of the point inside the eye
                    $precision localIrisRadius = length(irisUVCentered);
                    LimbalRingFactor = localIrisRadius > (1.0 - LimbalRingSize) ? lerp(0.1, 1.0, saturate(1.0 - localIrisRadius) / LimbalRingSize) : 1.0;
                    LimbalRingFactor = PositivePow(LimbalRingFactor, LimbalRingIntensity);
                    LimbalRingFactor = lerp(LimbalRingFactor, PositivePow(LimbalRingFactor, LimbalRingFade), 1.0 - NdotV);
                }
                ";
        }
    }
}
