using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class FlipbookNode : IStandardNode
    {
        static string Name = "Flipbook";
        static int Version = 1;
        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            new FunctionDescriptor[] {
                new(
                    1,
                    "Flip",
@"
{
    Tile = fmod(Tile, Width * Height);
    tileCount = float2(1.0, 1.0) / float2(Width, Height);
    tileXY.x = InvertY * Height - (floor(Tile * tileCount.x) + InvertY * 1);
    tileXY.y = InvertX * Width - ((Tile - Width * floor(Tile * tileCount.x)) + InvertX * 1);
    Out = (UV + abs(tileXY)) * tileCount;
}",
                    new ParameterDescriptor("UV", TYPE.Vec2, Usage.In),
                    new ParameterDescriptor("Width", TYPE.Float, Usage.In, new float[] { 1.0f}),
                    new ParameterDescriptor("Height", TYPE.Float, Usage.In, new float[] { 1.0f}),
                    new ParameterDescriptor("Tile", TYPE.Float, Usage.In),
                    new ParameterDescriptor("InvertX", TYPE.Bool, Usage.Static, new float[] { 0.0f}),
                    new ParameterDescriptor("InvertY", TYPE.Bool, Usage.Static, new float[] { 1.0f}),
                    new ParameterDescriptor("tileCount", TYPE.Vec2, Usage.Local),
                    new ParameterDescriptor("tileXY", TYPE.Vec2, Usage.Local),
                    new ParameterDescriptor("Out", TYPE.Vec2, Usage.Out)
                ),
                new(
                    1,
                    "Blend",
@"
{
    Blend = frac(Tile);
    tileCount.x = 1.0 / Width;
	tileCount.y = 1.0 / Height;
	IWidth = InvertX * Width;
	IHeight = InvertY * Height;
	
	Tile1 = fmod(Tile, Width * Height);
    tileXY.x = InvertY * Height - (floor(Tile1 * tileCount.x) + InvertY);
    tileXY.y = IHeight - ((Tile1 - Width * floor(Tile1 * tileCount.x)) + InvertX);
    UV0 = (UV + abs(tileXY)) * tileCount;
	
	Tile += 1;
	Tile2 = fmod(Tile, Width * Height);
    tileXY.x = IHeight - (floor(Tile2 * tileCount.x) + InvertY);
    tileXY.y = IWidth - ((Tile2 - Width * floor(Tile2 * tileCount.x)) + InvertX);
    UV1 = (UV + abs(tileXY)) * tileCount;
}",
                    new ParameterDescriptor("UV", TYPE.Vec2, Usage.In),
                    new ParameterDescriptor("Width", TYPE.Float, Usage.In, new float[] { 1.0f}),
                    new ParameterDescriptor("Height", TYPE.Float, Usage.In, new float[] { 1.0f}),
                    new ParameterDescriptor("IWidth", TYPE.Float, Usage.Local),
                    new ParameterDescriptor("IHeight", TYPE.Float, Usage.Local),
                    new ParameterDescriptor("Tile", TYPE.Float, Usage.In),
                    new ParameterDescriptor("Tile1", TYPE.Float, Usage.Local),
                    new ParameterDescriptor("Tile2", TYPE.Float, Usage.Local),
                    new ParameterDescriptor("InvertX", TYPE.Bool, Usage.Static, new float[] { 0.0f}),
                    new ParameterDescriptor("InvertY", TYPE.Bool, Usage.Static, new float[] { 1.0f}),
                    new ParameterDescriptor("tileCount", TYPE.Vec2, Usage.Local),
                    new ParameterDescriptor("tileXY", TYPE.Vec2, Usage.Local),
                    new ParameterDescriptor("UV0", TYPE.Vec2, Usage.Out),
                    new ParameterDescriptor("UV1", TYPE.Vec2, Usage.Out),
                    new ParameterDescriptor("Blend", TYPE.Float, Usage.Out)
                )
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "creates a flipbook, or texture sheet animation",
            categories: new string[1] { "UV" },
            synonyms: new string[2] { "atlas", "animation" },
            selectableFunctions: new()
            {
                { "Flip", "Flip" },
                { "Blend", "Blend" }
            },
            parameters: new ParameterUIDescriptor[7] {
                new ParameterUIDescriptor(
                    name: "UV",
                    tooltip: "the input UV coordinates"
                ),
                new ParameterUIDescriptor(
                    name: "Width",
                    tooltip: "the number of horizontal tiles in the atlas texture"
                ),
                new ParameterUIDescriptor(
                    name: "Height",
                    tooltip: "the number of vertical tiles in the atlas texture"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "UV coordinates for sampling the atlas texture"
                ),
                new ParameterUIDescriptor(
                    name: "UV0",
                    tooltip: "UVs for the first atlas texture sample"
                ),
                new ParameterUIDescriptor(
                    name: "UV1",
                    tooltip: "UVs for the second atlas texture sample"
                ),
                new ParameterUIDescriptor(
                    name: "Blend",
                    tooltip: "the T input of a Lerp node to blend between the 2 atlas samples"
                )
            }
        );
    }
}
