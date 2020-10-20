using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.ShaderGraph
{
    [SRPFilter(typeof(HDRenderPipeline))]
    [Title("Utility", "High Definition Render Pipeline", "Eye", "CirclePupilAnimation (Preview)")]
    class CirclePupilAnimation : CodeFunctionNode
    {
        public CirclePupilAnimation()
        {
            name = "Circle Pupil Animation (Preview)";
        }

        public override bool hasPreview
        {
            get { return false; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_CirclePupilAnimation", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_CirclePupilAnimation(
            [Slot(0, Binding.None, 0, 0, 0, 0)] Vector2 IrisUV,
            [Slot(1, Binding.None, 0, 0, 0, 0)] Vector1 PupilRadius,
            [Slot(2, Binding.None, 0, 0, 0, 0)] Vector1 PupilAperture,
            [Slot(3, Binding.None, 0, 0, 0, 0)] Vector1 MinimalPupilAperture,
            [Slot(4, Binding.None, 0, 0, 0, 0)] Vector1 MaximalPupilAperture,
            [Slot(5, Binding.None)] out Vector2 AnimatedIrisUV)
        {
            AnimatedIrisUV = Vector2.zero;
            return
                @"
                {
                    // Compute the normalized iris position
                    $precision2 irisUVCentered = (IrisUV - 0.5f) * 2.0f;

                    // Compute the radius of the point inside the eye
                    $precision localIrisRadius = length(irisUVCentered);

                    // First based on the pupil aperture, let's define the new position of the pupil
                    $precision newPupilRadius = PupilAperture > 0.5 ? lerp(PupilRadius, MaximalPupilAperture, (PupilAperture - 0.5) * 2.0) : lerp(MinimalPupilAperture, PupilRadius, PupilAperture * 2.0);

                    // If we are inside the pupil
                    $precision newIrisRadius = localIrisRadius < newPupilRadius ? ((PupilRadius / newPupilRadius) * localIrisRadius) : 1.0 - ((1.0 - PupilRadius) / (1.0 - newPupilRadius)) * (1.0 - localIrisRadius);
                    AnimatedIrisUV = irisUVCentered / localIrisRadius * newIrisRadius;

                    // Convert it back to UV space.
                    AnimatedIrisUV = (AnimatedIrisUV * 0.5 + $precision2(0.5, 0.5));
                }
                ";
        }
    }
}
