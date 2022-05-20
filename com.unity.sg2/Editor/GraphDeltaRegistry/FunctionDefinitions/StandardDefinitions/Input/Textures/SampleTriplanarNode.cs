using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class SampleTriplanarNode : IStandardNode
    {
        public static string Name = "SampleTriplanar";
        public static int Version = 1;

        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            new FunctionDescriptor[] {
                new(
                    1,
                    "SampleTriplanarDefault",
                    //TODO: need to figure out how to deal with this precision-specific SafePositivePow_float call
@"  Node_UV = Position * Tile;
    Node_Blend = SafePositivePow_float(Normal, min(Blend, floor(log2(Min_float())/log2(1/sqrt(3)))) );
    Node_Blend /= dot(Node_Blend, 1.0);
    Node_X = SAMPLE_TEXTURE2D(Texture.tex, Sampler.samplerstate, Node_UV.zy);
    Node_Y = SAMPLE_TEXTURE2D(Texture.tex, Sampler.samplerstate, Node_UV.xz);
    Node_Z = SAMPLE_TEXTURE2D(Texture.tex, Sampler.samplerstate, Node_UV.xy);
    Y = Node_Y * Node_Blend.y;
    XZ = (Node_X * Node_Blend.x) + (Node_Z * Node_Blend.z);
    XYZ = XZ + Y;",
                    new ParameterDescriptor("Texture", TYPE.Texture2D, Usage.In),
                    new ParameterDescriptor("Sampler", TYPE.SamplerState, Usage.In),
                    new ParameterDescriptor("Position", TYPE.Vec3, Usage.In, REF.WorldSpace_Position),
                    new ParameterDescriptor("Normal", TYPE.Vec3, Usage.In, REF.WorldSpace_Normal),
                    new ParameterDescriptor("Tile", TYPE.Float, Usage.In, new float[] { 1.0f }),
                    new ParameterDescriptor("Blend", TYPE.Float, Usage.In, new float[] { 1.0f }),
                    new ParameterDescriptor("XYZ", TYPE.Vec4, Usage.Out),
                    new ParameterDescriptor("XZ", TYPE.Vec4, Usage.Out),
                    new ParameterDescriptor("Y", TYPE.Vec4, Usage.Out),
                    new ParameterDescriptor("Node_UV", TYPE.Vec3, Usage.Local),
                    new ParameterDescriptor("Node_Blend", TYPE.Vec3, Usage.Local),
                    new ParameterDescriptor("Node_X", TYPE.Vec4, Usage.Local),
                    new ParameterDescriptor("Node_Y", TYPE.Vec4, Usage.Local),
                    new ParameterDescriptor("Node_Z", TYPE.Vec4, Usage.Local)
                ),
                new(
                    1,
                    "SampleTriplanarNormal",
                    //TODO: need to figure out how to deal with this precision-specific SafePositivePow_float call
@"  Node_UV = Position * Tile;
    Node_Blend = SafePositivePow_float(Normal, min(Blend, floor(log2(Min_float())/log2(1/sqrt(3)))) );
    Node_Blend /= dot(Node_Blend, 1.0);
    Node_X = UnpackNormal(SAMPLE_TEXTURE2D(Texture.tex, Sampler.samplerstate, Node_UV.zy));
    Node_Y = UnpackNormal(SAMPLE_TEXTURE2D(Texture.tex, Sampler.samplerstate, Node_UV.xz));
    Node_Z = UnpackNormal(SAMPLE_TEXTURE2D(Texture.tex, Sampler.samplerstate, Node_UV.xy));
    Node_X.xy = Node_X.xy + Normal.zy; Node_X.z = abs(Node_X.z) * Normal.x;
    Node_Y.xy = Node_Y.xy + Normal.xz; Node_Y.z = abs(Node_Y.z) * Normal.y;
    Node_Z.xy = Node_Z.xy + Normal.xy; Node_Y.z = abs(Node_Z.z) * Normal.z;
    Y.xyz = Node_Y.xzy * Node_Blend.y; Y.w = 1;
    XZ.xyz = (Node_X.zyx * Node_Blend.x) + (Node_Z.xyz * Node_Blend.z); XZ.w = 1;
    XYZ.xyz = XZ.xyz + Y.xyz; XYZ.w = 1;
    //transform from world to tangent space
    tangentTransform[0] = WorldSpaceTangent;
    tangentTransform[1] = WorldSpaceBiTangent;
    tangentTransform[2] = Normal;
    XYZ.rgb = TransformWorldToTangent(XYZ.rgb, tangentTransform, true);
    XZ.rgb = TransformWorldToTangent(XZ.rgb, tangentTransform, true);
    Y.rgb = TransformWorldToTangent(Y.rgb, tangentTransform, true);",
                    new ParameterDescriptor("Texture", TYPE.Texture2D, Usage.In),
                    new ParameterDescriptor("Sampler", TYPE.SamplerState, Usage.In),
                    new ParameterDescriptor("Position", TYPE.Vec3, Usage.In, REF.WorldSpace_Position),
                    new ParameterDescriptor("Normal", TYPE.Vec3, Usage.In, REF.WorldSpace_Normal),
                    new ParameterDescriptor("Tile", TYPE.Float, Usage.In, new float[] { 1.0f }),
                    new ParameterDescriptor("Blend", TYPE.Float, Usage.In, new float[] { 1.0f }),
                    new ParameterDescriptor("XYZ", TYPE.Vec4, Usage.Out),
                    new ParameterDescriptor("XZ", TYPE.Vec4, Usage.Out),
                    new ParameterDescriptor("Y", TYPE.Vec4, Usage.Out),
                    new ParameterDescriptor("Node_UV", TYPE.Vec3, Usage.Local),
                    new ParameterDescriptor("Node_Blend", TYPE.Vec3, Usage.Local),
                    new ParameterDescriptor("Node_X", TYPE.Vec3, Usage.Local),
                    new ParameterDescriptor("Node_Y", TYPE.Vec3, Usage.Local),
                    new ParameterDescriptor("Node_Z", TYPE.Vec3, Usage.Local),
                    new ParameterDescriptor("tangentTransform", TYPE.Mat3, Usage.Local),
                    new ParameterDescriptor("WorldSpaceTangent", TYPE.Vec3, Usage.Local, REF.WorldSpace_Tangent),
                    new ParameterDescriptor("WorldSpaceBiTangent", TYPE.Vec3, Usage.Local, REF.WorldSpace_Bitangent)
                ),
                new(
                    1,
                    "SampleTriplanar2Samples",
@"  //Credit Inigo Quilez: https://iquilezles.org/www/articles/biplanar/biplanar.htm
    Position *= Tile;
    Position.yz = -Position.yz;
	// grab coord derivatives for texturing
	dpdx = ddx(Position);
	dpdy = ddy(Position);
	n = abs(Normal);
	// determine major, minor, and median axis (in x; yz are following axis)
	int3 ma = (n.x>n.y && n.x>n.z) ? int3(0,1,2) : (n.y>n.z) ? int3(1,2,0) : int3(2,0,1);
	int3 mi = (n.x<n.y && n.x<n.z) ? int3(0,1,2) : (n.y<n.z) ? int3(1,2,0) : int3(2,0,1);
	int3 me = int3(3, 3, 3) - mi - ma;
	//create coordinates
	xCoords.x = Position[ma.y]; xCoords.y = Position[ma.z];
	xDDX.x = dpdx[ma.y]; xDDX.y = dpdx[ma.z];
	xDDY.x = dpdy[ma.y]; xDDY.y = dpdy[ma.z];
	yCoords.x = Position[me.y]; yCoords.y = Position[me.z];
	yDDX.x = dpdx[me.y]; yDDX.y = dpdx[me.z];
	yDDY.x = dpdy[me.y]; yDDY.y = dpdy[me.z];	
	// project+fetch
	Node_X = SAMPLE_TEXTURE2D_GRAD(Texture.tex, Sampler.samplerstate, xCoords, xDDX, xDDY);
	Node_Y = SAMPLE_TEXTURE2D_GRAD(Texture.tex, Sampler.samplerstate, yCoords, yDDX, yDDY);
	// blend factors
	w.x = n[ma.x]; 	w.y = n[me.x];
	// make local support
	w = saturate( (w-0.5773)/(1.0-0.5773));
	// shape transition
	w = pow( w, float2(contrast/8.0, contrast/8.0) );
	// blend and return
	XYZ = (Node_X*w.x + Node_Y*w.y) / (w.x + w.y);",
                    new ParameterDescriptor("Texture", TYPE.Texture2D, Usage.In),
                    new ParameterDescriptor("Sampler", TYPE.SamplerState, Usage.In),
                    new ParameterDescriptor("Position", TYPE.Vec3, Usage.In, REF.WorldSpace_Position),
                    new ParameterDescriptor("Normal", TYPE.Vec3, Usage.In, REF.WorldSpace_Normal),
                    new ParameterDescriptor("Tile", TYPE.Float, Usage.In, new float[] { 1.0f }),
                    new ParameterDescriptor("Contrast", TYPE.Float, Usage.In, new float[] { 1.0f }),
                    new ParameterDescriptor("XYZ", TYPE.Vec4, Usage.Out),
                    new ParameterDescriptor("n", TYPE.Vec3, Usage.Local),
                    new ParameterDescriptor("dpdx", TYPE.Vec3, Usage.Local),
                    new ParameterDescriptor("dpdy", TYPE.Vec3, Usage.Local),
                    new ParameterDescriptor("Node_X", TYPE.Vec4, Usage.Local),
                    new ParameterDescriptor("Node_Y", TYPE.Vec4, Usage.Local),
                    new ParameterDescriptor("w", TYPE.Vec2, Usage.Local),
                    new ParameterDescriptor("xCoords", TYPE.Vec2, Usage.Local),
                    new ParameterDescriptor("xDDX", TYPE.Vec2, Usage.Local),
                    new ParameterDescriptor("xDDY", TYPE.Vec2, Usage.Local),
                    new ParameterDescriptor("yCoords", TYPE.Vec2, Usage.Local),
                    new ParameterDescriptor("yDDX", TYPE.Vec2, Usage.Local),
                    new ParameterDescriptor("yDDY", TYPE.Vec2, Usage.Local)
                )
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Samples a texture three times and projects in front/back, top/bottom, and left/right.",
            categories: new string[2] { "Input", "Texture" },
            synonyms: new string[3] { "project", "box mapping", "round cube mapping" },
            selectableFunctions: new()
            {
                { "SampleTriplanarDefault", "Default" },
                { "SampleTriplanarNormal", "Normal" },
                { "SampleTriplanar2Samples", "2 Samples" }
            },
            parameters: new ParameterUIDescriptor[10] {
                new ParameterUIDescriptor(
                    name: "Texture",
                    tooltip: "the texture asset to sample"
                ),
                new ParameterUIDescriptor(
                    name: "Sampler",
                    tooltip: "the texture sampler to use for sampling the texture"
                ),
                new ParameterUIDescriptor(
                    name: "Position",
                    tooltip: "position is used for projecting the texture onto the mesh"
                ),
                new ParameterUIDescriptor(
                    name: "Normal",
                    tooltip: "the normal is used to mask and blend between the projections"
                ),
                new ParameterUIDescriptor(
                    name: "Tile",
                    tooltip: "the number of texture tiles per meter"
                ),
                new ParameterUIDescriptor(
                    name: "Blend",
                    tooltip: "the focus or blurriness of the blending between the projections"
                ),
                new ParameterUIDescriptor(
                    name: "Contrast",
                    tooltip: "controls the sharpness of the blending in the transitions areas"
                ),
                new ParameterUIDescriptor(
                    name: "XYZ",
                    tooltip: "texture projected front/back, left/right, top/bottom"
                ),
                new ParameterUIDescriptor(
                    name: "XZ",
                    tooltip: "texture projected front/back and left/right"
                ),
                new ParameterUIDescriptor(
                    name: "Y",
                    tooltip: "texture projected top/bottom"
                )
            }
        );
    }
}
