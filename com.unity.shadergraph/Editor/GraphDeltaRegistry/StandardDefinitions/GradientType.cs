

using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;
using com.unity.shadergraph.defs;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Registry.Types
{

    internal class SampleGradientNode : Defs.INodeDefinitionBuilder
    {
        public RegistryKey GetRegistryKey() => new RegistryKey { Name = "SampleGradientNode", Version = 1 };
        public RegistryFlags GetRegistryFlags() => RegistryFlags.Func;

        public const string kGradient = "Gradient";
        public const string kTime = "Time";
        public const string kOutput = "Out";

        public void BuildNode(INodeReader userData, INodeWriter generatedData, Registry registry)
        {
            generatedData.AddPort<GradientType>(userData, kGradient, true, registry);

            // setup a float1 for time port.
            var time = generatedData.AddPort<GraphType>(userData, kTime, true, registry);
            time.SetField(GraphType.kPrecision, GraphType.Precision.Single);
            time.SetField(GraphType.kPrimitive, GraphType.Primitive.Float);
            time.SetField(GraphType.kLength, GraphType.Length.One);
            time.SetField(GraphType.kHeight, GraphType.Height.One);

            // default for GraphType is a float4.
            generatedData.AddPort<GraphType>(userData, kOutput, false, registry);
        }

        private void PortToParam(string name, INodeReader node, ShaderFoundry.ShaderFunction.Builder builder, ShaderFoundry.ShaderContainer container, Registry registry)
        {
            node.TryGetPort(name, out var port);
            var shaderType = registry.GetShaderType((IFieldReader)port, container);
            if (port.IsInput())
                builder.AddInput(shaderType, name);
            else builder.AddOutput(shaderType, name);
        }

        public ShaderFoundry.ShaderFunction GetShaderFunction(INodeReader data, ShaderFoundry.ShaderContainer container, Registry registry)
        {
            var shaderFunctionBuilder = new ShaderFoundry.ShaderFunction.Builder(container, GetRegistryKey().Name);

            PortToParam(kGradient, data, shaderFunctionBuilder, container, registry);
            PortToParam(kTime, data, shaderFunctionBuilder, container, registry);
            PortToParam(kOutput, data, shaderFunctionBuilder, container, registry);

            var body =
@"float3 color = Gradient.colors[0].rgb;
    [unroll]
    for (int c = 1; c < Gradient.colorsLength; c++)
    {
        float colorPos = saturate((Time - Gradient.colors[c - 1].w) / (Gradient.colors[c].w - Gradient.colors[c - 1].w)) * step(c, Gradient.colorsLength - 1);
        color = lerp(color, Gradient.colors[c].rgb, lerp(colorPos, step(0.01, colorPos), Gradient.type));
    }
#ifdef UNITY_COLORSPACE_GAMMA
    color = LinearToSRGB(color);
#endif
    float alpha = Gradient.alphas[0].x;
    [unroll]
    for (int a = 1; a < Gradient.alphasLength; a++)
    {
        float alphaPos = saturate((Time - Gradient.alphas[a - 1].y) / (Gradient.alphas[a].y - Gradient.alphas[a - 1].y)) * step(a, Gradient.alphasLength - 1);
        alpha = lerp(alpha, Gradient.alphas[a].x, lerp(alphaPos, step(0.01, alphaPos), Gradient.type));
    }
    Out = float4(color, alpha);";

            shaderFunctionBuilder.AddLine(body);
            return shaderFunctionBuilder.Build();
        }
    }


    /// <summary>
    /// Constructor node with a static gradient type input; this is purely so that the gradient UI Widget has a node to use.
    /// </summary>
    internal class GradientNode : Defs.INodeDefinitionBuilder
    {
        public RegistryKey GetRegistryKey() => new RegistryKey { Name = "GradientNode", Version = 1 };
        public RegistryFlags GetRegistryFlags() => RegistryFlags.Func;

        public const string kInlineStatic = "InlineStatic";
        public const string kOutput = "Out";

        public void BuildNode(INodeReader userData, INodeWriter generatedData, Registry registry)
        {
            var input = generatedData.AddPort<GradientType>(userData, kInlineStatic, true, registry);
            input.SetField<bool>("IsStatic", true); // TODO: This is just the hint for UI to use the large gradient editor.
            var output = generatedData.AddPort<GradientType>(userData, kOutput, false, registry);
        }

        public ShaderFoundry.ShaderFunction GetShaderFunction(INodeReader data, ShaderFoundry.ShaderContainer container, Registry registry)
        {
            var shaderFunctionBuilder = new ShaderFoundry.ShaderFunction.Builder(container, GetRegistryKey().Name);
            data.TryGetPort(kInlineStatic, out var port);

            var shaderType = registry.GetShaderType((IFieldReader)port, container);
            shaderFunctionBuilder.AddInput(shaderType, kInlineStatic);
            shaderFunctionBuilder.AddOutput(shaderType, kOutput);
            shaderFunctionBuilder.AddLine($"{kOutput} = {kInlineStatic};");
            return shaderFunctionBuilder.Build();
        }
    }

    public static class GradientTypeHelpers
    {
        public static Gradient GetGradient(IFieldReader field)
        {
            field.GetField<GradientMode>(GradientType.kGradientMode, out var mode);
            field.GetField<int>(GradientType.kColorCount, out var colorCount);
            field.GetField<int>(GradientType.kAlphaCount, out var alphaCount);

            List<GradientColorKey> colors = new();
            List<GradientAlphaKey> alphas = new();
            for (int i = 0; i < colorCount; ++i)
            {
                field.GetField(GradientType.kColor(i), out GradientColorKey colorKey);
                colors.Add(colorKey);
            }
            for (int i = 0; i < alphaCount; ++i)
            {
                field.GetField(GradientType.kAlpha(i), out GradientAlphaKey alphaKey);
                alphas.Add(alphaKey);
            }

            var result = new Gradient();
            result.mode = mode;
            result.SetKeys(colors.ToArray(), alphas.ToArray());
            return result;
        }

        public static void SetGradient(IFieldWriter field, Gradient gradient)
        {
            field.SetField(GradientType.kGradientMode, gradient.mode);
            field.SetField(GradientType.kColorCount, gradient.colorKeys.Length);
            field.SetField(GradientType.kAlphaCount, gradient.alphaKeys.Length);

            for (int i = 0; i < 8 && i < gradient.colorKeys.Length; ++i)
                field.SetField(GradientType.kColor(i), gradient.colorKeys[i]);

            for (int i = 0; i < 8 && i < gradient.alphaKeys.Length; ++i)
                field.SetField(GradientType.kAlpha(i), gradient.alphaKeys[i]);
        }
    }

    /// <summary>
    /// Represents the Gradient type found in Functions.hlsl, similar to the C# version, except it's key count is fixed to 8.
    /// </summary>
    internal class GradientType : Defs.ITypeDefinitionBuilder
    {
        public static RegistryKey kRegistryKey => new RegistryKey { Name = "GradientType", Version = 1 };
        public RegistryKey GetRegistryKey() => kRegistryKey;
        public RegistryFlags GetRegistryFlags() => RegistryFlags.Type;


        #region LocalNames
        // These types map directly from the Gradient class, maybe there is a more procedural way to go from simple classes -> CLDS Safe container fields?
        public const string kGradientMode = "GradientMode"; // GradientMode
        public const string kColorCount = "ColorCount";     // int
        public const string kAlphaCount = "AlphaCount";     // int
        public static string kColor(int i) => $"Color{i}";         // GradientColorKey
        public static string kAlpha(int i) => $"Alpha{i}";         // GardientAlphaKey
        #endregion

        public void BuildType(IFieldReader userData, IFieldWriter typeWriter, Registry registry)
        {
            typeWriter.SetField(kGradientMode, GradientMode.Blend);
            typeWriter.SetField(kColorCount, 2);
            typeWriter.SetField(kAlphaCount, 2);
            typeWriter.SetField(kColor(0), new GradientColorKey(Color.black, 0));
            typeWriter.SetField(kColor(1), new GradientColorKey(Color.white, 1));
            typeWriter.SetField(kAlpha(0), new GradientAlphaKey(1, 0));
            typeWriter.SetField(kAlpha(1), new GradientAlphaKey(1, 1));

            // TODO: Precision; the Gradient type we use in Functions.hlsl does not handle precision, despite surrounding shader code.
            // Ideally, we could just generate the complete Gradient Struct per precision type, instead of using the one from Functions.hlsl;
            // this would depend on ShaderFoundry reintroducing better support for structs. For now we'll use the built-in type.
            // Note that we could also not default to generating 16 total keys either, and potentially key against any underlying graph type,
            // but for the purposes of working with the gradient widget, this makes the most sense.
        }

        private string ColorKeyToDecl(GradientColorKey key) => $"float4({key.color.r},{key.color.g},{key.color.b},{key.time})";
        private string AlphaKeyToDecl(GradientAlphaKey key) => $"float2({key.alpha},{key.time})";


        string Defs.ITypeDefinitionBuilder.GetInitializerList(IFieldReader data, Registry registry)
        {
            data.GetField<int>(kColorCount, out var colorCount);
            data.GetField<int>(kAlphaCount, out var alphaCount);
            data.GetField<GradientMode>(kGradientMode, out var gradientMode);

            string alpha = "";
            string color = "";
            for(int i = 0; i < 8; ++i)
            {
                var localColor =  "float4(0,0,0,0)";
                var localAlpha = "float2(0,0)";

                if (i < colorCount)
                {
                    data.GetField(kColor(i), out GradientColorKey colorKey);
                    localColor = ColorKeyToDecl(colorKey);
                }
                if (i < alphaCount)
                {
                    data.GetField(kAlpha(i), out GradientAlphaKey alphaKey);
                    localAlpha = AlphaKeyToDecl(alphaKey);
                }


                color += localColor + (i < 7 ? ", " : "");
                alpha += localAlpha + (i < 7 ? ", " : "");
            }

            return $"NewGradient({(int)gradientMode}, {colorCount}, {alphaCount}, {color}, {alpha})";
        }

        ShaderFoundry.ShaderType Defs.ITypeDefinitionBuilder.GetShaderType(IFieldReader data, ShaderFoundry.ShaderContainer container, Registry registry)
        {
            // We could potentially support a much broader range of types, precision and array lengths,
            // though GradientTypeAssignment would need to handle conversions.
            // The underlying gradient type currently described in Functions.hlsl does not support variable precision- it's single precision only.
            var gradientBuilder = new ShaderFoundry.ShaderType.StructBuilder(container, "Gradient");
            gradientBuilder.DeclaredExternally();
            return gradientBuilder.Build();
        }
    }

    internal class GradientTypeAssignment : Defs.ICastDefinitionBuilder
    {
        public RegistryKey GetRegistryKey() => new RegistryKey { Name = "GradientTypeAssignment", Version = 1 };
        public RegistryFlags GetRegistryFlags() => RegistryFlags.Cast;
        public (RegistryKey, RegistryKey) GetTypeConversionMapping() => (GradientType.kRegistryKey, GradientType.kRegistryKey);
        public bool CanConvert(IFieldReader src, IFieldReader dst) => true;

        public ShaderFoundry.ShaderFunction GetShaderCast(IFieldReader src, IFieldReader dst, ShaderFoundry.ShaderContainer container, Registry registry)
        {
            // There is currently no need to cast these, but if graphType was used for the underlying type, or precision was introduced--
            // this is where we would map the data accordingly.

            // The src/dst type in this case should reliably just be what's provided from GradientType.
            var srcType = registry.GetTypeBuilder(src.GetRegistryKey()).GetShaderType(src, container, registry);
            var dstType = registry.GetTypeBuilder(dst.GetRegistryKey()).GetShaderType(dst, container, registry);
            string castName = $"Cast{srcType.Name}_{dstType.Name}";
            var builder = new ShaderFoundry.ShaderFunction.Builder(container, castName);
            builder.AddInput(srcType, "In");
            builder.AddOutput(dstType, "Out");
            builder.AddLine("Out = In;");
            return builder.Build();
        }
    }
}


