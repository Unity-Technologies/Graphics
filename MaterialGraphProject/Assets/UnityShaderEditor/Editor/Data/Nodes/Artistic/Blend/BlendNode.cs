using System.Reflection;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Title("Art", "BlendMode")]
    public class BlendModeNode : CodeFunctionNode
    {
        public BlendModeNode()
        {
            name = "BlendMode";
        }

        string GetCurrentBlendName()
        {
            return System.Enum.GetName(typeof(BlendModesEnum), m_BlendMode);
        }

        [SerializeField]
        BlendModesEnum m_BlendMode;

        [EnumControl("")]
        public BlendModesEnum blendMode
        {
            get { return m_BlendMode; }
            set
            {
                if (m_BlendMode == value)
                    return;

                m_BlendMode = value;
                if (onModified != null)
                {
                    onModified(this, ModificationScope.Graph);
                }
            }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod(string.Format("Unity_Blend{0}", GetCurrentBlendName()),
                BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_BlendBurn(
            [Slot(0, Binding.None)] DynamicDimensionVector top,
            [Slot(1, Binding.None)] DynamicDimensionVector bottom,
            [Slot(2, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
    result =  1.0 - (1.0 - bottom)/top;
}";
        }

        static string Unity_BlendDarken(
            [Slot(0, Binding.None)] DynamicDimensionVector top,
            [Slot(1, Binding.None)] DynamicDimensionVector bottom,
            [Slot(2, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
    result = min(bottom, top);
}";
        }

        static string Unity_BlendDifference(
            [Slot(0, Binding.None)] DynamicDimensionVector top,
            [Slot(1, Binding.None)] DynamicDimensionVector bottom,
            [Slot(2, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
    result = abs(bottom-top);
}";
        }

        static string Unity_BlendDodge(
            [Slot(0, Binding.None)] DynamicDimensionVector top,
            [Slot(1, Binding.None)] DynamicDimensionVector bottom,
            [Slot(2, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
    result = bottom / (1.0 - top);
}";
        }

        static string Unity_BlendDivide(
            [Slot(0, Binding.None)] DynamicDimensionVector top,
            [Slot(1, Binding.None)] DynamicDimensionVector bottom,
            [Slot(2, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
    result = bottom / (top + 0.000000000001);
}";
        }

        static string Unity_BlendExclusion(
            [Slot(0, Binding.None)] DynamicDimensionVector top,
            [Slot(1, Binding.None)] DynamicDimensionVector bottom,
            [Slot(2, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
    result = bottom + top - (2.0 * bottom * top);;
}";
        }

        static string Unity_BlendHardLight(
            [Slot(0, Binding.None)] DynamicDimensionVector top,
            [Slot(1, Binding.None)] DynamicDimensionVector bottom,
            [Slot(2, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
    {precision}{slot2dimension} result1 = 1.0 - 2.0 * (1.0 - top) * (1.0 - bottom);
    {precision}{slot2dimension} result2 = 2.0 * top * bottom;
    {precision}{slot2dimension} zeroOrOne = step(top, 0.5);
    result = result2 * zeroOrOne + (1 - zeroOrOne) * result1;
}";
        }

        static string Unity_BlendHardMix(
            [Slot(0, Binding.None)] DynamicDimensionVector top,
            [Slot(1, Binding.None)] DynamicDimensionVector bottom,
            [Slot(2, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
    result = step(1-top, bottom);
}";
        }

        static string Unity_BlendLighten(
            [Slot(0, Binding.None)] DynamicDimensionVector top,
            [Slot(1, Binding.None)] DynamicDimensionVector bottom,
            [Slot(2, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
    result = max(bottom, top);
}";
        }

        static string Unity_BlendLinearBurn(
            [Slot(0, Binding.None)] DynamicDimensionVector top,
            [Slot(1, Binding.None)] DynamicDimensionVector bottom,
            [Slot(2, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
    result = top + bottom - 1.0;
}";
        }

        static string Unity_BlendLinearDodge(
            [Slot(0, Binding.None)] DynamicDimensionVector top,
            [Slot(1, Binding.None)] DynamicDimensionVector bottom,
            [Slot(2, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
    result = top + bottom;
}";
        }

        static string Unity_BlendLinearLight_AddSub(
            [Slot(0, Binding.None)] DynamicDimensionVector top,
            [Slot(1, Binding.None)] DynamicDimensionVector bottom,
            [Slot(2, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
    result = bottom + 2.0 * top - 1.0;
}";
        }

        static string Unity_BlendMultiply(
            [Slot(0, Binding.None)] DynamicDimensionVector top,
            [Slot(1, Binding.None)] DynamicDimensionVector bottom,
            [Slot(2, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
    result = top * bottom;
}";
        }

        static string Unity_BlendNegation(
            [Slot(0, Binding.None)] DynamicDimensionVector top,
            [Slot(1, Binding.None)] DynamicDimensionVector bottom,
            [Slot(2, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
    result = 1.0 - abs(1.0 - bottom - top);;
}";
        }

        static string Unity_BlendScreen(
            [Slot(0, Binding.None)] DynamicDimensionVector top,
            [Slot(1, Binding.None)] DynamicDimensionVector bottom,
            [Slot(2, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
    result = 1.0 - (1.0-bottom) * (1.0 - top);
}";
        }

        static string Unity_BlendOverlay(
            [Slot(0, Binding.None)] DynamicDimensionVector top,
            [Slot(1, Binding.None)] DynamicDimensionVector bottom,
            [Slot(2, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
    {precision}{slot2dimension} result1 = 1.0 - 2.0 * (1.0 - top) * (1.0 - bottom);
    {precision}{slot2dimension} result2 = 2.0 * top * bottom;
    {precision}{slot2dimension} zeroOrOne = step(bottom, 0.5);
    result = result2 * zeroOrOne + (1 - zeroOrOne) * result1;
}
";
        }

        static string Unity_BlendPinLight(
            [Slot(0, Binding.None)] DynamicDimensionVector top,
            [Slot(1, Binding.None)] DynamicDimensionVector bottom,
            [Slot(2, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
    {precision}{slot2dimension} check = step (0.5, top);
    {precision}{slot2dimension} result1 = check * max(2.0*(top - 0.5), bottom);
    result = result1 + (1.0 - check) * min(2.0 * top, bottom);
}
";
        }

        static string Unity_BlendSoftLight(
            [Slot(0, Binding.None)] DynamicDimensionVector top,
            [Slot(1, Binding.None)] DynamicDimensionVector bottom,
            [Slot(2, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
    {precision}{slot2dimension} result1= 2.0 * bottom * top +bottom*top - 2.0 * bottom*bottom*top;
    {precision}{slot2dimension} result2= 2.0* sqrt(bottom) * top - sqrt(bottom) + 2.0 * bottom - 2.0 * bottom*top;
    {precision}{slot2dimension} zeroOrOne = step(0.5, top);
    result = result2 * zeroOrOne + (1 - zeroOrOne) * result1;
}
";
        }

        static string Unity_BlendVividLight(
            [Slot(0, Binding.None)] DynamicDimensionVector top,
            [Slot(1, Binding.None)] DynamicDimensionVector bottom,
            [Slot(2, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
    {precision}{slot2dimension} result1 = 1.0-(1.0-bottom)/(2.0*top);
    {precision}{slot2dimension} result2 = bottom/(2.0*(1.0-top));
    {precision}{slot2dimension} zeroOrOne = step(0.5, top);
    result = result2 * zeroOrOne + (1 - zeroOrOne) * result1;
}
";
        }

        static string Unity_BlendSubtract(
            [Slot(0, Binding.None)] DynamicDimensionVector top,
            [Slot(1, Binding.None)] DynamicDimensionVector bottom,
            [Slot(2, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
    result = bottom - top;
}
";
        }
    }
}
