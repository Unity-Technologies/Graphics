using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.ShaderGraph
{
    [SRPFilter(typeof(HDRenderPipeline))]
    [Title("Utility", "High Definition Render Pipeline", "Eye", "IrisOffset")]
    class IrisOffset : CodeFunctionNode
    {
        public IrisOffset()
        {
            name = "Iris Offset";
        }

        public override bool hasPreview
        {
            get { return false; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_IrisOffset", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_IrisOffset(
            [Slot(0, Binding.None, 0, 0, 0, 0)] Vector2 IrisUV,
            [Slot(1, Binding.None, 0, 0, 0, 0)] Vector2 IrisOffset,
            [Slot(2, Binding.None)] out Vector2 DisplacedIrisUV)
        {
            DisplacedIrisUV = Vector3.zero;
            return
@"
                {
                    DisplacedIrisUV = (IrisUV + IrisOffset);
                }
                ";
        }
    }
}
