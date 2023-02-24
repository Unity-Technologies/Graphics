using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class Noise3DNode : IStandardNode
    {
        static string Name = "Noise3D";
        static int Version = 1;
        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            "ValueFBM",
            functions: new FunctionDescriptor[] {
                new(
                    "ridge",
@"    h = abs(h);
    h = offset - h;
    h = h * h;
    Out = h;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("h", TYPE.Float, Usage.In),
                        new ParameterDescriptor("offset", TYPE.Float, Usage.In),
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out)
                    },
                    isHelper: true
                ),
                new(
                    "hashTexture",
                    @"    Out = SAMPLE_TEXTURE2D_LOD(NoiseHashTexture.tex, NoiseHashTexture.samplerstate, uv, 0);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("uv", TYPE.Vec2, Usage.In),
                        new ParameterDescriptor("NoiseHashTexture", TYPE.Texture2D, Usage.In),
                        new ParameterDescriptor("Out", TYPE.Vec4, Usage.Out)
                    },
                    isHelper: true
                ),
                new(
                    "valueNoise",
@"   i = floor(p);
    f = p - i;
    f = f * f * (3.0 - 2.0 * f);
    Hash_Tchou_3_1_float(i + float3(0, 0, 0), s);
    Hash_Tchou_3_1_float(i + float3(1, 0, 0), t);
    Hash_Tchou_3_1_float(i + float3(0, 0, 1), u);
    Hash_Tchou_3_1_float(i + float3(1, 0, 1), v);
    Hash_Tchou_3_1_float(i + float3(0, 1, 0), w);
    Hash_Tchou_3_1_float(i + float3(1, 1, 0), x);
    Hash_Tchou_3_1_float(i + float3(0, 1, 1), y);
    Hash_Tchou_3_1_float(i + float3(1, 1, 1), z);
    Out = lerp( lerp( lerp(s, t, f.x),
    				  lerp(u, v, f.x), f.z),
    			lerp( lerp(w, x, f.x),
    				  lerp(y, z, f.x), f.z), f.y) * 2 - 1;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("p", TYPE.Vec3, Usage.In),
                        new ParameterDescriptor("i", TYPE.Vec3, Usage.Local),
                        new ParameterDescriptor("f", TYPE.Vec3, Usage.Local),
                        new ParameterDescriptor("s", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("t", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("u", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("v", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("w", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("x", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("y", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("z", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out)
                    },
                    new string[]
                    {
                        "\"Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl\""
                    },
                    isHelper: true
                ),
                new(
                    "valueTextureNoise",
@"   i = floor(p);
    f = p - i;
    f = f * f * (3.0 - 2.0 * f);
    uv = (i.xy+float2(37.0,-17.0)*i.z) + f.xy;
    hashTexture((uv+0.5)/256.0, NoiseHashTexture, hash);
    rg = hash.yx;
    Out = lerp( rg.x, rg.y, f.z ) * 2 - 1;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("p", TYPE.Vec3, Usage.In),
                        new ParameterDescriptor("NoiseHashTexture", TYPE.Texture2D, Usage.In),
                        new ParameterDescriptor("i", TYPE.Vec3, Usage.Local),
                        new ParameterDescriptor("f", TYPE.Vec3, Usage.Local),
                        new ParameterDescriptor("uv", TYPE.Vec2, Usage.Local),
                        new ParameterDescriptor("hash", TYPE.Vec4, Usage.Local),
                        new ParameterDescriptor("rg", TYPE.Vec2, Usage.Local),
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out)
                    },
                    isHelper: true
                ),
                new(
                    "gradientNoise",
@"   i = floor(p);
    f = p - i;
    g = f * f * (3.0 - 2.0 * f);
    Hash_Tchou_3_3_float(i + float3(0, 0, 0), s);
    Hash_Tchou_3_3_float(i + float3(1, 0, 0), t);
    Hash_Tchou_3_3_float(i + float3(0, 1, 0), u);
    Hash_Tchou_3_3_float(i + float3(1, 1, 0), v);
    Hash_Tchou_3_3_float(i + float3(0, 0, 1), w);
    Hash_Tchou_3_3_float(i + float3(1, 0, 1), x);
    Hash_Tchou_3_3_float(i + float3(0, 1, 1), y);
    Hash_Tchou_3_3_float(i + float3(1, 1, 1), z);
    Out = lerp( lerp( lerp(dot(s*2-1, f - float3(0, 0, 0)),
                           dot(t*2-1, f - float3(1, 0, 0)),g.x),
    				  lerp(dot(u*2-1, f - float3(0, 1, 0)),
                           dot(v*2-1, f - float3(1, 1, 0)),g.x), g.y),
    			lerp( lerp(dot(w*2-1, f - float3(0, 0, 1)),
                           dot(x*2-1, f - float3(1, 0, 1)),g.x),
    				  lerp(dot(y*2-1, f - float3(0, 1, 1)),
                           dot(z*2-1, f - float3(1, 1, 1)),g.x), g.y), g.z);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("p", TYPE.Vec3, Usage.In),
                        new ParameterDescriptor("i", TYPE.Vec3, Usage.Local),
                        new ParameterDescriptor("f", TYPE.Vec3, Usage.Local),
                        new ParameterDescriptor("g", TYPE.Vec3, Usage.Local),
                        new ParameterDescriptor("s", TYPE.Vec3, Usage.Local),
                        new ParameterDescriptor("t", TYPE.Vec3, Usage.Local),
                        new ParameterDescriptor("u", TYPE.Vec3, Usage.Local),
                        new ParameterDescriptor("v", TYPE.Vec3, Usage.Local),
                        new ParameterDescriptor("w", TYPE.Vec3, Usage.Local),
                        new ParameterDescriptor("x", TYPE.Vec3, Usage.Local),
                        new ParameterDescriptor("y", TYPE.Vec3, Usage.Local),
                        new ParameterDescriptor("z", TYPE.Vec3, Usage.Local),
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out)
                    },
                    new string[]
                    {
                        "\"Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl\""
                    },
                    isHelper: true
                ),
                new(
                    "worleyNoise",
@"    for (int x = -1; x <= 1; ++x)
    {
        for(int y = -1; y <= 1; ++y)
        {
            for(int z = -1; z <= 1; ++z)
            {
                offset = float3(x, y, z);
                Hash_Tchou_3_3_float(floor(p) + offset, h);
                h = (h*2-1) * .4 + .3; // [.3, .7]
                h += offset;
                d = frac(p) - h;
                minDist = min(minDist, dot(d, d));
            }
        }
    }
    Out = minDist*2-1;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("p", TYPE.Vec3, Usage.In),
                        new ParameterDescriptor("i", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("h", TYPE.Vec3, Usage.Local),
                        new ParameterDescriptor("offset", TYPE.Vec3, Usage.Local),
                        new ParameterDescriptor("d", TYPE.Vec3, Usage.Local),
                        new ParameterDescriptor("minDist", TYPE.Float, Usage.Local, new float[] {1.0f}),
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out),
                    },
                    new string[]
                    {
                        "\"Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl\""
                    },
                    isHelper: true
                ),
                new(
                    "simplexNoise",
@"    i = floor(p + (p.x + p.y + p.z) * 0.333333333);
    d0 = p - (i - (i.x + i.y + i.z) * 0.166666667);
    e = step(float3(0.0, 0.0, 0.0), d0 - d0.yzx);
	i1 = e * (1.0 - e.zxy);
    i2 = 1.0 - e.zxy * (1.0 - e);
    d1 = d0 - (i1 - 1.0 * 0.166666667);
    d2 = d0 - (i2 - 2.0 * 0.166666667);
    d3 = d0 - (1.0 - 3.0 * 0.166666667);
    h = max(0.6 - float4(dot(d0, d0), dot(d1, d1), dot(d2, d2), dot(d3, d3)), 0.0);
	Hash_Tchou_3_3_float(i, s);
	Hash_Tchou_3_3_float(i + i1, t);
	Hash_Tchou_3_3_float(i + i2, u);
	Hash_Tchou_3_3_float(i + 1.0, v);
    n = h * h * h * h * float4(dot(d0, s*2-1), dot(d1, t*2-1), dot(d2, u*2-1), dot(d3, v*2-1));
    Out = dot(float4(31.316, 31.316, 31.316, 31.316), n);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("p", TYPE.Vec3, Usage.In),
                        new ParameterDescriptor("i", TYPE.Vec3, Usage.Local),
                        new ParameterDescriptor("d0", TYPE.Vec3, Usage.Local),
                        new ParameterDescriptor("e", TYPE.Vec3, Usage.Local),
                        new ParameterDescriptor("i1", TYPE.Vec3, Usage.Local),
                        new ParameterDescriptor("i2", TYPE.Vec3, Usage.Local),
                        new ParameterDescriptor("d1", TYPE.Vec3, Usage.Local),
                        new ParameterDescriptor("d2", TYPE.Vec3, Usage.Local),
                        new ParameterDescriptor("d3", TYPE.Vec3, Usage.Local),
                        new ParameterDescriptor("h", TYPE.Vec4, Usage.Local),
                        new ParameterDescriptor("s", TYPE.Vec3, Usage.Local),
                        new ParameterDescriptor("t", TYPE.Vec3, Usage.Local),
                        new ParameterDescriptor("u", TYPE.Vec3, Usage.Local),
                        new ParameterDescriptor("v", TYPE.Vec3, Usage.Local),
                        new ParameterDescriptor("n", TYPE.Vec4, Usage.Local),
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out),
                    },
                    new string[]
                    {
                        "\"Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl\""
                    },
                    isHelper: true
                ),
                new(
                    "ValueFBM",
@"   Position *= Scale;
    for(int i=0; i<Octaves; i++) {
        valueNoise(Position*freq, x);
        if (RotateOctaves) Position = mul(Position, randRotMat);
        sum += x*amp;
	    freq *= Lacunarity;
	    amp *= Gain;
    }
    Out = sum+0.5;
    if (sRGBOutput) Out = pow(Out, 2.2);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("Position", TYPE.Vec3, Usage.In, REF.WorldSpace_Position),
                        new ParameterDescriptor("Scale", TYPE.Vec3, Usage.In, new float[] { 10.0f, 10.0f, 10.0f }),
                        new ParameterDescriptor("Octaves", TYPE.Int, Usage.In, new float[] {3}),
                        new ParameterDescriptor("Lacunarity", TYPE.Float, Usage.In, new float[] {2.0f}),
                        new ParameterDescriptor("Gain", TYPE.Float, Usage.In, new float[] {0.5f}),
                        new ParameterDescriptor("RotateOctaves", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("sRGBOutput", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("randRotMat", TYPE.Mat3, Usage.Local, new float[]
                        {
                            0.5703109f, -0.6308292f, 0.5261179f,
                            0.8214105f, 0.4336779f, -0.3704164f,
                            0.0055037f, 0.6434113f,  0.7655009f
                        }),//rotates the octaves by x37, y19, z53 degrees
                        new ParameterDescriptor("freq", TYPE.Float, Usage.Local, new float[] {1.0f}),
                        new ParameterDescriptor("amp", TYPE.Float, Usage.Local, new float[] {0.5f}),
                        new ParameterDescriptor("sum", TYPE.Float, Usage.Local, new float[] {0.0f}),
                        new ParameterDescriptor("x", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out),
                    }
                ),
                new(
                    "ValueTurbulence",
@"   Position *= Scale;
    for(int i=0; i<Octaves; i++) {
        valueNoise(Position*freq, x);
        if (RotateOctaves) Position = mul(Position, randRotMat);
        sum += abs(x)*amp;
        freq *= Lacunarity;
        amp *= Gain;
    }
    Out = sum;
    if (sRGBOutput) Out = pow(Out, 2.2);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("Position", TYPE.Vec3, Usage.In, REF.WorldSpace_Position),
                        new ParameterDescriptor("Scale", TYPE.Vec3, Usage.In, new float[] { 10.0f, 10.0f, 10.0f }),
                        new ParameterDescriptor("Octaves", TYPE.Int, Usage.In, new float[] {3}),
                        new ParameterDescriptor("Lacunarity", TYPE.Float, Usage.In, new float[] {2.0f}),
                        new ParameterDescriptor("Gain", TYPE.Float, Usage.In, new float[] {0.5f}),
                        new ParameterDescriptor("RotateOctaves", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("sRGBOutput", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("randRotMat", TYPE.Mat3, Usage.Local, new float[]
                        {
                            0.5703109f, -0.6308292f, 0.5261179f,
                            0.8214105f, 0.4336779f, -0.3704164f,
                            0.0055037f, 0.6434113f,  0.7655009f
                        }),//rotates the octaves by x37, y19, z53 degrees
                        new ParameterDescriptor("freq", TYPE.Float, Usage.Local, new float[] {1.0f}),
                        new ParameterDescriptor("amp", TYPE.Float, Usage.Local, new float[] {1.0f}),
                        new ParameterDescriptor("sum", TYPE.Float, Usage.Local, new float[] {0.0f}),
                        new ParameterDescriptor("x", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out),
                    }
                ),
                new(
                    "ValueRidgedMF",
@"   Position *= Scale;
    for(int i=0; i<Octaves; i++) {
        valueNoise(Position*freq, x);
        if (RotateOctaves) Position = mul(Position, randRotMat);
        ridge(x, Offset, n);
		sum += n*amp*prev;
		prev = n;
		freq *= Lacunarity;
		amp *= Gain;
	}
	Out = sum;
    if (sRGBOutput) Out = pow(Out, 2.2);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("Position", TYPE.Vec3, Usage.In, REF.WorldSpace_Position),
                        new ParameterDescriptor("Scale", TYPE.Vec3, Usage.In, new float[] { 10.0f, 10.0f, 10.0f }),
                        new ParameterDescriptor("Octaves", TYPE.Int, Usage.In, new float[] {3}),
                        new ParameterDescriptor("Lacunarity", TYPE.Float, Usage.In, new float[] {2.0f}),
                        new ParameterDescriptor("Gain", TYPE.Float, Usage.In, new float[] {0.5f}),
                        new ParameterDescriptor("Offset", TYPE.Float, Usage.In, new float[] {1.0f}),
                        new ParameterDescriptor("RotateOctaves", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("sRGBOutput", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("randRotMat", TYPE.Mat3, Usage.Local, new float[]
                        {
                            0.5703109f, -0.6308292f, 0.5261179f,
                            0.8214105f, 0.4336779f, -0.3704164f,
                            0.0055037f, 0.6434113f,  0.7655009f
                        }),//rotates the octaves by x37, y19, z53 degrees
                        new ParameterDescriptor("freq", TYPE.Float, Usage.Local, new float[] {1.0f}),
                        new ParameterDescriptor("amp", TYPE.Float, Usage.Local, new float[] {0.5f}),
                        new ParameterDescriptor("sum", TYPE.Float, Usage.Local, new float[] {0.0f}),
                        new ParameterDescriptor("prev", TYPE.Float, Usage.Local, new float[] {1.0f}),
                        new ParameterDescriptor("x", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("n", TYPE.Float, Usage.Local, new float[] {0.0f}),
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out),
                    }
                ),
                new(
                    "GradientFBM",
@"   Position *= Scale;
    for(int i=0; i<Octaves; i++) {
        gradientNoise(Position*freq, x);
        if (RotateOctaves) Position = mul(Position, randRotMat);
        sum += x*amp;
	    freq *= Lacunarity;
	    amp *= Gain;
    }
    Out = sum+0.5;
    if (sRGBOutput) Out = pow(Out, 2.2);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("Position", TYPE.Vec3, Usage.In, REF.WorldSpace_Position),
                        new ParameterDescriptor("Scale", TYPE.Vec3, Usage.In, new float[] { 10.0f, 10.0f, 10.0f }),
                        new ParameterDescriptor("Octaves", TYPE.Int, Usage.In, new float[] {3}),
                        new ParameterDescriptor("Lacunarity", TYPE.Float, Usage.In, new float[] {2.0f}),
                        new ParameterDescriptor("Gain", TYPE.Float, Usage.In, new float[] {0.5f}),
                        new ParameterDescriptor("RotateOctaves", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("sRGBOutput", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("randRotMat", TYPE.Mat3, Usage.Local, new float[]
                        {
                            0.5703109f, -0.6308292f, 0.5261179f,
                            0.8214105f, 0.4336779f, -0.3704164f,
                            0.0055037f, 0.6434113f,  0.7655009f
                        }),//rotates the octaves by x37, y19, z53 degrees
                        new ParameterDescriptor("freq", TYPE.Float, Usage.Local, new float[] {1.0f}),
                        new ParameterDescriptor("amp", TYPE.Float, Usage.Local, new float[] {0.5f}),
                        new ParameterDescriptor("sum", TYPE.Float, Usage.Local, new float[] {0.0f}),
                        new ParameterDescriptor("x", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out),
                    }
                ),
                new(
                    "GradientTurbulence",
@"   Position *= Scale;
    for(int i=0; i<Octaves; i++) {
        gradientNoise(Position*freq, x);
        if (RotateOctaves) Position = mul(Position, randRotMat);
        sum += abs(x)*amp;
        freq *= Lacunarity;
        amp *= Gain;
    }
    Out = sum;
    if (sRGBOutput) Out = pow(Out, 2.2);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("Position", TYPE.Vec3, Usage.In, REF.WorldSpace_Position),
                        new ParameterDescriptor("Scale", TYPE.Vec3, Usage.In, new float[] { 10.0f, 10.0f, 10.0f }),
                        new ParameterDescriptor("Octaves", TYPE.Int, Usage.In, new float[] {3}),
                        new ParameterDescriptor("Lacunarity", TYPE.Float, Usage.In, new float[] {2.0f}),
                        new ParameterDescriptor("Gain", TYPE.Float, Usage.In, new float[] {0.5f}),
                        new ParameterDescriptor("RotateOctaves", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("sRGBOutput", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("randRotMat", TYPE.Mat3, Usage.Local, new float[]
                        {
                            0.5703109f, -0.6308292f, 0.5261179f,
                            0.8214105f, 0.4336779f, -0.3704164f,
                            0.0055037f, 0.6434113f,  0.7655009f
                        }),//rotates the octaves by x37, y19, z53 degrees
                        new ParameterDescriptor("freq", TYPE.Float, Usage.Local, new float[] {1.0f}),
                        new ParameterDescriptor("amp", TYPE.Float, Usage.Local, new float[] {1.0f}),
                        new ParameterDescriptor("sum", TYPE.Float, Usage.Local, new float[] {0.0f}),
                        new ParameterDescriptor("x", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out),
                    }
                ),
                new(
                    "GradientRidgedMF",
@"   Position *= Scale;
    for(int i=0; i<Octaves; i++) {
        gradientNoise(Position*freq, x);
        if (RotateOctaves) Position = mul(Position, randRotMat);
        ridge(x, Offset, n);
		sum += n*amp*prev;
		prev = n;
		freq *= Lacunarity;
		amp *= Gain;
	}
    Out = sum;
    if (sRGBOutput) Out = pow(Out, 2.2);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("Position", TYPE.Vec3, Usage.In, REF.WorldSpace_Position),
                        new ParameterDescriptor("Scale", TYPE.Vec3, Usage.In, new float[] { 10.0f, 10.0f, 10.0f }),
                        new ParameterDescriptor("Octaves", TYPE.Int, Usage.In, new float[] {3}),
                        new ParameterDescriptor("Lacunarity", TYPE.Float, Usage.In, new float[] {2.0f}),
                        new ParameterDescriptor("Gain", TYPE.Float, Usage.In, new float[] {0.5f}),
                        new ParameterDescriptor("Offset", TYPE.Float, Usage.In, new float[] {1.0f}),
                        new ParameterDescriptor("RotateOctaves", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("sRGBOutput", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("randRotMat", TYPE.Mat3, Usage.Local, new float[]
                        {
                            0.5703109f, -0.6308292f, 0.5261179f,
                            0.8214105f, 0.4336779f, -0.3704164f,
                            0.0055037f, 0.6434113f,  0.7655009f
                        }),//rotates the octaves by x37, y19, z53 degrees
                        new ParameterDescriptor("freq", TYPE.Float, Usage.Local, new float[] {1.0f}),
                        new ParameterDescriptor("amp", TYPE.Float, Usage.Local, new float[] {0.5f}),
                        new ParameterDescriptor("sum", TYPE.Float, Usage.Local, new float[] {0.0f}),
                        new ParameterDescriptor("prev", TYPE.Float, Usage.Local, new float[] {1.0f}),
                        new ParameterDescriptor("x", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("n", TYPE.Float, Usage.Local, new float[] {0.0f}),
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out),
                    }
                ),
                new(
                    "SimplexFBM",
@"   Position *= Scale;
    for(int i=0; i<Octaves; i++) {
        simplexNoise(Position*freq, x);
        if (RotateOctaves) Position = mul(Position, randRotMat);
        sum += x*amp;
	    freq *= Lacunarity;
	    amp *= Gain;
    }
    Out = sum+0.5;
    if (sRGBOutput) Out = pow(Out, 2.2);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("Position", TYPE.Vec3, Usage.In, REF.WorldSpace_Position),
                        new ParameterDescriptor("Scale", TYPE.Vec3, Usage.In, new float[] { 10.0f, 10.0f, 10.0f }),
                        new ParameterDescriptor("Octaves", TYPE.Int, Usage.In, new float[] {3}),
                        new ParameterDescriptor("Lacunarity", TYPE.Float, Usage.In, new float[] {2.0f}),
                        new ParameterDescriptor("Gain", TYPE.Float, Usage.In, new float[] {0.5f}),
                        new ParameterDescriptor("RotateOctaves", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("sRGBOutput", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("randRotMat", TYPE.Mat3, Usage.Local, new float[]
                        {
                            0.5703109f, -0.6308292f, 0.5261179f,
                            0.8214105f, 0.4336779f, -0.3704164f,
                            0.0055037f, 0.6434113f,  0.7655009f
                        }),//rotates the octaves by x37, y19, z53 degrees
                        new ParameterDescriptor("freq", TYPE.Float, Usage.Local, new float[] {1.0f}),
                        new ParameterDescriptor("amp", TYPE.Float, Usage.Local, new float[] {0.5f}),
                        new ParameterDescriptor("sum", TYPE.Float, Usage.Local, new float[] {0.0f}),
                        new ParameterDescriptor("x", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out),
                    }
                ),
                new(
                    "SimplexTurbulence",
@"   Position *= Scale;
    for(int i=0; i<Octaves; i++) {
        simplexNoise(Position*freq, x);
        if (RotateOctaves) Position = mul(Position, randRotMat);
        sum += abs(x)*amp;
        freq *= Lacunarity;
        amp *= Gain;
    }
    Out = sum;
    if (sRGBOutput) Out = pow(Out, 2.2);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("Position", TYPE.Vec3, Usage.In, REF.WorldSpace_Position),
                        new ParameterDescriptor("Scale", TYPE.Vec3, Usage.In, new float[] { 10.0f, 10.0f, 10.0f }),
                        new ParameterDescriptor("Octaves", TYPE.Int, Usage.In, new float[] {3}),
                        new ParameterDescriptor("Lacunarity", TYPE.Float, Usage.In, new float[] {2.0f}),
                        new ParameterDescriptor("Gain", TYPE.Float, Usage.In, new float[] {0.5f}),
                        new ParameterDescriptor("RotateOctaves", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("sRGBOutput", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("randRotMat", TYPE.Mat3, Usage.Local, new float[]
                        {
                            0.5703109f, -0.6308292f, 0.5261179f,
                            0.8214105f, 0.4336779f, -0.3704164f,
                            0.0055037f, 0.6434113f,  0.7655009f
                        }),//rotates the octaves by x37, y19, z53 degrees
                        new ParameterDescriptor("freq", TYPE.Float, Usage.Local, new float[] {1.0f}),
                        new ParameterDescriptor("amp", TYPE.Float, Usage.Local, new float[] {1.0f}),
                        new ParameterDescriptor("sum", TYPE.Float, Usage.Local, new float[] {0.0f}),
                        new ParameterDescriptor("x", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out),
                    }
                ),
                new(
                    "SimplexRidgedMF",
@"   Position *= Scale;
    for(int i=0; i<Octaves; i++) {
        simplexNoise(Position*freq, x);
        if (RotateOctaves) Position = mul(Position, randRotMat);
        ridge(x, Offset, n);
		sum += n*amp*prev;
		prev = n;
		freq *= Lacunarity;
		amp *= Gain;
	}
	Out = sum;
    if (sRGBOutput) Out = pow(Out, 2.2);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("Position", TYPE.Vec3, Usage.In, REF.WorldSpace_Position),
                        new ParameterDescriptor("Scale", TYPE.Vec3, Usage.In, new float[] { 10.0f, 10.0f, 10.0f }),
                        new ParameterDescriptor("Octaves", TYPE.Int, Usage.In, new float[] {3}),
                        new ParameterDescriptor("Lacunarity", TYPE.Float, Usage.In, new float[] {2.0f}),
                        new ParameterDescriptor("Gain", TYPE.Float, Usage.In, new float[] {0.5f}),
                        new ParameterDescriptor("Offset", TYPE.Float, Usage.In, new float[] {1.0f}),
                        new ParameterDescriptor("RotateOctaves", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("sRGBOutput", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("randRotMat", TYPE.Mat3, Usage.Local, new float[]
                        {
                            0.5703109f, -0.6308292f, 0.5261179f,
                            0.8214105f, 0.4336779f, -0.3704164f,
                            0.0055037f, 0.6434113f,  0.7655009f
                        }),//rotates the octaves by x37, y19, z53 degrees
                        new ParameterDescriptor("freq", TYPE.Float, Usage.Local, new float[] {1.0f}),
                        new ParameterDescriptor("amp", TYPE.Float, Usage.Local, new float[] {0.5f}),
                        new ParameterDescriptor("sum", TYPE.Float, Usage.Local, new float[] {0.0f}),
                        new ParameterDescriptor("prev", TYPE.Float, Usage.Local, new float[] {1.0f}),
                        new ParameterDescriptor("x", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("n", TYPE.Float, Usage.Local, new float[] {0.0f}),
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out),
                    }
                ),
                new(
                    "WorleyFBM",
@"   Position *= Scale;
    for(int i=0; i<Octaves; i++) {
        worleyNoise(Position*freq, x);
        if (RotateOctaves) Position = mul(Position, randRotMat);
        sum += x*amp;
	    freq *= Lacunarity;
	    amp *= Gain;
    }
    Out = sum*0.5+0.5;
    if (sRGBOutput) Out = pow(Out, 2.2);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("Position", TYPE.Vec3, Usage.In, REF.WorldSpace_Position),
                        new ParameterDescriptor("Scale", TYPE.Vec3, Usage.In, new float[] { 10.0f, 10.0f, 10.0f }),
                        new ParameterDescriptor("Octaves", TYPE.Int, Usage.In, new float[] {3}),
                        new ParameterDescriptor("Lacunarity", TYPE.Float, Usage.In, new float[] {2.0f}),
                        new ParameterDescriptor("Gain", TYPE.Float, Usage.In, new float[] {0.5f}),
                        new ParameterDescriptor("RotateOctaves", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("sRGBOutput", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("randRotMat", TYPE.Mat3, Usage.Local, new float[]
                        {
                            0.5703109f, -0.6308292f, 0.5261179f,
                            0.8214105f, 0.4336779f, -0.3704164f,
                            0.0055037f, 0.6434113f,  0.7655009f
                        }),//rotates the octaves by x37, y19, z53 degrees
                        new ParameterDescriptor("freq", TYPE.Float, Usage.Local, new float[] {1.0f}),
                        new ParameterDescriptor("amp", TYPE.Float, Usage.Local, new float[] {0.5f}),
                        new ParameterDescriptor("sum", TYPE.Float, Usage.Local, new float[] {0.0f}),
                        new ParameterDescriptor("x", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out),
                    }
                ),
                new(
                    "WorleyTurbulence",
@"   Position *= Scale;
    for(int i=0; i<Octaves; i++) {
        worleyNoise(Position*freq, x);
        if (RotateOctaves) Position = mul(Position, randRotMat);
        sum += abs(x)*amp;
        freq *= Lacunarity;
        amp *= Gain;
    }
    Out = sum*0.5;
    if (sRGBOutput) Out = pow(Out, 2.2);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("Position", TYPE.Vec3, Usage.In, REF.WorldSpace_Position),
                        new ParameterDescriptor("Scale", TYPE.Vec3, Usage.In, new float[] { 10.0f, 10.0f, 10.0f }),
                        new ParameterDescriptor("Octaves", TYPE.Int, Usage.In, new float[] {3}),
                        new ParameterDescriptor("Lacunarity", TYPE.Float, Usage.In, new float[] {2.0f}),
                        new ParameterDescriptor("Gain", TYPE.Float, Usage.In, new float[] {0.5f}),
                        new ParameterDescriptor("RotateOctaves", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("sRGBOutput", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("randRotMat", TYPE.Mat3, Usage.Local, new float[]
                        {
                            0.5703109f, -0.6308292f, 0.5261179f,
                            0.8214105f, 0.4336779f, -0.3704164f,
                            0.0055037f, 0.6434113f,  0.7655009f
                        }),//rotates the octaves by x37, y19, z53 degrees
                        new ParameterDescriptor("freq", TYPE.Float, Usage.Local, new float[] {1.0f}),
                        new ParameterDescriptor("amp", TYPE.Float, Usage.Local, new float[] {1.0f}),
                        new ParameterDescriptor("sum", TYPE.Float, Usage.Local, new float[] {0.0f}),
                        new ParameterDescriptor("x", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out),
                    }
                ),
                new(
                    "WorleyRidgedMF",
@"   Position *= Scale;
    for(int i=0; i<Octaves; i++) {
        worleyNoise(Position*freq, x);
        if (RotateOctaves) Position = mul(Position, randRotMat);
        ridge(x, Offset, n);
		sum += n*amp*prev;
		prev = n;
		freq *= Lacunarity;
		amp *= Gain;
	}
	Out = sum;
    if (sRGBOutput) Out = pow(Out, 2.2);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("Position", TYPE.Vec3, Usage.In, REF.WorldSpace_Position),
                        new ParameterDescriptor("Scale", TYPE.Vec3, Usage.In, new float[] { 10.0f, 10.0f, 10.0f }),
                        new ParameterDescriptor("Octaves", TYPE.Int, Usage.In, new float[] {3}),
                        new ParameterDescriptor("Lacunarity", TYPE.Float, Usage.In, new float[] {2.0f}),
                        new ParameterDescriptor("Gain", TYPE.Float, Usage.In, new float[] {0.5f}),
                        new ParameterDescriptor("Offset", TYPE.Float, Usage.In, new float[] {1.0f}),
                        new ParameterDescriptor("RotateOctaves", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("sRGBOutput", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("randRotMat", TYPE.Mat3, Usage.Local, new float[]
                        {
                            0.5703109f, -0.6308292f, 0.5261179f,
                            0.8214105f, 0.4336779f, -0.3704164f,
                            0.0055037f, 0.6434113f,  0.7655009f
                        }),//rotates the octaves by x37, y19, z53 degrees
                        new ParameterDescriptor("freq", TYPE.Float, Usage.Local, new float[] {1.0f}),
                        new ParameterDescriptor("amp", TYPE.Float, Usage.Local, new float[] {0.5f}),
                        new ParameterDescriptor("sum", TYPE.Float, Usage.Local, new float[] {0.0f}),
                        new ParameterDescriptor("prev", TYPE.Float, Usage.Local, new float[] {1.0f}),
                        new ParameterDescriptor("x", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("n", TYPE.Float, Usage.Local, new float[] {0.0f}),
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out),
                    }
                ),

                new(
                    "ValueTexFBM",
@"   Position *= Scale;
    for(int i=0; i<Octaves; i++) {
        valueTextureNoise(Position*freq, NoiseHashTexture, x);
        if (RotateOctaves) Position = mul(Position, randRotMat);
        sum += x*amp;
	    freq *= Lacunarity;
	    amp *= Gain;
    }
    Out = sum+0.5;
    if (sRGBOutput) Out = pow(Out, 2.2);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("Position", TYPE.Vec3, Usage.In, REF.WorldSpace_Position),
                        new ParameterDescriptor("Scale", TYPE.Vec3, Usage.In, new float[] { 10.0f, 10.0f, 10.0f }),
                        new ParameterDescriptor("Octaves", TYPE.Int, Usage.In, new float[] {3}),
                        new ParameterDescriptor("Lacunarity", TYPE.Float, Usage.In, new float[] {2.0f}),
                        new ParameterDescriptor("Gain", TYPE.Float, Usage.In, new float[] {0.5f}),
                        new ParameterDescriptor("NoiseHashTexture", TYPE.Texture2D, Usage.In),
                        new ParameterDescriptor("RotateOctaves", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("sRGBOutput", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("randRotMat", TYPE.Mat3, Usage.Local, new float[]
                        {
                            0.5703109f, -0.6308292f, 0.5261179f,
                            0.8214105f, 0.4336779f, -0.3704164f,
                            0.0055037f, 0.6434113f,  0.7655009f
                        }),//rotates the octaves by x37, y19, z53 degrees
                        new ParameterDescriptor("freq", TYPE.Float, Usage.Local, new float[] {1.0f}),
                        new ParameterDescriptor("amp", TYPE.Float, Usage.Local, new float[] {0.5f}),
                        new ParameterDescriptor("sum", TYPE.Float, Usage.Local, new float[] {0.0f}),
                        new ParameterDescriptor("x", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out),
                    }
                ),
                new(
                    "ValueTexTurbulence",
@"   Position *= Scale;
    for(int i=0; i<Octaves; i++) {
        valueTextureNoise(Position*freq, NoiseHashTexture, x);
        if (RotateOctaves) Position = mul(Position, randRotMat);
        sum += abs(x)*amp;
	    freq *= Lacunarity;
	    amp *= Gain;
    }
    Out = sum;
    if (sRGBOutput) Out = pow(Out, 2.2);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("Position", TYPE.Vec3, Usage.In, REF.WorldSpace_Position),
                        new ParameterDescriptor("Scale", TYPE.Vec3, Usage.In, new float[] { 10.0f, 10.0f, 10.0f }),
                        new ParameterDescriptor("Octaves", TYPE.Int, Usage.In, new float[] {3}),
                        new ParameterDescriptor("Lacunarity", TYPE.Float, Usage.In, new float[] {2.0f}),
                        new ParameterDescriptor("Gain", TYPE.Float, Usage.In, new float[] {0.5f}),
                        new ParameterDescriptor("NoiseHashTexture", TYPE.Texture2D, Usage.In),
                        new ParameterDescriptor("RotateOctaves", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("sRGBOutput", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("randRotMat", TYPE.Mat3, Usage.Local, new float[]
                        {
                            0.5703109f, -0.6308292f, 0.5261179f,
                            0.8214105f, 0.4336779f, -0.3704164f,
                            0.0055037f, 0.6434113f,  0.7655009f
                        }),//rotates the octaves by x37, y19, z53 degrees
                        new ParameterDescriptor("freq", TYPE.Float, Usage.Local, new float[] {1.0f}),
                        new ParameterDescriptor("amp", TYPE.Float, Usage.Local, new float[] {1.0f}),
                        new ParameterDescriptor("sum", TYPE.Float, Usage.Local, new float[] {0.0f}),
                        new ParameterDescriptor("x", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out),
                    }
                ),
                new(
                    "ValueTexRidgedMF",
@"   Position *= Scale;
    for(int i=0; i<Octaves; i++) {
        valueTextureNoise(Position*freq, NoiseHashTexture, x);
        if (RotateOctaves) Position = mul(Position, randRotMat);
        ridge(x, Offset, n);
		sum += n*amp*prev;
		prev = n;
		freq *= Lacunarity;
		amp *= Gain;
	}
	Out = sum;
    if (sRGBOutput) Out = pow(Out, 2.2);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("Position", TYPE.Vec3, Usage.In, REF.WorldSpace_Position),
                        new ParameterDescriptor("Scale", TYPE.Vec3, Usage.In, new float[] { 10.0f, 10.0f, 10.0f }),
                        new ParameterDescriptor("Octaves", TYPE.Int, Usage.In, new float[] {3}),
                        new ParameterDescriptor("Lacunarity", TYPE.Float, Usage.In, new float[] {2.0f}),
                        new ParameterDescriptor("Gain", TYPE.Float, Usage.In, new float[] {0.5f}),
                        new ParameterDescriptor("Offset", TYPE.Float, Usage.In, new float[] {1.0f}),
                        new ParameterDescriptor("NoiseHashTexture", TYPE.Texture2D, Usage.In),
                        new ParameterDescriptor("RotateOctaves", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("sRGBOutput", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("randRotMat", TYPE.Mat3, Usage.Local, new float[]
                        {
                            0.5703109f, -0.6308292f, 0.5261179f,
                            0.8214105f, 0.4336779f, -0.3704164f,
                            0.0055037f, 0.6434113f,  0.7655009f
                        }),//rotates the octaves by x37, y19, z53 degrees
                        new ParameterDescriptor("freq", TYPE.Float, Usage.Local, new float[] {1.0f}),
                        new ParameterDescriptor("amp", TYPE.Float, Usage.Local, new float[] {0.5f}),
                        new ParameterDescriptor("sum", TYPE.Float, Usage.Local, new float[] {0.0f}),
                        new ParameterDescriptor("prev", TYPE.Float, Usage.Local, new float[] {1.0f}),
                        new ParameterDescriptor("x", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("n", TYPE.Float, Usage.Local, new float[] {0.0f}),
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out),
                    }
                ),
                new(
                    "GradientTexFBM",
@"    Position = Position * Scale * 0.1; //adjust for texture scale - so it matches the others
    for(int i=0; i<Octaves; i++) {
        sum += (SAMPLE_TEXTURE3D_LOD(GradientVolume.tex, GradientVolume.samplerstate, Position*freq, 0).x * 2 -1) * amp;
        if (RotateOctaves) Position = mul(Position, randRotMat);
	    freq *= Lacunarity;
	    amp *= Gain;
    }
    Out = sum+0.5;
    if (sRGBOutput) Out = pow(Out, 2.2);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("Position", TYPE.Vec3, Usage.In, REF.WorldSpace_Position),
                        new ParameterDescriptor("Scale", TYPE.Vec3, Usage.In, new float[] { 10.0f, 10.0f, 10.0f }),
                        new ParameterDescriptor("Octaves", TYPE.Int, Usage.In, new float[] {3}),
                        new ParameterDescriptor("Lacunarity", TYPE.Float, Usage.In, new float[] {2.0f}),
                        new ParameterDescriptor("Gain", TYPE.Float, Usage.In, new float[] {0.5f}),
                        new ParameterDescriptor("GradientVolume", TYPE.Texture3D, Usage.In),
                        new ParameterDescriptor("RotateOctaves", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("sRGBOutput", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("randRotMat", TYPE.Mat3, Usage.Local, new float[]
                        {
                            0.5703109f, -0.6308292f, 0.5261179f,
                            0.8214105f, 0.4336779f, -0.3704164f,
                            0.0055037f, 0.6434113f,  0.7655009f
                        }),//rotates the octaves by x37, y19, z53 degrees
                        new ParameterDescriptor("freq", TYPE.Float, Usage.Local, new float[] {1.0f}),
                        new ParameterDescriptor("amp", TYPE.Float, Usage.Local, new float[] {0.5f}),
                        new ParameterDescriptor("sum", TYPE.Float, Usage.Local, new float[] {0.0f}),
                        new ParameterDescriptor("x", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out),
                    }
                ),
                new(
                    "GradientTexTurbulence",
@"    Position = Position * Scale * 0.1; //adjust for texture scale - so it matches the others
    for(int i=0; i<Octaves; i++) {
        sum += abs(SAMPLE_TEXTURE3D_LOD(GradientVolume.tex, GradientVolume.samplerstate, Position*freq, 0).x * 2 -1)*amp;
        if (RotateOctaves) Position = mul(Position, randRotMat);
        freq *= Lacunarity;
        amp *= Gain;
    }
   Out = sum;
    if (sRGBOutput) Out = pow(Out, 2.2);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("Position", TYPE.Vec3, Usage.In, REF.WorldSpace_Position),
                        new ParameterDescriptor("Scale", TYPE.Vec3, Usage.In, new float[] { 10.0f, 10.0f, 10.0f }),
                        new ParameterDescriptor("Octaves", TYPE.Int, Usage.In, new float[] {3}),
                        new ParameterDescriptor("Lacunarity", TYPE.Float, Usage.In, new float[] {2.0f}),
                        new ParameterDescriptor("Gain", TYPE.Float, Usage.In, new float[] {0.5f}),
                        new ParameterDescriptor("GradientVolume", TYPE.Texture3D, Usage.In),
                        new ParameterDescriptor("RotateOctaves", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("sRGBOutput", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("randRotMat", TYPE.Mat3, Usage.Local, new float[]
                        {
                            0.5703109f, -0.6308292f, 0.5261179f,
                            0.8214105f, 0.4336779f, -0.3704164f,
                            0.0055037f, 0.6434113f,  0.7655009f
                        }),//rotates the octaves by x37, y19, z53 degrees
                        new ParameterDescriptor("freq", TYPE.Float, Usage.Local, new float[] {1.0f}),
                        new ParameterDescriptor("amp", TYPE.Float, Usage.Local, new float[] {1.0f}),
                        new ParameterDescriptor("sum", TYPE.Float, Usage.Local, new float[] {0.0f}),
                        new ParameterDescriptor("x", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out),
                    }
                ),
                new(
                    "GradientTexRidgedMF",
@"    Position = Position * Scale * 0.1; //adjust for texture scale - so it matches the others
    for(int i=0; i<Octaves; i++) {
        ridge(SAMPLE_TEXTURE3D_LOD(GradientVolume.tex, GradientVolume.samplerstate, Position*freq, 0).x * 2 -1, Offset, n);
        if (RotateOctaves) Position = mul(Position, randRotMat);
		sum += n*amp*prev;
		prev = n;
		freq *= Lacunarity;
		amp *= Gain;
	}
    Out = sum;
    if (sRGBOutput) Out = pow(Out, 2.2);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("Position", TYPE.Vec3, Usage.In, REF.WorldSpace_Position),
                        new ParameterDescriptor("Scale", TYPE.Vec3, Usage.In, new float[] { 10.0f, 10.0f, 10.0f }),
                        new ParameterDescriptor("Octaves", TYPE.Int, Usage.In, new float[] {3}),
                        new ParameterDescriptor("Lacunarity", TYPE.Float, Usage.In, new float[] {2.0f}),
                        new ParameterDescriptor("Gain", TYPE.Float, Usage.In, new float[] {0.5f}),
                        new ParameterDescriptor("Offset", TYPE.Float, Usage.In, new float[] {1.0f}),
                        new ParameterDescriptor("GradientVolume", TYPE.Texture3D, Usage.In),
                        new ParameterDescriptor("RotateOctaves", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("sRGBOutput", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("randRotMat", TYPE.Mat3, Usage.Local, new float[]
                        {
                            0.5703109f, -0.6308292f, 0.5261179f,
                            0.8214105f, 0.4336779f, -0.3704164f,
                            0.0055037f, 0.6434113f,  0.7655009f
                        }),//rotates the octaves by x37, y19, z53 degrees
                        new ParameterDescriptor("freq", TYPE.Float, Usage.Local, new float[] {1.0f}),
                        new ParameterDescriptor("amp", TYPE.Float, Usage.Local, new float[] {0.5f}),
                        new ParameterDescriptor("sum", TYPE.Float, Usage.Local, new float[] {0.0f}),
                        new ParameterDescriptor("prev", TYPE.Float, Usage.Local, new float[] {1.0f}),
                        new ParameterDescriptor("x", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("n", TYPE.Float, Usage.Local, new float[] {0.0f}),
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out),
                    }
                )
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "creates 3d noise based on the selected options",
            category: "Procedural/Noise",
            displayName: "Noise 3D",
            synonyms: new string[5] { "perlin", "gradient", "value", "octave", "worley" },
            description: "pkg://Documentation~/previews/Noise3D.md",
            selectableFunctions: new Dictionary<string, string>
            {
                { "ValueTexFBM", "Value Texture Fractal Brownian Motion" },
                { "ValueTexTurbulence", "Value Texture Turbulence" },
                { "ValueTexRidgedMF", "Value Texture Ridged Multifractal" },
                { "GradientTexFBM", "Gradient Texture Fractal Brownian Motion" },
                { "GradientTexTurbulence", "Gradient Texture Turbulence" },
                { "GradientTexRidgedMF", "Gradient Texture Ridged Multifractal" },
                { "ValueFBM", "Value Math Fractal Brownian Motion" },
                { "ValueTurbulence", "Value Math Turbulence" },
                { "ValueRidgedMF", "Value Math Ridged Multifractal" },
                { "SimplexFBM", "Simplex Math Fractal Brownian Motion" },
                { "SimplexTurbulence", "Simplex Math Turbulence" },
                { "SimplexRidgedMF", "Simplex Math Ridged Multifractal" },
                { "GradientFBM", "Gradient Math Fractal Brownian Motion" },
                { "GradientTurbulence", "Gradient Math Turbulence" },
                { "GradientRidgedMF", "Gradient Math Ridged Multifractal" },
                { "WorleyFBM", "Worley Math Fractal Brownian Motion" },
                { "WorleyTurbulence", "Worley Math Turbulence" },
                { "WorleyRidgedMF", "Worley Math Ridged Multifractal" },

            },
            functionSelectorLabel: "Noise Type",
            parameters: new ParameterUIDescriptor[11] {
                new ParameterUIDescriptor(
                    name: "Position",
                    tooltip: "the coordinates used to create the noise",
                    options: REF.OptionList.Positions
                ),
                new ParameterUIDescriptor(
                    name: "Scale",
                    tooltip: "controls the size of the noise."
                ),
                new ParameterUIDescriptor(
                    name: "Ocataves",
                    tooltip: "the number of times to repeat the noise algorithm. More octaves creates more detail and more expensive noise."
                ),
                new ParameterUIDescriptor(
                    name: "Lacunarity",
                    tooltip: "the scale adjustment between each octave"
                ),
                new ParameterUIDescriptor(
                    name: "Gain",
                    tooltip: "the contrast adjustment between each octave"
                ),
                new ParameterUIDescriptor(
                    name: "Offset",
                    tooltip: "controls the brightess or height of the multifractal ridges"
                ),
                new ParameterUIDescriptor(
                    name: "GradientVolume",
                    displayName: "Gradient Volume",
                    tooltip: "the 3d texture to use for each octave instead of using math"
                ),
                new ParameterUIDescriptor(
                    name: "NoiseHashTexture",
                    displayName: "Noise Hash Texture",
                    tooltip: "the texture to use as a hash instead of using math"
                ),
                new ParameterUIDescriptor(
                    name: "RotateOctaves",
                    displayName: "Random Octave Rotation",
                    tooltip: "when true, each octave is rotated slightly to create better variation"
                ),
                new ParameterUIDescriptor(
                    name: "sRGBOutput",
                    displayName: "sRGB",
                    tooltip: "when true, the output is in sRGB space instead of linear"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    displayName: string.Empty,
                    tooltip: "a smooth, non-tiling noise pattern using the selected options"
                )
            }
        );
    }
}
