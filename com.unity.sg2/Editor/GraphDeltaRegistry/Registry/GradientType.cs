using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    internal class SampleGradientNode : INodeDefinitionBuilder
    {
        public static readonly Defs.NodeUIDescriptor kUIDescriptor
            = new Defs.NodeUIDescriptor(version: 1,
                name: "SampleGradient",
                displayName: "Sample Gradient",
                category: "Input/Gradient",
                tooltip: "Sample a gradient by the provided time.",
                synonyms: new string[] {"Gradient"});

        public RegistryKey GetRegistryKey() => new() {Name = "SampleGradient", Version = 1};
        public RegistryFlags GetRegistryFlags() => RegistryFlags.Func;

        public const string kGradient = "Gradient";
        public const string kTime = "Time";
        public const string kOutput = "Out";

        public void BuildNode(NodeHandler node, Registry registry)
        {
            node.AddPort<GradientType>(kGradient, true, registry);

            // setup a float1 for time port.
            var time = node.AddPort<GraphType>(kTime, true, registry);
            time.GetTypeField().GetSubField<GraphType.Precision>(GraphType.kPrecision).SetData(GraphType.Precision.Single);
            time.GetTypeField().GetSubField<GraphType.Primitive>(GraphType.kPrimitive).SetData(GraphType.Primitive.Float);
            time.GetTypeField().GetSubField<GraphType.Length>(GraphType.kLength).SetData(GraphType.Length.One);
            time.GetTypeField().GetSubField<GraphType.Height>(GraphType.kHeight).SetData(GraphType.Height.One);

            // default for GraphType is a float4.
            node.AddPort<GraphType>(kOutput, false, registry);
        }

        private void PortToParam(string name, NodeHandler node, ShaderFoundry.ShaderFunction.Builder builder, ShaderFoundry.ShaderContainer container, Registry registry)
        {
            var port = node.GetPort(name);
            var shaderType = registry.GetShaderType(port.GetTypeField(), container);
            if (port.IsInput)
                builder.AddInput(shaderType, name);
            else builder.AddOutput(shaderType, name);
        }

        public ShaderFoundry.ShaderFunction GetShaderFunction(NodeHandler node, ShaderFoundry.ShaderContainer container, Registry registry, out INodeDefinitionBuilder.Dependencies deps)
        {
            deps = new();
            var shaderFunctionBuilder = new ShaderFoundry.ShaderFunction.Builder(container, GetRegistryKey().Name);

            PortToParam(kGradient, node, shaderFunctionBuilder, container, registry);
            PortToParam(kTime, node, shaderFunctionBuilder, container, registry);
            PortToParam(kOutput, node, shaderFunctionBuilder, container, registry);

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
    internal class GradientNode : INodeDefinitionBuilder
    {
        public RegistryKey GetRegistryKey() => new RegistryKey { Name = "GradientNode", Version = 1 };
        public RegistryFlags GetRegistryFlags() => RegistryFlags.Func;

        public const string kInlineStatic = "Inline";
        public const string kOutput = "Out";

        public void BuildNode(NodeHandler node, Registry registry)
        {
            var input = node.AddPort<GradientType>(kInlineStatic, true, registry);

            input.GetTypeField().AddSubField<bool>("IsStatic", true); // TODO: This is just the hint for UI to use the large gradient editor.
            var output = node.AddPort<GradientType>(kOutput, false, registry);
        }

        public ShaderFoundry.ShaderFunction GetShaderFunction(NodeHandler node, ShaderFoundry.ShaderContainer container, Registry registry, out INodeDefinitionBuilder.Dependencies deps)
        {
            deps = new();
            var shaderFunctionBuilder = new ShaderFoundry.ShaderFunction.Builder(container, GetRegistryKey().Name);
            var port = node.GetPort(kInlineStatic);

            var shaderType = registry.GetShaderType(port.GetTypeField(), container);
            shaderFunctionBuilder.AddInput(shaderType, kInlineStatic);
            shaderFunctionBuilder.AddOutput(shaderType, kOutput);
            shaderFunctionBuilder.AddLine($"{kOutput} = {kInlineStatic};");
            return shaderFunctionBuilder.Build();
        }
    }

    public static class GradientTypeHelpers
    {
        #region Serializable Gradient Wrappers

        [Serializable]
        internal struct SerializableColorKey
        {
            [SerializeField]
            Color color;

            [SerializeField]
            float time;

            public SerializableColorKey(GradientColorKey colorKey)
            {
                color = colorKey.color;
                time = colorKey.time;
            }

            public GradientColorKey ToGradientColorKey() => new(color, time);
        }

        [Serializable]
        internal struct SerializableAlphaKey
        {
            [SerializeField]
            float alpha;

            [SerializeField]
            float time;

            public SerializableAlphaKey(GradientAlphaKey alphaKey)
            {
                alpha = alphaKey.alpha;
                time = alphaKey.time;
            }

            public GradientAlphaKey ToGradientAlphaKey() => new(alpha, time);
        }

        [Serializable]
        internal class SerializableGradient
        {
            [SerializeField]
            GradientMode m_Mode;

            [SerializeField]
            List<SerializableColorKey> m_Colors;

            [SerializeField]
            List<SerializableAlphaKey> m_Alphas;

            public SerializableGradient(Gradient gradient)
            {
                m_Mode = gradient.mode;

                m_Colors = new List<SerializableColorKey>(gradient.colorKeys.Length);
                foreach (var colorKey in gradient.colorKeys)
                {
                    m_Colors.Add(new SerializableColorKey(colorKey));
                }

                m_Alphas = new List<SerializableAlphaKey>(gradient.alphaKeys.Length);
                foreach (var alphaKey in gradient.alphaKeys)
                {
                    m_Alphas.Add(new SerializableAlphaKey(alphaKey));
                }
            }

            public Gradient GetGradient()
            {
                var g = new Gradient { mode = m_Mode };

                var colors = new GradientColorKey[m_Colors.Count];
                for (var i = 0; i < m_Colors.Count; ++i)
                {
                    colors[i] = m_Colors[i].ToGradientColorKey();
                }

                var alphas = new GradientAlphaKey[m_Alphas.Count];
                for (var i = 0; i < m_Alphas.Count; ++i)
                {
                    alphas[i] = m_Alphas[i].ToGradientAlphaKey();
                }

                g.SetKeys(colors, alphas);

                return g;
            }
        }

        #endregion

        public static Gradient GetGradient(FieldHandler field)
        {
            // This is possible when duplicating/cloning a variable declaration model
            // from another graph and it doesnt have a graphHandler reference
            if (field == null)
            {
                var defaultGradient = new Gradient(){
                    colorKeys = new GradientColorKey[] {
                        new(new Color(0, 0, 0), 0),
                        new(new Color(1, 1, 1), 1)
                    },
                    alphaKeys = new GradientAlphaKey[] {
                        new(1, 0),
                        new(1, 1)
                    }
                };

                return defaultGradient;
            }
            field.GetField<GradientMode>(GradientType.kGradientMode, out var mode);
            field.GetField<int>(GradientType.kColorCount, out var colorCount);
            field.GetField<int>(GradientType.kAlphaCount, out var alphaCount);

            List<GradientColorKey> colors = new();
            List<GradientAlphaKey> alphas = new();
            for (int i = 0; i < colorCount; ++i)
            {
                colors.Add(GetColorKey(field, i));
            }

            for (int i = 0; i < alphaCount; ++i)
            {
                alphas.Add(GetAlphaKey(field, i));
            }

            var result = new Gradient();
            result.mode = mode;
            result.SetKeys(colors.ToArray(), alphas.ToArray());
            return result;
        }

        public static void SetGradient(FieldHandler field, Gradient gradient)
        {
            field.GetSubField<GradientMode>(GradientType.kGradientMode).SetData(gradient.mode);
            field.GetSubField<int>(GradientType.kColorCount).SetData(gradient.colorKeys.Length);
            field.GetSubField<int>(GradientType.kAlphaCount).SetData(gradient.alphaKeys.Length);

            for (int i = 0; i < 8 && i < gradient.colorKeys.Length; ++i)
                SetColorKey(field, i, gradient.colorKeys[i]);

            for (int i = 0; i < 8 && i < gradient.alphaKeys.Length; ++i)
                SetAlphaKey(field, i, gradient.alphaKeys[i]);
        }

        internal static void SetColorKey(FieldHandler field, int idx, GradientColorKey colorKey)
        {
            var serializableKey = new SerializableColorKey(colorKey);
            (field.GetSubField<SerializableColorKey>(GradientType.kColor(idx))
                ?? field.AddSubField(GradientType.kColor(idx), serializableKey)).SetData(serializableKey);
        }

        internal static GradientColorKey GetColorKey(FieldHandler field, int idx)
        {
            return (field.GetSubField<SerializableColorKey>(GradientType.kColor(idx))?.GetData()
                ?? default).ToGradientColorKey();
        }

        internal static void SetAlphaKey(FieldHandler field, int idx, GradientAlphaKey alphaKey)
        {
            var serializableKey = new SerializableAlphaKey(alphaKey);
            (field.GetSubField<SerializableAlphaKey>(GradientType.kAlpha(idx))
                ?? field.AddSubField(GradientType.kAlpha(idx), serializableKey)).SetData(serializableKey);
        }

        internal static GradientAlphaKey GetAlphaKey(FieldHandler field, int idx)
        {
            return (field.GetSubField<SerializableAlphaKey>(GradientType.kAlpha(idx))?.GetData()
                ?? default).ToGradientAlphaKey();
        }
    }

    /// <summary>
    /// Represents the Gradient type found in Functions.hlsl, similar to the C# version, except it's key count is fixed to 8.
    /// </summary>
    internal class GradientType : ITypeDefinitionBuilder
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

        public void BuildType(FieldHandler field, Registry registry)
        {
            field.AddSubField(kGradientMode, GradientMode.Blend);
            field.AddSubField(kColorCount, 2);
            field.AddSubField(kAlphaCount, 2);
            GradientTypeHelpers.SetColorKey(field, 0, new GradientColorKey(Color.black, 0));
            GradientTypeHelpers.SetColorKey(field, 1, new GradientColorKey(Color.white, 1));
            GradientTypeHelpers.SetAlphaKey(field, 0, new GradientAlphaKey(1, 0));
            GradientTypeHelpers.SetAlphaKey(field, 1, new GradientAlphaKey(1, 1));

            // TODO: Precision; the Gradient type we use in Functions.hlsl does not handle precision, despite surrounding shader code.
            // Ideally, we could just generate the complete Gradient Struct per precision type, instead of using the one from Functions.hlsl;
            // this would depend on ShaderFoundry reintroducing better support for structs. For now we'll use the built-in type.
            // Note that we could also not default to generating 16 total keys either, and potentially key against any underlying graph type,
            // but for the purposes of working with the gradient widget, this makes the most sense.
        }

        private string ColorKeyToDecl(GradientColorKey key) => $"float4({key.color.r},{key.color.g},{key.color.b},{key.time})";
        private string AlphaKeyToDecl(GradientAlphaKey key) => $"float2({key.alpha},{key.time})";

        public void CopySubFieldData(FieldHandler src, FieldHandler dst)
        {
            GradientTypeHelpers.SetGradient(dst, GradientTypeHelpers.GetGradient(src));
        }

        string ITypeDefinitionBuilder.GetInitializerList(FieldHandler data, Registry registry)
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

        ShaderFoundry.ShaderType ITypeDefinitionBuilder.GetShaderType(FieldHandler data, ShaderFoundry.ShaderContainer container, Registry registry)
        {
            // We could potentially support a much broader range of types, precision and array lengths,
            // though GradientTypeAssignment would need to handle conversions.
            // The underlying gradient type currently described in Functions.hlsl does not support variable precision- it's single precision only.
            var gradientBuilder = new ShaderFoundry.ShaderType.StructBuilder(container, "Gradient");
            gradientBuilder.DeclaredExternally();
            return gradientBuilder.Build();
        }
    }

    internal class GradientTypeAssignment : ICastDefinitionBuilder
    {
        public RegistryKey GetRegistryKey() => new RegistryKey { Name = "GradientTypeAssignment", Version = 1 };
        public RegistryFlags GetRegistryFlags() => RegistryFlags.Cast;
        public (RegistryKey, RegistryKey) GetTypeConversionMapping() => (GradientType.kRegistryKey, GradientType.kRegistryKey);
        public bool CanConvert(FieldHandler src, FieldHandler dst) => true;

        public ShaderFoundry.ShaderFunction GetShaderCast(FieldHandler src, FieldHandler dst, ShaderFoundry.ShaderContainer container, Registry registry)
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
