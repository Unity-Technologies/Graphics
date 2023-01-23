using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.ShaderGraph
{
    [SRPFilter(typeof(HDRenderPipeline))]
    [Title("Utility", "High Definition Render Pipeline", "Eye", "IrisOutOfBoundColorClamp")]
    class IrisOutOfBoundColorClamp : CodeFunctionNode
    {
        public IrisOutOfBoundColorClamp()
        {
            name = "Iris Out Of Bound Color Clamp";
        }

        public override bool hasPreview
        {
            get { return false; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_IrisOutOfBoundColorClamp", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_IrisOutOfBoundColorClamp(
            [Slot(0, Binding.None, 0, 0, 0, 0)] Vector2 IrisUV,
            [Slot(1, Binding.None, 0, 0, 0, 0)] Vector3 IrisColor,
            [Slot(2, Binding.None, 0, 0, 0, 0)] Vector3 ClampColor,
            [Slot(3, Binding.None)] out Vector3 OutputColor)
        {
            OutputColor = Vector3.zero;
            return
@"
                {
                    OutputColor = (IrisUV.x < 0.0 || IrisUV.y < 0.0 || IrisUV.x > 1.0 || IrisUV.y > 1.0) ? ClampColor : IrisColor;
                }
                ";
        }
    }
}
