using System;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Defs
{
    /// <summary>
    /// The TestUIContextConnectionNode tests that the ObjectSpace_Position reference
    /// works as a default edge from the ContextNodes.
    /// ObjectSpace_Position should come from a context configuration and automatically
    /// be available.
    /// To Use:
    /// - Add a TestUIContextConnectionNode to the graph.
    /// - Notice that its preview is a quad of color, rather than the default.
    ///   This indicates that the Position parameter has been connected to a reference value.
    /// </summary>
    internal class TestUIContextConnectionNode : IStandardNode
    {
        public static string Name => "TestUIContextConnectionNode";
        public static int Version => 1;
        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
            "Out = Position;",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out),
                new ParameterDescriptor("Position", TYPE.Vec3, GraphType.Usage.Static, REF.ObjectSpace_Position)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Gets the location of the point in object, view, world, or tangent space.",
            category: "Test",
            synonyms: new string[1] { "location" },
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "Position",
                    options: REF.OptionList.Positions
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "the location of the point in the selected space."
                )
            }
        );
    }

    internal class TestUIReferrablesNode : IStandardNode
    {
        public static string Name => "TestUIReferrablesNode";
        public static int Version => 1;
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
            category: "Test",
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

    internal class TestNodeWithDependentFunction : IStandardNode
    {
        static string Name => "TestDepsNode";
        static int Version => 1;
        public static NodeDescriptor NodeDescriptor => new(
            name: Name,
            version: Version,
            functions: new FunctionDescriptor[] {
                new(
                    name: "TestDepsSecond",
                    body: "Out = In;",
                    parameters: new ParameterDescriptor[] {
                        new ParameterDescriptor(
                            name: "In",
                            type: TYPE.Float,
                            usage: GraphType.Usage.Static,
                            defaultValue: REF.WorldSpace_Normal
                        ),
                        new ParameterDescriptor(
                            name: "Out",
                            type: TYPE.Float,
                            usage: GraphType.Usage.Out
                        )
                    }
                ),
                new(
                    name: "TestDepsFirst",
                    body: "TestDepsSecond(In, Out);",
                    parameters: new ParameterDescriptor[] {
                        new ParameterDescriptor(
                            name: "In",
                            type: TYPE.Float,
                            usage: GraphType.Usage.Static,
                            defaultValue: REF.WorldSpace_Normal
                        ),
                        new ParameterDescriptor(
                            name: "Out",
                            type: TYPE.Float,
                            usage: GraphType.Usage.Out
                        )
                    }
                ),
                new(
                    name: "TestDepsMain",
                    body: "TestDepsFirst(In, Out);",
                    parameters: new ParameterDescriptor[] {
                        new ParameterDescriptor(
                            name: "In",
                            type: TYPE.Float,
                            usage: GraphType.Usage.Static,
                            defaultValue: REF.WorldSpace_Normal
                        ),
                        new ParameterDescriptor(
                            name: "Out",
                            type: TYPE.Float,
                            usage: GraphType.Usage.Out
                        )
                    }
                ),
            },
            mainFunction: "TestDepsMain"
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            name: Name,
            version: Version,
            tooltip: String.Empty,
            category: "Test",
            synonyms: Array.Empty<string>(),
            displayName: "Test Node with Inner Deps",
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

    internal class TestNodeWithInclude : IStandardNode
    {
        static string Name => "TestIncludeNode";
        static int Version => 1;
        public static NodeDescriptor NodeDescriptor => new(
            name: Name,
            version: Version,
            functions: new FunctionDescriptor[] {
                new(
                    name: "TestIncludeFunction",
                    includes: new string[]
                    {
                        // Also test whether we are deduplicating or not.
                        "\"Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl\"",
                        "\"Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl\""
                    },
                    body: "Hash_Tchou_2_1_float(In, Out);",
                    parameters: new ParameterDescriptor[] {
                        new ParameterDescriptor(
                            name: "In",
                            type: TYPE.Vec2,
                            usage: GraphType.Usage.Static,
                            defaultValue: REF.WorldSpace_Normal
                        ),
                        new ParameterDescriptor(
                            name: "Out",
                            type: TYPE.Float,
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
            category: "Test",
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
        static string Name => "TestUINodeWithDefault";

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
        public static string Name => "TestMultiFuctionNode";
        public static int Version => 1;
        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            functions: new FunctionDescriptor[] {
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
            category: "Test",
            synonyms: Array.Empty<string>(),
            displayName: "Test MultiFunction Node",
            hasPreview: false,
            selectableFunctions: new ()
            {
                { "Function1", "In Out Static" },
                { "Function2", "A B" }
            },
            functionSelectorLabel: "Selectable Functions"
        );
    }

    internal class TestUITexture2DNode : IStandardNode
    {
        public static string Name => "TestUITexture2DNode";

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
        public static string Name => "TestUITexture2DSaticNode";

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
        public static string Name => "TestUITexture3DNode";

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
        public static string Name => "TestUIMat3";
        public static int Version => 1;

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
            category: "Test",
            synonyms: Array.Empty<string>(),
            hasPreview: true
        );
    }

    internal class TestUIMat4Node : IStandardNode
    {
        public static string Name => "TestUIMat4";
        public static int Version => 1;

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
            category: "Test",
            synonyms: Array.Empty<string>(),
            hasPreview: true
        );
    }

    internal class TestUIIntNode : IStandardNode
    {
        public static string Name => "TestUIInt";
        public static int Version => 1;

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
            category: "Test",
            synonyms: Array.Empty<string>(),
            hasPreview: true
        );
    }

    internal class TestUIFloatNode : IStandardNode
    {
        public static string Name => "TestUIFloat";
        public static int Version => 1;

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
            category: "Test",
            synonyms: Array.Empty<string>(),
            hasPreview: true
        );
    }

    internal class TestUIBoolNode : IStandardNode
    {
        public static string Name => "TestUIBool";
        public static int Version => 1;

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
            category: "Test",
            synonyms: Array.Empty<string>(),
            hasPreview: true
        );
    }

    internal class TestUIVec2Node : IStandardNode
    {
        public static string Name => "TestUIVec2";
        public static int Version => 1;

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
            category: "Test",
            synonyms: Array.Empty<string>(),
            hasPreview: true
        );
    }

    internal class TestUIVec3Node : IStandardNode
    {
        public static string Name => "TestUIVec3";
        public static int Version => 1;

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
            category: "Test",
            synonyms: Array.Empty<string>(),
            hasPreview: true
        );
    }

    internal class TestUIVec4Node : IStandardNode
    {
        public static string Name => "TestUIVec4";
        public static int Version => 1;

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
            category: "Test",
            synonyms: Array.Empty<string>(),
            hasPreview: true
        );
    }

    internal class TestTruncate : IStandardNode
    {
        static string Name => "TestTruncate";
        static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name, // Name
            "Out = In.xyz;",
            new ParameterDescriptor[] {
                new ParameterDescriptor("In", TYPE.Vec4, GraphType.Usage.In),
                new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: String.Empty,
            category: "Test",
            synonyms: Array.Empty<string>(),
            hasPreview: true
        );
    }

    internal class TestUIColorRGBNode : IStandardNode
    {
        public static string Name => "TestUIColorRGB";
        public static int Version => 1;

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
            category: "Test",
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
        public static string Name => "TestUIColorRGBA";
        public static int Version => 1;

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
            category: "Test",
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

    internal class TestColorHDRNode : IStandardNode
    {
        public static string Name => "TestUIColorHDR";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            "TestUIColorHDR", // Name
            "Out = InHDR;",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("In", TYPE.Vec4, GraphType.Usage.In),
                new ParameterDescriptor("InHDR", TYPE.Vec4, GraphType.Usage.In),
                new ParameterDescriptor("Static", TYPE.Vec4, GraphType.Usage.Static),
                new ParameterDescriptor("StaticHDR", TYPE.Vec4, GraphType.Usage.Static),
                new ParameterDescriptor("Out", TYPE.Vec4, GraphType.Usage.Out)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: String.Empty,
            category: "Test",
            synonyms: Array.Empty<string>(),
            hasPreview: true,
            parameters: new ParameterUIDescriptor[] {
                new ParameterUIDescriptor(
                    name: "In",
                    displayName: "In",
                    tooltip: "Default color picker",
                    useColor: true
                ),
                new ParameterUIDescriptor(
                    name: "InHDR",
                    displayName: "InHDR",
                    tooltip: "HDR color picker",
                    useColor: true,
                    isHdr: true
                ),
                new ParameterUIDescriptor(
                    name: "Static",
                    displayName: "Static",
                    tooltip: "Default color picker",
                    useColor: true
                ),
                new ParameterUIDescriptor(
                    name: "StaticHDR",
                    displayName: "StaticHDR",
                    tooltip: "HDR color picker",
                    useColor: true,
                    isHdr: true
                ),
            }
        );
    }

    internal class TestUISliderNode : IStandardNode
    {
        public static string Name => "TestUISlider";
        public static int Version => 1;

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
            category: "Test",
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
        public static string Name => "TestUIProperties";
        public static int Version => 1;

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
            category: "Test",
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

    internal class TestUpgrade_V2 : IStandardNode
    {
        public static string Name => "TestUpgrade";
        public static int Version => 2;

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
            Name + "(V2)",
            tooltip: string.Empty,
            category: "Test/Upgradeable/V2",
            synonyms: Array.Empty<string>()
        );
    }

    internal class TestUpgrade_V3 : IStandardNode
    {
        public static string Name => "TestUpgrade";
        public static int Version => 3;

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
            Name + "(V3)",
            tooltip: string.Empty,
            category: "Test/Upgradeable/V3",
            synonyms: Array.Empty<string>()
        );
    }
}
