using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class TestMultiFunctionNode : IStandardNode
    {
        static string Name = "TestMultiFuctionNode";
        static int Version = 1;
        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            new FunctionDescriptor[] {
                new(
                    1,
                    "Function1",
                    "Local = In + Static; Out = Local;",
                    new ParameterDescriptor("In", TYPE.Int, GraphType.Usage.In),
                    new ParameterDescriptor("Static", TYPE.Int, GraphType.Usage.Static),
                    new ParameterDescriptor("Local", TYPE.Int, GraphType.Usage.Local),
                    new ParameterDescriptor("Out", TYPE.Int, GraphType.Usage.Out)
                ),
                new(
                    1,
                    "Function2",
                    "B = A;",
                    new ParameterDescriptor("A", TYPE.Int, GraphType.Usage.In),
                    new ParameterDescriptor("B", TYPE.Int, GraphType.Usage.Out)
                )
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: String.Empty,
            categories: new string [1] { "Test" },
            synonyms: Array.Empty<string>(),
            displayName: "Test MultiFunction Node",
            hasPreview: false,
            selectableFunctions: new ()
            {
                { "Function1", "In Out Static" },
                { "Function2", "A B" }
            }
        );
    }

    internal class TestUITexture2DNode : IStandardNode
    {
        static string Name = "TestUITexture2DNode";
        static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
            "Out = SAMPLE_TEXTURE2D(Texture.tex, OverrideSampler.samplerstate, Texture.GetTransformedUV(UV));",
            new ParameterDescriptor("Texture", TYPE.Texture2D, GraphType.Usage.In),
            new ParameterDescriptor("UV", TYPE.Vec2, GraphType.Usage.In),
            new ParameterDescriptor("OverrideSampler", TYPE.SamplerState, GraphType.Usage.In),
            new ParameterDescriptor("Out", TYPE.Vec4, GraphType.Usage.Out));
    }

    internal class TestUIMat3Node : IStandardNode
    {
        static string Name = "TestUIMat3";
        static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version, // Version
            Name, // Name
            "Out = float4(In[0][0], In[1][1], In[2][2], 1.0);",
            new ParameterDescriptor("In", TYPE.Mat3, GraphType.Usage.Static),
            new ParameterDescriptor("Out", TYPE.Vec4, GraphType.Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: String.Empty,
            categories: new string [1] { "Test" },
            synonyms: Array.Empty<string>(),
            hasPreview: true
        );
    }

    internal class TestUIMat4Node : IStandardNode
    {
        static string Name = "TestUIMat4";
        static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version, // Version
            Name, // Name
            "Out = float4(In[0][0], In[1][1], In[2][2], In[3][3]);",
            new ParameterDescriptor("In", TYPE.Mat4, GraphType.Usage.Static),
            new ParameterDescriptor("Out", TYPE.Vec4, GraphType.Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: String.Empty,
            categories: new string [1] { "Test" },
            synonyms: Array.Empty<string>(),
            hasPreview: true
        );
    }

    internal class TestUIIntNode : IStandardNode
    {
        static string Name = "TestUIInt";
        static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version, // Version
            Name, // Name
            "Local = In + Static; Out = Local;",
            new ParameterDescriptor("In", TYPE.Int, GraphType.Usage.In),
            new ParameterDescriptor("Static", TYPE.Int, GraphType.Usage.Static),
            new ParameterDescriptor("Local", TYPE.Int, GraphType.Usage.Local),
            new ParameterDescriptor("Out", TYPE.Int, GraphType.Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: String.Empty,
            categories: new string [1] { "Test" },
            synonyms: Array.Empty<string>(),
            hasPreview: true
        );
    }

    internal class TestUIFloatNode : IStandardNode
    {
        static string Name = "TestUIFloat";
        static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version, // Version
            Name, // Name
            "Local = In + Static; Out = Local;",
            new ParameterDescriptor("In", TYPE.Float, GraphType.Usage.In),
            new ParameterDescriptor("Static", TYPE.Float, GraphType.Usage.Static),
            new ParameterDescriptor("Local", TYPE.Float, GraphType.Usage.Local),
            new ParameterDescriptor("Out", TYPE.Float, GraphType.Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: String.Empty,
            categories: new string [1] { "Test" },
            synonyms: Array.Empty<string>(),
            hasPreview: true
        );
    }

    internal class TestUIBoolNode : IStandardNode
    {
        static string Name = "TestUIBool";
        static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version, // Version
            Name, // Name
            "Local = In || Static; Out = Local;",
            new ParameterDescriptor("In", TYPE.Bool, GraphType.Usage.In),
            new ParameterDescriptor("Static", TYPE.Bool, GraphType.Usage.Static),
            new ParameterDescriptor("Local", TYPE.Bool, GraphType.Usage.Local),
            new ParameterDescriptor("Out", TYPE.Bool, GraphType.Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: String.Empty,
            categories: new string [1] { "Test" },
            synonyms: Array.Empty<string>(),
            hasPreview: true
        );
    }

    internal class TestUIVec2Node : IStandardNode
    {
        static string Name = "TestUIVec2";
        static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version, // Version
            Name, // Name
            "Local = In + Static; Out = Local;",
            new ParameterDescriptor("In", TYPE.Vec2, GraphType.Usage.In),
            new ParameterDescriptor("Static", TYPE.Vec2, GraphType.Usage.Static),
            new ParameterDescriptor("Local", TYPE.Vec2, GraphType.Usage.Local),
            new ParameterDescriptor("Out", TYPE.Vec2, GraphType.Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: String.Empty,
            categories: new string [1] { "Test" },
            synonyms: Array.Empty<string>(),
            hasPreview: true
        );
    }

    internal class TestUIVec3Node : IStandardNode
    {
        static string Name = "TestUIVec3";
        static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version, // Version
            Name, // Name
            "Local = In + Static; Out = Local;",
            new ParameterDescriptor("In", TYPE.Vec3, GraphType.Usage.In),
            new ParameterDescriptor("Static", TYPE.Vec3, GraphType.Usage.Static),
            new ParameterDescriptor("Local", TYPE.Vec3, GraphType.Usage.Local),
            new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: String.Empty,
            categories: new string [1] { "Test" },
            synonyms: Array.Empty<string>(),
            hasPreview: true
        );
    }

    internal class TestUIVec4Node : IStandardNode
    {
        static string Name = "TestUIVec4";
        static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version, // Version
            Name, // Name
            "Local = In + Static; Out = Local;",
            new ParameterDescriptor("In", TYPE.Vec4, GraphType.Usage.In),
            new ParameterDescriptor("Static", TYPE.Vec4, GraphType.Usage.Static),
            new ParameterDescriptor("Local", TYPE.Vec4, GraphType.Usage.Local),
            new ParameterDescriptor("Out", TYPE.Vec4, GraphType.Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: String.Empty,
            categories: new string [1] { "Test" },
            synonyms: Array.Empty<string>(),
            hasPreview: true
        );
    }

    internal class TestUIColorRGBNode : IStandardNode
    {
        public static string Name = "TestUIColorRGB";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            1, // Version
            "TestUIColorRGB", // Name
            "Local = In + Static; Out = Local;",
            new ParameterDescriptor("In", TYPE.Vec3, GraphType.Usage.In),
            new ParameterDescriptor("Static", TYPE.Vec3, GraphType.Usage.Static),
            new ParameterDescriptor("Local", TYPE.Vec3, GraphType.Usage.Local),
            new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: String.Empty,
            categories: new string [1] { "Test" },
            synonyms: Array.Empty<string>(),
            hasPreview: true,
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "In",
                    displayName: "In",
                    tooltip: "input value",
                    useColor: true,
                    useSlider: false
                ),
                new ParameterUIDescriptor(
                    name: "Static",
                    displayName: "Static",
                    tooltip: String.Empty,
                    useColor: true,
                    useSlider: false
                )
            }
        );
    }

    internal class TestUIColorRGBANode : IStandardNode
    {
        public static string Name = "TestUIColorRGBA";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            1, // Version
            "TestUIColorRGBA", // Name
            "Local = In + Static; Out = Local;",
            new ParameterDescriptor("In", TYPE.Vec4, GraphType.Usage.In),
            new ParameterDescriptor("Static", TYPE.Vec4, GraphType.Usage.Static),
            new ParameterDescriptor("Local", TYPE.Vec4, GraphType.Usage.Local),
            new ParameterDescriptor("Out", TYPE.Vec4, GraphType.Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: String.Empty,
            categories: new string [1] { "Test" },
            synonyms: Array.Empty<string>(),
            hasPreview: true,
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "In",
                    displayName: "In",
                    tooltip: "input value",
                    useColor: true,
                    useSlider: false
                ),
                new ParameterUIDescriptor(
                    name: "Static",
                    displayName: "Static",
                    tooltip: String.Empty,
                    useColor: true,
                    useSlider: false
                )
            }
        );
    }

    internal class TestUISliderNode : IStandardNode
    {
        public static string Name = "TestUISlider";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            1, // Version
            "TestUISlider", // Name
            "Out = In;",
            new ParameterDescriptor("In", TYPE.Float, GraphType.Usage.Static),
            new ParameterDescriptor("Out", TYPE.Float, GraphType.Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: String.Empty,
            categories: new string [1] { "Test" },
            synonyms: Array.Empty<string>(),
            hasPreview: true,
            parameters: new ParameterUIDescriptor[1] {
                new ParameterUIDescriptor(
                    name: "In",
                    displayName: "In",
                    tooltip: "input value",
                    useColor: false,
                    useSlider: true
                )
            }
        );
    }

    internal class TestUIPropertyNode : IStandardNode
    {
        public static string Name = "TestUIProperties";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
            "Out = StaticInspectorIn;",
            new ParameterDescriptor("PortIn", TYPE.Float, GraphType.Usage.In),
            new ParameterDescriptor("StaticBodyIn", TYPE.Float, GraphType.Usage.Static),
            new ParameterDescriptor("StaticInspectorIn", TYPE.Float, GraphType.Usage.Static),
            new ParameterDescriptor("Out", TYPE.Float, GraphType.Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: String.Empty,
            categories: new string [1] { "Test" },
            synonyms: Array.Empty<string>(),
            hasPreview: true,
            parameters: new ParameterUIDescriptor[1] {
                new ParameterUIDescriptor(
                    name: "StaticInspectorIn",
                    displayName: "StaticInspectorIn",
                    tooltip: String.Empty,
                    useColor: false,
                    useSlider: true,
                    inspectorOnly: true
                )
            }
        );
    }
}
