using System.Reflection;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;

namespace UnityEditor.ShaderGraph
{
    enum ClampType
    {
        Fastest,
        Nicest
    };

    [Title("Procedural", "Shape", "Rectangle")]
    class RectangleNode : CodeFunctionNode
    {
        public RectangleNode()
        {
            name = "Rectangle";
            synonyms = new string[] { "square" };
        }

        [SerializeField]
        private ClampType m_ClampType = ClampType.Fastest;

        [EnumControl("")]
        public ClampType clampType
        {
            get { return m_ClampType; }
            set
            {
                if (m_ClampType == value)
                    return;

                m_ClampType = value;
                Dirty(ModificationScope.Graph);
            }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            switch (clampType)
            {
                case ClampType.Nicest:
                    return GetType().GetMethod("Unity_Rectangle_Nicest", BindingFlags.Static | BindingFlags.NonPublic);
                case ClampType.Fastest:
                default:
                    return GetType().GetMethod("Unity_Rectangle_Fastest", BindingFlags.Static | BindingFlags.NonPublic);
            }
        }

        static string Unity_Rectangle_Fastest(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 0.5f, 0, 0, 0)] Vector1 Width,
            [Slot(2, Binding.None, 0.5f, 0, 0, 0)] Vector1 Height,
            [Slot(3, Binding.None, ShaderStageCapability.Fragment)] out Vector1 Out)
        {
            return
@"
{
    $precision2 d = abs(UV * 2 - 1) - $precision2(Width, Height);
#if defined(SHADER_STAGE_RAY_TRACING)
    d = saturate((1 - saturate(d * 1e7)));
#else
    d = saturate(1 - d / fwidth(d));
#endif
    Out = min(d.x, d.y);
}";
        }

        static string Unity_Rectangle_Nicest(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 0.5f, 0, 0, 0)] Vector1 Width,
            [Slot(2, Binding.None, 0.5f, 0, 0, 0)] Vector1 Height,
            [Slot(3, Binding.None, ShaderStageCapability.Fragment)] out Vector1 Out)
        {
            return
@"
{
    UV = UV * 2.0 - 1.0;
    $precision2 w = $precision2(Width, Height);     // rectangle width/height
#if defined(SHADER_STAGE_RAY_TRACING)
    $precision2 o = saturate(0.5f + 1e7 * (w - abs(UV)));
    o = min(o, 1e7 * w * 2.0f);
#else
    $precision2 f = min(fwidth(UV), 0.5f);
    $precision2 k = 1.0f / f;
    $precision2 o = saturate(0.5f + k * (w - abs(UV)));
    o = min(o, k * w * 2.0f);
#endif
    Out = o.x * o.y;
}";
        }
    }
}
