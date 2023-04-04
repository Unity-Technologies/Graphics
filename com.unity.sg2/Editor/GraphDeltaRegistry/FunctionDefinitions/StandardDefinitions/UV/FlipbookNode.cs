using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class FlipbookNode : IStandardNode
    {
        public static string Name => "Flipbook";
        public static int Version => 1;
        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            functions: new FunctionDescriptor[] {
                new(
                    "FlipFrames",
@"  Tile = floor(fmod(Tile + float(0.00001), Width * Height));
    tileCount = float2(1.0, 1.0) / float2(Width, Height);
    tileXY.x = InvertX * Width - ((Tile - Width * floor(Tile * tileCount.x)) + InvertX);
    tileXY.y = InvertY * Height - (floor(Tile * tileCount.x) + InvertY);
    Out = (UV + abs(tileXY)) * tileCount;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("UV", TYPE.Vec2, Usage.In, REF.UV0),
                        new ParameterDescriptor("Width", TYPE.Float, Usage.In, new float[] { 1.0f}),
                        new ParameterDescriptor("Height", TYPE.Float, Usage.In, new float[] { 1.0f}),
                        new ParameterDescriptor("Tile", TYPE.Float, Usage.In),
                        new ParameterDescriptor("InvertX", TYPE.Bool, Usage.Static, new float[] { 0.0f}),
                        new ParameterDescriptor("InvertY", TYPE.Bool, Usage.Static, new float[] { 1.0f}),
                        new ParameterDescriptor("tileCount", TYPE.Vec2, Usage.Local),
                        new ParameterDescriptor("tileXY", TYPE.Vec2, Usage.Local),
                        new ParameterDescriptor("Out", TYPE.Vec2, Usage.Out)
                    }
                ),
                new(
                    "BlendFrames",
@"  Blend = frac(Tile);
    tileCount.x = 1.0 / Width;
	tileCount.y = 1.0 / Height;
	IWidth = InvertX * Width;
	IHeight = InvertY * Height;

	Tile1 = floor(fmod(Tile + float(0.00001), Width * Height));
    tileXY.x = IWidth - ((Tile1 - Width * floor(Tile1 * tileCount.x)) + InvertX);
    tileXY.y = IHeight - (floor(Tile1 * tileCount.x) + InvertY);
    UV0 = (UV + abs(tileXY)) * tileCount;

	Tile += 1;
	Tile2 = floor(fmod(Tile + float(0.00001), Width * Height));
    tileXY.x = IWidth - ((Tile2 - Width * floor(Tile2 * tileCount.x)) + InvertX);
    tileXY.y = IHeight - (floor(Tile2 * tileCount.x) + InvertY);
    UV1 = (UV + abs(tileXY)) * tileCount;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("UV", TYPE.Vec2, Usage.In, REF.UV0),
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
                    }
                )
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "creates a flipbook, or texture sheet animation",
            category: "UV",
            synonyms: new string[2] { "atlas", "animation" },
            selectableFunctions: new()
            {
                { "FlipFrames", "Flip Frames" },
                { "BlendFrames", "Blend Frames" }
            },
            functionSelectorLabel: "Mode",
            description: "pkg://Documentation~/previews/Flipbook.md",
            parameters: new ParameterUIDescriptor[10] {
                new ParameterUIDescriptor(
                    name: "UV",
                    tooltip: "the input UV coordinates",
                    options: REF.OptionList.UVs
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
                    name: "Tile",
                    tooltip: "index of the current tile where 0 is the first and 1 is the last"
                ),
                new ParameterUIDescriptor(
                    name: "InvertX",
                    tooltip: "inverts the horizontal axis of the UVs",
                    displayName: "Invert X"
                ),
                new ParameterUIDescriptor(
                    name: "InvertY",
                    tooltip: "inverts the vertical axis of the UVs",
                    displayName: "Invert Y"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    displayName: string.Empty,
                    tooltip: "UVs for sampling the atlas texture"
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
