using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class BlendNode : IStandardNode
    {
        static string Name = "Blend";
        static int Version = 1;
        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            new FunctionDescriptor[] {
                new(
                    1,
                    "Burn",
@"
{
    Out =  1.0 - (1.0 - Blend)/(Base + 0.000000000001);
    if (UseOpacity) Out = lerp(Base, Out, Opacity);
}",
                    new ParameterDescriptor("Base", TYPE.Vector, GraphType.Usage.In),
                    new ParameterDescriptor("Blend", TYPE.Vector, GraphType.Usage.In),
                    new ParameterDescriptor("Opacity", TYPE.Float, GraphType.Usage.In, new float[] { 1.0f }),
                    new ParameterDescriptor("UseOpacity", TYPE.Bool, GraphType.Usage.Static, new float[] { 1.0f }),
                    new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
                ),
                new(
                    1,
                    "Darken",
@"
{
    Out = min(Blend, Base);
    if (UseOpacity) Out = lerp(Base, Out, Opacity);
}",
                    new ParameterDescriptor("Base", TYPE.Vector, GraphType.Usage.In),
                    new ParameterDescriptor("Blend", TYPE.Vector, GraphType.Usage.In),
                    new ParameterDescriptor("Opacity", TYPE.Float, GraphType.Usage.In, new float[] { 1.0f }),
                    new ParameterDescriptor("UseOpacity", TYPE.Bool, GraphType.Usage.Static, new float[] { 1.0f }),
                    new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
                ),
                new(
                    1,
                    "Difference",
@"
{
    Out = abs(Blend - Base);
    if (UseOpacity) Out = lerp(Base, Out, Opacity);
}",
                    new ParameterDescriptor("Base", TYPE.Vector, GraphType.Usage.In),
                    new ParameterDescriptor("Blend", TYPE.Vector, GraphType.Usage.In),
                    new ParameterDescriptor("Opacity", TYPE.Float, GraphType.Usage.In, new float[] { 1.0f }),
                    new ParameterDescriptor("UseOpacity", TYPE.Bool, GraphType.Usage.Static, new float[] { 1.0f }),
                    new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
                ),
                new(
                    1,
                    "Dodge",
@"
{
    Out = Base / (1.0 - clamp(Blend, 0.000001, 0.999999));
    if (UseOpacity) Out = lerp(Base, Out, Opacity);
}",
                    new ParameterDescriptor("Base", TYPE.Vector, GraphType.Usage.In),
                    new ParameterDescriptor("Blend", TYPE.Vector, GraphType.Usage.In),
                    new ParameterDescriptor("Opacity", TYPE.Float, GraphType.Usage.In, new float[] { 1.0f }),
                    new ParameterDescriptor("UseOpacity", TYPE.Bool, GraphType.Usage.Static, new float[] { 1.0f }),
                    new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
                ),
                new(
                    1,
                    "Divide",
@"
{
    Out = Base / (Blend + 0.000000000001);
    if (UseOpacity) Out = lerp(Base, Out, Opacity);
}",
                    new ParameterDescriptor("Base", TYPE.Vector, GraphType.Usage.In),
                    new ParameterDescriptor("Blend", TYPE.Vector, GraphType.Usage.In),
                    new ParameterDescriptor("Opacity", TYPE.Float, GraphType.Usage.In, new float[] { 1.0f }),
                    new ParameterDescriptor("UseOpacity", TYPE.Bool, GraphType.Usage.Static, new float[] { 1.0f }),
                    new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
                ),
                new(
                    1,
                    "Exclusion",
@"
{
    Out = Blend + Base - (2.0 * Blend * Base);
    if (UseOpacity) Out = lerp(Base, Out, Opacity);
}",
                    new ParameterDescriptor("Base", TYPE.Vector, GraphType.Usage.In),
                    new ParameterDescriptor("Blend", TYPE.Vector, GraphType.Usage.In),
                    new ParameterDescriptor("Opacity", TYPE.Float, GraphType.Usage.In, new float[] { 1.0f }),
                    new ParameterDescriptor("UseOpacity", TYPE.Bool, GraphType.Usage.Static, new float[] { 1.0f }),
                    new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
                ),
                new(
                    1,
                    "HardLight",
@"
{
    zeroOrOne = step(Blend, 0.5);
    Out = (2.0 * Base * Blend) * zeroOrOne + (1 - zeroOrOne) * (1.0 - 2.0 * (1.0 - Base) * (1.0 - Blend));
    if (UseOpacity) Out = lerp(Base, Out, Opacity);
}",
                    new ParameterDescriptor("Base", TYPE.Vector, GraphType.Usage.In),
                    new ParameterDescriptor("Blend", TYPE.Vector, GraphType.Usage.In),
                    new ParameterDescriptor("Opacity", TYPE.Float, GraphType.Usage.In, new float[] { 1.0f }),
                    new ParameterDescriptor("UseOpacity", TYPE.Bool, GraphType.Usage.Static, new float[] { 1.0f }),
                    new ParameterDescriptor("zeroOrOne", TYPE.Vector, GraphType.Usage.Local),
                    new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
                ),
                new(
                    1,
                    "HardMix",
@"
{
    Out = step(1 - Base, Blend);
    if (UseOpacity) Out = lerp(Base, Out, Opacity);
}",
                    new ParameterDescriptor("Base", TYPE.Vector, GraphType.Usage.In),
                    new ParameterDescriptor("Blend", TYPE.Vector, GraphType.Usage.In),
                    new ParameterDescriptor("Opacity", TYPE.Float, GraphType.Usage.In, new float[] { 1.0f }),
                    new ParameterDescriptor("UseOpacity", TYPE.Bool, GraphType.Usage.Static, new float[] { 1.0f }),
                    new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
                ),
                new(
                    1,
                    "Lighten",
@"
{
    Out = max(Blend, Base);
    if (UseOpacity) Out = lerp(Base, Out, Opacity);
}",
                    new ParameterDescriptor("Base", TYPE.Vector, GraphType.Usage.In),
                    new ParameterDescriptor("Blend", TYPE.Vector, GraphType.Usage.In),
                    new ParameterDescriptor("Opacity", TYPE.Float, GraphType.Usage.In, new float[] { 1.0f }),
                    new ParameterDescriptor("UseOpacity", TYPE.Bool, GraphType.Usage.Static, new float[] { 1.0f }),
                    new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
                ),
                new(
                    1,
                    "LinearBurn",
@"
{
    Out = Base + Blend - 1.0;
    if (UseOpacity) Out = lerp(Base, Out, Opacity);
}",
                    new ParameterDescriptor("Base", TYPE.Vector, GraphType.Usage.In),
                    new ParameterDescriptor("Blend", TYPE.Vector, GraphType.Usage.In),
                    new ParameterDescriptor("Opacity", TYPE.Float, GraphType.Usage.In, new float[] { 1.0f }),
                    new ParameterDescriptor("UseOpacity", TYPE.Bool, GraphType.Usage.Static, new float[] { 1.0f }),
                    new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
                ),
                new(
                    1,
                    "LinearDodge",
@"
{
    Out = Base + Blend;
    if (UseOpacity) Out = lerp(Base, Out, Opacity);
}",
                    new ParameterDescriptor("Base", TYPE.Vector, GraphType.Usage.In),
                    new ParameterDescriptor("Blend", TYPE.Vector, GraphType.Usage.In),
                    new ParameterDescriptor("Opacity", TYPE.Float, GraphType.Usage.In, new float[] { 1.0f }),
                    new ParameterDescriptor("UseOpacity", TYPE.Bool, GraphType.Usage.Static, new float[] { 1.0f }),
                    new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
                ),
                new(
                    1,
                    "LinearLight",
@"
{
    Out = Blend < 0.5 ? max(Base + (2 * Blend) - 1, 0) : min(Base + 2 * (Blend - 0.5), 1);
    if (UseOpacity) Out = lerp(Base, Out, Opacity);
}",
                    new ParameterDescriptor("Base", TYPE.Vector, GraphType.Usage.In),
                    new ParameterDescriptor("Blend", TYPE.Vector, GraphType.Usage.In),
                    new ParameterDescriptor("Opacity", TYPE.Float, GraphType.Usage.In, new float[] { 1.0f }),
                    new ParameterDescriptor("UseOpacity", TYPE.Bool, GraphType.Usage.Static, new float[] { 1.0f }),
                    new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
                ),
                new(
                    1,
                    "LinearLightAddSub",
@"
{
    Out = Blend + 2.0 * Base - 1.0;
    if (UseOpacity) Out = lerp(Base, Out, Opacity);
}",
                    new ParameterDescriptor("Base", TYPE.Vector, GraphType.Usage.In),
                    new ParameterDescriptor("Blend", TYPE.Vector, GraphType.Usage.In),
                    new ParameterDescriptor("Opacity", TYPE.Float, GraphType.Usage.In, new float[] { 1.0f }),
                    new ParameterDescriptor("UseOpacity", TYPE.Bool, GraphType.Usage.Static, new float[] { 1.0f }),
                    new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
                ),
                new(
                    1,
                    "Multiply",
@"
{
    Out = Base * Blend;
    if (UseOpacity) Out = lerp(Base, Out, Opacity);
}",
                    new ParameterDescriptor("Base", TYPE.Vector, GraphType.Usage.In),
                    new ParameterDescriptor("Blend", TYPE.Vector, GraphType.Usage.In),
                    new ParameterDescriptor("Opacity", TYPE.Float, GraphType.Usage.In, new float[] { 1.0f }),
                    new ParameterDescriptor("UseOpacity", TYPE.Bool, GraphType.Usage.Static, new float[] { 1.0f }),
                    new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
                ),
                new(
                    1,
                    "Negation",
@"
{
    Out = 1.0 - abs(1.0 - Blend - Base);
    if (UseOpacity) Out = lerp(Base, Out, Opacity);
}",
                    new ParameterDescriptor("Base", TYPE.Vector, GraphType.Usage.In),
                    new ParameterDescriptor("Blend", TYPE.Vector, GraphType.Usage.In),
                    new ParameterDescriptor("Opacity", TYPE.Float, GraphType.Usage.In, new float[] { 1.0f }),
                    new ParameterDescriptor("UseOpacity", TYPE.Bool, GraphType.Usage.Static, new float[] { 1.0f }),
                    new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
                ),
                new(
                    1,
                    "Overlay",
@"
{
    zeroOrOne = step(Base, 0.5);
    Out = (2.0 * Base * Blend) * zeroOrOne + (1 - zeroOrOne) * (1.0 - 2.0 * (1.0 - Base) * (1.0 - Blend));
    if (UseOpacity) Out = lerp(Base, Out, Opacity);	
}",
                    new ParameterDescriptor("Base", TYPE.Vector, GraphType.Usage.In),
                    new ParameterDescriptor("Blend", TYPE.Vector, GraphType.Usage.In),
                    new ParameterDescriptor("Opacity", TYPE.Float, GraphType.Usage.In, new float[] { 1.0f }),
                    new ParameterDescriptor("UseOpacity", TYPE.Bool, GraphType.Usage.Static, new float[] { 1.0f }),
                    new ParameterDescriptor("zeroOrOne", TYPE.Vector, GraphType.Usage.Local),
                    new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
                ),
                new(
                    1,
                    "PinLight",
@"
{
    check = step (0.5, Blend);
    Out = (check * max(2.0 * (Base - 0.5), Blend)) + (1.0 - check) * min(2.0 * Base, Blend);
    if (UseOpacity) Out = lerp(Base, Out, Opacity);	
}",
                    new ParameterDescriptor("Base", TYPE.Vector, GraphType.Usage.In),
                    new ParameterDescriptor("Blend", TYPE.Vector, GraphType.Usage.In),
                    new ParameterDescriptor("Opacity", TYPE.Float, GraphType.Usage.In, new float[] { 1.0f }),
                    new ParameterDescriptor("UseOpacity", TYPE.Bool, GraphType.Usage.Static, new float[] { 1.0f }),
                    new ParameterDescriptor("check", TYPE.Vector, GraphType.Usage.Local),
                    new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
                ),
                new(
                    1,
                    "Screen",
@"
{
    Out = 1.0 - (1.0 - Blend) * (1.0 - Base);
    if (UseOpacity) Out = lerp(Base, Out, Opacity);
}",
                    new ParameterDescriptor("Base", TYPE.Vector, GraphType.Usage.In),
                    new ParameterDescriptor("Blend", TYPE.Vector, GraphType.Usage.In),
                    new ParameterDescriptor("Opacity", TYPE.Float, GraphType.Usage.In, new float[] { 1.0f }),
                    new ParameterDescriptor("UseOpacity", TYPE.Bool, GraphType.Usage.Static, new float[] { 1.0f }),
                    new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
                ),
                new(
                    1,
                    "SoftLight",
@"
{
    zeroOrOne = step(0.5, Blend);
    Out = (sqrt(Base) * (2.0 * Blend - 1.0) + 2.0 * Base * (1.0 - Blend)) * zeroOrOne + (1 - zeroOrOne) * (2.0 * Base * Blend + Base * Base * (1.0 - 2.0 * Blend));
    if (UseOpacity) Out = lerp(Base, Out, Opacity);
}",
                    new ParameterDescriptor("Base", TYPE.Vector, GraphType.Usage.In),
                    new ParameterDescriptor("Blend", TYPE.Vector, GraphType.Usage.In),
                    new ParameterDescriptor("Opacity", TYPE.Float, GraphType.Usage.In, new float[] { 1.0f }),
                    new ParameterDescriptor("UseOpacity", TYPE.Bool, GraphType.Usage.Static, new float[] { 1.0f }),
                    new ParameterDescriptor("zeroOrOne", TYPE.Vector, GraphType.Usage.Local),
                    new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
                ),
                new(
                    1,
                    "Subtract",
@"
{
    Out = Base - Blend;
    if (UseOpacity) Out = lerp(Base, Out, Opacity);
}",
                    new ParameterDescriptor("Base", TYPE.Vector, GraphType.Usage.In),
                    new ParameterDescriptor("Blend", TYPE.Vector, GraphType.Usage.In),
                    new ParameterDescriptor("Opacity", TYPE.Float, GraphType.Usage.In, new float[] { 1.0f }),
                    new ParameterDescriptor("UseOpacity", TYPE.Bool, GraphType.Usage.Static, new float[] { 1.0f }),
                    new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
                ),
                new(
                    1,
                    "VividLight",
@"
{
    Base = clamp(Base, 0.000001, 0.999999);
    zeroOrOne = step(0.5, Base);
    Out = (Blend / (2.0 * (1.0 - Base))) * zeroOrOne + (1 - zeroOrOne) * (1.0 - (1.0 - Blend) / (2.0 * Base));
    if (UseOpacity) Out = lerp(Base, Out, Opacity);
}",
                    new ParameterDescriptor("Base", TYPE.Vector, GraphType.Usage.In),
                    new ParameterDescriptor("Blend", TYPE.Vector, GraphType.Usage.In),
                    new ParameterDescriptor("Opacity", TYPE.Float, GraphType.Usage.In, new float[] { 1.0f }),
                    new ParameterDescriptor("UseOpacity", TYPE.Bool, GraphType.Usage.Static, new float[] { 1.0f }),
                    new ParameterDescriptor("zeroOrOne", TYPE.Vector, GraphType.Usage.Local),
                    new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
                ),
                new(
                    1,
                    "Overwrite",
@"
{
    Out = lerp(Base, Blend, Opacity);
}",
                    new ParameterDescriptor("Base", TYPE.Vector, GraphType.Usage.In),
                    new ParameterDescriptor("Blend", TYPE.Vector, GraphType.Usage.In),
                    new ParameterDescriptor("Opacity", TYPE.Float, GraphType.Usage.In, new float[] { 1.0f }),
                    new ParameterDescriptor("UseOpacity", TYPE.Bool, GraphType.Usage.Static, new float[] { 1.0f }),
                    new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
                )
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "mixes a base and blend color with the selected mode",
            categories: new string[2] { "Artistic", "Blend" },
            synonyms: new string[20] { "burn", "darken", "difference", "dodge", "divide", "exclusion", "hard light", "hard mix", "linear burn", "linear dodge", "linear light", "multiply", "negate", "overlay", "pin light", "screen", "soft light", "subtract", "vivid light", "overwrite" },
            selectableFunctions: new()
            {
                { "Burn", "Burn" },
                { "Darken", "Darken" },
                { "Difference", "Difference" },
                { "Dodge", "Dodge" },
                { "Divide", "Divide" },
                { "Exclusion", "Exclusion" },
                { "HardLight", "Hard Light" },
                { "HardMix", "Hard Mix" },
                { "Lighten", "Lighten" },
                { "LinearBurn", "Linear Burn" },
                { "LinearDodge", "Linear Dodge" },
                { "LinearLight", "Linear Light" },
                { "LinearLightAddSub", "Linear Light Add Sub" },
                { "Multiply", "Multiply" },
                { "Negation", "Negation" },
                { "Overlay", "Overlay" },
                { "PinLight", "Pin Light" },
                { "Screen", "Screen" },
                { "SoftLight", "Soft Light" },
                { "Subtract", "Subtract" },
                { "VividLight", "Vivid Light" },
                { "Overwrite", "Overwrite" }
            },
            parameters: new ParameterUIDescriptor[4] {
                new ParameterUIDescriptor(
                    name: "Base",
                    tooltip: "the base layer value"
                ),
                new ParameterUIDescriptor(
                    name: "Blend",
                    tooltip: "the blend layer value"
                ),
                new ParameterUIDescriptor(
                    name: "Opacity",
                    tooltip: "the amount of contribution of the blend layer"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "base and blend values blended using the selected blend mode"
                )
            }
        );
    }
}
