using System.Reflection;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Title("Artistic", "Blend", "Blend")]
    public class BlendNode : CodeFunctionNode
    {
        public BlendNode()
        {
            name = "Blend";
        }

        string GetCurrentBlendName()
        {
            return System.Enum.GetName(typeof(BlendMode), m_BlendMode);
        }

        [SerializeField]
        BlendMode m_BlendMode = BlendMode.Overlay;

        [EnumControl("")]
        public BlendMode blendMode
        {
            get { return m_BlendMode; }
            set
            {
                if (m_BlendMode == value)
                    return;

                m_BlendMode = value;
                Dirty(ModificationScope.Graph);
            }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod(string.Format("Unity_Blend_{0}", GetCurrentBlendName()),
                BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Blend_Burn(
            [Slot(0, Binding.None)] DynamicDimensionVector A,
            [Slot(1, Binding.None)] DynamicDimensionVector B,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out =  1.0 - (1.0 - B)/A;
}";
        }

        static string Unity_Blend_Darken(
            [Slot(0, Binding.None)] DynamicDimensionVector A,
            [Slot(1, Binding.None)] DynamicDimensionVector B,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = min(B, A);
}";
        }

        static string Unity_Blend_Difference(
            [Slot(0, Binding.None)] DynamicDimensionVector A,
            [Slot(1, Binding.None)] DynamicDimensionVector B,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = abs(B - A);
}";
        }

        static string Unity_Blend_Dodge(
            [Slot(0, Binding.None)] DynamicDimensionVector A,
            [Slot(1, Binding.None)] DynamicDimensionVector B,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = B / (1.0 - A);
}";
        }

        static string Unity_Blend_Divide(
            [Slot(0, Binding.None)] DynamicDimensionVector A,
            [Slot(1, Binding.None)] DynamicDimensionVector B,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = A / (B + 0.000000000001);
}";
        }

        static string Unity_Blend_Exclusion(
            [Slot(0, Binding.None)] DynamicDimensionVector A,
            [Slot(1, Binding.None)] DynamicDimensionVector B,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = B + A - (2.0 * B * A);
}";
        }

        static string Unity_Blend_HardLight(
            [Slot(0, Binding.None)] DynamicDimensionVector A,
            [Slot(1, Binding.None)] DynamicDimensionVector B,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    {precision}{slot2dimension} result1 = 1.0 - 2.0 * (1.0 - A) * (1.0 - B);
    {precision}{slot2dimension} result2 = 2.0 * A * B;
    {precision}{slot2dimension} zeroOrOne = step(A, 0.5);
    Out = result2 * zeroOrOne + (1 - zeroOrOne) * result1;
}";
        }

        static string Unity_Blend_HardMix(
            [Slot(0, Binding.None)] DynamicDimensionVector A,
            [Slot(1, Binding.None)] DynamicDimensionVector B,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = step(1 - A, B);
}";
        }

        static string Unity_Blend_Lighten(
            [Slot(0, Binding.None)] DynamicDimensionVector A,
            [Slot(1, Binding.None)] DynamicDimensionVector B,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = max(B, A);
}";
        }

        static string Unity_Blend_LinearBurn(
            [Slot(0, Binding.None)] DynamicDimensionVector A,
            [Slot(1, Binding.None)] DynamicDimensionVector B,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = A + B - 1.0;
}";
        }

        static string Unity_Blend_LinearDodge(
            [Slot(0, Binding.None)] DynamicDimensionVector A,
            [Slot(1, Binding.None)] DynamicDimensionVector B,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = A + B;
}";
        }

        static string Unity_Blend_LinearLightAddSub(
            [Slot(0, Binding.None)] DynamicDimensionVector A,
            [Slot(1, Binding.None)] DynamicDimensionVector B,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = B + 2.0 * A - 1.0;
}";
        }

        static string Unity_Blend_Multiply(
            [Slot(0, Binding.None)] DynamicDimensionVector A,
            [Slot(1, Binding.None)] DynamicDimensionVector B,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = A * B;
}";
        }

        static string Unity_Blend_Negation(
            [Slot(0, Binding.None)] DynamicDimensionVector A,
            [Slot(1, Binding.None)] DynamicDimensionVector B,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = 1.0 - abs(1.0 - B - A);
}";
        }

        static string Unity_Blend_Screen(
            [Slot(0, Binding.None)] DynamicDimensionVector A,
            [Slot(1, Binding.None)] DynamicDimensionVector B,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = 1.0 - (1.0 - B) * (1.0 - A);
}";
        }

        static string Unity_Blend_Overlay(
            [Slot(0, Binding.None)] DynamicDimensionVector A,
            [Slot(1, Binding.None)] DynamicDimensionVector B,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    {precision}{slot2dimension} result1 = 1.0 - 2.0 * (1.0 - A) * (1.0 - B);
    {precision}{slot2dimension} result2 = 2.0 * A * B;
    {precision}{slot2dimension} zeroOrOne = step(B, 0.5);
    Out = result2 * zeroOrOne + (1 - zeroOrOne) * result1;
}
";
        }

        static string Unity_Blend_PinLight(
            [Slot(0, Binding.None)] DynamicDimensionVector A,
            [Slot(1, Binding.None)] DynamicDimensionVector B,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    {precision}{slot2dimension} check = step (0.5, A);
    {precision}{slot2dimension} result1 = check * max(2.0 * (A - 0.5), B);
    Out = result1 + (1.0 - check) * min(2.0 * A, B);
}
";
        }

        static string Unity_Blend_SoftLight(
            [Slot(0, Binding.None)] DynamicDimensionVector A,
            [Slot(1, Binding.None)] DynamicDimensionVector B,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    {precision}{slot2dimension} result1 = 2.0 * B * A + B * A - 2.0 * B * B * A;
    {precision}{slot2dimension} result2 = 2.0 * sqrt(B) * A - sqrt(B) + 2.0 * B - 2.0 * B * A;
    {precision}{slot2dimension} zeroOrOne = step(0.5, A);
    Out = result2 * zeroOrOne + (1 - zeroOrOne) * result1;
}
";
        }

        static string Unity_Blend_VividLight(
            [Slot(0, Binding.None)] DynamicDimensionVector A,
            [Slot(1, Binding.None)] DynamicDimensionVector B,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    {precision}{slot2dimension} result1 = 1.0 - (1.0 - B) / (2.0 * A);
    {precision}{slot2dimension} result2 = B / (2.0 * (1.0 - A));
    {precision}{slot2dimension} zeroOrOne = step(0.5, A);
    Out = result2 * zeroOrOne + (1 - zeroOrOne) * result1;
}
";
        }

        static string Unity_Blend_Subtract(
            [Slot(0, Binding.None)] DynamicDimensionVector A,
            [Slot(1, Binding.None)] DynamicDimensionVector B,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = A - B;
}
";
        }
    }
}
