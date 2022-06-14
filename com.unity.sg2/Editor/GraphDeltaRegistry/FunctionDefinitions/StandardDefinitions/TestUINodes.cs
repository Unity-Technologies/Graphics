using System;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class TestUIReferrablesNode : IStandardNode
    {
        static string Name = "TestUIReferrablesNode";
        static int Version = 1;
        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            mainFunction: Name,
            new FunctionDescriptor[] {
                new(
                    Name,
                    "Out = UV;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor(
                            name: "UV",
                            type: TYPE.Vec2,
                            usage: GraphType.Usage.Static,
                            defaultValue: REF.WorldSpace_Normal
                        ),
                        new ParameterDescriptor(
                            name: "Out",
                            type: TYPE.Vec2,
                            usage: GraphType.Usage.Out
                        )
                    }
                )
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: String.Empty,
            categories: new string[] { "Test" },
            synonyms: Array.Empty<string>(),
            displayName: "Test Referrables Node",
            hasPreview: false,
            parameters: new ParameterUIDescriptor[]
            {
                new (
                    name: "UV",
                    options: REF.OptionList.Normals
                ),
                new (
                    name: "Out"
                )
            }
        );
    }

    internal class TestNodeWithInclude : IStandardNode
    {
        static readonly string Name = "TestIncludeNode";
        static readonly int Version = 1;
        public static NodeDescriptor NodeDescriptor => new(
            name: Name,
            version: Version,
            functions: new FunctionDescriptor[] {
                new(
                    name: "TestIncludeFunction",
                    includes: new string[]
                    {
                        "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"
                    },
                    body: "Out = In;",
                    parameters: new ParameterDescriptor[] {
                        new ParameterDescriptor(
                            name: "In",
                            type: TYPE.Vec2,
                            usage: GraphType.Usage.Static,
                            defaultValue: REF.WorldSpace_Normal
                        ),
                        new ParameterDescriptor(
                            name: "Out",
                            type: TYPE.Vec2,
                            usage: GraphType.Usage.Out
                        )
                    }
                )
            },
            mainFunction: "TestIncludeFunction"
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            name: Name,
            version: Version,
            tooltip: String.Empty,
            categories: new string[] { "Test" },
            synonyms: Array.Empty<string>(),
            displayName: "Test Node with Include",
            hasPreview: false,
            parameters: new ParameterUIDescriptor[]
            {
                new (
                    name: "In",
                    options: REF.OptionList.Normals
                ),
                new (
                    name: "Out"
                )
            }
        );
    }

    internal class TestUINodeWithDefault : IStandardNode
    {
        static readonly string Name = "TestUINodeWithDefault";

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
            "Out = In;",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor(
                    "Out",
                    TYPE.Vec2,
                    GraphType.Usage.Out
                ),
                new ParameterDescriptor(
                    "In",
                    TYPE.Vec2,
                    GraphType.Usage.In,
                    Vector2.zero
                )
            }
        );
    }

    internal class TestMultiFunctionNode : IStandardNode
    {
        static readonly string Name = "TestMultiFuctionNode";
        static readonly int Version = 1;
        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            new FunctionDescriptor[] {
                new(
                    "Function1",
                    "Local = In + Static; Out = Local;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("In", TYPE.Int, GraphType.Usage.In),
                        new ParameterDescriptor("Static", TYPE.Int, GraphType.Usage.Static),
                        new ParameterDescriptor("Local", TYPE.Int, GraphType.Usage.Local),
                        new ParameterDescriptor("Out", TYPE.Int, GraphType.Usage.Out)
                    }
                ),
                new(
                    "Function2",
                    "B = A;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("A", TYPE.Int, GraphType.Usage.In),
                        new ParameterDescriptor("B", TYPE.Int, GraphType.Usage.Out)
                    }
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
        static readonly string Name = "TestUITexture2DNode";

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
            "Out = SAMPLE_TEXTURE2D(Texture.tex, OverrideSampler.samplerstate, Texture.GetTransformedUV(UV));",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("Texture", TYPE.Texture2D, GraphType.Usage.In),
                new ParameterDescriptor("UV", TYPE.Vec2, GraphType.Usage.In),
                new ParameterDescriptor("OverrideSampler", TYPE.SamplerState, GraphType.Usage.In),
                new ParameterDescriptor("Out", TYPE.Vec4, GraphType.Usage.Out)
            }
        );
    }

    internal class TestUITexture2DStaticNode : IStandardNode
    {
        static readonly string Name = "TestUITexture2DSaticNode";

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
            "Out = Texture;",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("Texture", TYPE.Texture2D, GraphType.Usage.Static),
                new ParameterDescriptor("Out", TYPE.Texture2D, GraphType.Usage.Out)
            }
        );
    }

    internal class TestUITexture3DNode : IStandardNode
    {
        static readonly string Name = "TestUITexture3DNode";

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
            "Out = SAMPLE_TEXTURE3D(Texture.tex, OverrideSampler.samplerstate, UVW);",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("Texture", TYPE.Texture3D, GraphType.Usage.In),
                new ParameterDescriptor("UVW", TYPE.Vec3, GraphType.Usage.In),
                new ParameterDescriptor("OverrideSampler", TYPE.SamplerState, GraphType.Usage.In),
                new ParameterDescriptor("Out", TYPE.Vec4, GraphType.Usage.Out)
            }
        );
    }

    internal class TestUIMat3Node : IStandardNode
    {
        static readonly string Name = "TestUIMat3";
        static readonly int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name, // Name
            "Out = float4(In[0][0], In[1][1], In[2][2], 1.0);",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("In", TYPE.Mat3, GraphType.Usage.Static),
                new ParameterDescriptor("Out", TYPE.Vec4, GraphType.Usage.Out)
            }
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
        static readonly string Name = "TestUIMat4";
        static readonly int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name, // Name
            "Out = float4(In[0][0], In[1][1], In[2][2], In[3][3]);",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("In", TYPE.Mat4, GraphType.Usage.Static),
                new ParameterDescriptor("Out", TYPE.Vec4, GraphType.Usage.Out)
            }
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
        static readonly string Name = "TestUIInt";
        static readonly int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name, // Name
            "Local = In + Static; Out = Local;",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("In", TYPE.Int, GraphType.Usage.In),
                new ParameterDescriptor("Static", TYPE.Int, GraphType.Usage.Static),
                new ParameterDescriptor("Local", TYPE.Int, GraphType.Usage.Local),
                new ParameterDescriptor("Out", TYPE.Int, GraphType.Usage.Out)
            }
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
        static readonly string Name = "TestUIFloat";
        static readonly int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name, // Name
            "Local = In + Static; Out = Local;",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("In", TYPE.Float, GraphType.Usage.In),
                new ParameterDescriptor("Static", TYPE.Float, GraphType.Usage.Static),
                new ParameterDescriptor("Local", TYPE.Float, GraphType.Usage.Local),
                new ParameterDescriptor("Out", TYPE.Float, GraphType.Usage.Out)
            }
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
        static readonly string Name = "TestUIBool";
        static readonly int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name, // Name
            "Local = In || Static; Out = Local;",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("In", TYPE.Bool, GraphType.Usage.In),
                new ParameterDescriptor("Static", TYPE.Bool, GraphType.Usage.Static),
                new ParameterDescriptor("Local", TYPE.Bool, GraphType.Usage.Local),
                new ParameterDescriptor("Out", TYPE.Bool, GraphType.Usage.Out)
            }
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
        static readonly string Name = "TestUIVec2";
        static readonly int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name, // Name
            "Local = In + Static; Out = Local;",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("In", TYPE.Vec2, GraphType.Usage.In),
                new ParameterDescriptor("Static", TYPE.Vec2, GraphType.Usage.Static),
                new ParameterDescriptor("Local", TYPE.Vec2, GraphType.Usage.Local),
                new ParameterDescriptor("Out", TYPE.Vec2, GraphType.Usage.Out)
            }
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
        static readonly string Name = "TestUIVec3";
        static readonly int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name, // Name
            "Local = In + Static; Out = Local;",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("In", TYPE.Vec3, GraphType.Usage.In),
                new ParameterDescriptor("Static", TYPE.Vec3, GraphType.Usage.Static),
                new ParameterDescriptor("Local", TYPE.Vec3, GraphType.Usage.Local),
                new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
            }
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
        static readonly string Name = "TestUIVec4";
        static readonly int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name, // Name
            "Local = In + Static; Out = Local;",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("In", TYPE.Vec4, GraphType.Usage.In),
                new ParameterDescriptor("Static", TYPE.Vec4, GraphType.Usage.Static),
                new ParameterDescriptor("Local", TYPE.Vec4, GraphType.Usage.Local),
                new ParameterDescriptor("Out", TYPE.Vec4, GraphType.Usage.Out)
            }
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
            "TestUIColorRGB", // Name
            "Local = In + Static; Out = Local;",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("In", TYPE.Vec3, GraphType.Usage.In),
                new ParameterDescriptor("Static", TYPE.Vec3, GraphType.Usage.Static),
                new ParameterDescriptor("Local", TYPE.Vec3, GraphType.Usage.Local),
                new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: String.Empty,
            categories: new string [] { "Test" },
            synonyms: Array.Empty<string>(),
            hasPreview: true,
            parameters: new ParameterUIDescriptor[] {
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
            "TestUIColorRGBA", // Name
            "Local = In + Static; Out = Local;",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("In", TYPE.Vec4, GraphType.Usage.In),
                new ParameterDescriptor("Static", TYPE.Vec4, GraphType.Usage.Static),
                new ParameterDescriptor("Local", TYPE.Vec4, GraphType.Usage.Local),
                new ParameterDescriptor("Out", TYPE.Vec4, GraphType.Usage.Out)
            }
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
            "TestUISlider", // Name
            "Out = In;",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("In", TYPE.Float, GraphType.Usage.Static),
                new ParameterDescriptor("Out", TYPE.Float, GraphType.Usage.Out)
            }
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
            Name,
            "Out = StaticInspectorIn;",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("PortIn", TYPE.Float, GraphType.Usage.In),
                new ParameterDescriptor("StaticBodyIn", TYPE.Float, GraphType.Usage.Static),
                new ParameterDescriptor("StaticInspectorIn", TYPE.Float, GraphType.Usage.Static),
                new ParameterDescriptor("Out", TYPE.Float, GraphType.Usage.Out)
            }
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

    internal class TestUpgrade_V1 : IStandardNode
    {
        public static string Name = "TestUpgrade";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
            "Out = In;",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("In", TYPE.Float, GraphType.Usage.In),
                new ParameterDescriptor("Out", TYPE.Float, GraphType.Usage.Out)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name + "(V1)",
            tooltip: string.Empty,
            categories: new[] { "Test", "Upgradeable", "V1" },
            synonyms: Array.Empty<string>()
        );
    }

    internal class TestUpgrade_V2 : IStandardNode
    {
        public static string Name = "TestUpgrade";
        public static int Version = 2;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
            "Out = In + In2;",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("In", TYPE.Float, GraphType.Usage.In),
                new ParameterDescriptor("In2", TYPE.Float, GraphType.Usage.In),
                new ParameterDescriptor("Out", TYPE.Float, GraphType.Usage.Out)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name + "(V2)",
            tooltip: string.Empty,
            categories: new[] { "Test", "Upgradeable", "V2" },
            synonyms: Array.Empty<string>()
        );
    }
}
