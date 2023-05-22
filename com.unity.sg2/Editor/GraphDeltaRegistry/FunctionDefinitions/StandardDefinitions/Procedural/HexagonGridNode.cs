using System;
using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class HexagonGridNode : IStandardNode
    {
        static string Name = "HexagonGrid";
        static int Version = 1;
        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            "One",
            functions: new FunctionDescriptor[] {
                new (
                    "HexGridFunction",
@"   UV *= Scale;
    hexUVs.x = UV.x * 2.0 * rcpsqrtthree; hexUVs.y = UV.y + UV.x * rcpsqrtthree;
    fracHexUVs = frac(hexUVs);
    triangleGrid = step(fracHexUVs.xy,fracHexUVs.yx);
    flooredHexUVs = floor(hexUVs);
    diamondGrid = fmod(flooredHexUVs.x + flooredHexUVs.y, 3.0);
    oneDiamonds = step(1.0,diamondGrid);
    twoDiamonds = step(2.0,diamondGrid);
    Out.y = dot( triangleGrid, 1.0 - fracHexUVs.yx + (fracHexUVs.x + fracHexUVs.y - 1.0) * oneDiamonds  + (fracHexUVs.yx - 2.0 * fracHexUVs.xy) * twoDiamonds );
    Out.x = Out.y > LineWidth;
    Hash_Tchou_2_1_float(floor(flooredHexUVs + oneDiamonds - twoDiamonds * triangleGrid), Out.z);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("UV", TYPE.Vec2, Usage.In, REF.UV0),
                        new ParameterDescriptor("Scale", TYPE.Vec2, Usage.In, new float[] {10.0f, 10.0f}),
                        new ParameterDescriptor("LineWidth", TYPE.Float, Usage.In, new float[] {0.05f}),
                        new ParameterDescriptor("Out", TYPE.Vec3, Usage.Out),
                        new ParameterDescriptor("hexUVs", TYPE.Vec2, Usage.Local),
                        new ParameterDescriptor("rcpsqrtthree", TYPE.Float, Usage.Local, new float[] {0.57735026919f}),
                        new ParameterDescriptor("fracHexUVs", TYPE.Vec2, Usage.Local),
                        new ParameterDescriptor("triangleGrid", TYPE.Vec2, Usage.Local),
                        new ParameterDescriptor("flooredHexUVs", TYPE.Vec2, Usage.Local),
                        new ParameterDescriptor("diamondGrid", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("oneDiamonds", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("twoDiamonds", TYPE.Float, Usage.Local),
                    },
                    new string[]
                    {
                        "\"Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl\""
                    },
                    isHelper: true
                ),
                new(
                    "One",
@"   HexGridFunction(UV, Scale, LineWidth, Out);
    Grid = Out.x;
    EdgeDistance = Out.y;
    TileID = Out.z;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("UV", TYPE.Vec2, Usage.In, REF.UV0),
                        new ParameterDescriptor("Scale", TYPE.Vec2, Usage.In, new float[] {10.0f, 10.0f}),
                        new ParameterDescriptor("LineWidth", TYPE.Float, Usage.In, new float[] {0.05f}),
                        new ParameterDescriptor("Out", TYPE.Vec3, Usage.Local),
                        new ParameterDescriptor("Grid", TYPE.Float, Usage.Out),
                        new ParameterDescriptor("EdgeDistance", TYPE.Float, Usage.Out),
                        new ParameterDescriptor("TileID", TYPE.Float, Usage.Out),
                    }
                ),
                new(
                    "Four",
@"   offset.x = ddx(UV); offset.y = ddy(UV);
    HexGridFunction(UV + (float2(0.0,0.33)*offset), Scale, LineWidth, Out);
    Total += Out;
    HexGridFunction(UV + (float2(-0.33,0.0)*offset), Scale, LineWidth, Out);
    Total += Out;
    HexGridFunction(UV + (float2(0.0,-0.33)*offset), Scale, LineWidth, Out);
    Total += Out;
    HexGridFunction(UV + (float2(0.33, 0.0)*offset), Scale, LineWidth, Out);
    Total += Out;
    Total /= 4;
    Grid = Total.x;
    EdgeDistance = Total.y;
    TileID = Total.z;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("UV", TYPE.Vec2, Usage.In, REF.UV0),
                        new ParameterDescriptor("Scale", TYPE.Vec2, Usage.In, new float[] {10.0f, 10.0f}),
                        new ParameterDescriptor("LineWidth", TYPE.Float, Usage.In, new float[] {0.05f}),
                        new ParameterDescriptor("offset", TYPE.Vec2, Usage.Local),
                        new ParameterDescriptor("Out", TYPE.Vec3, Usage.Local),
                        new ParameterDescriptor("Total", TYPE.Vec3, Usage.Local, new float[] {0.0f}),
                        new ParameterDescriptor("Grid", TYPE.Float, Usage.Out),
                        new ParameterDescriptor("EdgeDistance", TYPE.Float, Usage.Out),
                        new ParameterDescriptor("TileID", TYPE.Float, Usage.Out),
                    }
                ),
                new(
                    "Nine",
@"   offset.x = ddx(UV); offset.y = ddy(UV);
    HexGridFunction(UV + (float2(0.33,0.33)*offset), Scale, LineWidth, Out);
    Total += Out;
    HexGridFunction(UV + (float2(0.0,0.33)*offset), Scale, LineWidth, Out);
    Total += Out;
    HexGridFunction(UV + (float2(-0.33,0.33)*offset), Scale, LineWidth, Out);
    Total += Out;

    HexGridFunction(UV + (float2(0.33,0.0)*offset), Scale, LineWidth, Out);
    Total += Out;
    HexGridFunction(UV + (float2(0.0,0.0)*offset), Scale, LineWidth, Out);
    Total += Out;
    HexGridFunction(UV + (float2(-0.33,0.0)*offset), Scale, LineWidth, Out);
    Total += Out;

    HexGridFunction(UV + (float2(0.33,-0.33)*offset), Scale, LineWidth, Out);
    Total += Out;
    HexGridFunction(UV + (float2(0.0,-0.33)*offset), Scale, LineWidth, Out);
    Total += Out;
    HexGridFunction(UV + (float2(-0.33,-0.33)*offset), Scale, LineWidth, Out);
    Total += Out;

    Total /= 9;
    Grid = Total.x;
    EdgeDistance = Total.y;
    TileID = Total.z;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("UV", TYPE.Vec2, Usage.In, REF.UV0),
                        new ParameterDescriptor("Scale", TYPE.Vec2, Usage.In, new float[] {10.0f, 10.0f}),
                        new ParameterDescriptor("LineWidth", TYPE.Float, Usage.In, new float[] {0.05f}),
                        new ParameterDescriptor("offset", TYPE.Vec2, Usage.Local),
                        new ParameterDescriptor("Out", TYPE.Vec3, Usage.Local),
                        new ParameterDescriptor("Total", TYPE.Vec3, Usage.Local, new float[] {0.0f}),
                        new ParameterDescriptor("Grid", TYPE.Float, Usage.Out),
                        new ParameterDescriptor("EdgeDistance", TYPE.Float, Usage.Out),
                        new ParameterDescriptor("TileID", TYPE.Float, Usage.Out),
                    }
                )
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Hexagon Grid",
            tooltip: "Creates a hexagon grid pattern and supporting data.",
            category: "Procedural",
            synonyms: Array.Empty<string>(),
            description: "pkg://Documentation~/previews/HexagonGrid.md",
            selectableFunctions: new Dictionary<string, string>
            {
                { "One", "One" },
                { "Four", "Four" },
                { "Nine", "Nine" }
            },
            functionSelectorLabel: "Samples",
            parameters: new ParameterUIDescriptor[6] {
                new ParameterUIDescriptor(
                    name: "UV",
                    tooltip: "the input UV",
                    options: REF.OptionList.UVs
                ),
                new ParameterUIDescriptor(
                    name: "Scale",
                    tooltip: "controls the size of the grid"
                ),
                new ParameterUIDescriptor(
                    name: "LineWidth",
                    displayName: "Line Width",
                    tooltip: "controls the thickness of the grid lines"
                ),
                new ParameterUIDescriptor(
                    name: "Grid",
                    tooltip: "a regular grid of hexagon tiles"
                ),
                new ParameterUIDescriptor(
                    name: "EdgeDistance",
                    displayName: "Edge Distance",
                    tooltip: "the distance from the edge to the center of each tile"
                ),
                new ParameterUIDescriptor(
                    name: "TileID",
                    displayName: "Tile ID",
                    tooltip: "a random value for each hexagon tile"
                )
            }
        );
    }
}
