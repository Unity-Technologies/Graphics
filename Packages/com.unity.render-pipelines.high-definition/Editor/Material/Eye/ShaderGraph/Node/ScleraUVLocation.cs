using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.ShaderGraph
{
    [SRPFilter(typeof(HDRenderPipeline))]
    [Title("Utility", "High Definition Render Pipeline", "Eye", "ScleraUVLocation")]
    class ScleraUVLocation : CodeFunctionNode
    {
        public ScleraUVLocation()
        {
            name = "Sclera UV Location";
        }

        public override bool hasPreview
        {
            get { return false; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_ScleraUVLocation", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_ScleraUVLocation(
            [Slot(0, Binding.None, 0, 0, 0, 0)] Vector3 PositionOS,
            [Slot(2, Binding.None)] out Vector2 ScleraUV)
        {
            ScleraUV = Vector2.zero;
            return
@"
                {
                    ScleraUV =  PositionOS.xy + $precision2(0.5, 0.5);
                }
                ";
        }
    }
}
