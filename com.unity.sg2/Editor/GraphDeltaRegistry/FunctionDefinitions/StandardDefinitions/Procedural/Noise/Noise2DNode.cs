using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class Noise2DNode : IStandardNode
    {
        static string Name = "Noise2D";
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
    g = f * f * (3.0 - 2.0 * f);
    Hash_Tchou_2_1_float(i + float2(0, 0), s);
    Hash_Tchou_2_1_float(i + float2(1, 0), t);
    Hash_Tchou_2_1_float(i + float2(0, 1), u);
    Hash_Tchou_2_1_float(i + float2(1, 1), v);
    Out = lerp(lerp(s, t, g.x),
               lerp(u, v, g.x), g.y) * 2 - 1;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("p", TYPE.Vec2, Usage.In),
                        new ParameterDescriptor("i", TYPE.Vec2, Usage.Local),
                        new ParameterDescriptor("f", TYPE.Vec2, Usage.Local),
                        new ParameterDescriptor("g", TYPE.Vec2, Usage.Local),
                        new ParameterDescriptor("s", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("t", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("u", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("v", TYPE.Float, Usage.Local),
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
    w = f * f * (3.0 - 2.0 * f);
    uv = (i+float2(37.0,-17.0)) + f;
    hashTexture((uv+0.5)/256.0, NoiseHashTexture, hash);
    Out = hash.x * 2 - 1;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("p", TYPE.Vec2, Usage.In),
                        new ParameterDescriptor("NoiseHashTexture", TYPE.Texture2D, Usage.In),
                        new ParameterDescriptor("i", TYPE.Vec2, Usage.Local),
                        new ParameterDescriptor("f", TYPE.Vec2, Usage.Local),
                        new ParameterDescriptor("w", TYPE.Vec2, Usage.Local),
                        new ParameterDescriptor("uv", TYPE.Vec2, Usage.Local),
                        new ParameterDescriptor("hash", TYPE.Vec4, Usage.Local),
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out)
                    },
                    isHelper: true
                ),
                new(
                    "gradientNoise",
@"   i = floor(p);
    f = p - i;
    g = f * f * (3.0 - 2.0 * f);
    Hash_Tchou_2_2_float(i + float2(0, 0), s);
    Hash_Tchou_2_2_float(i + float2(1, 0), t);
    Hash_Tchou_2_2_float(i + float2(0, 1), u);
    Hash_Tchou_2_2_float(i + float2(1, 1), v);
    Out = lerp( lerp( dot( s*2-1, f - float2(0.0,0.0) ), 
					  dot( t*2-1, f - float2(1.0,0.0) ), g.x),
				 lerp(dot( u*2-1, f - float2(0.0,1.0) ), 
				      dot( v*2-1, f - float2(1.0,1.0) ), g.x), g.y);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("p", TYPE.Vec2, Usage.In),
                        new ParameterDescriptor("i", TYPE.Vec2, Usage.Local),
                        new ParameterDescriptor("f", TYPE.Vec2, Usage.Local),
                        new ParameterDescriptor("g", TYPE.Vec2, Usage.Local),
                        new ParameterDescriptor("s", TYPE.Vec2, Usage.Local),
                        new ParameterDescriptor("t", TYPE.Vec2, Usage.Local),
                        new ParameterDescriptor("u", TYPE.Vec2, Usage.Local),
                        new ParameterDescriptor("v", TYPE.Vec2, Usage.Local),
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
            offset = float2(x, y);
            Hash_Tchou_2_2_float(floor(p) + offset, h);
            h = (h*2-1) * .4 + .3; // [.3, .7]
            h += offset;
            d = frac(p) - h;
            minDist = min(minDist, dot(d, d));
        }
    }
    Out = minDist*2-1;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("p", TYPE.Vec2, Usage.In),
                        new ParameterDescriptor("h", TYPE.Vec2, Usage.Local),
                        new ParameterDescriptor("offset", TYPE.Vec2, Usage.Local),
                        new ParameterDescriptor("d", TYPE.Vec2, Usage.Local),
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
                    "ValueFBM",
@"   UV *= Scale;
    for(int i=0; i<Octaves; i++) {
        valueNoise(UV*freq, x);
        if (RotateOctaves) UV = mul(UV, randRotMat);
        sum += x*amp;
	    freq *= Lacunarity;
	    amp *= Gain;
    }
    Out = sum+0.5;
    if (sRGBOutput) Out = pow(Out, 2.2);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("UV", TYPE.Vec2, Usage.In, REF.UV0),
                        new ParameterDescriptor("Scale", TYPE.Vec2, Usage.In, new float[] { 10.0f, 10.0f }),
                        new ParameterDescriptor("Octaves", TYPE.Int, Usage.In, new float[] {3}),
                        new ParameterDescriptor("Lacunarity", TYPE.Float, Usage.In, new float[] {2.0f}),
                        new ParameterDescriptor("Gain", TYPE.Float, Usage.In, new float[] {0.5f}),
                        new ParameterDescriptor("RotateOctaves", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("sRGBOutput", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("randRotMat", TYPE.Mat2, Usage.Local, new float[] { 0.7986f, -0.6018f, 0.6018f, 0.7986f }),//matrix rotates 37 degrees
                        new ParameterDescriptor("freq", TYPE.Float, Usage.Local, new float[] {1.0f}),
                        new ParameterDescriptor("amp", TYPE.Float, Usage.Local, new float[] {0.5f}),
                        new ParameterDescriptor("sum", TYPE.Float, Usage.Local, new float[] {0.0f}),
                        new ParameterDescriptor("x", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out),
                    }
                ),
                new(
                    "ValueTurbulence",
@"   UV *= Scale;
    for(int i=0; i<Octaves; i++) {
        valueNoise(UV*freq, x);
        if (RotateOctaves) UV = mul(UV, randRotMat);
        sum += abs(x)*amp;
        freq *= Lacunarity;
        amp *= Gain;
    }
    Out = sum;
    if (sRGBOutput) Out = pow(Out, 2.2);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("UV", TYPE.Vec2, Usage.In, REF.UV0),
                        new ParameterDescriptor("Scale", TYPE.Vec2, Usage.In, new float[] { 10.0f, 10.0f }),
                        new ParameterDescriptor("Octaves", TYPE.Int, Usage.In, new float[] {3}),
                        new ParameterDescriptor("Lacunarity", TYPE.Float, Usage.In, new float[] {2.0f}),
                        new ParameterDescriptor("Gain", TYPE.Float, Usage.In, new float[] {0.5f}),
                        new ParameterDescriptor("RotateOctaves", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("sRGBOutput", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("randRotMat", TYPE.Mat2, Usage.Local, new float[] { 0.7986f, -0.6018f, 0.6018f, 0.7986f }),//matrix rotates 37 degrees
                        new ParameterDescriptor("freq", TYPE.Float, Usage.Local, new float[] {1.0f}),
                        new ParameterDescriptor("amp", TYPE.Float, Usage.Local, new float[] {1.0f}),
                        new ParameterDescriptor("sum", TYPE.Float, Usage.Local, new float[] {0.0f}),
                        new ParameterDescriptor("x", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out),
                    }
                ),
                new(
                    "ValueRidgedMF",
@"   UV *= Scale;
    for(int i=0; i<Octaves; i++) {
        valueNoise(UV*freq, x);
        if (RotateOctaves) UV = mul(UV, randRotMat);
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
                        new ParameterDescriptor("UV", TYPE.Vec2, Usage.In, REF.UV0),
                        new ParameterDescriptor("Scale", TYPE.Vec2, Usage.In, new float[] { 10.0f, 10.0f }),
                        new ParameterDescriptor("Octaves", TYPE.Int, Usage.In, new float[] {3}),
                        new ParameterDescriptor("Lacunarity", TYPE.Float, Usage.In, new float[] {2.0f}),
                        new ParameterDescriptor("Gain", TYPE.Float, Usage.In, new float[] {0.5f}),
                        new ParameterDescriptor("Offset", TYPE.Float, Usage.In, new float[] {1.0f}),
                        new ParameterDescriptor("RotateOctaves", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("sRGBOutput", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("randRotMat", TYPE.Mat2, Usage.Local, new float[] { 0.7986f, -0.6018f, 0.6018f, 0.7986f }),//matrix rotates 37 degrees
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
@"   UV *= Scale;
    for(int i=0; i<Octaves; i++) {
        gradientNoise(UV*freq, x);
        if (RotateOctaves) UV = mul(UV, randRotMat);
        sum += x*amp;
	    freq *= Lacunarity;
	    amp *= Gain;
    }
    Out = sum+0.5;
    if (sRGBOutput) Out = pow(Out, 2.2);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("UV", TYPE.Vec2, Usage.In, REF.UV0),
                        new ParameterDescriptor("Scale", TYPE.Vec2, Usage.In, new float[] { 10.0f, 10.0f }),
                        new ParameterDescriptor("Octaves", TYPE.Int, Usage.In, new float[] {3}),
                        new ParameterDescriptor("Lacunarity", TYPE.Float, Usage.In, new float[] {2.0f}),
                        new ParameterDescriptor("Gain", TYPE.Float, Usage.In, new float[] {0.5f}),
                        new ParameterDescriptor("RotateOctaves", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("sRGBOutput", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("randRotMat", TYPE.Mat2, Usage.Local, new float[] { 0.7986f, -0.6018f, 0.6018f, 0.7986f }),//matrix rotates 37 degrees
                        new ParameterDescriptor("freq", TYPE.Float, Usage.Local, new float[] {1.0f}),
                        new ParameterDescriptor("amp", TYPE.Float, Usage.Local, new float[] {0.5f}),
                        new ParameterDescriptor("sum", TYPE.Float, Usage.Local, new float[] {0.0f}),
                        new ParameterDescriptor("x", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out),
                    }
                ),
                new(
                    "GradientTurbulence",
@"   UV *= Scale;
    for(int i=0; i<Octaves; i++) {
        gradientNoise(UV*freq, x);
        if (RotateOctaves) UV = mul(UV, randRotMat);
        sum += abs(x)*amp;
        freq *= Lacunarity;
        amp *= Gain;
    }
    Out = sum;
    if (sRGBOutput) Out = pow(Out, 2.2);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("UV", TYPE.Vec2, Usage.In, REF.UV0),
                        new ParameterDescriptor("Scale", TYPE.Vec2, Usage.In, new float[] { 10.0f, 10.0f }),
                        new ParameterDescriptor("Octaves", TYPE.Int, Usage.In, new float[] {3}),
                        new ParameterDescriptor("Lacunarity", TYPE.Float, Usage.In, new float[] {2.0f}),
                        new ParameterDescriptor("Gain", TYPE.Float, Usage.In, new float[] {0.5f}),
                        new ParameterDescriptor("RotateOctaves", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("sRGBOutput", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("randRotMat", TYPE.Mat2, Usage.Local, new float[] { 0.7986f, -0.6018f, 0.6018f, 0.7986f }),//matrix rotates 37 degrees
                        new ParameterDescriptor("freq", TYPE.Float, Usage.Local, new float[] {1.0f}),
                        new ParameterDescriptor("amp", TYPE.Float, Usage.Local, new float[] {1.0f}),
                        new ParameterDescriptor("sum", TYPE.Float, Usage.Local, new float[] {0.0f}),
                        new ParameterDescriptor("x", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out),
                    }
                ),
                new(
                    "GradientRidgedMF",
@"   UV *= Scale;
    for(int i=0; i<Octaves; i++) {
        gradientNoise(UV*freq, x);
        if (RotateOctaves) UV = mul(UV, randRotMat);
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
                        new ParameterDescriptor("UV", TYPE.Vec2, Usage.In, REF.UV0),
                        new ParameterDescriptor("Scale", TYPE.Vec2, Usage.In, new float[] { 10.0f, 10.0f }),
                        new ParameterDescriptor("Octaves", TYPE.Int, Usage.In, new float[] {3}),
                        new ParameterDescriptor("Lacunarity", TYPE.Float, Usage.In, new float[] {2.0f}),
                        new ParameterDescriptor("Gain", TYPE.Float, Usage.In, new float[] {0.5f}),
                        new ParameterDescriptor("Offset", TYPE.Float, Usage.In, new float[] {1.0f}),
                        new ParameterDescriptor("RotateOctaves", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("sRGBOutput", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("randRotMat", TYPE.Mat2, Usage.Local, new float[] { 0.7986f, -0.6018f, 0.6018f, 0.7986f }),//matrix rotates 37 degrees
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
@"   UV *= Scale;
    for(int i=0; i<Octaves; i++) {
        worleyNoise(UV*freq, x);
        if (RotateOctaves) UV = mul(UV, randRotMat);
        sum += x*amp;
	    freq *= Lacunarity;
	    amp *= Gain;
    }
    Out = sum*0.5+0.5;
    if (sRGBOutput) Out = pow(Out, 2.2);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("UV", TYPE.Vec2, Usage.In, REF.UV0),
                        new ParameterDescriptor("Scale", TYPE.Vec2, Usage.In, new float[] { 10.0f, 10.0f }),
                        new ParameterDescriptor("Octaves", TYPE.Int, Usage.In, new float[] {3}),
                        new ParameterDescriptor("Lacunarity", TYPE.Float, Usage.In, new float[] {2.0f}),
                        new ParameterDescriptor("Gain", TYPE.Float, Usage.In, new float[] {0.5f}),
                        new ParameterDescriptor("RotateOctaves", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("sRGBOutput", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("randRotMat", TYPE.Mat2, Usage.Local, new float[] { 0.7986f, -0.6018f, 0.6018f, 0.7986f }),//matrix rotates 37 degrees
                        new ParameterDescriptor("freq", TYPE.Float, Usage.Local, new float[] {1.0f}),
                        new ParameterDescriptor("amp", TYPE.Float, Usage.Local, new float[] {0.5f}),
                        new ParameterDescriptor("sum", TYPE.Float, Usage.Local, new float[] {0.0f}),
                        new ParameterDescriptor("x", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out),
                    }
                ),
                new(
                    "WorleyTurbulence",
@"   UV *= Scale;
    for(int i=0; i<Octaves; i++) {
        worleyNoise(UV*freq, x);
        if (RotateOctaves) UV = mul(UV, randRotMat);
        sum += abs(x)*amp;
        freq *= Lacunarity;
        amp *= Gain;
    }
    Out = sum*0.5;
    if (sRGBOutput) Out = pow(Out, 2.2);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("UV", TYPE.Vec2, Usage.In, REF.UV0),
                        new ParameterDescriptor("Scale", TYPE.Vec2, Usage.In, new float[] { 10.0f, 10.0f }),
                        new ParameterDescriptor("Octaves", TYPE.Int, Usage.In, new float[] {3}),
                        new ParameterDescriptor("Lacunarity", TYPE.Float, Usage.In, new float[] {2.0f}),
                        new ParameterDescriptor("Gain", TYPE.Float, Usage.In, new float[] {0.5f}),
                        new ParameterDescriptor("RotateOctaves", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("sRGBOutput", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("randRotMat", TYPE.Mat2, Usage.Local, new float[] { 0.7986f, -0.6018f, 0.6018f, 0.7986f }),//matrix rotates 37 degrees
                        new ParameterDescriptor("freq", TYPE.Float, Usage.Local, new float[] {1.0f}),
                        new ParameterDescriptor("amp", TYPE.Float, Usage.Local, new float[] {1.0f}),
                        new ParameterDescriptor("sum", TYPE.Float, Usage.Local, new float[] {0.0f}),
                        new ParameterDescriptor("x", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out),
                    }
                ),
                new(
                    "WorleyRidgedMF",
@"   UV *= Scale;
    for(int i=0; i<Octaves; i++) {
        worleyNoise(UV*freq, x);
        if (RotateOctaves) UV = mul(UV, randRotMat);
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
                        new ParameterDescriptor("UV", TYPE.Vec2, Usage.In, REF.UV0),
                        new ParameterDescriptor("Scale", TYPE.Vec2, Usage.In, new float[] { 10.0f, 10.0f }),
                        new ParameterDescriptor("Octaves", TYPE.Int, Usage.In, new float[] {3}),
                        new ParameterDescriptor("Lacunarity", TYPE.Float, Usage.In, new float[] {2.0f}),
                        new ParameterDescriptor("Gain", TYPE.Float, Usage.In, new float[] {0.5f}),
                        new ParameterDescriptor("Offset", TYPE.Float, Usage.In, new float[] {1.0f}),
                        new ParameterDescriptor("RotateOctaves", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("sRGBOutput", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("randRotMat", TYPE.Mat2, Usage.Local, new float[] { 0.7986f, -0.6018f, 0.6018f, 0.7986f }),//matrix rotates 37 degrees
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
@"   UV *= Scale;
    for(int i=0; i<Octaves; i++) {
        valueTextureNoise(UV*freq, NoiseHashTexture, x);
        if (RotateOctaves) UV = mul(UV, randRotMat);
        sum += x*amp;
	    freq *= Lacunarity;
	    amp *= Gain;
    }
    Out = sum+0.5;
    if (sRGBOutput) Out = pow(Out, 2.2);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("UV", TYPE.Vec2, Usage.In, REF.UV0),
                        new ParameterDescriptor("Scale", TYPE.Vec2, Usage.In, new float[] { 10.0f, 10.0f }),
                        new ParameterDescriptor("Octaves", TYPE.Int, Usage.In, new float[] {3}),
                        new ParameterDescriptor("Lacunarity", TYPE.Float, Usage.In, new float[] {2.0f}),
                        new ParameterDescriptor("Gain", TYPE.Float, Usage.In, new float[] {0.5f}),
                        new ParameterDescriptor("NoiseHashTexture", TYPE.Texture2D, Usage.In),
                        new ParameterDescriptor("RotateOctaves", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("sRGBOutput", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("randRotMat", TYPE.Mat2, Usage.Local, new float[] { 0.7986f, -0.6018f, 0.6018f, 0.7986f }),//matrix rotates 37 degrees
                        new ParameterDescriptor("freq", TYPE.Float, Usage.Local, new float[] {1.0f}),
                        new ParameterDescriptor("amp", TYPE.Float, Usage.Local, new float[] {0.5f}),
                        new ParameterDescriptor("sum", TYPE.Float, Usage.Local, new float[] {0.0f}),
                        new ParameterDescriptor("x", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out),
                    }
                ),
                new(
                    "ValueTexTurbulence",
@"   UV *= Scale;
    for(int i=0; i<Octaves; i++) {
        valueTextureNoise(UV*freq, NoiseHashTexture, x);
        if (RotateOctaves) UV = mul(UV, randRotMat);
        sum += abs(x)*amp;
	    freq *= Lacunarity;
	    amp *= Gain;
    }
    Out = sum;
    if (sRGBOutput) Out = pow(Out, 2.2);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("UV", TYPE.Vec2, Usage.In, REF.UV0),
                        new ParameterDescriptor("Scale", TYPE.Vec2, Usage.In, new float[] { 10.0f, 10.0f }),
                        new ParameterDescriptor("Octaves", TYPE.Int, Usage.In, new float[] {3}),
                        new ParameterDescriptor("Lacunarity", TYPE.Float, Usage.In, new float[] {2.0f}),
                        new ParameterDescriptor("Gain", TYPE.Float, Usage.In, new float[] {0.5f}),
                        new ParameterDescriptor("NoiseHashTexture", TYPE.Texture2D, Usage.In),
                        new ParameterDescriptor("RotateOctaves", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("sRGBOutput", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("randRotMat", TYPE.Mat2, Usage.Local, new float[] { 0.7986f, -0.6018f, 0.6018f, 0.7986f }),//matrix rotates 37 degrees
                        new ParameterDescriptor("freq", TYPE.Float, Usage.Local, new float[] {1.0f}),
                        new ParameterDescriptor("amp", TYPE.Float, Usage.Local, new float[] {1.0f}),
                        new ParameterDescriptor("sum", TYPE.Float, Usage.Local, new float[] {0.0f}),
                        new ParameterDescriptor("x", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out),
                    }
                ),
                new(
                    "ValueTexRidgedMF",
@"   UV *= Scale;
    for(int i=0; i<Octaves; i++) {
        valueTextureNoise(UV*freq, NoiseHashTexture, x);
        if (RotateOctaves) UV = mul(UV, randRotMat);
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
                        new ParameterDescriptor("UV", TYPE.Vec2, Usage.In, REF.UV0),
                        new ParameterDescriptor("Scale", TYPE.Vec2, Usage.In, new float[] { 10.0f, 10.0f }),
                        new ParameterDescriptor("Octaves", TYPE.Int, Usage.In, new float[] {3}),
                        new ParameterDescriptor("Lacunarity", TYPE.Float, Usage.In, new float[] {2.0f}),
                        new ParameterDescriptor("Gain", TYPE.Float, Usage.In, new float[] {0.5f}),
                        new ParameterDescriptor("Offset", TYPE.Float, Usage.In, new float[] {1.0f}),
                        new ParameterDescriptor("NoiseHashTexture", TYPE.Texture2D, Usage.In),
                        new ParameterDescriptor("RotateOctaves", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("sRGBOutput", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("randRotMat", TYPE.Mat2, Usage.Local, new float[] { 0.7986f, -0.6018f, 0.6018f, 0.7986f }),//matrix rotates 37 degrees
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
@"    UV = 0.1 * UV * Scale; //adjust for texture scale - so it matches the others
    for(int i=0; i<Octaves; i++) {
        sum += (SAMPLE_TEXTURE2D_LOD(GradientTexture.tex, GradientTexture.samplerstate, UV*freq, 0).x * 2 -1) * amp;
        if (RotateOctaves) UV = mul(UV, randRotMat);
	    freq *= Lacunarity;
	    amp *= Gain;
    }
    Out = sum+0.5;
    if (sRGBOutput) Out = pow(Out, 2.2);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("UV", TYPE.Vec2, Usage.In, REF.UV0),
                        new ParameterDescriptor("Scale", TYPE.Vec2, Usage.In, new float[] { 10.0f, 10.0f }),
                        new ParameterDescriptor("Octaves", TYPE.Int, Usage.In, new float[] {3}),
                        new ParameterDescriptor("Lacunarity", TYPE.Float, Usage.In, new float[] {2.0f}),
                        new ParameterDescriptor("Gain", TYPE.Float, Usage.In, new float[] {0.5f}),
                        new ParameterDescriptor("GradientTexture", TYPE.Texture2D, Usage.In),
                        new ParameterDescriptor("RotateOctaves", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("sRGBOutput", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("randRotMat", TYPE.Mat2, Usage.Local, new float[] { 0.7986f, -0.6018f, 0.6018f, 0.7986f }),//matrix rotates 37 degrees
                        new ParameterDescriptor("freq", TYPE.Float, Usage.Local, new float[] {1.0f}),
                        new ParameterDescriptor("amp", TYPE.Float, Usage.Local, new float[] {0.5f}),
                        new ParameterDescriptor("sum", TYPE.Float, Usage.Local, new float[] {0.0f}),
                        new ParameterDescriptor("x", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out),
                    }
                ),
                new(
                    "GradientTexTurbulence",
@"    UV = 0.1 * UV * Scale; //adjust for texture scale - so it matches the others
    for(int i=0; i<Octaves; i++) {
        sum += abs(SAMPLE_TEXTURE2D_LOD(GradientTexture.tex, GradientTexture.samplerstate, UV*freq, 0).x * 2 -1)*amp;
        if (RotateOctaves) UV = mul(UV, randRotMat);
        freq *= Lacunarity;
        amp *= Gain;
    }
    Out = sum;
    if (sRGBOutput) Out = pow(Out, 2.2);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("UV", TYPE.Vec2, Usage.In, REF.UV0),
                        new ParameterDescriptor("Scale", TYPE.Vec2, Usage.In, new float[] { 10.0f, 10.0f }),
                        new ParameterDescriptor("Octaves", TYPE.Int, Usage.In, new float[] {3}),
                        new ParameterDescriptor("Lacunarity", TYPE.Float, Usage.In, new float[] {2.0f}),
                        new ParameterDescriptor("Gain", TYPE.Float, Usage.In, new float[] {0.5f}),
                        new ParameterDescriptor("GradientTexture", TYPE.Texture2D, Usage.In),
                        new ParameterDescriptor("RotateOctaves", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("sRGBOutput", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("randRotMat", TYPE.Mat2, Usage.Local, new float[] { 0.7986f, -0.6018f, 0.6018f, 0.7986f }),//matrix rotates 37 degrees
                        new ParameterDescriptor("freq", TYPE.Float, Usage.Local, new float[] {1.0f}),
                        new ParameterDescriptor("amp", TYPE.Float, Usage.Local, new float[] {1.0f}),
                        new ParameterDescriptor("sum", TYPE.Float, Usage.Local, new float[] {0.0f}),
                        new ParameterDescriptor("x", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out),
                    }
                ),
                new(
                    "GradientTexRidgedMF",
@"    UV = 0.1 * UV * Scale; //adjust for texture scale - so it matches the others
    for(int i=0; i<Octaves; i++) {
        ridge(SAMPLE_TEXTURE2D_LOD(GradientTexture.tex, GradientTexture.samplerstate, UV*freq, 0).x * 2 -1, Offset, n);
        if (RotateOctaves) UV = mul(UV, randRotMat);
		sum += n*amp*prev;
		prev = n;
		freq *= Lacunarity;
		amp *= Gain;
	}
    Out = sum;
    if (sRGBOutput) Out = pow(Out, 2.2);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("UV", TYPE.Vec2, Usage.In, REF.UV0),
                        new ParameterDescriptor("Scale", TYPE.Vec2, Usage.In, new float[] { 10.0f, 10.0f }),
                        new ParameterDescriptor("Octaves", TYPE.Int, Usage.In, new float[] {3}),
                        new ParameterDescriptor("Lacunarity", TYPE.Float, Usage.In, new float[] {2.0f}),
                        new ParameterDescriptor("Gain", TYPE.Float, Usage.In, new float[] {0.5f}),
                        new ParameterDescriptor("Offset", TYPE.Float, Usage.In, new float[] {1.0f}),
                        new ParameterDescriptor("GradientTexture", TYPE.Texture2D, Usage.In),
                        new ParameterDescriptor("RotateOctaves", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("sRGBOutput", TYPE.Bool, Usage.Static),
                        new ParameterDescriptor("randRotMat", TYPE.Mat2, Usage.Local, new float[] { 0.7986f, -0.6018f, 0.6018f, 0.7986f }),//matrix rotates 37 degrees
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
            tooltip: "creates 2d noise based on the selected options",
            category: "Procedural/Noise",
            displayName: "Noise 2D",
            synonyms: new string[5] { "perlin", "gradient", "value", "octave", "worley" },
            selectableFunctions: new()
            {
                { "ValueFBM", "Value Math Fractal Brownian Motion" },
                { "ValueTurbulence", "Value Math Turbulence" },
                { "ValueRidgedMF", "Value Math Ridged Multifractal" },
                { "GradientFBM", "Gradient Math Fractal Brownian Motion" },
                { "GradientTurbulence", "Gradient Math Turbulence" },
                { "GradientRidgedMF", "Gradient Math Ridged Multifractal" },
                { "WorleyFBM", "Worley Math Fractal Brownian Motion" },
                { "WorleyTurbulence", "Worley Math Turbulence" },
                { "WorleyRidgedMF", "Worley Math Ridged Multifractal" },
                { "ValueTexFBM", "Value Texture Fractal Brownian Motion" },
                { "ValueTexTurbulence", "Value Texture Turbulence" },
                { "ValueTexRidgedMF", "Value Texture Ridged Multifractal" },
                { "GradientTexFBM", "Gradient Texture Fractal Brownian Motion" },
                { "GradientTexTurbulence", "Gradient Texture Turbulence" },
                { "GradientTexRidgedMF", "Gradient Texture Ridged Multifractal" }
            },
            functionSelectorLabel: "Noise Type",
            parameters: new ParameterUIDescriptor[11] {
                new ParameterUIDescriptor(
                    name: "UV",
                    tooltip: "the coordinates used to create the noise",
                    options: REF.OptionList.UVs
                ),
                new ParameterUIDescriptor(
                    name: "Scale",
                    tooltip: "controls the size of the noise"
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
                    name: "GradientTexture",
                    displayName: "Gradient Texture",
                    tooltip: "the texture to use for each octave instead of using math"
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
                    tooltip: "a smooth, non-tiling noise pattern using the selected options"
                )
            }
        );
    }
}
