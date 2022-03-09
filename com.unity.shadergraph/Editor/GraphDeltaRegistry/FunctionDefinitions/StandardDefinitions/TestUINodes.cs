using System.Collections.Generic;
using UnityEditor.ShaderGraph.Registry.Types;

namespace com.unity.shadergraph.defs
{
    internal class TestUIMat3Node : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1, // Version
            "TestUIMat3", // Name
            "Out = float4(In[0][0], In[1][1], In[2][2], 1.0);",
            new ParameterDescriptor("In", TYPE.Mat3, GraphType.Usage.Static),
            new ParameterDescriptor("Out", TYPE.Vec4, GraphType.Usage.Out)
        );
    }

    internal class TestUIMat4Node : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1, // Version
            "TestUIMat4", // Name
            "Out = float4(In[0][0], In[1][1], In[2][2], In[3][3]);",
            new ParameterDescriptor("In", TYPE.Mat4, GraphType.Usage.Static),
            new ParameterDescriptor("Out", TYPE.Vec4, GraphType.Usage.Out)
        );
    }

    internal class TestUIIntNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1, // Version
            "TestUIInt", // Name
            "Local = In + Static; Out = Local;",
            new ParameterDescriptor("In", TYPE.Int, GraphType.Usage.In),
            new ParameterDescriptor("Static", TYPE.Int, GraphType.Usage.Static),
            new ParameterDescriptor("Local", TYPE.Int, GraphType.Usage.Local),
            new ParameterDescriptor("Out", TYPE.Int, GraphType.Usage.Out)
        );
    }

    internal class TestUIFloatNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1, // Version
            "TestUIFloat", // Name
            "Local = In + Static; Out = Local;",
            new ParameterDescriptor("In", TYPE.Float, GraphType.Usage.In),
            new ParameterDescriptor("Static", TYPE.Float, GraphType.Usage.Static),
            new ParameterDescriptor("Local", TYPE.Float, GraphType.Usage.Local),
            new ParameterDescriptor("Out", TYPE.Float, GraphType.Usage.Out)
        );
    }

    internal class TestUIBoolNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1, // Version
            "TestUIBool", // Name
            "Local = In || Static; Out = Local;",
            new ParameterDescriptor("In", TYPE.Bool, GraphType.Usage.In),
            new ParameterDescriptor("Static", TYPE.Bool, GraphType.Usage.Static),
            new ParameterDescriptor("Local", TYPE.Bool, GraphType.Usage.Local),
            new ParameterDescriptor("Out", TYPE.Bool, GraphType.Usage.Out)
        );
    }

    internal class TestUIVec2Node : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1, // Version
            "TestUIVec2", // Name
            "Local = In + Static; Out = Local;",
            new ParameterDescriptor("In", TYPE.Vec2, GraphType.Usage.In),
            new ParameterDescriptor("Static", TYPE.Vec2, GraphType.Usage.Static),
            new ParameterDescriptor("Local", TYPE.Vec2, GraphType.Usage.Local),
            new ParameterDescriptor("Out", TYPE.Vec2, GraphType.Usage.Out)
        );
    }

    internal class TestUIVec3Node : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1, // Version
            "TestUIVec3", // Name
            "Local = In + Static; Out = Local;",
            new ParameterDescriptor("In", TYPE.Vec3, GraphType.Usage.In),
            new ParameterDescriptor("Static", TYPE.Vec3, GraphType.Usage.Static),
            new ParameterDescriptor("Local", TYPE.Vec3, GraphType.Usage.Local),
            new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
        );
    }

    internal class TestUIVec4Node : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1, // Version
            "TestUIVec4", // Name
            "Local = In + Static; Out = Local;",
            new ParameterDescriptor("In", TYPE.Vec4, GraphType.Usage.In),
            new ParameterDescriptor("Static", TYPE.Vec4, GraphType.Usage.Static),
            new ParameterDescriptor("Local", TYPE.Vec4, GraphType.Usage.Local),
            new ParameterDescriptor("Out", TYPE.Vec4, GraphType.Usage.Out)
        );
    }

    internal class TestUIColorRGBNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1, // Version
            "TestUIColorRGB", // Name
            "Out = In;",
            new ParameterDescriptor("In", TYPE.Vec3, GraphType.Usage.Static),
            new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
        );

        public static Dictionary<string, float> UIHints => new()
        {
            {"In.UseColor", 1} // Use color picker for In (float value is ignored)
        };
    }

    internal class TestUIColorRGBANode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1, // Version
            "TestUIColorRGBA", // Name
            "Out = In;",
            new ParameterDescriptor("In", TYPE.Vec4, GraphType.Usage.Static),
            new ParameterDescriptor("Out", TYPE.Vec4, GraphType.Usage.Out)
        );

        public static Dictionary<string, float> UIHints => new()
        {
            {"In.UseColor", 1} // Use color picker for In (float value is ignored)
        };
    }

    internal class TestUISliderNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1, // Version
            "TestUISlider", // Name
            "Out = In;",
            new ParameterDescriptor("In", TYPE.Float, GraphType.Usage.Static),
            new ParameterDescriptor("Out", TYPE.Float, GraphType.Usage.Out)
        );

        public static Dictionary<string, float> UIHints => new()
        {
            { "In.UseSlider", 1 } // Use a slider for In (float value is ignored)
        };
    }
}
