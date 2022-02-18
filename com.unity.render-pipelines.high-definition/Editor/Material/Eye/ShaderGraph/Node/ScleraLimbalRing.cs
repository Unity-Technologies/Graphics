using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.ShaderGraph
{
    [SRPFilter(typeof(HDRenderPipeline))]
    [Title("Utility", "High Definition Render Pipeline", "Eye", "ScleraLimbalRing (Preview)")]
    class ScleraLimbalRing : CodeFunctionNode
    {
        public ScleraLimbalRing()
        {
            name = "Sclera Limbal Ring (Preview)";
        }

        public override bool hasPreview
        {
            get { return false; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_ScleraLimbalRing", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_ScleraLimbalRing(
            [Slot(0, Binding.None, 0, 0, 0, 0)] Vector3 PositionOS,
            [Slot(1, Binding.None, 0, 0, 0, 0)] Vector3 ViewDirectionOS,
            [Slot(2, Binding.None, 0, 0, 0, 0)] Vector1 IrisRadius,
            [Slot(3, Binding.None, 0, 0, 0, 0)] Vector1 LimbalRingSize,
            [Slot(4, Binding.None, 0, 0, 0, 0)] Vector1 LimbalRingFade,
            [Slot(5, Binding.None, 0, 0, 0, 0)] Vector1 LimbalRingIntensity,
            [Slot(6, Binding.None)] out Vector1 LimbalRingFactor)
        {
            LimbalRingFactor = new Vector1();
            return
@"
                {
                    $precision NdotV = dot($precision3(0.0, 0.0, 1.0), ViewDirectionOS);
                    // Compute the radius of the point inside the eye
                    $precision scleraRadius = length(PositionOS.xy);
                    LimbalRingFactor = scleraRadius > IrisRadius ? (scleraRadius > (LimbalRingSize + IrisRadius) ? 1.0 : lerp(0.5, 1.0, (scleraRadius - IrisRadius) / (LimbalRingSize))) : 1.0;
                    LimbalRingFactor = PositivePow(LimbalRingFactor, LimbalRingIntensity);
                    LimbalRingFactor = lerp(LimbalRingFactor, PositivePow(LimbalRingFactor, LimbalRingFade), 1.0 - NdotV);
                }
                ";
        }
    }
}
